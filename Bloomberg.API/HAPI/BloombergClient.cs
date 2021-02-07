using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bloomberg.API.HAPI.Authentication;
using Bloomberg.API.HAPI.Model;
using Bloomberg.API.HAPI.StreamingEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI
{
    /// <summary>
    /// Implementation of the calls to the API to be used by higher level API
    ///
    /// Heavily based off logic found in the HAPI sample code
    /// </summary>
    public class BloombergClient
    {
        private readonly ILogger<BloombergClient> logger;

        private readonly HttpClient session;
        private readonly SseClient notificationClient;
        private bool shutdownRequested = false;
        
        private readonly IDictionary<string, EventWaitHandle> waitHandles = new ConcurrentDictionary<string, EventWaitHandle>();
        private readonly IDictionary<string, string> replyUrls = new ConcurrentDictionary<string, string>();
        private BackgroundWorker eventWorker;
        
        public bool IsConnected { get; private set; }
        
        private string GetTypeForUrl(Type typeInfo)
        {
            string type = typeInfo.Name.ToLower()+"s";
            if (type == "fieldlists") return "fieldLists";
            return type;
        }

        public BloombergClient(ILogger<BloombergClient> logger)
        {
            this.logger = logger;

            Credential credentials = null;
            credentials = Credential.LoadCredential("credentials.txt");

            if (credentials != null)
            {
                session = new HttpClient(new BloombergAuthenticationHandler(logger, tokenMaker: credentials));
            }
            else
            {
                session = new HttpClient(new BloombergAuthenticationHandler(logger));
            }

            notificationClient = new SseClient(MakeUri("/eap/notifications/sse"), session);
        }

        private void EventWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "Bloomberg Event Poller";
            }

            BackgroundWorker worker = sender as BackgroundWorker;
            logger.LogInformation("Background Worker Started");

            while (!shutdownRequested)
            {
                var bloombergEvent = notificationClient.ReadEvent().Result;

                if (bloombergEvent != null)
                {
                    logger.LogInformation("Got Bloomberg Event: " + bloombergEvent);
                    
                    if (bloombergEvent.IsHeartbeat)
                    {
                        logger.LogInformation("Received heartbeat event, keep waiting for events");
                        continue;
                    }
                    
                    var notification = JsonConvert.DeserializeObject<DeliveryNotification>(bloombergEvent.Data);
                    logger.LogInformation($"Received reply delivery notification event: {bloombergEvent.Data}");

                    var deliveryDistributionId = notification.Generated.Identifier;
                    var replyUrl = notification.Generated.Id;
                    
                    if (waitHandles.ContainsKey(deliveryDistributionId))
                    {
                        replyUrls.Add(deliveryDistributionId, replyUrl);
                        waitHandles[deliveryDistributionId].Set();
                    }
                }

                if (worker.CancellationPending)
                {
                    logger.LogInformation("Background Worker Exiting");
                    e.Cancel = true;
                    break;
                }
            }
            
            eventWorker.CancelAsync();
            eventWorker = null;
            e.Cancel = true;
        }

        public async Task Initialise()
        {
            await notificationClient.Connect();

            if (eventWorker == null)
            {
                eventWorker = new BackgroundWorker {WorkerSupportsCancellation = true};
                eventWorker.DoWork += EventWorker_DoWork;
                eventWorker.RunWorkerAsync();
            }

            IsConnected = true;
        }

        /// <summary>
        /// Constructs a URI referencing the specified resource on the BEAP server.
        /// </summary>
        /// <param name="path">Path to the resource referenced by the URI.</param>
        /// <returns>Constructed URI.</returns>
        private Uri MakeUri(string path)
        {
            if(path.Contains("http")) return new Uri(path);
            var uriBuilder = new UriBuilder {
                Scheme = "https",
                Host = "api.bloomberg.com",
                Path = path.Contains("eap") ? path : "/eap/" + path,
            };
            return uriBuilder.Uri;
        }

        private JsonSerializerSettings SerializerSettings()
        {
            return new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                TypeNameHandling = TypeNameHandling.None,
                ObjectCreationHandling = ObjectCreationHandling.Auto,
            };
        }

        /// <summary>
        /// Sends a POST request to BEAP server.
        /// </summary>
        /// <typeparam name="TPayload">The type of payload before serialization.</typeparam>
        /// <param name="uri">URI of the requested resource.</param>
        /// <param name="payload">Request body.</param>
        /// <returns>Location of a successfully created resource.</returns>
        private async Task<(HttpStatusCode code, string location)> HttpPost<TPayload>(string path, TPayload payload)
        {
            var body = JsonConvert.SerializeObject(payload, typeof(TPayload), Formatting.Indented, SerializerSettings());
            
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await session.PostAsync(MakeUri(path), content);

            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.Conflict)
            {
                var result = await response.Content.ReadAsStringAsync();
                throw new Exception("HttpPost :: Unexpected response: " + result);
            }

            return (response.StatusCode, response.Headers.Location?.ToString());
        }

        /// <summary>
        /// Sends a PATCH request to BEAP server.
        /// </summary>
        /// <typeparam name="TPayload">The type of payload before serialization.</typeparam>
        /// <param name="uri">URI of the requested resource.</param>
        /// <param name="payload">Request body.</param>
        /// <returns>Location of a successfully created resource.</returns>
        private async Task<(HttpStatusCode code, string location)> HttpPatch<TPayload>(string path, TPayload payload)
        {
            var body = JsonConvert.SerializeObject(payload, Formatting.Indented, SerializerSettings());
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), MakeUri(path));
            request.Content = content;
            
            var response = await session.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.NotFound)
            {
                var result = await response.Content.ReadAsStringAsync();
                throw new Exception("HttpPatch :: Unexpected response" + result);
            }

            return (response.StatusCode, response.Headers.Location?.ToString());
        }

        /// <summary>
        /// Sends a GET request to BEAP server.
        /// </summary>
        /// <typeparam name="TPayload">The type of payload before serialization.</typeparam>
        /// <param name="uri">URI of the requested resource.</param>
        /// <param name="payload">Request body.</param>
        /// <returns>Location of a successfully created resource.</returns>
        private async Task<string> HttpGet(string path)
        {
            var response = await session.GetAsync(MakeUri(path));
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unexpected response");
            }

            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// Sends a GET request to BEAP server.
        /// </summary>
        /// <typeparam name="TPayload">The type of payload before serialization.</typeparam>
        /// <param name="uri">URI of the requested resource.</param>
        /// <param name="payload">Request body.</param>
        /// <returns>Location of a successfully created resource.</returns>
        private async Task<Stream> HttpGetStream(string path)
        {
            Thread.Sleep(15000);
            var response = await session.GetAsync(MakeUri(path));
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Unexpected response");
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<T> CreateItem<T>(string catalogIdentifier, T item)
            where T : BasicContainer
        {
            if(string.IsNullOrWhiteSpace(item.Identifier)) throw new ArgumentException("Item does not have its identifier set", nameof(item));
            string type = GetTypeForUrl(typeof(T));
            
            var (status, newPath) = await HttpPost($"catalogs/{catalogIdentifier}/{type}", item);

            //If it turns out the item already exists
            if (status == HttpStatusCode.Conflict && !(item is Request))
            {
                return await UpdateItem(catalogIdentifier, item);
            }
            if (status == HttpStatusCode.Conflict && item is Request)
            {
                var t = await GetRequest(catalogIdentifier, item.Identifier);
                if (t is T tt) return tt;
                return item;
            }

            var newItem = await HttpGet(newPath);
            return JsonConvert.DeserializeObject<T>(newItem);
        }

        public async Task<T> UpdateItem<T>(string catalogIdentifier, T item)
            where T : BasicContainer
        {
            var itemIdentifier = item.Identifier;
            if(string.IsNullOrWhiteSpace(itemIdentifier)) throw new ArgumentException("Item does not have its identifier set", nameof(item));
            string type = GetTypeForUrl(typeof(T));

            //Set type to null
            var typeProp = typeof(T).GetProperty("Type");
            if(typeProp != null) typeProp.SetValue(item, null);
            item.Identifier = null;

            var (status, newPath) = await HttpPatch($"catalogs/{catalogIdentifier}/{type}/{itemIdentifier}", item);

            //If the item does not already exist - set it up
            if (status == HttpStatusCode.NotFound)
            {
                item.Identifier = itemIdentifier;
                if(typeProp != null) typeProp.SetValue(item, typeof(T).Name);
                return await CreateItem<T>(catalogIdentifier, item);
            }

            var newItem = await HttpGet(newPath);
            return JsonConvert.DeserializeObject<T>(newItem);
        }

        public async Task<Stream> WaitForResult(Request request)
        {
            var expectedDistributionId = $"{request.Identifier}.bbg";
            waitHandles.Add(expectedDistributionId, new EventWaitHandle(false, EventResetMode.ManualReset));
            
            await FromWaitHandle(waitHandles[expectedDistributionId], TimeSpan.FromSeconds(500));

            if (replyUrls.ContainsKey(expectedDistributionId))
            {
                var replyUrl = replyUrls[expectedDistributionId];
                // Download reply file from server.
                var raw = await HttpGetStream(replyUrl);
                return raw;
            }

            return null;
        }
        
        private Task<bool> FromWaitHandle(WaitHandle handle, TimeSpan timeout)
        {
            // Handle synchronous cases.
            var alreadySignalled = handle.WaitOne(0);
            if (alreadySignalled)
                return Task.FromResult(true);
            if (timeout == TimeSpan.Zero)
                return Task.FromResult(false);

            // Register all asynchronous cases.
            var tcs = new TaskCompletionSource<bool>();
            var threadPoolRegistration = 
                ThreadPool.RegisterWaitForSingleObject(handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    tcs, 
                    timeout, 
                    true);

            tcs.Task.ContinueWith(_ =>
            {
                threadPoolRegistration.Unregister(handle);
            }, TaskScheduler.Default);

            return tcs.Task;
        }

        public async Task<Catalog> GetRequestCatalog()
        {
            var catalogs = await GetCatalogs();
            return catalogs.FirstOrDefault(x => x.SubscriptionType == "scheduled");
        }

        /// <summary>
        /// /catalogs/
        ///
        /// A list of available catalogs
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Catalog>> GetCatalogs()
        {
            var data = await HttpGet("catalogs/");
            return JsonConvert.DeserializeObject<BasicCollection<Catalog>>(data).Contains;
        }

        /// <summary>
        /// /catalogs/{identifier}
        ///
        /// Gets what is available in a catalog
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Catalog>> GetCatalog(string identifier)
        {
            var data = await HttpGet($"catalogs/{identifier}");
            return JsonConvert.DeserializeObject<BasicCollection<Catalog>>(data).Contains;
        }

        public async Task<IEnumerable<Dataset>> GetDatasets(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets");
            return JsonConvert.DeserializeObject<BasicCollection<Dataset>>(data).Contains;
        }

        public async Task<Dataset> GetDataset(string catalogIdentifier, string dataSetIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets/{dataSetIdentifier}/");
            return JsonConvert.DeserializeObject<Dataset>(data);
        }

        public async Task<IEnumerable<Snapshot>> GetSnapshots(string catalogIdentifier, string dataSetIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets/{dataSetIdentifier}/snapshots");
            return JsonConvert.DeserializeObject<BasicCollection<Snapshot>>(data).Contains;;
        }

        public async Task<Snapshot> GetSnapshot(string catalogIdentifier, string dataSetIdentifier, string snapshotIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets/{dataSetIdentifier}/snapshots/{snapshotIdentifier}");
            return JsonConvert.DeserializeObject<Snapshot>(data);
        }

        public async Task<IEnumerable<Distribution>> GetDistributions(string catalogIdentifier, string dataSetIdentifier, string snapshotIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets/{dataSetIdentifier}/snapshots/{snapshotIdentifier}/distributions");
            return JsonConvert.DeserializeObject<BasicCollection<Distribution>>(data).Contains;;
        }

        public async Task<Distribution> GetDistribution(string catalogIdentifier, string dataSetIdentifier, string snapshotIdentifier, string distributionIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/datasets/{dataSetIdentifier}/snapshots/{snapshotIdentifier}/distributions/{distributionIdentifier}");
            return JsonConvert.DeserializeObject<Distribution>(data);
        }

        public async Task<IEnumerable<Request>> GetRequests(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/requests");
            return JsonConvert.DeserializeObject<BasicCollection<Request>>(data).Contains;
        }

        public async Task<Request> GetRequest(string catalogIdentifier, string requestIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/requests/{requestIdentifier}");
            return JsonConvert.DeserializeObject<Request>(data);
        }

        public async Task<IEnumerable<Universe>> GetUniverses(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/universes");
            return JsonConvert.DeserializeObject<BasicCollection<Universe>>(data).Contains;;
        }

        public async Task<Universe> GetUniverse(string catalogIdentifier, string universeIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/universes/{universeIdentifier}");
            return JsonConvert.DeserializeObject<Universe>(data);
        }

        public async Task<IEnumerable<FieldList>> GetFieldLists(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/fieldLists");
            return JsonConvert.DeserializeObject<BasicCollection<FieldList>>(data).Contains;
        }

        public async Task<FieldList> GetFieldList(string catalogIdentifier, string fieldListIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/fieldLists/{fieldListIdentifier}");
            return JsonConvert.DeserializeObject<FieldList>(data);
        }
        
        public async Task<IEnumerable<Trigger>> GetTriggers(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/triggers");
            return JsonConvert.DeserializeObject<BasicCollection<Trigger>>(data).Contains;
        }

        public async Task<Trigger> GetTrigger(string catalogIdentifier, string triggerIdentifier)
        {
            var data = await HttpGet($"catalogs/{catalogIdentifier}/triggers/{triggerIdentifier}");
            return JsonConvert.DeserializeObject<Trigger>(data);
        }

        public async Task<string> GetOntology()
        {
            var data = await HttpGet($"ontology");
            return data;
        }

        public async Task<string> GetFields(string catalogIdentifier)
        {
            var data = await HttpGet($"catalogs/bbg/fields");
            return data;
        }

        public async Task<string> GetField(string fieldIdentifier)
        {
            var data = await HttpGet($"catalogs/bbg/fields/{fieldIdentifier}");
            return data;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Bloomberglp.Blpapi;
using Bloomberg.API.Model;
using Bloomberg.API.Model.Enriched;
using Bloomberg.API.Model.Enriched.BloombergTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bloomberg.API.Implementation
{
    internal class DisconnectedFromBloombergException : Exception
    {
        public DisconnectedFromBloombergException()
        {
        }

        public DisconnectedFromBloombergException(string message) : base(message)
        {
        }

        public DisconnectedFromBloombergException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DisconnectedFromBloombergException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class TerminalBloombergService : IBloombergService
    {
        #region Generic Terminal API Implementation logic (with multi-threading)

        private const string BloombergApiReferenceService = "//blp/refdata";

        private static readonly Name SecurityData = new Name("securityData");
        private static readonly Name Security = new Name("security");
        private static readonly Name FieldData = new Name("fieldData");
        private static readonly Name ResponseError = new Name("responseError");
        private static readonly Name SecurityError = new Name("securityError");

        private readonly ILogger<IBloombergService> logger;

        private Session session;
        private bool isConnected;
        private bool isConnecting;
        private readonly object connectionLock = new object();
        private BackgroundWorker eventWorker;
        private readonly IDictionary<long, EventWaitHandle> waitHandles = new ConcurrentDictionary<long, EventWaitHandle>();
        private readonly IDictionary<long, List<Message>> responses = new ConcurrentDictionary<long, List<Message>>();

        public TerminalBloombergService(ILogger<IBloombergService> logger, bool autoConnect = true)
        {
            this.logger = logger;
            if (autoConnect)
            {
                Task.Run(() => Connect());
            }
        }

        public event EventHandler<bool> BloombergReady;

        public void Connect()
        {
            var locked = Monitor.TryEnter(connectionLock, TimeSpan.FromSeconds(1));

            if (!locked)
            {
                logger.LogInformation("Bloomberg Service already attempting to connect. So will not try again.");
                return;
            }
            isConnecting = true;

            if (session == null)
            {
                logger.LogInformation("Starting the Bloomberg Service. Attempting to connect to Bloomberg API");

                session = new Session();

                if (!session.Start())
                {
                    session = null;
                    logger.LogWarning("Failed to start Bloomberg Session");
                }
                else
                {
                    if (!session.OpenService(BloombergApiReferenceService))
                    {
                        session = null;
                        logger.LogWarning("Could not open the Bloomberg Api Reference Data Service");
                    }
                }

                if (session != null)
                {
                    logger.LogInformation("Connected to Bloomberg. Starting Background Worker");

                    isConnected = true;
                    eventWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
                    eventWorker.DoWork += EventWorker_DoWork;
                    eventWorker.RunWorkerAsync();
                    BloombergReady?.Invoke(this, true);
                }
            }

            isConnecting = false;

            Monitor.Exit(connectionLock);
        }

        private void EventWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "Bloomberg Event Poller";
            }

            BackgroundWorker worker = sender as BackgroundWorker;

            logger.LogInformation("Background Worker Started");

            while (true)
            {
                var bloombergEvent = session.NextEvent(5000);

                if (bloombergEvent != null)
                {
                    logger.LogTrace("Got Bloomberg Event: " + bloombergEvent);

                    switch (bloombergEvent.Type)
                    {
                        case Event.EventType.RESPONSE:
                            ProcessEvent(bloombergEvent);
                            break;
                        case Event.EventType.PARTIAL_RESPONSE:
                            ProcessEvent(bloombergEvent);
                            break;
                        case Event.EventType.SERVICE_STATUS:
                            foreach (var msg in bloombergEvent.GetMessages())
                            {
                                if (msg.MessageType.Equals(new Name("SessionStopped")))
                                {
                                    logger.LogInformation("Background Worker Exiting");
                                    eventWorker.CancelAsync();
                                    eventWorker = null;
                                    session.Stop();
                                    session = null;
                                    isConnected = false;
                                    e.Cancel = true;
                                    return;
                                }
                            }
                            break;
                    }
                }

                if (worker.CancellationPending)
                {
                    logger.LogInformation("Background Worker Exiting");
                    e.Cancel = true;
                    break;
                }
            }

            e.Cancel = true;
        }

        private void ProcessEvent(Event bloombergEvent)
        {

            long correlationId = -1;

            foreach (var msg in bloombergEvent.GetMessages())
            {
                correlationId = msg.CorrelationID.Value;

                if (!responses.ContainsKey(correlationId))
                {
                    responses.Add(correlationId, new List<Message>());
                }

                logger.LogDebug("Message Recieved for " + correlationId);
                responses[correlationId].Add(msg);

                if (bloombergEvent.Type == Event.EventType.PARTIAL_RESPONSE && msg.FragmentType == Message.Fragment.END)
                {
                    logger.LogDebug($"All messages recieved for {correlationId}. Signalling Requestor");
                    waitHandles[correlationId].Set();
                }
            }

            if (bloombergEvent.Type == Event.EventType.RESPONSE)
            {
                logger.LogInformation($"All messages recieved for {correlationId}. Signalling Requestor");
                waitHandles[correlationId].Set();
            }
        }

        public void WaitUntilConnected()
        {
            if (!IsConnected && !isConnecting)
            {
                Connect();
            }

            if (!IsConnected && isConnecting)
            {
                int numberOfSleeps = 0;
                while (isConnecting && numberOfSleeps < 30)
                {
                    Thread.Sleep(500);
                    numberOfSleeps++;
                }
            }

            if (!IsConnected)
            {
                throw new DisconnectedFromBloombergException();
            }
        }

        /// <summary>
        /// Acquired from: http://stackoverflow.com/a/24121847/98409
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
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

        public void Shutdown()
        {
            if (eventWorker != null)
            {
                eventWorker.CancelAsync();
            }
        }

        public bool IsConnected => isConnected;
        public Service GetReferenceDataService()
        {
            WaitUntilConnected();
            return session.GetService(BloombergApiReferenceService);
        }

        #endregion

        public async Task<BResponse> RequestAsync(BRequest request)
        {
            if (!isConnected) return new BResponse();

            var service = session.GetService(BloombergApiReferenceService);
            var req = service.CreateRequest("ReferenceDataRequest");

            foreach (var sec in request.Identifiers)
            {
                req.Append("securities", EnrichedBRequest.FormatIdentifier(sec));
            }

            foreach (var field in request.Fields)
            {
                req.Append("fields", field);
            }

            if (request.Overrides.Any())
            {
                var overrides = req.GetElement("overrides");

                // Assuming all the overrides are the same - for terminal overrides are request level, for HAPI they are field & identifer level
                foreach (var kvp in request.Overrides.First().Overrides)
                {
                    var or = overrides.AppendElement();
                    or.SetElement("fieldId", kvp.Key);
                    or.SetElement("value", kvp.Value.ToString());
                }
            }

            var id = new CorrelationID();
            waitHandles.Add(id.Value, new EventWaitHandle(false, EventResetMode.ManualReset));

            logger.LogDebug((string)($"Sending Request to Bloomberg {id.Value}: " + request));

            var result = session.SendRequest(req, id);

            await FromWaitHandle(waitHandles[id.Value], TimeSpan.FromMinutes(15));

            logger.LogInformation($"Responses recieved for Request Id {id.Value}");

            var messages = responses[id.Value];

            waitHandles.Remove(id.Value);
            responses.Remove(id.Value);

            var bloombergResponse = new BResponse();

            foreach (var message in messages)
            {
                logger.LogDebug("Processing Bloomberg Response Message: " + message);

                var el = message.AsElement;
                if (el.HasElement(ResponseError))
                {
                    var firstErrorElement = el.GetElement(ResponseError).GetElement("message");
                    logger.LogError("Got a Response Error: " + firstErrorElement.GetValueAsString());
                }

                var secData = el.GetElement(SecurityData);
                logger.LogDebug("Got " + secData.NumValues + " Security Responses back");

                for (int i = 0; i < secData.NumValues; i++)
                {
                    var sec = secData.GetValueAsElement(i);

                    var secId = sec.GetElementAsString(Security);
                    
                    // Do we have a sec id like /ISIN/xxxx /CUSIP/aaaa or /BBGID/ddddd
                    if (secId.StartsWith("/") && secId.IndexOf('/', 1) > 0)
                    {
                        var secondSlashIndex = secId.IndexOf('/', 1);
                        secId = secId.Substring(secondSlashIndex + 1);
                    }

                    if (sec.HasElement(SecurityError))
                    {
                        logger.LogWarning(
                            $"Security {secId} has the following Error {sec.GetElement(SecurityError).GetElementAsString(new Name("message"))}");
                    }

                    var fieldData = sec.GetElement(FieldData);
                    var data = new Dictionary<string, string>();
                    foreach (var raw in fieldData.Elements)
                    {
                        var fieldName = raw.Name.ToString();
                        if (!bloombergResponse.Fields.Contains(fieldName)) bloombergResponse.AddField(fieldName);
                        
                        if (raw.IsArray || raw.IsComplexType)
                        {
                            var complexObj = ExtractComplexObjectFromResponse(fieldName, raw, logger);

                            if (complexObj != null)
                            {
                                data.Add(raw.Name.ToString(), JsonConvert.SerializeObject(complexObj));
                            }
                        }
                        else
                        {
                            data.Add(raw.Name.ToString(), raw.GetValueAsString());
                        }
                    }
                    bloombergResponse.AddEntry((secId, 0, data));
                }
            }

            bloombergResponse.Source = ResponseSource.Terminal;
            return bloombergResponse;
        }

        private static object ExtractComplexObjectFromResponse(string fieldName, Element raw, ILogger logger)
        {
            object complexData = null;

            var field = fieldName.BloombergField();

            if (BloombergFieldExtensions.IsEnumerableOf(field.BloombergType(), typeof(IBloombergType)))
            {
                var type = field.BloombergType();
                var innerType = BloombergFieldExtensions.GetInnerType(type);
                var listType = typeof(List<>).MakeGenericType(innerType);

                var list = Activator.CreateInstance(listType);
                for (int vi = 0; vi < raw.NumValues; vi++)
                {
                    var entry = raw.GetValueAsElement(vi);
                    var newItem = Activator.CreateInstance(innerType);
                    newItem.GetType().GetMethod("ReadElement").Invoke(newItem, new[] {entry});
                    list.GetType().GetMethod("Add").Invoke(list, new[] {newItem});
                }

                complexData = list;
            }
            else if (field.BloombergType() == typeof(IDictionary<DateTime, decimal>))
            {
                var dict = new Dictionary<DateTime, decimal>();
                for (int vi = 0; vi < raw.NumValues; vi++)
                {
                    int keyIndex = 0, valueIndex = 1;
                    if (field.UseProvidedIndexes())
                    {
                        (keyIndex, valueIndex) = field.ProvidedIndexes();
                    }

                    var entry = raw.GetValueAsElement(vi);
                    var dt = DateTime.Parse(entry.GetElement(keyIndex).GetValueAsString());
                    var factRaw = entry.GetElement(valueIndex).GetValueAsString();
                    var fact = factRaw.Contains("E")
                        ? decimal.Parse(factRaw, NumberStyles.Float)
                        : decimal.Parse(factRaw);

                    if (dict.ContainsKey(dt))
                    {
                        dict[dt] = fact;
                        logger.LogWarning($"While parsing {field} got duplicate keys {dt}-> {entry.GetElement(1).GetValueAsString()}");
                    }
                    else dict.Add(dt, fact);
                }

                if (dict.Any())
                {
                    complexData = dict;
                }
            }
            else if (field.BloombergType() == typeof(IDictionary<string, string>))
            {
                var dict = new Dictionary<string, string>();
                for (int vi = 0; vi < raw.NumValues; vi++)
                {
                    var entry = raw.GetValueAsElement(vi);
                    var key = entry.GetElement(0).GetValueAsString();
                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(entry.GetElement(0).GetValueAsString(),
                            entry.GetElement(1).GetValueAsString());
                    }
                    else
                    {
                        if (key != "STRIP-TO")
                            logger.LogError($"Bloomberg sent down different data for the same key twice. We have ignored the second entry. {field} -> {key} -> {entry.GetElement(1).GetValueAsString()}");
                    }
                }

                if (dict.Any())
                {
                    complexData = dict;
                }
            }
            else if (field.BloombergType() == typeof(IDictionary<int, string>))
            {
                var dict = new Dictionary<int, string>();
                for (int vi = 0; vi < raw.NumValues; vi++)
                {
                    var entry = raw.GetValueAsElement(vi);
                    var key = entry.GetElement(0).GetValueAsInt32();

                    if (field.UseIndexAsKeyDictionary())
                    {
                        key = vi;
                    }

                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(key, entry.GetElement(1).GetValueAsString());
                    }
                    else
                    {
                        logger.LogError($"Bloomberg sent down different data for the same key twice. We have ignored the second entry. {field} -> {key} -> {entry.GetElement(1).GetValueAsString()}");
                    }
                }

                if (dict.Any())
                {
                    complexData = dict;
                }
            }

            return complexData;
        }
    }
}

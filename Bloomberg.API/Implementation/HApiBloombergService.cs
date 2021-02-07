using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bloomberg.API.HAPI;
using Bloomberg.API.HAPI.Decoder;
using Bloomberg.API.HAPI.Model;
using Bloomberg.API.HAPI.Parser;
using Bloomberg.API.Model;
using Microsoft.Extensions.Logging;

namespace Bloomberg.API.Implementation
{
    public class HApiBloombergService : IBloombergService
    {
        private readonly ILogger<IBloombergService> logger;

        private readonly BloombergClient bloombergClient;

        static string GetIdentifierType(string fullIdentifier)
        {
            if (fullIdentifier.IsIsin()) return "ISIN";
            if (fullIdentifier.IsCusip()) return "CUSIP";
            return "TICKER";
        }

        static string GetIdentifier(string fullIdentifier)
        {
            switch (GetIdentifierType(fullIdentifier))
            {
                case "ISIN":
                case "CUSIP":
                    return fullIdentifier;
                case "TICKER":
                    return fullIdentifier;
            }
            
            return fullIdentifier;
        }

        public HApiBloombergService(BloombergClient bloombergClient, ILogger<IBloombergService> logger)
        {
            this.logger = logger;
            this.bloombergClient = bloombergClient;
            bloombergClient.Initialise().Wait();
        }

        public async Task<string> BDP(string ticker, string field)
        {
            var response = await RequestAsync(new BRequest(ticker, field));
            return response.GetValue(ticker, field);
        }

        public async Task<BResponse> RequestAsync(BRequest request)
        {
            var jobId = Guid.NewGuid().ToString("N").Substring(0, 20);
            using (logger.BeginScope(jobId))
            {

                logger.LogInformation(
                    $"REQ:{jobId} Recieved request with {request.Identifiers.Count} identifiers, and {request.Fields.Count} fields");

                // Process:
                // Create & Save Universe

                var requestCatalog = await bloombergClient.GetRequestCatalog();

                var universe = new Universe
                {
                    Identifier = "u" + jobId,
                    Title = "u" + jobId,
                    Description = "u" + jobId,
                    Contains = request.Identifiers.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).Select(x =>
                        new Security
                        {
                            Type = "Identifier",
                            IdentifierType = GetIdentifierType(x),
                            IdentifierValue = GetIdentifier(x)
                        }).ToArray(),
                };

                // Create & Save Field list
                var fieldList = new FieldList
                {
                    Identifier = "f" + jobId,
                    Title = "f" + jobId,
                    Description = "f" + jobId,
                    Type = request.IsHistorical ? "HistoryFieldList" : "DataFieldList",
                    Contains = request.Fields.Distinct().Select(x => new Field {Mnemonic = x}).ToArray()
                };

                // Create & Save Submit Trigger
                var trigger = new Trigger
                {
                    Type = "SubmitTrigger",
                    Identifier = "t" + jobId,
                    Title = "t" + jobId,
                    Description = "t" + jobId,
                };

                // Create & Save Request

                universe = await bloombergClient.CreateItem(requestCatalog.Identifier, universe);
                fieldList = await bloombergClient.CreateItem(requestCatalog.Identifier, fieldList);
                trigger = await bloombergClient.CreateItem(requestCatalog.Identifier, trigger);

                logger.LogInformation($"REQ:{jobId} - Prequistes setup");

                var bbgRequest = new Request
                {
                    Type = "DataRequest",
                    Identifier = "r" + jobId,
                    Title = "r" + jobId,
                    Description = "r" + jobId,
                    Universe = new Uri(universe.AdditionalData["@context"]["@base"].ToString()),
                    FieldList = new Uri(fieldList.AdditionalData["@context"]["@base"].ToString()),
                    Trigger = new Uri(trigger.AdditionalData["@context"]["@base"].ToString()),

                };

                if (request.IsHistorical)
                {
                    var historicalRequest = new HistoryRequest()
                    {
                        Identifier = "r" + jobId,
                        Title = "r" + jobId,
                        Description = "r" + jobId,
                        Universe = new Uri(universe.AdditionalData["@context"]["@base"].ToString()),
                        FieldList = new Uri(fieldList.AdditionalData["@context"]["@base"].ToString()),
                        Trigger = new Uri(trigger.AdditionalData["@context"]["@base"].ToString()),
                        RuntimeOptions = new HistoryRuntimeOptions
                        {
                            Period = "daily",
                            DateRange = new IntervalDateRange
                            {
                                StartDate = request.HistoricalFromDate.Value.ToString("yyyy-MM-dd"),
                                EndDate = request.HistoricalEndDate.Value.ToString("yyyy-MM-dd"),
                            }
                        }
                    };
                    bbgRequest = historicalRequest;
                }

                bbgRequest = await bloombergClient.CreateItem(requestCatalog.Identifier, bbgRequest);

                logger.LogInformation($"REQ:{jobId} - Request created - waiting for results");

                // Wait for results
                var result = await bloombergClient.WaitForResult(bbgRequest);

                logger.LogInformation($"REQ:{jobId} - Results recieved");
                return DecodeResponse(result, bbgRequest.Identifier);
            }
        }

        public bool IsConnected => bloombergClient.IsConnected;

        private BResponse DecodeResponse(Stream bbgResponse, string requestId)
        {
            //Buffer locally so we can seek around the stream
            var sourceStream = new MemoryStream();
            bbgResponse.CopyTo(sourceStream);
            sourceStream.Position = 0;
            
            // Dump Raw
            SaveMemoryStreamToFile(sourceStream, $"{requestId}.bbg.raw");
            
            // if we didnt need to decode just parse the plain text
            return BloombergResponseParser.Parse(new StreamReader(BloombergDecoder.DecodeResponse(sourceStream)), logger);
        }

        private void SaveMemoryStreamToFile(Stream stream, string filename)
        {
            var saveLocation = $".\\BloombergTemp\\{filename}";
            
            using (var fileStream = File.OpenWrite(saveLocation))
            {
                stream.CopyTo(fileStream);
                stream.Position = 0;
                fileStream.Flush();
                fileStream.Close();
            }
        }
        

    }
}

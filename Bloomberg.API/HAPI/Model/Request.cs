using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomberg.API.HAPI.Model
{
    /// Simplified data model for request.
    public class Request : BasicContainer
    {
        public Request()
        {
            Type = "Request";
        }
        
        [JsonProperty("@type")] public string Type { get; set; }

        public bool ShouldSerializeType()
        {
            return !string.IsNullOrWhiteSpace(Type);
        }

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("universe")] public Uri Universe { get; set; }

        [JsonProperty("fieldList")] public Uri FieldList { get; set; }

        [JsonProperty("trigger")] public Uri Trigger { get; set; }

        [JsonProperty("formatting")] public DataFormat Formatting { get; set; } = new DataFormat();
        
        [JsonProperty("terminalIdentity", NullValueHandling = NullValueHandling.Ignore)]
        public BlpTerminalIdentity TerminalIdentity { get; set; }
        
        [JsonProperty(PropertyName = "pricingSourceOptions", NullValueHandling = NullValueHandling.Ignore)]
        public HistoryPricingSourceOptions PricingSourceOptions { get; set; }
    }
}
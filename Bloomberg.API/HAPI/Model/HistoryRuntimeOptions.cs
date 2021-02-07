using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a simplified data model for a history runtime options.
    /// </summary>
    public class HistoryRuntimeOptions
    {
        [JsonProperty("@type")] public string Type { get; set; } = "HistoryRuntimeOptions";

        [JsonProperty("historyPriceCurrency", NullValueHandling = NullValueHandling.Ignore)]
        public string HistoryPriceCurrency { get; set; }

        [JsonProperty("period")]
        public string Period { get; set; }

        [JsonProperty("dateRange")]
        public IntervalDateRange DateRange { get; set; }
    }
}
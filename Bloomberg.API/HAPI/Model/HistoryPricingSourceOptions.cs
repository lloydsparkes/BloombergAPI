using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a simplified data model for a history pricing source options.
    /// </summary>
    public class HistoryPricingSourceOptions
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("exclusive")]
        public bool Exclusive { get; set; }
    }
}
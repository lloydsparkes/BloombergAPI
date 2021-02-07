using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a simplified data model for a date range options.
    /// </summary>
    public class IntervalDateRange
    {
        [JsonProperty("@type")] public string Type { get; set; } = "IntervalDateRange";

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("endDate")]
        public string EndDate { get; set; }
    }
}
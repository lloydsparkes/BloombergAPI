using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a simplified data model for a history format options.
    /// </summary>
    public class HistoryFormat
    {
        [JsonProperty("@type")] public string Type { get; set; } = "HistoryFormat";

        [JsonProperty("dateFormat")]
        public string DateFormat { get; set; } = "yyyymmdd";
    }
}
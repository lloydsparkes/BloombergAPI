using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a data model for delivery notifications from BEAP.
    /// </summary>
    class DeliveryNotification
    {
        [JsonProperty("@context")]
        public Context Context { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("endedAtTime")]
        public string EndedAtTime { get; set; }

        [JsonProperty("generated")]
        public Distribution Generated { get; set; }
    }
}
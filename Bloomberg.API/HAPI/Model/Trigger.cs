using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// Simplified data model for trigger.
    public class Trigger : BasicContainer
    {
        [JsonProperty("@type")] public string Type { get; set; }

        public bool ShouldSerializeType()
        {
            return !string.IsNullOrWhiteSpace(Type);
        }
        
        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("description")] public string Description { get; set; }
    }
}
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    ///     Simplified data model for field list.
    /// </summary>
    public class FieldList : BasicContainer
    {
        [JsonProperty("@type")] public string Type { get; set; } = "DataFieldList";

        public bool ShouldSerializeType()
        {
            return !string.IsNullOrWhiteSpace(Type);
        }

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("contains")] public Field[] Contains { get; set; }
    }
}
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    ///     Data model representing single security in HAPI.
    /// </summary>
    public class Security
    {
        [JsonProperty("@type")] public string Type { get; set; }

        [JsonProperty("identifierType")] public string IdentifierType { get; set; }

        [JsonProperty("identifierValue")] public string IdentifierValue { get; set; }

        [JsonProperty(PropertyName = "fieldOverrides", NullValueHandling = NullValueHandling.Ignore)]
        public FieldOverride[] FieldOverrides { get; set; }
    }
}
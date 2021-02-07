using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    ///     Data model for field override section in HAPI.
    /// </summary>
    public class FieldOverride
    {
        [JsonProperty("@type")] public string Type { get; set; }

        [JsonProperty("cleanName", NullValueHandling = NullValueHandling.Ignore)] public string CleanName { get; set; }

        [JsonProperty("mnemonic", NullValueHandling = NullValueHandling.Ignore)] public string Mnemonic { get; set; }

        [JsonProperty("override")] public string Override { get; set; }
    }
}
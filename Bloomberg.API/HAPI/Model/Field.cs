using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// Data model for single field in HAPI.
    /// </summary>
    public class Field
    {
        [JsonProperty(PropertyName = "mnemonic", NullValueHandling = NullValueHandling.Ignore)]
        public string Mnemonic { get; set; }

        [JsonProperty(PropertyName = "cleanName", NullValueHandling = NullValueHandling.Ignore)]
        public string CleanName { get; set; }
    }
}
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents 'digest' part of DLNS notification.
    /// </summary>
    public class DigestModel
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("digestValue")]
        public string DigestValue { get; set; }

        [JsonProperty("digestAlgorithm")]
        public string DigestAlgorithm { get; set; }
    }
}
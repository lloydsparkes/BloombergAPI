using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class is a simplified data model for a Bloomberg Terminal identity.
    /// </summary>
    public class BlpTerminalIdentity
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("userNumber")]
        public int UserNumber { get; set; }

        [JsonProperty("serialNumber")]
        public int SerialNumber { get; set; }

        [JsonProperty("workStation")]
        public int WorkStation { get; set; }
    }
}
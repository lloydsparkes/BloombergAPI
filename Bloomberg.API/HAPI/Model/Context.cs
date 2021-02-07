using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents 'context' part of DLNS notification.
    /// </summary>
    public class Context
    {
        [JsonProperty("@vocab")]
        public string Vocab { get; set; }
    }
}
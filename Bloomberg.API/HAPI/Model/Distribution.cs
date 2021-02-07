using System;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents so-called 'generated' part of DLNS notification.
    /// </summary>
    public class Distribution : BasicContainer
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("digest")]
        public DigestModel Digest { get; set; }

        [JsonProperty("snapshot")]
        public Snapshot Snapshot { get; set; }

        [JsonProperty("accessible")] 
        public bool Accessible { get; set; }
    }
}
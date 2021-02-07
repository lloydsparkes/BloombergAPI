using System;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents 'snapshot' part of DLNS notification.
    /// </summary>
    public class Snapshot : BasicContainer
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("@id")]
        public Uri Id { get; set; }

        [JsonProperty("issued")]
        public string Issued { get; set; }

        [JsonProperty("dataset")]
        public Dataset Dataset { get; set; }
    }
}
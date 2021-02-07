using System;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    /// This class represents 'dataset' part of DLNS notification.
    /// </summary>
    public class Dataset : BasicContainer
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("catalog")]
        public Catalog Catalog { get; set; }
    }
}
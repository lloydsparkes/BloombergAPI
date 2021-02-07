using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomberg.API.HAPI.Model
{
    /// <summary>
    ///     Simplified data model for universe.
    /// </summary>
    public class Universe : BasicContainer
    {
        [JsonProperty("@type")] public string Type { get; set; } = "Universe";

        public bool ShouldSerializeType()
        {
            return !string.IsNullOrWhiteSpace(Type);
        }

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("contains")] public Security[] Contains { get; set; }
    }
}
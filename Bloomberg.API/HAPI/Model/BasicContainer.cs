using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomberg.API.HAPI.Model
{
    public class BasicContainer
    {
        [JsonProperty("identifier")] 
        public string Identifier { get; set; }

        public bool ShouldSerializeIdentifier()
        {
            return !string.IsNullOrWhiteSpace(Identifier);
        }

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
    }
}

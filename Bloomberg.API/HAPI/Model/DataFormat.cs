using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    public class DataFormat
    {
        [JsonProperty("@type")] public string Type { get; set; } = "DataFormat";
        
        // For some reason this is not the default - so force it to default
        [JsonProperty("dateFormat")] public string DateFormat { get; set; } = "yyyymmdd";
    }
}

using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    public class HistoryRequest : Request
    {
        public HistoryRequest()
        {
            Type = "HistoryRequest";
        }

        [JsonProperty(PropertyName = "formatting", NullValueHandling = NullValueHandling.Ignore)]
        public new HistoryFormat Formatting { get; set; } = new HistoryFormat();

        [JsonProperty(PropertyName = "runtimeOptions", NullValueHandling = NullValueHandling.Ignore)]
        public HistoryRuntimeOptions RuntimeOptions { get; set; }
        
    }
}
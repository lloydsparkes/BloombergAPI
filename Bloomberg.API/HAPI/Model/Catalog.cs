using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    public class Catalog : BasicContainer
    {
        [JsonProperty("subscriptionType")] public string SubscriptionType { get; set; }
    }
}
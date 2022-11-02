using Newtonsoft.Json;

namespace Elasticsearch.Initalize.Models
{
    public class ApiKeyResponse
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("api_key")]
        public string? ApiKeyValue { get; set; }
    }
}



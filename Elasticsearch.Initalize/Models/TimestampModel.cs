using Nest;
using Newtonsoft.Json;

namespace Elasticsearch.Initalize.Models
{
    public class TimestampModel
    {
        [JsonProperty("@timestamp")]
        [Date(Name = "@timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [Keyword(Name = "CosmosDocumentId")]
        public string? CosmosDocumentId { get; set; }
    }
}

using Newtonsoft.Json;

namespace Elasticsearch.Initalize.Models
{
    public class BulkIndexResponse
    {
        [JsonProperty("errors")]
        public string? Errors { get; set; }

        [JsonProperty("items")]
        public Item[]? Items { get; set; }
    }

    // TODO switch to json property attributes
    public class Item
    {
        [JsonProperty("create")]
        public Create? Create { get; set; }
    }

    public class Create
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("error")]
        public Error? Error { get; set; }
    }

    public class Error
    {
        [JsonProperty("caused_by")]
        public CausedBy? CausedBy { get; set; }

        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }

    public class CausedBy
    {
        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }
}

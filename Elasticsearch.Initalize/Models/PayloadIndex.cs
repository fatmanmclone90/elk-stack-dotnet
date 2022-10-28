using Nest;
using Newtonsoft.Json;

namespace Elasticsearch.Initalize.Models
{
    public class PayloadIndex : ICosmosDocument
    {
        [JsonProperty("@timestamp")]
        [Date(Name = "@timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [Keyword]
        public string? CosmosDocumentId { get; set; }

        [Keyword]
        public List<string>? Barcode { get; set; }

        [Keyword]
        public List<string>? CorrelationId { get; set; }

        [Keyword]
        public List<string>? DemandId { get; set; }

        [Keyword]
        public List<string>? IsValid { get; set; }

        [Keyword]
        public List<string>? Location { get; set; }

        [Keyword]
        public List<string>? MessageType { get; set; }

        [Keyword]
        public List<string>? Order { get; set; }

        [Keyword]
        public List<string>? OrderId { get; set; }

        [Keyword]
        public List<string>? PackAreaId { get; set; }

        [Keyword]
        public List<string>? ParcelBarcode { get; set; }

        [Keyword]
        public List<string>? ParcelId { get; set; }

        [Keyword]
        public List<string>? SKU { get; set; }

        [Keyword]
        public List<string>? Status { get; set; }

        [Keyword]
        public List<string>? UniqueKey { get; set; }

        [Keyword]
        public List<string>? UPOS { get; set; }

        [Keyword]
        public List<string>? UUID { get; set; }

        [Text]
        public string? Payload { get; set; }
    }
}

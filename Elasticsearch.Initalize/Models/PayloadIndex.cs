using Nest;

namespace Elasticsearch.Initalize.Models
{
    public class PayloadIndex : TimestampModel
    {
        [Keyword(Name = "Barcode")]
        public List<string>? Barcode { get; set; }

        [Keyword(Name = "CorrelationId")]
        public List<string>? CorrelationId { get; set; }

        [Keyword(Name = "DemandId")]
        public List<string>? DemandId { get; set; }

        [Keyword(Name = "IsValid")]
        public List<string>? IsValid { get; set; }

        [Keyword(Name = "Location")]
        public List<string>? Location { get; set; }

        [Keyword(Name = "MessageType")]
        public List<string>? MessageType { get; set; }

        [Keyword(Name = "Order")]
        public List<string>? Order { get; set; }

        [Keyword(Name = "OrderId")]
        public List<string>? OrderId { get; set; }

        [Keyword(Name = "PackAreaId")]
        public List<string>? PackAreaId { get; set; }

        [Keyword(Name = "ParcelBarcode")]
        public List<string>? ParcelBarcode { get; set; }

        [Keyword(Name = "ParcelId")]
        public List<string>? ParcelId { get; set; }

        [Keyword(Name = "SKU")]
        public List<string>? SKU { get; set; }

        [Keyword(Name = "Status")]
        public List<string>? Status { get; set; }

        [Keyword(Name = "UniqueKey")]
        public List<string>? UniqueKey { get; set; }

        [Keyword(Name = "UPOS")]
        public List<string>? UPOS { get; set; }

        [Keyword(Name = "UUID")]
        public List<string>? UUID { get; set; }

        [Text(Name = "Payload")]
        public string? Payload { get; set; }
    }
}

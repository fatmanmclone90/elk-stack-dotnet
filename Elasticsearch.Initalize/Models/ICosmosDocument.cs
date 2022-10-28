namespace Elasticsearch.Initalize.Models
{
    public interface ICosmosDocument
    {
        public DateTimeOffset Timestamp { get; set; }

        public string? CosmosDocumentId { get; set; }
    }
}

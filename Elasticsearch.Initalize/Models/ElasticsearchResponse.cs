namespace Elasticsearch.Initalize.Models
{
    public class BulkIndexResponse
    {
        public string? errors { get; set; }

        public Item[]? Items { get; set; }
    }

    // TODO switch to json property attributes
    public class Item
    {
        public Create? create { get; set; }
    }

    public class Create
    {
        public string? _Id { get; set; }

        public int status { get; set; }

        public Error? error { get; set; }
    }

    public class Error
    {
        public CausedBy? caused_by { get; set; }

        public string? reason { get; set; }
    }

    public class CausedBy
    {
        public string? reason { get; set; }
    }
}

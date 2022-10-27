using Elasticsearch.Net;
using Newtonsoft.Json;

var lowlevelClient = new ElasticLowLevelClient(
    new ConnectionConfiguration(new Uri("http://localhost:9200")));

var indexPattern = "dft";
var dataStreamName = $"{indexPattern}_data_stream";

var barcode = Guid.NewGuid().ToString();
var event1 = new IndexPayload
{
    Timestamp = DateTime.UtcNow,
    Barcode = new List<string> { barcode, Guid.NewGuid().ToString() },
    CorrelationId = new List<string> { Guid.NewGuid().ToString() },
    DemandId = new List<string> { Guid.NewGuid().ToString() },
    IsValid = new List<string> { Guid.NewGuid().ToString() },
    Location = new List<string> { Guid.NewGuid().ToString() },
    MessageType = new List<string> { "D6_OrderStatus" },
    Order = new List<string> { Guid.NewGuid().ToString() },
    OrderId = new List<string> { Guid.NewGuid().ToString() },
    PackAreaId = new List<string> { Guid.NewGuid().ToString() },
    ParcelBarcode = new List<string> { Guid.NewGuid().ToString() },
    ParcelId = new List<string> { Guid.NewGuid().ToString() },
    SKU = new List<string> { Guid.NewGuid().ToString() },
    Status = new List<string> { Guid.NewGuid().ToString() },
    UniqueKey = new List<string> { "InvalidRequest" },
    UPOS = new List<string> { Guid.NewGuid().ToString() },
    UUID = new List<string> { Guid.NewGuid().ToString() },
};
var event2 = new IndexPayload
{
    Timestamp = DateTime.UtcNow,
    Barcode = new List<string> { barcode, Guid.NewGuid().ToString() },
    CorrelationId = new List<string> { Guid.NewGuid().ToString() },
    DemandId = new List<string> { Guid.NewGuid().ToString() },
    IsValid = new List<string> { Guid.NewGuid().ToString() },
    Location = new List<string> { Guid.NewGuid().ToString() },
    MessageType = new List<string> { "D6_OrderStatus" },
    Order = new List<string> { Guid.NewGuid().ToString() },
    OrderId = new List<string> { Guid.NewGuid().ToString() },
    PackAreaId = new List<string> { Guid.NewGuid().ToString() },
    ParcelBarcode = new List<string> { Guid.NewGuid().ToString() },
    ParcelId = new List<string> { Guid.NewGuid().ToString() },
    SKU = new List<string> { Guid.NewGuid().ToString() },
    Status = new List<string> { Guid.NewGuid().ToString() },
    UniqueKey = new List<string> { "InvalidRequest" },
    UPOS = new List<string> { Guid.NewGuid().ToString() },
    UUID = new List<string> { Guid.NewGuid().ToString() },
};

await WriteDataStream(
    lowlevelClient,
    dataStreamName,
    new[] { event1, event2 });

static async Task WriteDataStream(
    ElasticLowLevelClient client,
    string dataStreamName,
    IEnumerable<IndexPayload> events)
{
    List<string> operations = new();
    foreach (var @event in events)
    {
        operations.Add(JsonConvert.SerializeObject(new { create = new { _id = Guid.NewGuid().ToString() } }));
        operations.Add(JsonConvert.SerializeObject(@event));
    }

    var response = await client.BulkAsync<StringResponse>(
        dataStreamName,
        PostData.MultiJson(operations));

    if (!response.Success)
    {
        Console.WriteLine(response.OriginalException.Message);
        throw new Exception("Failed to index");
    }
    else
    {
        var bulkIndexResponse = JsonConvert.DeserializeObject<BulkIndexResponse>(response.Body);

        if (bulkIndexResponse?.Items?.Any(x => x.create?.status != 201) == true)
        {
            var faileItems = bulkIndexResponse.Items.Where(x => x.create?.status != 201);
            foreach (var failedItem in faileItems)
            {
                Console.WriteLine($"{failedItem?.create?._Id ?? "unknown id"}\t{failedItem?.create?.error?.caused_by?.reason ?? "unknown error"}");
            }
        }
    }
}


class IndexPayload

{
    [JsonProperty("@timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    public List<string>? Barcode { get; set; }

    public List<string>? CorrelationId { get; set; }

    public List<string>? DemandId { get; set; }

    public List<string>? IsValid { get; set; }

    public List<string>? Location { get; set; }

    public List<string>? MessageType { get; set; }

    public List<string>? Order { get; set; }

    public List<string>? OrderId { get; set; }

    public List<string>? PackAreaId { get; set; }

    public List<string>? ParcelBarcode { get; set; }

    public List<string>? ParcelId { get; set; }

    public List<string>? SKU { get; set; }

    public List<string>? Status { get; set; }

    public List<string>? UniqueKey { get; set; }

    public List<string>? UPOS { get; set; }

    public List<string>? UUID { get; set; }

    public string? Payload { get; set; }
}

class BulkIndexResponse
{
    public string? errors { get; set; }

    public Item[]? Items { get; set; }
}

class Item
{
    public Create? create { get; set; }
}

class Create
{
    public string? _Id { get; set; }
    
    public int status { get; set; }

    public Error? error { get; set; }
}

class Error
{
    public CausedBy? caused_by { get; set; }

    public string? reason { get; set; }
}

class CausedBy
{
    public string? reason { get; set; }
}
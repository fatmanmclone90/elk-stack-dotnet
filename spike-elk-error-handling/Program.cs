using Elasticsearch.Net;
using Newtonsoft.Json;

var lowlevelClient = new ElasticLowLevelClient(
    new ConnectionConfiguration(new Uri("http://localhost:9200")));

var indexPattern = "dft";
var dataStreamName = $"{indexPattern}_data_stream";

var dftEvent = new DftEvent
{
    @timestamp = DateTime.UtcNow,
    Skus = new List<String>
    {
        "foo-bar",
        "bar-foo",
    }
};

await WriteDataStreamWithId(
    lowlevelClient,
    dataStreamName,
    new[] { dftEvent });

static async Task WriteDataStreamNoId(
    ElasticLowLevelClient client,
    string dataStreamName,
    IEnumerable<DftEvent> payloads)
{
    var bulkOperation = JsonConvert.SerializeObject(new { create = new { } });

    List<string> operations = new();
    foreach (var payload in payloads)
    {
        operations.Add(bulkOperation);
        operations.Add(JsonConvert.SerializeObject(payload));
    }

    var notDft = new NotDftEvent
    {
        PayloadString = "",
        Skus = new[] { "123" }.ToList(),
    };

    operations.Add(bulkOperation);
    operations.Add(JsonConvert.SerializeObject(notDft));

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
                Console.WriteLine(failedItem.create?.error?.caused_by?.reason ?? "unknown");
            }
        }
    }
}

static async Task WriteDataStreamWithId(
    ElasticLowLevelClient client,
    string dataStreamName,
    IEnumerable<DftEvent> payloads)
{
    List<string> operations = new();
    foreach (var payload in payloads)
    {
        operations.Add(JsonConvert.SerializeObject(new { create = new { _id = payload.Id } }));
        operations.Add(JsonConvert.SerializeObject(payload));
    }

    var notDft = new NotDftEvent
    {
        PayloadString = "",
        Skus = new[] { "123" }.ToList(),
    };

    operations.Add(JsonConvert.SerializeObject(new { create = new { _id = notDft.Id } }));
    operations.Add(JsonConvert.SerializeObject(notDft));

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
                Console.WriteLine($"{failedItem.create._Id ?? "unknown id"}\t{failedItem.create?.error?.caused_by?.reason ?? "unknown error"}");
            }
        }
    }
}


class NotDftEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? PayloadString { get; set; }

    public List<string>? Skus { get; set; }
}

class DftEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonProperty("@timestamp")]
    public DateTimeOffset @timestamp { get; set; }

    public string? PayloadString { get; set; }

    public List<string>? Skus { get; set; }
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
using Elasticsearch.Initalize;
using Elasticsearch.Initalize.Models;
using Elasticsearch.Initialize;
using Newtonsoft.Json;

var apiKeyFilePath = @".\keys\create_security_keys.json";
if (!File.Exists(apiKeyFilePath))
{
    throw new InvalidDataException("No API Keys found, ensure cluster is running");
}

string text = File.ReadAllText(@".\keys\create_security_keys.json");
var apiKey = JsonConvert.DeserializeObject<ApiKeyResponse>(text);

if (apiKey == null || apiKey.Id == null || apiKey.ApiKeyValue == null)
{
    throw new InvalidDataException("No API Keys found, ensure cluster is running");
}

// load .env file used for docker compose
DotNetEnv.Env.TraversePath().Load();

var elasticUsername = Environment.GetEnvironmentVariable("ELASTIC_USERNAME");
var elasticPassword = Environment.GetEnvironmentVariable("ELASTIC_PASSWORD");

var elasticSearchWrapper = new ElasticsearchClientWrapper(
    "http://localhost:9200",
    dataStreamName: "dft_data_stream",
    apiKey.Id,
    apiKey.ApiKeyValue);

var kibanaWrapper = new KibanaClientWrapper(
    "http://localhost:5601",
    "dft_data_stream",
    elasticUsername,
    elasticPassword);

var barcode = Guid.NewGuid().ToString();
var event1 = new PayloadIndex
{
    Timestamp = DateTime.UtcNow,
    CosmosDocumentId = Guid.NewGuid().ToString(),
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
var event2 = new PayloadIndex
{
    Timestamp = DateTime.UtcNow,
    CosmosDocumentId = Guid.NewGuid().ToString(),
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

await elasticSearchWrapper.CreateLifeCycle(numberOfDaysBeforeDelete: 5);

await elasticSearchWrapper.CreateTemplate<PayloadIndex>();

// ideally would be enforced by interface and read from json property
await kibanaWrapper.CreateIndexPattern("@timestamp");

await elasticSearchWrapper.BulkIndex<PayloadIndex>(new[] { event1, event2 });

Console.WriteLine("SUCCESS");

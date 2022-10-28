using Elasticsearch.Initalize;
using Elasticsearch.Initalize.Models;
using Elasticsearch.Initialize;

var elasticSearchWrapper = new ElasticsearchClientWrapper("http://localhost:9200", "dft_data_stream");
var kibanaWrapper = new KibanaClientWrapper("http://localhost:5601", "dft_data_stream");

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

using Elasticsearch.Net;
using Nest;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var client = new ElasticClient(
    new ConnectionSettings(new Uri("http://localhost:9200/"))
    .DefaultFieldNameInferrer(p => p));

var shouldUpdate = true;
var indexPattern = "dft";
var templateName = $"{indexPattern}-template";
var lifeCycleName = $"{indexPattern}-lifecycle";
var dataStreamName = $"{indexPattern}_data_stream";

var indexPatternTitle = $"{dataStreamName}*";
var indexPatternId = dataStreamName;

var lifeCycle = await CreateLifeCycle(
    client,
    lifeCycleName,
    tryUpdate: shouldUpdate);
Console.WriteLine(lifeCycle);

var createTemplate = await PutDataStreamTemplate(
    client,
    templateName,
    indexPattern,
    lifeCycleName,
    tryUpdate: shouldUpdate);
Console.WriteLine(createTemplate);

await CreateIndexPattern(indexPatternId, indexPatternTitle);

var barcode = Guid.NewGuid().ToString();
var payload = new IndexPayload
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
payload.Payload = JsonSerializer.Serialize(payload);

var writeData = await WriteDataStream(
client,
dataStreamName,
payload);

Console.WriteLine(writeData);

var searchData = await GetData(client, dataStreamName, "Barcode", barcode);

Console.WriteLine(searchData);

static async Task<IEnumerable<IndexPayload>> GetData(
    ElasticClient client,
    string dataStream,
    string property,
    string value)
{
    var retryCount = 3;
    var counter = 0;
    var documents = new List<IndexPayload>();
    while (counter < retryCount)
    {
        var searchResponse = await client.LowLevel.SearchAsync<SearchResponse<IndexPayload>>(
            dataStream,
            PostData.Serializable(new
            {
                query = new
                {
                    match_phrase = new Dictionary<string, string>
                    {
                    { property, value }
                    }
                }
            }));

        if (searchResponse.Documents.Count > 0)
        {
            documents = searchResponse.Documents.ToList();
            break;
        }
        await Task.Delay(1000);
        counter++;
    }

    return documents;
}

static async Task<StringResponse> PutDataStreamTemplate(
    ElasticClient client,
    string templateName,
    string indexPattern,
    string lifeCycleName,
    bool tryUpdate)
{
    // investigate ways to do this without NEST
    var typeMappingDescriptor = new TypeMappingDescriptor<IndexPayload>().AutoMap();

    var templateExistsResponse = await client.LowLevel.Indices.GetTemplateV2ForAllAsync<StringResponse>(templateName);

    // need to decide how frequently we would check the tempalte
    if (templateExistsResponse.HttpStatusCode == 404 || tryUpdate)
    {
        var templateCreateResponse = client.LowLevel.Indices.PutTemplateV2ForAll<StringResponse>(
            templateName,
            PostData.Serializable(new
            {
                index_patterns = new[] { $"{indexPattern}*" },
                data_stream = new { },
                template = new
                {
                    mappings = typeMappingDescriptor,
                    settings = new
                    {
                        index = new
                        {
                            lifecycle = new
                            {
                                name = lifeCycleName,
                            }
                        }
                    }
                },
            }));

        return templateCreateResponse;
    }
    else
    {
        return templateExistsResponse;
    }
}

static async Task<StringResponse> CreateLifeCycle(
    ElasticClient client,
    string lifeCycleName,
    bool tryUpdate)
{
    var lifeCycleExists = await client.LowLevel.IndexLifecycleManagement.GetLifecycleAsync<StringResponse>(lifeCycleName);

    // need to decide how frequently we would check the tempalte
    if (lifeCycleExists.HttpStatusCode == 404 || tryUpdate)
    {
        var lifeCycleCreated = await client.LowLevel.IndexLifecycleManagement.PutLifecycleAsync<StringResponse>(
            lifeCycleName,
            PostData.Serializable(new
            {
                policy = new
                {
                    phases = new
                    {
                        hot = new
                        {
                            actions = new
                            {
                                rollover = new
                                {
                                    max_age = "1d",
                                    max_size = "50gb",
                                },
                                set_priority = new
                                {
                                    priority = 1
                                }
                            },
                            min_age = "0ms"
                        },
                        delete = new
                        {
                            min_age = "5d",
                            actions = new
                            {
                                delete = new { }
                            }
                        }
                    }
                }
            }));

        return lifeCycleCreated;
    }
    else
    {
        return lifeCycleExists;
    }
}

static async Task<StringResponse> WriteDataStream(
    ElasticClient client,
    string dataStreamName,
    Object payload)
{
    return await client.LowLevel.IndexAsync<StringResponse>(
        dataStreamName,
        PostData.Serializable(payload));
}


static async Task CreateIndexPattern(
    string indexPatternId,
    string indexPatternTitle)
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5601"),
    };
    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("kbn-xsrf", "this is a required header");

    var getResponse = await httpClient
        .GetAsync($"/api/saved_objects/_find?type=index-pattern&search_fields=title&search={indexPatternTitle}");

    if (getResponse.StatusCode != HttpStatusCode.OK)
    {
        throw new Exception(getResponse.StatusCode.ToString());
    }

    var savedObject = await getResponse.Content.ReadFromJsonAsync<SavedObject>();

    if (savedObject == null || savedObject.Total == 0)
    {
        var postResponse = await httpClient.PostAsJsonAsync(
            $"api/saved_objects/index-pattern/{indexPatternId}?overwrite=false",
            new
            {
                attributes = new
                {
                    title = indexPatternTitle,
                    timeFieldName = "@timestamp",
                }
            });

        if (postResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Failed to create index pattern");
        }
    }
}

class IndexPayload

{
    [Date(Name = "@timestamp")] 
    public DateTimeOffset Timestamp { get; set; }

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

class SavedObject
{
    public int Total { get; set; }
}
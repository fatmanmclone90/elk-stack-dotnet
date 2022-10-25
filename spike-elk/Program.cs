using Elasticsearch.Net;
using Nest;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

var client = new ElasticClient(new Uri("http://localhost:9200/"));
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

var code = $"some code -:+ {DateTime.UtcNow:yyyymmddHHMM}";
var dftEvent = new DftEvent
{
    Code = code,
    PartialCode = $"foobar - {DateTime.UtcNow:u}",
    SortOrder = new Random().Next(0, 1000),
    Timestamp = DateTime.UtcNow,
};

dftEvent.PayloadString = JsonSerializer.Serialize(dftEvent);
//dftEvent.PayloadObject = new
//{
//    SomeBar = 1,
//    SomeFoo = "yeah"
//};
//dftEvent.PayloadObjectNested = new
//{
//    SomeBar = 1,
//    SomeFoo = "yeah",
//    SomeHad = "nah"
//};
dftEvent.BarcodeList = new List<Barcode>
{
    new Barcode
    {
        Id = "homer",
    },
    new Barcode
    {
        Id = "bart",
    },
    new Barcode
{
    Id = "bart123",
}
};
dftEvent.BarcodeListString = new List<string>
{
    "notbart",
};

var writeData = await WritenDataStream(
client,
dataStreamName,
dftEvent);

Console.WriteLine(writeData);

var searchData = await GetData(client, dataStreamName, "code", code);

Console.WriteLine(searchData);

static async Task<IEnumerable<DftEvent>> GetData(
    ElasticClient client,
    string dataStream,
    string property,
    string value)
{
    var retryCount = 3;
    var counter = 0;
    var documents = new List<DftEvent>();
    while (counter < retryCount)
    {
        var searchResponse = await client.LowLevel.SearchAsync<SearchResponse<DftEvent>>(
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
    var typeMappingDescriptor = new TypeMappingDescriptor<DftEvent>().AutoMap();

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

static async Task<StringResponse> WritenDataStream(
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
class DftEvent
{
    [Keyword(Ignore =true)]public Guid Id { get; set; } = Guid.NewGuid();

    [Date(Name = "@timestamp")] public DateTimeOffset Timestamp { get; set; }

    // full text search only
    [Keyword] public string? Code { get; set; }

    [Keyword] public string? SomeOtherField { get; set; }

    // allows for partial text search
    [Text] public string? PartialCode { get; set; }

    public int? SortOrder { get; set; }

    [Text] public string? PayloadString { get; set; }

    //[Object] public object? PayloadObject { get; set; }

    //[Nested] public object? PayloadObjectNested { get; set; }

    [Nested] public List<Barcode>? BarcodeList { get; set; }

    [Keyword] public List<string>? BarcodeListString { get; set; }
}

class SavedObject
{
    public int Total { get; set; }
}

class Barcode
{
    public string? Id { get; set; }
}
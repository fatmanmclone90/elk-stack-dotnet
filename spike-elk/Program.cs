using Elasticsearch.Net;
using Nest;
using static System.Net.Mime.MediaTypeNames;

var client = new ElasticClient(new Uri("http://localhost:9200/"));

var indexPattern = "dft";
var templateName = $"{indexPattern}-template";
var lifeCycleName = $"{indexPattern}-lifecycle";
var dataStreamName = $"{indexPattern}_data_stream";

var lifeCycle = await CreateLifeCycle(
    client,
    templateName);
Console.WriteLine(lifeCycle);

var createTemplate = await PutDataStreamTemplate(
    client,
    templateName,
    indexPattern,
    lifeCycleName,
    tryUpdate: true);
Console.WriteLine(createTemplate);

var code = $"some code -:+ {DateTime.UtcNow:yyyymmddHHMM}";
var writeData = await WritenDataStream(
    client,
    dataStreamName,
    new MyDocument
    {
        Code = code,
        PartialCode = $"foobar - {DateTime.UtcNow:u}",
        SortOrder = new Random().Next(0, 1000),
        Timestamp = DateTime.UtcNow,
    });

Console.WriteLine(writeData);

var searchData = await GetData(client, dataStreamName, "code", code);

Console.WriteLine(searchData);

static async Task<IEnumerable<MyDocument>> GetData(
    ElasticClient client,
    string dataStream,
    string property,
    string value)
{
    var retryCount = 3;
    var counter = 0;
    var documents = new List<MyDocument>();
    while (counter < retryCount)
    {
        var searchResponse = await client.LowLevel.SearchAsync<SearchResponse<MyDocument>>(
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
    var typeMappingDescriptor = new TypeMappingDescriptor<MyDocument>().AutoMap();

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
    string lifeCycleName)
{
    var lifeCycleExists = await client.LowLevel.IndexLifecycleManagement.GetLifecycleAsync<StringResponse>(lifeCycleName);

    // need to decide how frequently we would check the tempalte
    if (lifeCycleExists.HttpStatusCode == 404)
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

class MyDocument
{
    [Date(Name = "@timestamp")] public DateTimeOffset Timestamp { get; set; }

    // full text search only
    [Keyword] public string? Code { get; set; }

    // allows for partial text search
    [Text] public string? PartialCode { get; set; }

    public int SortOrder { get; set; }
}
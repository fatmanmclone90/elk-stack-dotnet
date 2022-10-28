using Elasticsearch.Initalize.Models;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elasticsearch.Initialize
{
    internal class ElasticsearchClientWrapper
    {
        private readonly ElasticLowLevelClient client;
        private readonly string templateName;
        private readonly string lifeCycleName;
        private readonly string dataStreamName;

        public ElasticsearchClientWrapper(
            string elasticSearchUrl,
            string dataStreamName)
        {
            this.client = new ElasticLowLevelClient(
                new ConnectionConfiguration(new Uri(elasticSearchUrl)));

            this.dataStreamName = dataStreamName;
            this.templateName = $"{dataStreamName}-template";
            this.lifeCycleName = $"{dataStreamName}-lifecycle";
        }

        public async Task CreateLifeCycle(int numberOfDaysBeforeDelete)
        {
            var lifeCycleCreated = await client.IndexLifecycleManagement.PutLifecycleAsync<StringResponse>(
                this.lifeCycleName,
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
                                min_age = $"{numberOfDaysBeforeDelete}d",
                                actions = new
                                {
                                    delete = new { }
                                }
                            }
                        }
                    }
                }));

            if (!lifeCycleCreated.Success || lifeCycleCreated.HttpStatusCode != 200)
            {
                throw new InvalidDataException("unable to create lifecycle", lifeCycleCreated.OriginalException);
            }
        }

        public async Task CreateTemplate<T>() where T : class, ICosmosDocument
        {
            var mappings = this.CreatMappings<T>();
            var response = await client.Indices.PutTemplateV2ForAllAsync<StringResponse>(
                this.templateName,
                PostData.String(JsonConvert.SerializeObject(
                    new
                    {
                        index_patterns = new[] { $"{this.dataStreamName}*" },
                        data_stream = new { },
                        template = new
                        {
                            mappings = mappings,
                            settings = new
                            {
                                index = new
                                {
                                    lifecycle = new
                                    {
                                        name = this.lifeCycleName,
                                    }
                                }
                            }
                        }
                    })));

            if (!response.Success || response.HttpStatusCode != 200)
            {
                throw new InvalidDataException("unable to create template", response.OriginalException);
            }
        }

        public async Task BulkIndex<T>(IEnumerable<T> events) where T : class, ICosmosDocument
        {
            List<string> operations = new();
            foreach (var @event in events)
            {
                operations.Add(JsonConvert.SerializeObject(new { create = new { _id = @event.CosmosDocumentId } }));
                operations.Add(JsonConvert.SerializeObject(@event));
            }

            var response = await this.client.BulkAsync<StringResponse>(
                this.dataStreamName,
                PostData.MultiJson(operations));

            if (!response.Success)
            {
                Console.WriteLine(response.OriginalException.Message);
                throw new InvalidDataException("Failed to index", response.OriginalException);
            }
            else
            {
                var bulkIndexResponse = JsonConvert.DeserializeObject<BulkIndexResponse>(response.Body);

                if (bulkIndexResponse?.Items?.Any(x => x.create?.status != 201) == true)
                {
                    var faileItems = bulkIndexResponse.Items.Where(x => x.create?.status != 201);
                    var errors = new List<string>();
                    foreach (var failedItem in faileItems)
                    {
                        errors.Add($"{failedItem?.create?._Id ?? "unknown id"}\t{failedItem?.create?.error?.caused_by?.reason ?? "unknown error"}");
                    }
                    
                    throw new InvalidDataException($"Failed to Bulk Index {errors}");
                }
            }
        }

        // Hackery, still using NEST
        private JObject CreatMappings<T>() where T : class, ICosmosDocument
        {
            var typeMappingDescriptor = new TypeMappingDescriptor<T>().AutoMap();
            var mappingsJson = new ElasticClient().SourceSerializer.SerializeToString(typeMappingDescriptor);

            return JObject.Parse(mappingsJson);
        }
    }
}

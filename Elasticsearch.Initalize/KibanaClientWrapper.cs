using System.Net;
using System.Net.Http.Json;
using static Elasticsearch.Initalize.Models.KibanaResponse;

namespace Elasticsearch.Initalize
{
    public class KibanaClientWrapper
    {
        private readonly HttpClient httpClient;
        private readonly string dataStreamName;

        public KibanaClientWrapper(string kibanaUrl, string dataStreamName)
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = new Uri(kibanaUrl),
            };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("kbn-xsrf", "this is a required header");

            this.dataStreamName = dataStreamName;
        }

        public async Task CreateIndexPattern(string timestampField)
        {
            var response = await this.httpClient
                .GetAsync($"/api/saved_objects/_find?type=index-pattern&search_fields=title&search={this.dataStreamName}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new InvalidDataException($"Staus Code {response.StatusCode} and content {content}");
            }

            var savedObject = await response.Content.ReadFromJsonAsync<SavedObject>();

            if (savedObject == null || savedObject.Total == 0)
            {
                var postResponse = await httpClient.PostAsJsonAsync(
                    $"api/saved_objects/index-pattern/{this.dataStreamName}?overwrite=false",
                    new
                    {
                        attributes = new
                        {
                            title = $"{this.dataStreamName}*",
                            timeFieldName = timestampField,
                        }
                    });

                if (postResponse.StatusCode != HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new InvalidDataException($"Staus Code {postResponse.StatusCode} and content {content}");
                }
            }
        }
    }
}

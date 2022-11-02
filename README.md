# elk-stack-dotnet

Using data streams in Elasticsearch with x-pack.  Solution continas a docker-compose file to launch the ELK stack and a console app to instantiate objects.

# Launching ELK Stack

## Setting Environment Variables

Set variables in .env file.

```
docker compose rm
docker compose up --remove-orphans
```

Login to Kibana using the ELASTICSEARCH_USERNAME and ELASTICSEARCH_PASSWORD values set in compose file.

## Create API Key

On Startup an API key will be created with details saved to `./keys`.

Copy Id and api_key into ElasticsearachClientWrapper instantiation.

Above API key gives permissions to instantiate the cluster and send data.

If this does not work, the API key can be manually created using the below request.

From DEV console:

```
POST /_security/api_key
{
  "name": "my-api-key",
  "expiration": "1d",
  "role_descriptors": {
    "dft_data_stream_role": {
      "cluster": [
        "manage_ilm",
        "manage_index_templates"
      ],
      "index": [
        {
          "names": [
            "dft_data_stream*"
          ],
          "privileges": [
            "read",
            "create",
            "create_index",
            "index",
            "write"
          ]
        }
      ]
    }
  }
}
```

# Running the Console App

## Create Elasticsearch Objects

Set the correct values in ELasticsearchClientWrapper class, taking values from docker-compose
```
    - ELASTICSEARCH_USERNAME=some-username
    - ELASTICSEARCH_PASSWORD=some-password
```

## Create Kibana Index Pattern

Set the correct values in KibanaClientWrapper class, taking values from docker-compose
```
    - ELASTICSEARCH_USERNAME=some-username
    - ELASTICSEARCH_PASSWORD=some-password
```

Run the Console app, this will:
- Create an ILM Policy
- Create an Index Template
- Create a Kibana Index Pattern
- Bulk Index sample documents

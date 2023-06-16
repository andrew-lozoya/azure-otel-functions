# opentelemetry-azure-isolated-function
```
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "OTEL_EXPORTER_OTLP_ENDPOINT": "https://otlp.nr-data.net:4317",
    "OTEL_EXPORTER_OTLP_HEADERS": "api-key=<INGEST_LICENSE_KEY>",
    "OTEL_SERVICE_NAME": "HttpTriggerSimple"
  },
    "Console": {
      "LogLevel": {
        "Default": "Information"
      }
    }
}
```

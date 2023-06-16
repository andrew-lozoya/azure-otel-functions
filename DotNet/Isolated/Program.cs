using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "Unknown", serviceVersion: "1.0.0")
    .AddAttributes(new Dictionary<string, object>() {
        { "faas.instance", "1234" }
    });

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(fw => fw.UseDefaultWorkerMiddleware())
    .ConfigureLogging(builder => builder
        .AddOpenTelemetry(builder => builder
            // Add resource attributes to all logs
            .SetResourceBuilder(resourceBuilder)
            // Local environment variables are used to override the default values of the OtlpExporterOptions
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md#environment-variables
            .AddOtlpExporter()
            .AddConsoleExporter()
        )
    )
    
    .ConfigureServices(services => services
        .AddOpenTelemetry()
        .WithTracing(builder => builder
            .AddHttpClientInstrumentation()
            .SetSampler(new AlwaysOnSampler())
            // Subscribed instrumentation sources
            .AddSource("manualInstrumentation")
            // Add resource attributes to all spans
            .SetResourceBuilder(resourceBuilder)
            // Local environment variables are used to override the default values of the OtlpExporterOptions
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md#environment-variables
            .AddOtlpExporter()
        ))
    .Build();

host.Run();
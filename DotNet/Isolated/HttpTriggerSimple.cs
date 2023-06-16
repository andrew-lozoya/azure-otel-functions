using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionsOpenTelemetry
{
    public class HttpTrigger
    {
        private readonly ILogger log;
        private readonly static HttpClient client = new HttpClient();


        public HttpTrigger(ILoggerFactory loggerFactory)
        {
            log = loggerFactory.CreateLogger<HttpTrigger>();
        }
        // Define helper functions for manual instrumentation
        public static ActivitySource ManualInstrumentationSource = new ActivitySource("manualInstrumentation");
        public static Activity? StartActivity(HttpRequestData req, FunctionContext fc)
        {
            // Retrieve resource attributes
            var answer = ManualInstrumentationSource.StartActivity(req.Method.ToUpper() + " " + req.Url.AbsolutePath, ActivityKind.Server);
            answer?.AddTag("http.url", req.Url);
            answer?.AddTag("faas.invocation_id", fc.InvocationId.ToString());
            answer?.AddTag("faas.name", Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") + "/" + fc.FunctionDefinition.Name);
            return answer;
        }

        public static HttpResponseData FinishActivity(HttpResponseData response, Activity? activity)
        {
            activity?.AddTag("http.status_code", ((int)response.StatusCode));
            return response;
        }

        [Function("HttpTrigger")]
        // Add the FunctionContext parameter to capture per-invocation information
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext fc)
        {
            //([POST|GET] /api/httptrigger) Span StartActivity
            using (var activity = StartActivity(req, fc))
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("Hello world!");

                //Usage of OpenTelemetry.Instrumentation.Http
                client.GetStringAsync("http://example.com/");

                return FinishActivity(response, activity);
            }
        }
    }
}
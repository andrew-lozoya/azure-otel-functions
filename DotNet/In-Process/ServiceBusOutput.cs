using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace FunctionsOpenTelemetry
{
    public static class HttpTrigger
    {
        [FunctionName("ServiceBusOutput")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log,
                [ServiceBus("printqueues", Connection = "opentelemetrydemo_SERVICEBUS")] IAsyncCollector<ServiceBusMessage> collector) // Service Bus output binding
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            // IAsyncCollector allows sending multiple messages in a single function invocation
            await collector.AddAsync(CreateMessage($"Hello, {name}"));

            log.LogInformation("Sending messages to send to Queue.");

            return new OkObjectResult(responseMessage);
        }

        private static ServiceBusMessage CreateMessage(string body, string traceparent = null)
        {
            ServiceBusMessage message = new ServiceBusMessage(body);
            System.Diagnostics.Activity.Current?.AddTag("customAttribute", "1234");
            return message;
        }
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ACR_TriggerFunc
{
    public class AcrImageTrigger
    {
        private readonly ILogger _logger;

        public AcrImageTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AcrImageTrigger>();
        }

        [Function("AcrImageTrigger")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation($"Event received: {eventGridEvent.EventType}");

            
            if (eventGridEvent.EventType == "Microsoft.ContainerRegistry.ImagePushed")
            {
                _logger.LogInformation($"Image pushed event detected: {eventGridEvent.Subject}");

               
                var eventData = JsonSerializer.Deserialize<JsonElement>(eventGridEvent.Data.ToString());
                string repository = eventData.GetProperty("target").GetProperty("repository").GetString();
                string tag = eventData.GetProperty("target").GetProperty("tag").GetString();

                _logger.LogInformation($"Repository: {repository}, Tag: {tag}");

                
                 if (repository == "frontend" || repository == "backend")
                 {

                    await TriggerAzureDevOpsPipeline();
                 }
                
                
            }
        }

        private async Task TriggerAzureDevOpsPipeline()
        {
            // Get PAT from environment variable
            string pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            if (string.IsNullOrEmpty(pat))
            {
                _logger.LogError("Azure DevOps PAT not found in environment variables");
                return;
            }

            // Azure DevOps organization and project details
            string organization = "DersimDevOpsPG";
            string project = "CloudNinja-Infra-recon";
            string pipelineId = "16"; // Your deployment pipeline ID

            using (var client = new HttpClient())
            {
                // Set up authentication
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));

                // API URL for triggering a pipeline run
                string url = $"https://dev.azure.com/{organization}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=6.0";

                // Create request body (can be customized with parameters if needed) 
                var requestBody = new
                {
                    resources = new { },
                    templateParameters = new { }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                // Send request to trigger pipeline
                _logger.LogInformation($"Triggering pipeline: {url}");
                var response = await client.PostAsync(url, content);

                // Log result
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Pipeline trigger response: {response.StatusCode}");
                _logger.LogInformation($"Response content: {responseContent}");
            }
        }
    }
}

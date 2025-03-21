using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
namespace ACR_TriggerFunc
{
    public class AcrImageTrigger
    {
        private readonly ILogger<AcrImageTrigger> _logger;

        public AcrImageTrigger(ILogger<AcrImageTrigger> logger)
        {
            _logger = logger;
        }

        [FunctionName("AcrImageTrigger")]
        public async Task Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
                ILogger log)
        {
            log.LogInformation($"event Recieved: {eventGridEvent.EventType}");

            if(eventGridEvent.EventType == "Microsoft.ContainerRegistry.ImagePushed")
            {
                log.LogInformation($"Image Pushed Event Recieved");

                dynamic eventData = JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                string repository = eventData.target.repository;
                string tag = eventData.target.tag;

                log.LogInformation($"Repository: {repository}, Tag: {tag}");

                if (repository == "frontend" || repository == "backend")
                {
                    log.LogInformation($"Repository: {repository} is valid");
                    await TriggerAzureDevOpsPipeline(log);
                }
            }
        }
        
        private static async Task TriggerAzureDevOpsPipeline(ILogger log)
        {
            string pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            if (string.IsNullOrEmpty(pat))
            {
                log.LogError("Azure DevOps PAT is not set");
                return;
            }

            string organization = "DersimDevOpsPG";
            string project = "CloudNinja-Infra-Recon";
            string pipelineId = "16";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));

                string url = $"https://dev.azure.com/{organization}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=6.0";

                var requestBody = new
                {
                    resources = new { },
                    templateParameters = new { }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");

                log.LogInformation($"Triggering Azure DevOps Pipeline: {pipelineId}");
                var response = await client.PostAsync(url, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                log.LogInformation($"Response: {responseContent}");
                log.LogInformation($"Pipeline Triggered");
            }
        }

    }
}

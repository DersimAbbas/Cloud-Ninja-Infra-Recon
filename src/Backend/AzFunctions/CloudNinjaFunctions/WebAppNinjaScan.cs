using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.AppService;
using Newtonsoft.Json;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Net;
using System.Linq;

namespace CloudNinjaInfraRecon
{
    public class WebAppNinjaScan
    {
        private readonly ILogger _logger;

        // Ninja discovery phrases for web apps
        private static readonly string[] ninjaWebAppDiscoveryPhrases = new string[]
        {
            "Ninja discovered a vulnerable web gate at",
            "Shadow warrior found security gaps in web fortress",
            "Silent infiltration successful through web portal",
            "Stealth breach detected in web defenses",
            "Ninja slipped through web security at"
        };

        // Ninja severity phrases for web apps
        private static readonly string[] ninjaWebAppSeverityPhrases = new string[]
        {
            "This web fortress has cracks in its walls!",
            "The web castle's defenses are weaker than rice paper!",
            "Even a novice ninja could breach this web portal!",
            "This web security is as thin as a ninja's blade!",
            "The web gate is unguarded against enemy clans!"
        };

        // Ninja all clear phrases for web apps
        private static readonly string[] ninjaWebAppAllClearPhrases = new string[]
        {
            "Ninja web reconnaissance complete! Your web fortress stands strong.",
            "Shadow warrior found no gaps in your web defenses. Your portal is secure!",
            "Silent web inspection complete. Your digital domain is well-protected.",
            "Ninja clan approves of your web security measures. No enemy can breach!",
            "The way of web security is strong with this one. No vulnerabilities detected!"
        };

        public WebAppNinjaScan(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebAppNinjaScan>();
        }

        [Function("WebAppNinjaScan")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Web App Ninja Scan mission started.");

            // Parse query parameters
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string resourceGroupName = queryDictionary["resourceGroupName"];
            var accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            var containerName = Environment.GetEnvironmentVariable("STORAGE_CONTAINER_NAME");

            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new DefaultAzureCredential());

            var container = blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();

            if (string.IsNullOrEmpty(containerName))
            {
                containerName = "ninja-recon-logs";
            }

            var response = req.CreateResponse();

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Please provide a resource group name");
                return response;
            }

            try
            {

                var credential = new DefaultAzureCredential();
                var armClient = new ArmClient(credential);
              
                var subscription = await armClient.GetDefaultSubscriptionAsync();

                var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroupName);
                var resourceGroup = resourceGroupResource.Value;

                // Scan for web app vulnerabilities
                var webAppVulnerabilities = new List<WebAppVulnerability>();
                int webAppCount = 0;

                // Get all web apps in the resource group using the AppService client
                _logger.LogInformation($"Ninja scanning web apps in resource group: {resourceGroup.Data.Name}");

               
                var websiteCollection = resourceGroup.GetWebSites();

                await foreach (var webSite in websiteCollection.GetAllAsync())
                {
                    webAppCount++;
                    _logger.LogInformation($"Ninja inspecting web app: {webSite.Data.Name}");

                    // Check if HTTPS Only is enabled
                    if (webSite.Data.IsHttpsOnly != true)
                    {
                        webAppVulnerabilities.Add(new WebAppVulnerability
                        {
                            ResourceId = webSite.Id.ToString(),
                            ResourceName = webSite.Data.Name,
                            VulnerabilityType = "HTTPS Not Enforced",
                            Description = "Web app does not enforce HTTPS-only access",
                            Severity = "Medium",
                            Recommendation = "Enable HTTPS Only in the web app configuration"
                        });
                    }

                    // Get the site configuration
                    var siteConfig = webSite.Data.SiteConfig;

                    // Check if TLS version is at least 1.2
                    var minTlsVersion = siteConfig?.MinTlsVersion?.ToString();
                    if (string.IsNullOrEmpty(minTlsVersion) ||
                        string.Compare(minTlsVersion, "1.2") < 0)
                    {
                        webAppVulnerabilities.Add(new WebAppVulnerability
                        {
                            ResourceId = webSite.Id.ToString(),
                            ResourceName = webSite.Data.Name,
                            VulnerabilityType = "Outdated TLS Version",
                            Description = "Web app allows outdated TLS versions (less than 1.2)",
                            Severity = "High",
                            Recommendation = "Set minimum TLS version to 1.2 or higher"
                        });
                    }

                    // Check if FTP deployment is enabled (less secure)
                   
                    var ftpsState = siteConfig?.FtpsState?.ToString();
                    if (ftpsState == "AllAllowed" || ftpsState == "FtpsOnly")
                    {
                        webAppVulnerabilities.Add(new WebAppVulnerability
                        {
                            ResourceId = webSite.Id.ToString(),
                            ResourceName = webSite.Data.Name,
                            VulnerabilityType = "FTP Deployment Enabled",
                            Description = "Web app allows FTP/FTPS deployments which can be less secure",
                            Severity = "Low",
                            Recommendation = "Disable FTP/FTPS deployment and use only Git or other secure deployment methods"
                        });
                    }

                    // Check if remote debugging is enabled
                    if (siteConfig?.IsRemoteDebuggingEnabled == true)
                    {
                        webAppVulnerabilities.Add(new WebAppVulnerability
                        {
                            ResourceId = webSite.Id.ToString(),
                            ResourceName = webSite.Data.Name,
                            VulnerabilityType = "Remote Debugging Enabled",
                            Description = "Web app has remote debugging enabled which should not be used in production",
                            Severity = "Medium",
                            Recommendation = "Disable remote debugging in production environments"
                        });
                    }

                 
                }

                // Save results to storage account based on security status
                string logFileName = $"webapp-ninja-scan-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json";
                string ninjaMessage;
                string securityStatus;

               
                switch (webAppVulnerabilities.Count)
                {
                    case 0:
                       
                        var random = new Random();
                        ninjaMessage = ninjaWebAppAllClearPhrases[random.Next(ninjaWebAppAllClearPhrases.Length)];
                        securityStatus = "Secure";

                       
                        await SaveSecureStatusToStorage(ninjaMessage, webAppCount, resourceGroup.Data.Name,
                            accountName, containerName, logFileName);
                        break;

                    default:
                       
                        var rand = new Random();
                        var severityPhrase = ninjaWebAppSeverityPhrases[rand.Next(ninjaWebAppSeverityPhrases.Length)];
                        ninjaMessage = $"{ninjaWebAppDiscoveryPhrases[rand.Next(ninjaWebAppDiscoveryPhrases.Length)]}. {severityPhrase}";
                        securityStatus = "Vulnerable";

                       
                        await SaveVulnerabilityReportToStorage(webAppVulnerabilities, accountName, containerName, logFileName);
                        break;
                }

                
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new
                {
                    Message = "Web App Ninja Scan mission completed successfully",
                    NinjaReport = ninjaMessage,
                    SecurityStatus = securityStatus,
                    WebAppVulnerabilitiesCount = webAppVulnerabilities.Count,
                    WebAppVulnerabilities = webAppVulnerabilities,
                    ResourcesScanned = new
                    {
                        ResourceGroup = resourceGroup.Data.Name,
                        WebAppCount = webAppCount
                    },
                    LogFileName = logFileName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Web App Ninja Scan: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error in Web App Ninja Scan: {ex.Message}");
                return response;
            }
        }
        private BlobContainerClient GetContainerClient(string accountName, string containerName)
        {
            var client = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new DefaultAzureCredential());
            return client.GetBlobContainerClient(containerName);
        }
        private async Task SaveVulnerabilityReportToStorage(List<WebAppVulnerability> webAppVulnerabilities,
            string accountName, string containerName, string blobName)
        {
            try
            {

                var containerClient = GetContainerClient(accountName, containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(blobName);
                var report = new
                {
                    ScanTime = DateTime.UtcNow,
                    SecurityStatus = "Vulnerable",
                    WebAppVulnerabilities = webAppVulnerabilities
                };
                var json = JsonConvert.SerializeObject(report, Formatting.Indented);
              
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    Metadata = new Dictionary<string, string> {
                    { "ScanType", "WebAppNinjaScan" },
                    { "SecurityStatus", "Vulnerable" }
                }
                });

                _logger.LogInformation($"Successfully saved web app vulnerability report to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving web app vulnerability report to storage: {ex.Message}");
                throw;
            }
        }

        private async Task SaveSecureStatusToStorage(string ninjaMessage, int webAppCount,
            string resourceGroupName, string accountName, string containerName, string blobName)
        {
            try
            {
                var containerClient = GetContainerClient(accountName, containerName);        
                await containerClient.CreateIfNotExistsAsync();
               
                var blobClient = containerClient.GetBlobClient(blobName);

                // Create a secure status report
                var secureReport = new
                {
                    ScanTime = DateTime.UtcNow,
                    SecurityStatus = "Secure",
                    NinjaReport = ninjaMessage,
                    ScannedResources = new
                    {
                        ResourceGroup = resourceGroupName,
                        WebAppCount = webAppCount,
                        Message = "All web apps properly secured"
                    }
                };
                var json = JsonConvert.SerializeObject(secureReport, Formatting.Indented);

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    Metadata = new Dictionary<string, string> {
                    { "ScanType", "WebAppNinjaScan" },
                    { "SecurityStatus", "Secure" }
                }
                });

                _logger.LogInformation($"Successfully saved secure web app status report to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving secure web app status to storage: {ex.Message}");
                throw;
            }
        }
    }


    public class WebAppVulnerability
    {
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public string VulnerabilityType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string Recommendation { get; set; }
    }
}

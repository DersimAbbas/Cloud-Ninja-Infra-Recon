using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Newtonsoft.Json;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Linq;
using System.IO;
using System.Net;
using CloudNinjaFunctions;

namespace CloudNinjaInfraRecon
{
    public class FortressBreach
    {
        private readonly ILogger _logger;

        // Ninja discovery phrases
        private static readonly string[] ninjaDiscoveryPhrases = new string[]
        {
            "Ninja discovered an unguarded entrance at",
            "Shadow warrior found exposed gateway at",
            "Silent infiltration successful through",
            "Stealth breach detected at",
            "Ninja slipped through security gap at"
        };

        // Ninja severity phrases
        private static readonly string[] ninjaSeverityPhrases = new string[]
        {
            "This breach is as wide as a samurai's stance!",
            "The fortress walls might as well be made of paper!",
            "Even a novice ninja could infiltrate this!",
            "This security hole is bigger than a sumo wrestler!",
            "The castle gate is wide open to enemy clans!"
        };
        private static readonly string[] ninjaAllClearPhrases = new string[]
        {
            "Ninja reconnaissance complete! Your fortress walls stand strong against intruders.",
            "Shadow warrior found no gaps in your defenses. Your castle is secure!",
            "Silent inspection complete. Your digital domain is well-protected by invisible guardians.",
            "Ninja clan approves of your security measures. No enemy can breach these walls!",
            "The way of security is strong with this one. No vulnerabilities detected by the ninja scouts."
        };



        public FortressBreach(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FortressBreach>();
        }


        [Function("FortressBreach")]
        public async Task<HttpResponseData> Run(
     [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Fortress Breach ninja mission started.");

            // Parse query parameters
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string resourceGroupName = queryDictionary["resourceGroupName"];
            var accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            var containerName = Environment.GetEnvironmentVariable("STORAGE_CONTAINER_NAME");



            // Handle null containerName
            if (string.IsNullOrEmpty(containerName))
            {
                containerName = "ninja-recon-logs";
            }

            // Create response
            var response = req.CreateResponse();

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Please provide a resource group name");
                return response;
            }

            try
            {
                // Authenticate with Azure using DefaultAzureCredential (Managed Identity)

                var credential = new DefaultAzureCredential();

                var blobserviceClient = new BlobServiceClient(
                  new Uri($"https://{accountName}.blob.core.windows.net"),
                  new DefaultAzureCredential());



                var armClient = new ArmClient(credential);

                var container = blobserviceClient.GetBlobContainerClient(containerName);
                await container.CreateIfNotExistsAsync();

                // Get the subscription and resource group
                var subscription = await armClient.GetDefaultSubscriptionAsync();
                var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroupName);
                var resourceGroup = resourceGroupResource.Value;

                var exposedEndpoints = new List<ExposedEndpoint>();
                int publicIpCount = 0;
                int nsgCount = 0;

                // Scan resource group for public IP addresses and network security groups
                _logger.LogInformation($"Scanning resource group: {resourceGroup.Id.Name}");

                // Count NSGs separately
                var nsgCollection = resourceGroup.GetNetworkSecurityGroups();
                await foreach (var nsg in nsgCollection.GetAllAsync())
                {
                    nsgCount++;
                    _logger.LogInformation($"Ninja examining NSG: {nsg.Data.Name}");
                }

                // Get all public IP addresses in the resource group
                var publicIpCollection = resourceGroup.GetPublicIPAddresses();

                await foreach (var publicIp in publicIpCollection.GetAllAsync())
                {
                    publicIpCount++;
                    _logger.LogInformation($"Ninja inspecting public IP: {publicIp.Data.Name} with address {publicIp.Data.IPAddress ?? "unassigned"}");

                    // Check if the public IP is associated with a resource and has an IP address
                    if (publicIp.Data.IPAddress != null)
                    {
                        // Get network interfaces associated with this public IP
                        var associatedNics = new List<string>();
                        if (publicIp.Data.IPConfiguration != null)
                        {
                            var nicId = publicIp.Data.IPConfiguration.Id;
                            if (!string.IsNullOrEmpty(nicId))
                            {
                                associatedNics.Add(nicId);
                            }
                        }

                        // Check network security groups for open ports
                        var openPorts = await ScanForOpenPorts(resourceGroup, associatedNics);

                        // Create a report with ninja humor
                        var random = new Random();
                        var discoveryPhrase = ninjaDiscoveryPhrases[random.Next(ninjaDiscoveryPhrases.Length)];
                        var severityPhrase = ninjaSeverityPhrases[random.Next(ninjaSeverityPhrases.Length)];

                        var exposedEndpoint = new ExposedEndpoint
                        {
                            ResourceId = publicIp.Id.ToString(),
                            ResourceName = publicIp.Data.Name,
                            ResourceGroup = resourceGroup.Id.Name,
                            IpAddress = publicIp.Data.IPAddress,
                            OpenPorts = openPorts,
                            ScanTime = DateTime.UtcNow,
                            NinjaReport = $"{discoveryPhrase} {publicIp.Data.IPAddress}. {severityPhrase}",
                            Severity = openPorts.Count > 5 ? "High" : (openPorts.Count > 2 ? "Medium" : "Low")
                        };

                        if (openPorts.Count > 0)
                        {
                            exposedEndpoints.Add(exposedEndpoint);
                        }
                    }
                }

                // Save results to storage account based on security status
                string logFileName = $"fortress-breach-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json";
                string ninjaMessage;
                string securityStatus;

                // Use switch/case approach for different security statuses
                switch (exposedEndpoints.Count)
                {
                    case 0:
                        // Secure case - no exposed endpoints
                        var random = new Random();
                        ninjaMessage = ninjaAllClearPhrases[random.Next(ninjaAllClearPhrases.Length)];
                        securityStatus = "Secure";

                        // Save secure status report
                        await SaveSecureStatusToStorage(ninjaMessage, publicIpCount, nsgCount, resourceGroup.Id.Name,
                            accountName, containerName, logFileName);
                        break;

                    default:
                        // Insecure case - exposed endpoints found
                        ninjaMessage = "Security vulnerabilities detected! The ninja has found ways to infiltrate your fortress.";
                        securityStatus = "Vulnerable";

                        // Save vulnerability report
                        await SaveResultsToStorage(exposedEndpoints, accountName, containerName, logFileName);
                        break;
                }

                // Create success response
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new
                {
                    Message = "Fortress Breach mission completed successfully",
                    NinjaReport = ninjaMessage,
                    SecurityStatus = securityStatus,
                    ExposedEndpointsCount = exposedEndpoints.Count,
                    ExposedEndpoints = exposedEndpoints,
                    ResourcesScanned = new
                    {
                        ResourceGroup = resourceGroup.Id.Name,
                        PublicIpCount = publicIpCount,
                        NetworkSecurityGroupCount = nsgCount
                    },
                    LogFileName = logFileName
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Fortress Breach: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Error in Fortress Breach: {ex.Message}");
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

        private async Task<List<OpenPort>> ScanForOpenPorts(ResourceGroupResource resourceGroup, List<string> nicIds)
        {
            var openPorts = new List<OpenPort>();

            // Get all NSGs in the resource group
            var nsgCollection = resourceGroup.GetNetworkSecurityGroups();

            await foreach (var nsg in nsgCollection.GetAllAsync())
            {
                // Check if this NSG is associated with any of our NICs
                bool isAssociated = false;
                foreach (var nicId in nicIds)
                {
                    if (nsg.Data.NetworkInterfaces != null)
                    {
                        foreach (var nic in nsg.Data.NetworkInterfaces)
                        {
                            if (nic.Id.ToString().Equals(nicId, StringComparison.OrdinalIgnoreCase))
                            {
                                isAssociated = true;
                                break;
                            }
                        }
                    }

                    if (isAssociated) break;
                }

                if (!isAssociated && nsg.Data.Subnets != null)
                {
                    isAssociated = nsg.Data.Subnets.Count > 0;
                }

                if (isAssociated || nicIds.Count == 0)
                {
                    foreach (var rule in nsg.Data.SecurityRules)
                    {
                        if (rule.Direction == SecurityRuleDirection.Inbound &&
                            (rule.Access == SecurityRuleAccess.Allow) &&
                            (rule.SourceAddressPrefix == "*" || rule.SourceAddressPrefix == "Internet" ||
                             (rule.SourceAddressPrefixes != null && (rule.SourceAddressPrefixes.Contains("*") || rule.SourceAddressPrefixes.Contains("Internet")))))
                        {
                            // This rule allows inbound traffic from the internet
                            string portRange = rule.DestinationPortRange;

                            if (!string.IsNullOrEmpty(portRange))
                            {
                                // Handle single port or port range
                                if (portRange.Contains("-"))
                                {
                                    var parts = portRange.Split('-');
                                    if (parts.Length == 2 && int.TryParse(parts[0], out int startPort) && int.TryParse(parts[1], out int endPort))
                                    {
                                        for (int port = startPort; port <= endPort; port++)
                                        {
                                            openPorts.Add(new OpenPort
                                            {
                                                Port = port,
                                                Protocol = rule.Protocol.ToString(),
                                                RuleName = rule.Name,
                                                NsgName = nsg.Data.Name
                                            });
                                        }
                                    }
                                }
                                else if (int.TryParse(portRange, out int port))
                                {
                                    openPorts.Add(new OpenPort
                                    {
                                        Port = port,
                                        Protocol = rule.Protocol.ToString(),
                                        RuleName = rule.Name,
                                        NsgName = nsg.Data.Name
                                    });
                                }
                            }

                            // Handle multiple port ranges
                            if (rule.DestinationPortRanges != null)
                            {
                                foreach (var multiPortRange in rule.DestinationPortRanges)
                                {
                                    if (multiPortRange.Contains("-"))
                                    {
                                        var parts = multiPortRange.Split('-');
                                        if (parts.Length == 2 && int.TryParse(parts[0], out int startPort) && int.TryParse(parts[1], out int endPort))
                                        {
                                            for (int port = startPort; port <= endPort; port++)
                                            {
                                                openPorts.Add(new OpenPort
                                                {
                                                    Port = port,
                                                    Protocol = rule.Protocol.ToString(),
                                                    RuleName = rule.Name,
                                                    NsgName = nsg.Data.Name
                                                });
                                            }
                                        }
                                    }
                                    else if (int.TryParse(multiPortRange, out int port))
                                    {
                                        openPorts.Add(new OpenPort
                                        {
                                            Port = port,
                                            Protocol = rule.Protocol.ToString(),
                                            RuleName = rule.Name,
                                            NsgName = nsg.Data.Name
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return openPorts;
        }


        private async Task SaveResultsToStorage(List<ExposedEndpoint> exposedEndpoints, string accountName, string containerName, string blobName)
        {
            var containerClient = GetContainerClient(accountName, containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            var json = JsonConvert.SerializeObject(exposedEndpoints, Formatting.Indented);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, new BlobUploadOptions { Metadata = new Dictionary<string, string> { { "ScanType", "FortressBreach" } } });
            _logger.LogInformation($"Saved scan results to {blobName}");
        }
        private async Task SaveSecureStatusToStorage(string ninjaMessage, int publicIpCount, int nsgCount,
        string resourceGroupName, string accountName, string containerName, string blobName)
        {
            try
            {
                // Create a BlobServiceClient
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
                        PublicIpCount = publicIpCount,
                        NetworkSecurityGroupCount = nsgCount,
                        Message = "All resources properly secured"
                    }
                };

                // Convert the results to JSON
                var json = JsonConvert.SerializeObject(secureReport, Formatting.Indented);

                // Upload the JSON to the blob
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    Metadata = new Dictionary<string, string> {
                    { "ScanType", "FortressBreach" },
                    { "SecurityStatus", "Secure" }
                }
                });

                _logger.LogInformation($"Successfully saved secure status report to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving secure status to storage: {ex.Message}");
                throw;
            }
        }
    }

   
  
}

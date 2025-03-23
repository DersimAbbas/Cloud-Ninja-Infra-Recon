using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Identity;
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
using CloudNinjaFunctions;

namespace CloudNinjaInfraRecon
{
    public static class FortressBreach
    {
        private static readonly string[] ninjaDiscoveryPhrases = new string[]
        {
            "Ninja discovered an unguarded entrance at",
            "Shadow warrior found exposed gateway at",
            "Silent infiltration successful through",
            "Stealth breach detected at",
            "Ninja slipped through security gap at"
        };

        private static readonly string[] ninjaSeverityPhrases = new string[]
        {
            "This breach is as wide as a samurai's stance!",
            "The fortress walls might as well be made of paper!",
            "Even a novice ninja could infiltrate this!",
            "This security hole is bigger than a sumo wrestler!",
            "The castle gate is wide open to enemy clans!"
        };

        [FunctionName("FortressBreach")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Fortress Breach ninja mission started.");

            string resourceGroupName = req.Query["resourceGroupName"];
            string storageConnectionString = req.Query["storageConnectionString"];
            string containerName = req.Query["containerName"];

            // Handle null containerName
            if (string.IsNullOrEmpty(containerName))
            {
                containerName = "ninja-recon-logs";
            }

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                return new BadRequestObjectResult("Please provide a resource group name");
            }

            if (string.IsNullOrEmpty(storageConnectionString))
            {
                return new BadRequestObjectResult("Please provide a storage connection string");
            }

            try
            {
                // Authenticate with Azure using DefaultAzureCredential (Managed Identity)
                var credential = new DefaultAzureCredential();
                var armClient = new ArmClient(credential);

                // Get the subscription and resource group
                var subscription = await armClient.GetDefaultSubscriptionAsync();
                var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroupName);
                var resourceGroup = resourceGroupResource.Value;

                var exposedEndpoints = new List<ExposedEndpoint>();

                // Scan resource group for public IP addresses and network security groups
                log.LogInformation($"Scanning resource group: {resourceGroup.Id.Name}");

                // Get all public IP addresses in the resource group
                var publicIpCollection = resourceGroup.GetPublicIPAddresses();

                await foreach (var publicIp in publicIpCollection.GetAllAsync())
                {
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
                        var openPorts = await ScanForOpenPorts(resourceGroup, associatedNics, log);

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

                        exposedEndpoints.Add(exposedEndpoint);
                    }
                }

                // Save results to storage account
                string logFileName = $"fortress-breach-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json";
                await SaveResultsToStorage(exposedEndpoints, storageConnectionString, containerName, logFileName, log);

                return new OkObjectResult(new
                {
                    Message = "Fortress Breach mission completed successfully",
                    ExposedEndpointsCount = exposedEndpoints.Count,
                    ExposedEndpoints = exposedEndpoints,
                    LogFileName = logFileName
                });
            }
            catch (Exception ex)
            {
                log.LogError($"Error in Fortress Breach: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task<List<OpenPort>> ScanForOpenPorts(ResourceGroupResource resourceGroup, List<string> nicIds, ILogger log)
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

        private static async Task SaveResultsToStorage(List<ExposedEndpoint> exposedEndpoints, string connectionString, string containerName, string blobName, ILogger log)
        {
            try
            {
                // Create a BlobServiceClient
                var blobServiceClient = new BlobServiceClient(connectionString);

                // Get a reference to the container
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Create the container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync();

                // Get a reference to the blob
                var blobClient = containerClient.GetBlobClient(blobName);

                // Convert the results to JSON
                var json = JsonConvert.SerializeObject(exposedEndpoints, Formatting.Indented);

                // Upload the JSON to the blob
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, new BlobUploadOptions { Metadata = new Dictionary<string, string> { { "ScanType", "FortressBreach" } } });

                log.LogInformation($"Successfully saved scan results to blob: {blobName}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error saving results to storage: {ex.Message}");
                throw;
            }
        }
    }

   
}

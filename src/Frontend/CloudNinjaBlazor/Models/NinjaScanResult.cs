using System.Text.Json.Serialization;

namespace CloudNinjaBlazor.Models
{
    public class NinjaScanResult
    {
        [JsonPropertyName("Message")]
        public string? Message { get; set; }

        [JsonPropertyName("NinjaReport")]
        public string? NinjaReport { get; set; }

        [JsonPropertyName("SecurityStatus")]
        public string? SecurityStatus { get; set; }

        [JsonPropertyName("WebAppVulnerabilitiesCount")]
        public int webAppVurnCount { get; set; }

        [JsonPropertyName("WebAppVulnerabilities")]
        public List<WebAppVurnability>? WebAppVulnarbilities { get; set; }

        [JsonPropertyName("ResourcesScanned")]
        public ResourceScanned? resourcesScanned { get; set; }

        [JsonPropertyName("ExposedEndPointsCount")]
        public int ExposedEndPointsCount { get; set; }

        public List<ExposedEndpoint>? ExposedEndPoints { get; set; }

        public string LogFileName { get; set; }
    }

    public class WebAppVurnability
    {
        [JsonPropertyName("ResourceName")]
        public string? ResourceName { get; set; }

        [JsonPropertyName("VulnerabilityType")]
        public string? VurnabilityType { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("Severity")]
        public string? Severity { get; set; }

        [JsonPropertyName("Recommendation")]
        public string? Recommendation { get; set; }
    }

    public class ExposedEndpoint
    {
        [JsonPropertyName("ResourceId")]
        public string? ResourceId { get; set; }
        [JsonPropertyName("ResourceName")]
        public string? ResourceName { get; set; }
        [JsonPropertyName("ResourceGroup")]
        public string? ResourceGroup { get; set; }
        [JsonPropertyName("IpAddress")]
        public string? IpAddress { get; set; }
        [JsonPropertyName("OpenPorts")]
        public List<int>? OpenPorts { get; set; }
        [JsonPropertyName("ScanTime")]
        public DateTime ScanTime { get; set; }
        [JsonPropertyName("NinjaReport")]
        public string? NinjaReport { get; set; }
        [JsonPropertyName("SecurityStatus")]
        public string? Severity { get; set; }
        [JsonPropertyName("Description")]
        public string? Description { get; set; }
    }

    public class ResourceScanned
    {
        [JsonPropertyName("ResourceGroup")]
        public string? ResourceGroup { get; set; }

        [JsonPropertyName("WebAppCount")]
        public int webAppCount { get; set; }
    }
}

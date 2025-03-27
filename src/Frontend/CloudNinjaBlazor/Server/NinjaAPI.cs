using CloudNinjaBlazor.Models;
using System.Text.Json;

namespace CloudNinjaBlazor.Server
{
    public class NinjaAPI
    {
        private readonly HttpClient _httpClient;

        public NinjaAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<NinjaScanResult> ScanWebAppAsync()
        {
            try
            {
                // Adjust the URL if needed.
                var response = await _httpClient.GetAsync($"/api/ninjawebScan");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"JSON response: {json}");
                Console.ResetColor();

                var scan = JsonSerializer.Deserialize<NinjaScanResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return scan;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();

                return null;
            }
        }
    }
}

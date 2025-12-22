using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Connector for the EC3 (Embodied Carbon in Construction Calculator) API.
    /// This is a template implementation - update the API endpoints and models
    /// based on EC3's actual API documentation.
    /// </summary>
    public class Ec3ToolConnector : IAutodeskToolConnector
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string EC3_BASE_URL = "https://buildingtransparency.org/api";

        public string ToolName => "EC3 Database";
        public string ToolId => "ec3";

        public Ec3ToolConnector(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return false;

            try
            {
                // Test connection by making a simple API call
                var response = await _httpClient.GetAsync($"{EC3_BASE_URL}/materials?limit=1");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> FetchDataAsync(Dictionary<string, string> parameters)
        {
            var result = new Dictionary<string, object>();

            if (parameters == null || !parameters.ContainsKey("action"))
            {
                throw new ArgumentException("Missing required 'action' parameter");
            }

            string action = parameters["action"];

            switch (action.ToLowerInvariant())
            {
                case "search_materials":
                    string query = parameters.ContainsKey("query") ? parameters["query"] : "";
                    var materials = await SearchMaterialsAsync(query);
                    result["materials"] = materials;
                    break;

                case "get_epd":
                    if (!parameters.ContainsKey("epd_id"))
                    {
                        throw new ArgumentException("Missing required 'epd_id' parameter");
                    }
                    string epdId = parameters["epd_id"];
                    var epd = await GetEpdAsync(epdId);
                    result["epd"] = epd;
                    break;

                default:
                    throw new ArgumentException($"Unknown action: {action}");
            }

            return result;
        }

        private async Task<List<Ec3Material>> SearchMaterialsAsync(string query)
        {
            string endpoint = $"{EC3_BASE_URL}/materials?q={Uri.EscapeDataString(query)}&limit=50";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            // Parse response based on EC3's actual API schema
            // This is a placeholder implementation
            return new List<Ec3Material>();
        }

        private async Task<Ec3Epd> GetEpdAsync(string epdId)
        {
            string endpoint = $"{EC3_BASE_URL}/epds/{epdId}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Ec3Epd>(jsonResponse);
        }
    }

    // EC3 Data Models - Update based on actual EC3 API schema
    public class Ec3Material
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Gwp { get; set; } // Global Warming Potential
        public string Manufacturer { get; set; }
        public string EpdId { get; set; }
    }

    public class Ec3Epd
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public double GwpA1A3 { get; set; } // kgCO2e
        public string DeclaredUnit { get; set; }
        public DateTime ValidUntil { get; set; }
    }
}

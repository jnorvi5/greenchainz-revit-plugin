using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// EC3 (Embodied Carbon in Construction Calculator) API Service
    /// Building Transparency - Real EPD and carbon factor data
    /// </summary>
    public class Ec3ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BASE_URL = "https://buildingtransparency.org/api";

        public Ec3ApiService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public bool HasValidApiKey => !string.IsNullOrEmpty(_apiKey) && _apiKey.Length > 10;

        /// <summary>
        /// Search for materials in EC3 database
        /// </summary>
        public async Task<List<Ec3Material>> SearchMaterialsAsync(string query, string category = null)
        {
            try
            {
                string url = $"{BASE_URL}/materials?search={Uri.EscapeDataString(query)}";
                if (!string.IsNullOrEmpty(category))
                    url += $"&category={Uri.EscapeDataString(category)}";
                url += "&page_size=20";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Ec3SearchResult>(json);
                    return result?.Results ?? new List<Ec3Material>();
                }
                
                return new List<Ec3Material>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EC3 Search Error: {ex.Message}");
                return new List<Ec3Material>();
            }
        }

        /// <summary>
        /// Get carbon factor (GWP) for a specific material category
        /// </summary>
        public async Task<Ec3CarbonFactor> GetCarbonFactorAsync(string materialCategory)
        {
            try
            {
                // Map common material names to EC3 categories
                string ec3Category = MapToEc3Category(materialCategory);
                
                string url = $"{BASE_URL}/materials/statistics?category={Uri.EscapeDataString(ec3Category)}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var stats = JsonConvert.DeserializeObject<Ec3Statistics>(json);
                    
                    return new Ec3CarbonFactor
                    {
                        Category = materialCategory,
                        Ec3Category = ec3Category,
                        AverageGwp = stats?.GwpAverage ?? 0,
                        MinGwp = stats?.GwpMin ?? 0,
                        MaxGwp = stats?.GwpMax ?? 0,
                        Unit = stats?.Unit ?? "kgCO2e/unit",
                        SampleSize = stats?.Count ?? 0,
                        Source = "EC3 Building Transparency"
                    };
                }
                
                return GetFallbackCarbonFactor(materialCategory);
            }
            catch
            {
                return GetFallbackCarbonFactor(materialCategory);
            }
        }

        /// <summary>
        /// Get EPD (Environmental Product Declaration) data for a material
        /// </summary>
        public async Task<List<Ec3Epd>> GetEpdsAsync(string materialName)
        {
            try
            {
                string url = $"{BASE_URL}/epds?search={Uri.EscapeDataString(materialName)}&page_size=10";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Ec3EpdResult>(json);
                    return result?.Results ?? new List<Ec3Epd>();
                }
                
                return new List<Ec3Epd>();
            }
            catch
            {
                return new List<Ec3Epd>();
            }
        }

        /// <summary>
        /// Get material categories from EC3
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                string url = $"{BASE_URL}/categories";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
                
                return GetDefaultCategories();
            }
            catch
            {
                return GetDefaultCategories();
            }
        }

        private string MapToEc3Category(string materialName)
        {
            string name = materialName.ToLower();
            
            if (name.Contains("concrete") || name.Contains("cement")) return "Concrete";
            if (name.Contains("steel") || name.Contains("rebar")) return "Steel";
            if (name.Contains("aluminum") || name.Contains("aluminium")) return "Aluminum";
            if (name.Contains("wood") || name.Contains("timber") || name.Contains("lumber")) return "Wood";
            if (name.Contains("clt") || name.Contains("glulam") || name.Contains("mass timber")) return "Mass Timber";
            if (name.Contains("glass") || name.Contains("glazing")) return "Glass";
            if (name.Contains("gypsum") || name.Contains("drywall")) return "Gypsum Board";
            if (name.Contains("insulation")) return "Insulation";
            if (name.Contains("carpet")) return "Carpet";
            if (name.Contains("cmu") || name.Contains("masonry") || name.Contains("block")) return "CMU";
            if (name.Contains("brick")) return "Brick";
            if (name.Contains("roof") && name.Contains("membrane")) return "Roofing";
            
            return "Other";
        }

        private Ec3CarbonFactor GetFallbackCarbonFactor(string materialCategory)
        {
            // CLF v2021 baseline values as fallback
            var factors = new Dictionary<string, double>
            {
                { "concrete", 340 },
                { "steel", 1370 },
                { "aluminum", 12800 },
                { "wood", 110 },
                { "glass", 1500 },
                { "gypsum", 200 },
                { "insulation", 50 },
                { "carpet", 890 },
                { "cmu", 180 },
                { "brick", 200 }
            };

            string key = materialCategory.ToLower();
            double gwp = 100; // default

            foreach (var kvp in factors)
            {
                if (key.Contains(kvp.Key))
                {
                    gwp = kvp.Value;
                    break;
                }
            }

            return new Ec3CarbonFactor
            {
                Category = materialCategory,
                AverageGwp = gwp,
                MinGwp = gwp * 0.7,
                MaxGwp = gwp * 1.3,
                Unit = "kgCO2e/m³",
                Source = "CLF v2021 Baseline (EC3 unavailable)"
            };
        }

        private List<string> GetDefaultCategories()
        {
            return new List<string>
            {
                "Concrete", "Steel", "Aluminum", "Wood", "Mass Timber",
                "Glass", "Gypsum Board", "Insulation", "Carpet", "CMU",
                "Brick", "Roofing", "Cladding"
            };
        }
    }

    // EC3 API Response Models
    public class Ec3SearchResult
    {
        [JsonProperty("results")]
        public List<Ec3Material> Results { get; set; }
        
        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class Ec3Material
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("gwp")]
        public double Gwp { get; set; }
        
        [JsonProperty("gwp_unit")]
        public string GwpUnit { get; set; }
        
        [JsonProperty("declared_unit")]
        public string DeclaredUnit { get; set; }
        
        [JsonProperty("plant_or_group")]
        public string PlantOrGroup { get; set; }
        
        [JsonProperty("epd_url")]
        public string EpdUrl { get; set; }
    }

    public class Ec3Statistics
    {
        [JsonProperty("gwp_average")]
        public double GwpAverage { get; set; }
        
        [JsonProperty("gwp_min")]
        public double GwpMin { get; set; }
        
        [JsonProperty("gwp_max")]
        public double GwpMax { get; set; }
        
        [JsonProperty("unit")]
        public string Unit { get; set; }
        
        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class Ec3CarbonFactor
    {
        public string Category { get; set; }
        public string Ec3Category { get; set; }
        public double AverageGwp { get; set; }
        public double MinGwp { get; set; }
        public double MaxGwp { get; set; }
        public string Unit { get; set; }
        public int SampleSize { get; set; }
        public string Source { get; set; }
    }

    public class Ec3EpdResult
    {
        [JsonProperty("results")]
        public List<Ec3Epd> Results { get; set; }
    }

    public class Ec3Epd
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
        
        [JsonProperty("program_operator")]
        public string ProgramOperator { get; set; }
        
        [JsonProperty("gwp")]
        public double Gwp { get; set; }
        
        [JsonProperty("valid_until")]
        public string ValidUntil { get; set; }
        
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}

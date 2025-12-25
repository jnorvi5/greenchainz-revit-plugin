using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service for submitting RFQs to the GreenChainz web API and finding suppliers
    /// </summary>
    public class RfqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        // API URLs - tries production first, falls back to local
        private static readonly string[] API_URLS = new[]
        {
            "https://web-cj3efhrgf-greenchainz-vercel.vercel.app/api",  // Production (Vercel)
            "https://greenchainz-revit-plugin.vercel.app/api",  // Alt production
            "http://localhost:3000/api"  // Local development
        };

        public RfqService(string apiUrl = null)
        {
            _baseUrl = apiUrl ?? Environment.GetEnvironmentVariable("GREENCHAINZ_API_URL") ?? API_URLS[0];
            
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Submit RFQ to the GreenChainz API and get supplier matches
        /// </summary>
        public async Task<RfqResponse> SubmitRfqAsync(RFQRequest request)
        {
            // Try each API URL until one works
            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl}/rfq";
                    string json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<RfqResponse>(responseBody);
                        result.ApiUsed = baseUrl;
                        return result;
                    }
                }
                catch (HttpRequestException)
                {
                    // Try next URL
                    continue;
                }
                catch (TaskCanceledException)
                {
                    // Timeout, try next
                    continue;
                }
            }

            // All APIs failed, save locally
            return SaveRfqLocally(request, "All API endpoints unavailable");
        }

        /// <summary>
        /// Get list of sustainable suppliers by material category
        /// </summary>
        public async Task<List<Supplier>> GetSuppliersAsync(string category = null)
        {
            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl}/suppliers";
                    if (!string.IsNullOrEmpty(category))
                    {
                        url += $"?category={Uri.EscapeDataString(category)}";
                    }

                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<SupplierResponse>(json);
                        return result?.Suppliers ?? new List<Supplier>();
                    }
                }
                catch
                {
                    continue;
                }
            }

            return GetFallbackSuppliers(category);
        }

        /// <summary>
        /// Save RFQ locally when API is unavailable
        /// </summary>
        private RfqResponse SaveRfqLocally(RFQRequest request, string reason)
        {
            try
            {
                string rfqFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "GreenChainz", "RFQs"
                );
                System.IO.Directory.CreateDirectory(rfqFolder);

                string rfqId = $"RFQ-{DateTime.Now:yyyyMMdd-HHmmss}";
                string filePath = System.IO.Path.Combine(rfqFolder, $"{rfqId}.json");

                var rfqData = new
                {
                    Id = rfqId,
                    Request = request,
                    CreatedAt = DateTime.Now,
                    Status = "pending_sync",
                    Reason = reason
                };

                System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(rfqData, Formatting.Indented));

                var suppliers = GetFallbackSuppliers(null);

                return new RfqResponse
                {
                    Success = true,
                    RfqId = rfqId,
                    Message = $"RFQ saved locally. Found {suppliers.Count} potential suppliers.\n\nFile: {filePath}\n\nReason: {reason}",
                    Suppliers = suppliers,
                    ApiUsed = "local"
                };
            }
            catch (Exception ex)
            {
                return new RfqResponse
                {
                    Success = false,
                    Message = $"Failed to save RFQ: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Fallback supplier list when API is unavailable
        /// </summary>
        private List<Supplier> GetFallbackSuppliers(string category)
        {
            var suppliers = new List<Supplier>
            {
                new Supplier
                {
                    Id = "carboncure",
                    Name = "CarbonCure Technologies",
                    Categories = new List<string> { "concrete", "ready-mix", "low-carbon" },
                    Certifications = new List<string> { "EPD", "Carbon Negative", "LEED Contributing" },
                    Region = "North America",
                    SustainabilityScore = 96,
                    ContactEmail = "info@carboncure.com",
                    Website = "https://www.carboncure.com"
                },
                new Supplier
                {
                    Id = "nucor",
                    Name = "Nucor Corporation",
                    Categories = new List<string> { "steel", "structural steel", "rebar", "metal" },
                    Certifications = new List<string> { "EPD", "ISO 14001", "Responsible Steel" },
                    Region = "North America",
                    SustainabilityScore = 91,
                    ContactEmail = "products@nucor.com",
                    Website = "https://www.nucor.com"
                },
                new Supplier
                {
                    Id = "ssab",
                    Name = "SSAB",
                    Categories = new List<string> { "steel", "fossil-free steel" },
                    Certifications = new List<string> { "EPD", "Science Based Targets" },
                    Region = "Global",
                    SustainabilityScore = 95,
                    ContactEmail = "info@ssab.com",
                    Website = "https://www.ssab.com"
                },
                new Supplier
                {
                    Id = "structurlam",
                    Name = "Structurlam",
                    Categories = new List<string> { "wood", "mass timber", "CLT", "glulam" },
                    Certifications = new List<string> { "FSC", "PEFC", "EPD" },
                    Region = "North America",
                    SustainabilityScore = 96,
                    ContactEmail = "info@structurlam.com",
                    Website = "https://www.structurlam.com"
                },
                new Supplier
                {
                    Id = "nordic",
                    Name = "Nordic Structures",
                    Categories = new List<string> { "wood", "CLT", "mass timber" },
                    Certifications = new List<string> { "FSC", "EPD" },
                    Region = "North America",
                    SustainabilityScore = 94,
                    ContactEmail = "info@nordic.ca",
                    Website = "https://www.nordic.ca"
                },
                new Supplier
                {
                    Id = "guardian",
                    Name = "Guardian Glass",
                    Categories = new List<string> { "glass", "glazing", "architectural glass" },
                    Certifications = new List<string> { "EPD", "Cradle to Cradle", "ISO 14001" },
                    Region = "Global",
                    SustainabilityScore = 88,
                    ContactEmail = "buildingproducts@guardian.com",
                    Website = "https://www.guardianglass.com"
                },
                new Supplier
                {
                    Id = "rockwool",
                    Name = "Rockwool",
                    Categories = new List<string> { "insulation", "mineral wool", "stone wool" },
                    Certifications = new List<string> { "EPD", "Cradle to Cradle", "GREENGUARD" },
                    Region = "Global",
                    SustainabilityScore = 92,
                    ContactEmail = "info@rockwool.com",
                    Website = "https://www.rockwool.com"
                },
                new Supplier
                {
                    Id = "novelis",
                    Name = "Novelis",
                    Categories = new List<string> { "aluminum", "rolled aluminum" },
                    Certifications = new List<string> { "EPD", "ASI Certified", "ISO 14001" },
                    Region = "Global",
                    SustainabilityScore = 93,
                    ContactEmail = "info@novelis.com",
                    Website = "https://www.novelis.com"
                },
                new Supplier
                {
                    Id = "hydro",
                    Name = "Hydro Aluminum",
                    Categories = new List<string> { "aluminum", "extrusions" },
                    Certifications = new List<string> { "EPD", "ASI Certified" },
                    Region = "Global",
                    SustainabilityScore = 94,
                    ContactEmail = "contact@hydro.com",
                    Website = "https://www.hydro.com"
                },
                new Supplier
                {
                    Id = "usg",
                    Name = "USG Corporation",
                    Categories = new List<string> { "gypsum", "drywall", "ceiling" },
                    Certifications = new List<string> { "EPD", "GREENGUARD Gold" },
                    Region = "North America",
                    SustainabilityScore = 85,
                    ContactEmail = "info@usg.com",
                    Website = "https://www.usg.com"
                }
            };

            if (!string.IsNullOrEmpty(category))
            {
                string cat = category.ToLower();
                return suppliers.FindAll(s => 
                    s.Categories.Exists(c => c.ToLower().Contains(cat) || cat.Contains(c.ToLower()))
                );
            }

            return suppliers;
        }
    }

    // Response Models
    public class RfqResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("rfqId")]
        public string RfqId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("suppliers")]
        public List<Supplier> Suppliers { get; set; }

        public string ApiUsed { get; set; }
    }

    public class SupplierResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("suppliers")]
        public List<Supplier> Suppliers { get; set; }
    }

    public class Supplier
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("categories")]
        public List<string> Categories { get; set; }

        [JsonProperty("certifications")]
        public List<string> Certifications { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("sustainabilityScore")]
        public int SustainabilityScore { get; set; }

        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }

        [JsonProperty("carbonReduction")]
        public string CarbonReduction { get; set; }

        public string CategoriesDisplay => Categories != null ? string.Join(", ", Categories) : "";
        public string CertificationsDisplay => Certifications != null ? string.Join(", ", Certifications) : "";
    }
}

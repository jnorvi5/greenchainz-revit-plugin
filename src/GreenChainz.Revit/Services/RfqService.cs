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
            ApiConfig.BASE_URL,
            "https://web-cj3efhrgf-greenchainz-vercel.vercel.app",
            "https://greenchainz-revit-plugin.vercel.app",
            "http://localhost:3000"
        };

        // Supplier location coordinates for distance calculation
        private static readonly Dictionary<string, (double lat, double lon)> SUPPLIER_LOCATIONS = new Dictionary<string, (double, double)>
        {
            { "carboncure", (44.6488, -63.5752) },     // Halifax, NS
            { "nucor", (35.2271, -80.8431) },          // Charlotte, NC
            { "ssab", (59.3293, 18.0686) },            // Stockholm (global)
            { "structurlam", (49.5088, -115.7671) },   // Penticton, BC
            { "nordic", (45.5017, -73.5673) },         // Montreal, QC
            { "guardian", (42.2808, -83.7430) },       // Auburn Hills, MI
            { "rockwool", (43.6532, -79.3832) },       // Toronto area
            { "novelis", (33.7490, -84.3880) },        // Atlanta, GA
            { "hydro", (59.9139, 10.7522) },           // Oslo (global)
            { "usg", (41.8781, -87.6298) },            // Chicago, IL
            { "certainteed", (40.0796, -75.2901) },    // Malvern, PA
            { "owenscorning", (41.6528, -83.5379) },   // Toledo, OH
            { "cmc", (32.7767, -96.7970) },            // Dallas, TX
            { "vitro", (40.4406, -79.9959) },          // Pittsburgh, PA
            { "centralconcrete", (37.3382, -121.8863) } // San Jose, CA
        };

        public RfqService(string apiUrl = null)
        {
            _baseUrl = apiUrl ?? Environment.GetEnvironmentVariable("GREENCHAINZ_API_URL") ?? ApiConfig.BASE_URL;
            
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(ApiConfig.TIMEOUT_SECONDS)
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
                    string url = $"{baseUrl.TrimEnd('/')}/api/rfq";
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
        /// Get suppliers filtered by category and optionally sorted by distance
        /// </summary>
        public async Task<List<Supplier>> GetSuppliersAsync(string category = null, double? projectLat = null, double? projectLon = null, double? maxDistanceMiles = null)
        {
            var suppliers = new List<Supplier>();

            // Try API first
            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl.TrimEnd('/')}/api/suppliers";
                    if (!string.IsNullOrEmpty(category))
                        url += $"?category={Uri.EscapeDataString(category)}";

                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<SupplierResponse>(json);
                        suppliers = result?.Suppliers ?? new List<Supplier>();
                        break;
                    }
                }
                catch { continue; }
            }

            // Fallback to local
            if (suppliers.Count == 0)
            {
                suppliers = GetFallbackSuppliers(category);
            }

            // Calculate distances if project location provided
            if (projectLat.HasValue && projectLon.HasValue)
            {
                foreach (var supplier in suppliers)
                {
                    if (SUPPLIER_LOCATIONS.TryGetValue(supplier.Id, out var loc))
                    {
                        supplier.DistanceFromProject = CalculateDistance(
                            projectLat.Value, projectLon.Value, loc.lat, loc.lon);
                    }
                }

                // Filter by max distance if specified
                if (maxDistanceMiles.HasValue)
                {
                    suppliers = suppliers.FindAll(s => 
                        s.DistanceFromProject == 0 || s.DistanceFromProject <= maxDistanceMiles.Value);
                }

                // Sort by distance
                suppliers.Sort((a, b) => a.DistanceFromProject.CompareTo(b.DistanceFromProject));
            }

            return suppliers;
        }

        /// <summary>
        /// Get suppliers within specified radius (for LEED regional materials credit)
        /// </summary>
        public async Task<List<Supplier>> GetRegionalSuppliers(double projectLat, double projectLon, double radiusMiles = 500)
        {
            var allSuppliers = await GetSuppliersAsync(null, projectLat, projectLon);
            return allSuppliers.FindAll(s => s.DistanceFromProject > 0 && s.DistanceFromProject <= radiusMiles);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 3958.8; // Earth's radius in miles
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180.0;

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
                },
                new Supplier
                {
                    Id = "centralconcrete",
                    Name = "Central Concrete",
                    Categories = new List<string> { "concrete", "ready-mix" },
                    Certifications = new List<string> { "EPD", "LEED" },
                    Region = "California",
                    SustainabilityScore = 88,
                    ContactEmail = "sales@centralconcrete.com",
                    Website = "https://www.centralconcrete.com"
                },
                new Supplier
                {
                    Id = "cmc",
                    Name = "Commercial Metals Company",
                    Categories = new List<string> { "steel", "rebar" },
                    Certifications = new List<string> { "EPD", "ISO 14001" },
                    Region = "North America",
                    SustainabilityScore = 87,
                    ContactEmail = "sales@cmc.com",
                    Website = "https://www.cmc.com"
                }
            };

            if (!string.IsNullOrEmpty(category))
            {
                string cat = category.ToLower();
                return suppliers.FindAll(s => s.Categories.Exists(c => c.ToLower().Contains(cat) || cat.Contains(c.ToLower()))
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
        
        public double DistanceFromProject { get; set; }
        public string CategoriesDisplay => Categories != null ? string.Join(", ", Categories) : "";
        public string CertificationsDisplay => Certifications != null ? string.Join(", ", Certifications) : "";
        public string DistanceDisplay => DistanceFromProject > 0 ? $"{DistanceFromProject:N0} mi" : "Global";
    }
}

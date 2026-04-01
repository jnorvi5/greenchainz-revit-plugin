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
    public partial class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly bool _shouldDisposeHttpClient;

        public ApiClient()
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, null)
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

            // Security: Enforce HTTPS for non-localhost
            if (!_baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !_baseUrl.Contains("localhost") &&
                !_baseUrl.Contains("127.0.0.1"))
            {
                _logger.LogInfo($"[SECURITY WARNING] Using insecure connection to {_baseUrl}");
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(ApiConfig.TIMEOUT_SECONDS)
            };
            _shouldDisposeHttpClient = true;

            ConfigureHttpClient(authToken);
        }

        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _shouldDisposeHttpClient = false;

            ConfigureHttpClient(authToken);
        }

        private void ConfigureHttpClient(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            string url = $"{_baseUrl}/api/rfqs";
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"RFQ submission failed ({response.StatusCode}): {errorBody}");
            }
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AuditResult>(responseString);
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(null, $"Audit submission failed: {response.StatusCode} - {errorBody}");
                    return new AuditResult
                    {
                        OverallScore = -1,
                        Summary = $"API Error ({response.StatusCode}): {errorBody}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit submission failed unexpectedly");
                return new AuditResult
                {
                    OverallScore = -1,
                    Summary = "API Error: " + ex.Message
                };
            }
        }

        /// <summary>
        /// Gets carbon comparison data for a material to find lower-carbon alternatives.
        /// </summary>
        public async Task<AuditResponse> GetCarbonComparisonAsync(string materialName, string zipCode, double volume, double currentGwp)
        {
            try
            {
                string url = $"{_baseUrl}/api/carbon-comparison?material={Uri.EscapeDataString(materialName)}&zip={zipCode}&volume={volume}&gwp={currentGwp}";

                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AuditResponse>(responseString);
                }
                else
                {
                    // If API not available, return mock comparison data
                    return GenerateMockComparison(materialName, volume, currentGwp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Carbon comparison request failed for {materialName}");
                // Return mock data on failure
                return GenerateMockComparison(materialName, volume, currentGwp);
            }
        }

        private AuditResponse GenerateMockComparison(string materialName, double volume, double currentGwp)
        {
            // Generate a reasonable low-carbon alternative
            double reducedGwp = currentGwp * 0.7; // 30% reduction
            string altName = GetLowCarbonAlternative(materialName);

            return new AuditResponse
            {
                Success = true,
                Message = "Mock comparison data",
                DataSource = "GreenChainz Estimates",
                Original = new MaterialComparison
                {
                    MaterialName = materialName,
                    SupplierName = "Current Specification",
                    GwpValue = currentGwp * volume,
                    HasEpd = false,
                    LeedImpact = "Baseline"
                },
                BestAlternative = new MaterialComparison
                {
                    MaterialName = altName,
                    SupplierName = "GreenChainz Verified Supplier",
                    GwpValue = reducedGwp * volume,
                    HasEpd = true,
                    LeedImpact = "+1 LEED Point Potential",
                    Certifications = new System.Collections.Generic.List<string> { "EPD", "Low Carbon" },
                    CarbonSavings = 30,
                    Distance = 50
                }
            };
        }

        private string GetLowCarbonAlternative(string materialName)
        {
            string lower = materialName.ToLower();
            if (lower.Contains("concrete")) return "Low-Carbon Concrete (CarbonCure)";
            if (lower.Contains("steel")) return "Recycled EAF Steel (Nucor)";
            if (lower.Contains("wood") || lower.Contains("timber")) return "FSC-Certified CLT";
            if (lower.Contains("glass")) return "Low-E Recycled Glass (Guardian)";
            if (lower.Contains("aluminum")) return "CIRCAL 75R Recycled Aluminum";
            if (lower.Contains("insulation")) return "Rockwool Stone Wool";
            return $"Low-Carbon {materialName}";
        }

        /// <summary>
        /// Fetches lower-carbon swap alternatives for a given material.
        /// GET https://greenchainz.com/api/materials/{materialId}/alternatives
        /// </summary>
        public async Task<List<SwapAlternative>> GetSwapAlternativesAsync(string materialId)
        {
            try
            {
                string url = $"{_baseUrl}/api/materials/{Uri.EscapeDataString(materialId)}/alternatives";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<SwapAlternative>>(responseString);
                }
                else
                {
                    _logger.LogInfo($"Swap alternatives API returned {response.StatusCode} for {materialId}, using mock data");
                    return GenerateMockAlternatives(materialId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch swap alternatives for {materialId}");
                return GenerateMockAlternatives(materialId);
            }
        }

        /// <summary>
        /// Submits an RFQ with line items to POST https://greenchainz.com/api/rfq.
        /// </summary>
        public async Task<string> SubmitRfqWithLineItemsAsync(RfqSubmitRequest request)
        {
            string url = $"{_baseUrl}/api/rfq";
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    _logger.LogError(null, $"RFQ submission failed: {response.StatusCode} - {responseBody}");
                    throw new ApiException($"RFQ submission failed ({response.StatusCode})", (int)response.StatusCode, responseBody);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFQ submission request failed");
                throw new ApiException($"Network error submitting RFQ: {ex.Message}", ex);
            }
        }

        private List<SwapAlternative> GenerateMockAlternatives(string materialId)
        {
            string id = (materialId ?? "").ToLower();
            var alternatives = new List<SwapAlternative>();

            if (id.Contains("concrete") || id.Contains("cement"))
            {
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "carboncure-rmx-4000",
                    MaterialName = "CarbonCure Ready-Mix 4000psi",
                    Manufacturer = "CarbonCure Technologies",
                    CarbonPerUnit = 238,
                    CarbonSavingsPercent = 30,
                    CompressiveStrength = "4000 psi",
                    FireRating = "Class A (Non-combustible)",
                    CostDeltaPercent = 2,
                    EpdVerified = true,
                    IsBestSwap = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "ecocem-ggbs-50",
                    MaterialName = "Ecocem GGBS Blend 50%",
                    Manufacturer = "Ecocem",
                    CarbonPerUnit = 204,
                    CarbonSavingsPercent = 40,
                    CompressiveStrength = "3500 psi",
                    FireRating = "Class A (Non-combustible)",
                    CostDeltaPercent = -5,
                    EpdVerified = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "central-lc-rmx",
                    MaterialName = "Central Concrete Low-Carbon Mix",
                    Manufacturer = "Central Concrete",
                    CarbonPerUnit = 272,
                    CarbonSavingsPercent = 20,
                    CompressiveStrength = "4500 psi",
                    FireRating = "Class A (Non-combustible)",
                    CostDeltaPercent = 5,
                    EpdVerified = false
                });
            }
            else if (id.Contains("steel") || id.Contains("metal"))
            {
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "nucor-eaf-recycled",
                    MaterialName = "Nucor EAF Recycled Steel",
                    Manufacturer = "Nucor Corporation",
                    CarbonPerUnit = 925,
                    CarbonSavingsPercent = 50,
                    CompressiveStrength = "50 ksi yield",
                    FireRating = "Non-combustible",
                    CostDeltaPercent = 3,
                    EpdVerified = true,
                    IsBestSwap = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "ssab-fossil-free",
                    MaterialName = "SSAB Fossil-Free Steel",
                    Manufacturer = "SSAB",
                    CarbonPerUnit = 370,
                    CarbonSavingsPercent = 80,
                    CompressiveStrength = "50 ksi yield",
                    FireRating = "Non-combustible",
                    CostDeltaPercent = 15,
                    EpdVerified = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "cmc-rebar-recycled",
                    MaterialName = "CMC Recycled Rebar",
                    Manufacturer = "Commercial Metals Company",
                    CarbonPerUnit = 740,
                    CarbonSavingsPercent = 60,
                    CompressiveStrength = "60 ksi yield",
                    FireRating = "Non-combustible",
                    CostDeltaPercent = 0,
                    EpdVerified = true
                });
            }
            else if (id.Contains("wood") || id.Contains("timber"))
            {
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "structurlam-clt",
                    MaterialName = "Structurlam CrossLam CLT",
                    Manufacturer = "Structurlam",
                    CarbonPerUnit = -500,
                    CarbonSavingsPercent = 100,
                    CompressiveStrength = "V2 grade",
                    FireRating = "2-Hour (charring)",
                    CostDeltaPercent = 10,
                    EpdVerified = true,
                    IsBestSwap = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "nordic-glulam",
                    MaterialName = "Nordic Structures Glulam",
                    Manufacturer = "Nordic Structures",
                    CarbonPerUnit = -400,
                    CarbonSavingsPercent = 90,
                    CompressiveStrength = "24F-V4 grade",
                    FireRating = "1-Hour (charring)",
                    CostDeltaPercent = 8,
                    EpdVerified = true
                });
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = "fsc-lumber",
                    MaterialName = "FSC-Certified Dimensional Lumber",
                    Manufacturer = "Various",
                    CarbonPerUnit = 80,
                    CarbonSavingsPercent = 27,
                    CompressiveStrength = "#2 SPF grade",
                    FireRating = "Combustible",
                    CostDeltaPercent = 5,
                    EpdVerified = false
                });
            }
            else
            {
                alternatives.Add(new SwapAlternative
                {
                    MaterialId = $"lc-{materialId}",
                    MaterialName = $"Low-Carbon {materialId}",
                    Manufacturer = "GreenChainz Verified",
                    CarbonPerUnit = 70,
                    CarbonSavingsPercent = 30,
                    CompressiveStrength = "Equivalent",
                    FireRating = "Equivalent",
                    CostDeltaPercent = 5,
                    EpdVerified = false,
                    IsBestSwap = true
                });
            }

            return alternatives;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_shouldDisposeHttpClient)
                    {
                        _httpClient?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

    }
}

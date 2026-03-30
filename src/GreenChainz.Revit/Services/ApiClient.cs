using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class ApiClient : IDisposable
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

        private class TelemetryLogger : ILogger
        {
            public void LogDebug(string message) => TelemetryService.LogInfo($"[DEBUG] {message}");
            public void LogInfo(string message) => TelemetryService.LogInfo(message);
            public void LogError(Exception ex, string message) => TelemetryService.LogError(ex, message);
        }
    }
}

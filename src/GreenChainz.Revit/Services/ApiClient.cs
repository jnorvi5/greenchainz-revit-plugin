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
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly bool _shouldDisposeHttpClient;

        public ApiClient()
<<<<<<< HEAD
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, null)
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
=======
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), new TelemetryLogger())
        {
        }

        public ApiClient(ILogger logger)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

<<<<<<< HEAD
            // Security: Enforce HTTPS for non-localhost
            if (!_baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !_baseUrl.Contains("localhost") &&
                !_baseUrl.Contains("127.0.0.1"))
            {
                _logger.LogInfo($"[SECURITY WARNING] Using insecure connection to {_baseUrl}");
            }

=======
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
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

<<<<<<< HEAD
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
=======
        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(string)) return (T)(object)responseString;
                    return JsonConvert.DeserializeObject<T>(responseString);
                }
                else
                {
                    _logger.LogError(null, $"API Error: {response.StatusCode} - {responseString}");
                    throw new ApiException($"API Error {response.StatusCode}", (int)response.StatusCode, responseString);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API request failed");
                throw;
            }
        }

        #region Real-Time Messaging
        public async Task<object> GetConversationsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/messaging.getConversations");
            return await SendRequestAsync<object>(request);
        }

        public async Task<object> GetMessagesAsync(int conversationId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/messaging.getMessages?input={{\"conversationId\":{conversationId}}}");
            return await SendRequestAsync<object>(request);
        }

        public async Task<object> SendMessageAsync(int conversationId, string content)
        {
            var body = new { conversationId, content };
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/trpc/messaging.send")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            return await SendRequestAsync<object>(request);
        }

        public async Task<object> SendWithAgentAsync(int conversationId, string content, string context = null)
        {
            var body = new { conversationId, content, conversationContext = context };
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/trpc/messaging.sendWithAgent")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            return await SendRequestAsync<object>(request);
        }
        #endregion

        #region RFQ & Scorecards
        public async Task<object> SubmitRFQ(object rfqData)
        {
            // Maps to the Next.js API endpoint /api/rfqs
            var json = JsonConvert.SerializeObject(rfqData);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/rfqs")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await SendRequestAsync<object>(httpRequest);
        }

        public async Task<CcpsBreakdown> GetMaterialScorecardAsync(int materialId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/materials.getById?input={{\"id\":{materialId}}}");
            var result = await SendRequestAsync<dynamic>(request);
            return JsonConvert.DeserializeObject<CcpsBreakdown>(JsonConvert.SerializeObject(result.result.data.ccpsByPersona.default));
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
        }
        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_shouldDisposeHttpClient) _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

<<<<<<< HEAD
        private class TelemetryLogger : ILogger
        {
            public void LogDebug(string message) => TelemetryService.LogInfo($"[DEBUG] {message}");
            public void LogInfo(string message) => TelemetryService.LogInfo(message);
            public void LogError(Exception ex, string message) => TelemetryService.LogError(ex, message);
        }
=======
    public class CcpsBreakdown
    {
        public double CarbonScore { get; set; }
        public double ComplianceScore { get; set; }
        public double CertificationScore { get; set; }
        public double CostScore { get; set; }
        public double SupplyChainScore { get; set; }
        public double HealthScore { get; set; }
        public double CcpsTotal { get; set; }
        public int SourcingDifficulty { get; set; }
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
    }
}

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

        // Default constructor uses production URL and TelemetryLogger
        public ApiClient()
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), new TelemetryLogger())
        {
        }

        // Constructor with logger injection
        public ApiClient(ILogger logger)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        // Constructor with explicit params
        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

            // Security: Enforce HTTPS for non-localhost
            if (!_baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !_baseUrl.Contains("localhost") &&
                !_baseUrl.Contains("127.0.0.1"))
            {
                _logger.LogInformation($"[SECURITY WARNING] Using insecure connection to {_baseUrl}");
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(ApiConfig.TIMEOUT_SECONDS)
            };
            _shouldDisposeHttpClient = true;

            ConfigureHttpClient(authToken);
        }

        // Constructor for dependency injection of HttpClient
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _shouldDisposeHttpClient = false;

            ConfigureHttpClient(authToken);
        }

        // Overload to maintain backward compatibility
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient)
            : this(baseUrl, authToken, httpClient, null)
        {
        }

        private void ConfigureHttpClient(string authToken)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(authToken))
            {
                if (_httpClient.DefaultRequestHeaders.Authorization == null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }
            }
        }

        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            string method = request.Method.ToString();
            string url = request.RequestUri.ToString();

            _logger.LogDebug($"Sending {method} request to {url}");
            // SECURITY: Do not log request body to avoid PII leakage

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)responseString;
                    }

                    return JsonConvert.DeserializeObject<T>(responseString);
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(null, $"API request failed: {response.StatusCode} - {errorBody}");
                    throw new ApiException($"API request failed with status code {response.StatusCode}", (int)response.StatusCode, errorBody);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response");
                throw new ApiException($"Failed to parse API response: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed");
                throw;
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            string method = request.Method.ToString();
            string url = request.RequestUri.ToString();

            _logger.LogDebug($"Sending {method} request to {url}");

            try
            {
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HTTP request failed for {url}");
                throw;
            }
        }

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
             if (request == null) throw new ArgumentNullException(nameof(request));

             string url = $"{_baseUrl}/api/rfq";
             string json = JsonConvert.SerializeObject(request);
             var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
             {
                 Content = new StringContent(json, Encoding.UTF8, "application/json")
             };

             _logger.LogInformation($"Submitting RFQ for project: {request.ProjectName}");
             // SECURITY: Request body logging removed to prevent PII leakage (ProjectAddress, SpecialInstructions)

             try
             {
                 return await SendRequestAsync<string>(httpRequest);
             }
             catch (Exception ex)
             {
                 // Wrapping to match original behavior intent
                 throw new Exception($"RFQ submission failed: {ex.Message}", ex);
             }
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _logger.LogInformation($"Submitting audit for project: {request.ProjectName}");
            // SECURITY: Do not log full audit request details as they may contain sensitive project data

            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);

            // Re-using the generic SendRequestAsync to ensure consistent error handling
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                return await SendRequestAsync<AuditResult>(httpRequest);
            }
            catch (ApiException ex)
            {
                // Preserve original behavior: return error object instead of throwing
                return new AuditResult
                {
                    OverallScore = -1,
                    Summary = "API Error: " + (ex.ResponseBody ?? ex.Message)
                };
            }
            catch (Exception ex)
            {
                 return new AuditResult
                {
                    OverallScore = -1,
                    Summary = "API Error: " + ex.Message
                };
            }
        }

        public async Task<MaterialsResponse> GetMaterialsAsync(string category = null, string search = null)
        {
            _logger.LogDebug($"Getting materials with category={category}, search={search}");

            var uriBuilder = new UriBuilder($"{_baseUrl}/materials");
            var query = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");

            if (query.Count > 0)
            {
                uriBuilder.Query = string.Join("&", query);
            }

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            return await SendRequestAsync<MaterialsResponse>(httpRequest);
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

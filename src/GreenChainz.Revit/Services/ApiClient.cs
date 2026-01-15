using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<ApiClient> _logger;
        private readonly string _authToken;
        private readonly IRevitLogger _logger;
        private readonly ILogger<ApiClient> _logger;
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly bool _shouldDisposeHttpClient;

        // Default constructor uses production URL and TelemetryLogger
        public ApiClient()
            : this(ApiConfig.BASE_URL, null, new TelemetryLogger())
        {
        }

        // Constructor with logger injection
        public ApiClient(ILogger logger)
            : this(ApiConfig.BASE_URL, null, logger)
        {
        }

        // Constructor with URL and Auth Token (creates own HttpClient)
        // Default constructor uses default base URL and file logger
        public ApiClient(ILogger logger = null)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        // Constructor with base URL and auth token, creates its own HttpClient
        // Default constructor
        public ApiClient(ILogger logger = null)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        public ApiClient(ILogger logger = null)
            : this("https://api.greenchainz.com", null, logger)
        {
        }

        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
        public ApiClient()
            : this("https://api.greenchainz.com", null, null, null)
        {
        }

        // Constructor with logger injection
        public ApiClient(ILogger<ApiClient> logger)
            : this("https://api.greenchainz.com", null, null, logger)
        {
        }

        public ApiClient(string baseUrl, string authToken = null, HttpClient httpClient = null, ILogger<ApiClient> logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger;

            if (httpClient != null)
            {
                _httpClient = httpClient;
            }
            else
            {
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
            }
            : this("https://api.greenchainz.com", null, new FileLogger())
            : this("https://api.greenchainz.com", null, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, null)
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger<ApiClient> logger)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger;
            : this(baseUrl, authToken, new TelemetryLogger())
            : this(baseUrl, authToken, new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
        {
        }

        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? new TelemetryLogger();
            : this(baseUrl, authToken, new FileLogger())
             : this(baseUrl, authToken, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new FileLogger();
        {
        }

        public ApiClient(string baseUrl, string authToken, IRevitLogger logger)
        {
            _logger = logger ?? new TelemetryLogger();
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _authToken = authToken;
            _logger = logger ?? new TelemetryLogger();
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken())
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
                _logger.LogInfo($"[SECURITY WARNING] Using insecure connection to {_baseUrl}");
                // In a stricter environment, we would throw:
                // throw new ArgumentException("HTTPS is required for remote connections.");
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(ApiConfig.TIMEOUT_SECONDS)
            };
            _shouldDisposeHttpClient = true;

            ConfigureHttpClient(authToken);
        }

        // Constructor for dependency injection of HttpClient
        // Constructor for DI/Testing
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _shouldDisposeHttpClient = false;

            ConfigureHttpClient(authToken);
        }

        // Backward compatibility
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


            _logger.LogDebug($"Sending {method} request to {url}");

            // SECURITY: Request body logging removed to prevent PII leakage
            // Previous insecure code: _logger.LogDebug($"Request body: {jsonBody}");

            try
            {
                return await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed");
                throw;
            }
        }

        private async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            try
            {
                HttpResponseMessage response = await SendRequestAsync(request);

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
                    // Sanitize log: don't log full error body if it contains secrets, but usually error bodies are safe-ish.
                    // We'll log the status code.
                    _logger.LogError(new Exception($"API Error {response.StatusCode}"), $"Request to {url} failed with status {response.StatusCode}");

                    // The exception message contains the error body for the caller to handle, but not logged blindly
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

        public ApiClient(string baseUrl, string authToken, HttpClient httpClient)
            : this(baseUrl, authToken, httpClient, new TelemetryLogger())
        {
        }

            // Security: Avoid logging full PII/proprietary info. Log generic info.
            _logger.LogInfo($"Submitting RFQ for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/rfq";
            string json = JsonConvert.SerializeObject(request);

            using (var message = new HttpRequestMessage(HttpMethod.Post, url))
            {
                message.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await SendRequestAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"RFQ submission failed ({response.StatusCode}): {errorBody}");
                }
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Security: Do NOT log the full JSON body here as it might contain sensitive project data.
                // Previous corrupted version had explicit logging of the body. We removed it.

                try
                {
                    return await SendRequestAsync<string>(requestMessage);
                }
                catch (ApiException ex)
                {
                     // Maintain API contract for callers expecting generic Exception
                     throw new Exception($"RFQ submission failed: {ex.Message}", ex);
                }
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, IRevitLogger logger)
        {
            _logger = logger ?? new TelemetryLogger();
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _authToken = authToken;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            if (!string.IsNullOrEmpty(authToken) && _httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        public async Task<T> SendRequestAsync<T>(HttpRequestMessage request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new ApiException($"Request failed ({response.StatusCode}): {errorBody}", (int)response.StatusCode, errorBody);
                }
            }
            catch (ApiException)
            {
                 throw new Exception($"RFQ submission failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"RFQ submission failed: {ex.Message}", ex);
            }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new ApiException($"Unexpected error: {ex.Message}", ex);
        public async Task<MaterialsResponse> GetMaterialsAsync(string category = null, string search = null)
        {
            // Logging as requested
            _logger.LogDebug($"Getting materials with category={category}, search={search}");

            var uriBuilder = new UriBuilder($"{_baseUrl}/materials");
            var query = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");

            if (query.Count > 0)
            {
                uriBuilder.Query = string.Join("&", query);
            }

            HttpResponseMessage response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MaterialsResponse>(content);
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new ApiException($"Request failed: {response.ReasonPhrase}", (int)response.StatusCode, errorBody);
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

             _logger.LogInfo($"Submitting RFQ for project: {request.ProjectName}");
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

        public async Task<AuditResponse> SubmitAuditAsync(AuditRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Logging added as requested
            _logger?.LogInformation($"Submitting audit for project: {request.ProjectName}");

            // SECURITY: Do not log full audit request details as they may contain sensitive project data

            _logger.LogInfo($"Submitting audit for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);

            using (var message = new HttpRequestMessage(HttpMethod.Post, url))
            {
                message.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await SendRequestAsync(message);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AuditResult>(responseString);
                }
                else
                {
                    return new AuditResult
                    {
                        OverallScore = -1,
                        Summary = "API Error: " + response.ReasonPhrase
                    };
                }
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            try
            {
                return await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError(ex, "Request timeout");

                throw new ApiException($"Request timeout after {ApiConfig.TIMEOUT_SECONDS} seconds", ex);
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            string url = $"{_baseUrl}/audit/extract-materials";
            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                return await SendRequestAsync<AuditResult>(httpRequest);
                string responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AuditResponse>(responseString);
                return await SendRequestAsync<AuditResult>(requestMessage);
            }
            catch (ApiException ex)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Audit submission failed ({response.StatusCode}): {errorBody}");
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

                try
                {
                    return await SendRequestAsync<AuditResult>(requestMessage);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)responseString;
                    }

                    return JsonConvert.DeserializeObject<T>(responseString);
                }
                catch (ApiException ex)
                {
                    // Fallback behavior as per original intent
                    _logger.LogError(ex, "Audit submission failed");
                    return new AuditResult
                    {
                        OverallScore = -1,
                        Summary = "API Error: " + (ex.ResponseBody ?? ex.Message)
                    };
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
                    string errorBody = await response.Content.ReadAsStringAsync();
                     _logger.LogError(null, $"API request failed: {response.StatusCode} - {errorBody}");
                    throw new ApiException($"API request failed with status code {response.StatusCode}", (int)response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HTTP request failed for {url}");
                throw;
            }
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
            public void LogDebug(string message)
            {
                TelemetryService.LogInfo($"[DEBUG] {message}");
            }

            public void LogInfo(string message)
            {
                TelemetryService.LogInfo(message);
            }

            public void LogError(Exception ex, string message)
            {
                TelemetryService.LogError(ex, message);
            }

            public void LogError(string message, Exception ex = null)
            {
                if (ex != null)
                    TelemetryService.LogError(ex, message);
                else
                    TelemetryService.LogInfo($"[ERROR] {message}");
            }
            public void LogDebug(string message) => TelemetryService.LogInfo($"[DEBUG] {message}");
            public void LogInfo(string message) => TelemetryService.LogInfo(message);
            public void LogError(Exception ex, string message) => TelemetryService.LogError(ex, message);
        }
    }
}

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

        // Default constructor uses default base URL and file logger
        public ApiClient(ILogger logger = null)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        // Constructor with base URL and auth token, creates its own HttpClient
        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

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
            _shouldDisposeHttpClient = false; // Don't dispose injected client

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

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            string method = request.Method.ToString();
            string url = request.RequestUri.ToString();

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
                    throw new ApiException($"API request failed with status code {response.StatusCode}: {errorBody}", (int)response.StatusCode, errorBody);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response");
                throw new ApiException($"Failed to parse API response: {ex.Message}", ex);
            }
        }

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInfo($"Submitting RFQ for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/rfq";
            string json = JsonConvert.SerializeObject(request);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                return await SendRequestAsync<string>(requestMessage);
            }
            catch (ApiException ex)
            {
                 throw new Exception($"RFQ submission failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"RFQ submission failed: {ex.Message}", ex);
            }
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // SECURITY: Do not log full audit request details as they may contain sensitive project data

            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                return await SendRequestAsync<AuditResult>(requestMessage);
            }
            catch (ApiException ex)
            {
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
        }
    }
}

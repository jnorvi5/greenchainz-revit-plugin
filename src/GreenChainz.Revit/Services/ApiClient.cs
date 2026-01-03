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
            : this(ApiConfig.BASE_URL, null, new TelemetryLogger())
        {
        }

        // Constructor with logger injection
        public ApiClient(ILogger logger)
            : this(ApiConfig.BASE_URL, null, logger)
        {
        }

        // Constructor with URL and Auth Token (creates own HttpClient)
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

        // Constructor for testing / dependency injection of HttpClient
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _shouldDisposeHttpClient = false; // Don't dispose injected client

            ConfigureHttpClient(authToken);
        }

        // Backward compatibility
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
                // Check if header already exists (relevant when reusing HttpClient)
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

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();

                    // Handle string return type directly to avoid JSON deserialization
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

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Security: Avoid logging full PII/proprietary info. Log generic info.
            _logger.LogInfo($"Submitting RFQ for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/rfq";
            string json = JsonConvert.SerializeObject(request);

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
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInfo($"Submitting audit for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    return await SendRequestAsync<AuditResult>(requestMessage);
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

        // Default logger implementation using TelemetryService
        private class TelemetryLogger : ILogger
        {
            public void LogDebug(string message)
            {
                // TelemetryService only has LogInfo, so we map Debug to Info with a prefix
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
        }
    }
}

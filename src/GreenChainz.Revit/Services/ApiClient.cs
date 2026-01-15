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

        // Default constructor
        public ApiClient(ILogger logger = null)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

        // Constructor with explicit params
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

        // Constructor for DI/Testing
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
                if (_httpClient.DefaultRequestHeaders.Authorization == null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }
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

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Fix for Review Comment: Changed LogInformation to LogInfo to match interface
            _logger.LogInfo($"Submitting audit for project: {request.ProjectName}");

            try
            {
                return await SendRequestAsync<AuditResult>(httpRequest);
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
            public void LogDebug(string message) => TelemetryService.LogInfo($"[DEBUG] {message}");
            public void LogInfo(string message) => TelemetryService.LogInfo(message);
            public void LogError(Exception ex, string message) => TelemetryService.LogError(ex, message);
        }
    }
}

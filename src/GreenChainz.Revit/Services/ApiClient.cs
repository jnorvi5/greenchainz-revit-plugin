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
            : this("https://api.greenchainz.com", null)
        {
        }

        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _shouldDisposeHttpClient = true;

            ConfigureHttpClient(authToken);
        }

        // Constructor for testing / dependency injection of HttpClient
        public ApiClient(string baseUrl, string authToken, HttpClient httpClient, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _shouldDisposeHttpClient = false; // Don't dispose injected client

            ConfigureHttpClient(authToken);
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

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string url = $"{_baseUrl}/api/rfqs";
            HttpResponseMessage response = await SendRequestAsync(HttpMethod.Post, url, request);

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

            HttpResponseMessage response = await SendRequestAsync(HttpMethod.Post, url, request);

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

        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url, object body)
        {
            var request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Logging request body using injected logger
                _logger.LogDebug($"Request body: {json}");
            }

            return await _httpClient.SendAsync(request);
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
        }
    }
}

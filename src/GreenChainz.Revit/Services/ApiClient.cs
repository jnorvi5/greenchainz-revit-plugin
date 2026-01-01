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

        public ApiClient(ILogger logger = null)
            : this("https://api.greenchainz.com", null, logger)
        {
        }

        public ApiClient()
            : this("https://api.greenchainz.com", null, new FileLogger())
            : this("https://api.greenchainz.com", null, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, new FileLogger())
             : this(baseUrl, authToken, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new FileLogger();
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken())
        {
        }

        public ApiClient(string baseUrl, string authToken = null, ILogger logger = null)
        {
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
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

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
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

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug($"Response status: {response.StatusCode}, body: {responseContent}");

            return response;
        }

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation($"Submitting RFQ for project: {request.ProjectName}");

            string url = $"{_baseUrl}/api/rfqs";
            HttpResponseMessage response = await SendRequestAsync(HttpMethod.Post, url, request);
            string url = $"{_baseUrl}/api/rfq";
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Refactored to use SendRequestAsync
            // Note: SendRequestAsync handles the sending, but we need to construct the request message first if we want to reuse it fully.
            // However, SendRequestAsync usually takes a RequestMessage or parameters.
            // Let's implement SendRequestAsync to take method, url, and content.

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            HttpResponseMessage response = await SendRequestAsync(httpRequest);
            // Log request body as per instructions (moved into SendRequestAsync logic effectively by checking content)
            // But we need to pass the body string if we want to log it before creating StringContent,
            // or read it from Content in SendRequestAsync. Reading from Content is better for encapsulation.

            HttpResponseMessage response = await SendRequestAsync(httpRequest, json); // Pass json string for logging convenience

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
            }
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            string url = $"{_baseUrl}/api/audit";
            string json = JsonConvert.SerializeObject(request);

            HttpResponseMessage response = await SendRequestAsync(HttpMethod.Post, url, request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            HttpResponseMessage response = await SendRequestAsync(httpRequest);
            HttpResponseMessage response = await SendRequestAsync(httpRequest, json);

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

        /// <summary>
        /// Sends the HTTP request with logging.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="jsonBody">Optional JSON body string for logging purposes.</param>
        /// <returns>The HTTP response.</returns>
        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, string jsonBody = null)
        {
            string method = request.Method.ToString();
            string url = request.RequestUri.ToString();

            if (!string.IsNullOrEmpty(jsonBody))
            {
                _logger.LogDebug($"Request body: {jsonBody}");
            }

            _logger.LogDebug($"Sending {method} request to {url}");

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

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

        public ApiClient()
            : this("https://api.greenchainz.com", null, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
             : this(baseUrl, authToken, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
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

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
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

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

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

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            HttpResponseMessage response = await SendRequestAsync(httpRequest);
            HttpResponseMessage response = await SendRequestAsync(httpRequest, json);

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
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

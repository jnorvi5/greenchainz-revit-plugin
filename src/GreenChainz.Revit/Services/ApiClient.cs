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
        private bool _disposed;

        public ApiClient()
            : this("https://api.greenchainz.com", null)
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
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string url = $"{_baseUrl}/api/rfqs";
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
            }
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
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
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

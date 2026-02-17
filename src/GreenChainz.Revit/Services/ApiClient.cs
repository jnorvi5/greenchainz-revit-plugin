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
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), new TelemetryLogger())
        {
        }

        public ApiClient(ILogger logger)
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken(), logger)
        {
        }

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
    }
}

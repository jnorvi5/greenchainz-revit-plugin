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
        private readonly string _authToken;
        private readonly IRevitLogger _logger;
        private bool _disposed;

        public ApiClient()
            : this("https://api.greenchainz.com", null)
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, new TelemetryLogger())
        {
        }

        public ApiClient(string baseUrl, string authToken, IRevitLogger logger)
        {
            _logger = logger ?? new TelemetryLogger();
            _baseUrl = (baseUrl ?? "https://api.greenchainz.com").TrimEnd('/');
            _authToken = authToken;
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

        public ApiClient(string baseUrl, string authToken, HttpClient httpClient)
            : this(baseUrl, authToken, httpClient, new TelemetryLogger())
        {
        }

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
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string url = $"{_baseUrl}/api/rfqs";
            string json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

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

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

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

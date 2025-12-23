using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class GreenChainzNetworkException : Exception
    {
        public int StatusCode { get; }
        public GreenChainzNetworkException(string message, int statusCode = 0) : base(message) { StatusCode = statusCode; }
    }

    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClient(string baseUrl, string authToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(authToken))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        // Retry Logic Wrapper
        private async Task<T> SendRequestAsync<T>(HttpMethod method, string url, object body = null)
        {
            int maxRetries = 3;
            int attempt = 0;
            
            while (true)
            {
                attempt++;
                try
                {
                    var request = new HttpRequestMessage(method, url);
                    if (body != null)
                        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(content);
                    }

                    // Fail immediately on Client Errors (4xx)
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        throw new GreenChainzNetworkException($"API Error: {response.StatusCode}", (int)response.StatusCode);

                    throw new HttpRequestException($"Server Error: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    // Retry on Server Errors or Network Glitches
                    if (attempt >= maxRetries || ex is GreenChainzNetworkException) throw;
                    
                    // Exponential Backoff: 1s, 2s, 4s
                    await Task.Delay((int)Math.Pow(2, attempt - 1) * 1000);
                }
            }
        }

        public async Task<MaterialsResponse> GetMaterialsAsync(string category = null)
        {
            return await SendRequestAsync<MaterialsResponse>(HttpMethod.Get, $"{_baseUrl}/materials?category={category}");
        }

        public async Task<AuditResponse> SubmitAuditAsync(AuditRequest request)
        {
            return await SendRequestAsync<AuditResponse>(HttpMethod.Post, $"{_baseUrl}/audit", request);
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}

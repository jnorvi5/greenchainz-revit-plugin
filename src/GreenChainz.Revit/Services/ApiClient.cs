using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://api.greenchainz.com"; // Placeholder URL

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Assuming POST /api/rfqs
                HttpResponseMessage response = await _httpClient.PostAsync($"{BaseUrl}/api/rfqs", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Assuming response contains an ID or success message.
                    // Let's parse it or just return it.
                    // If response is like { "id": "RFQ-123" }
                    return responseBody;
                }
                else
                {
                    throw new Exception($"Error submitting RFQ: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Log exception if logging exists
                throw new Exception($"Failed to submit RFQ: {ex.Message}", ex);
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer;
        private const string BaseUrl = "http://localhost:5000"; // Assuming local backend or placeholder

        public ApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _serializer = new JavaScriptSerializer();
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditRequest request)
        {
            try
            {
                string json = _serializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("/api/audit/extract-materials", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return _serializer.Deserialize<AuditResult>(responseString);
                }
                else
                {
                    // Handle error, maybe return a default result with error info or throw
                    return new AuditResult
                    {
                        CarbonScore = -1,
                        Rating = "Error",
                        Recommendations = new System.Collections.Generic.List<string> { "API Call Failed: " + response.ReasonPhrase }
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuditResult
                {
                    CarbonScore = -1,
                    Rating = "Error",
                    Recommendations = new System.Collections.Generic.List<string> { "Exception: " + ex.Message }
                };
            }
        }

        // Synchronous wrapper if needed for Revit command context which might not support async/await fully in older versions
        // But usually we can run async.
        public AuditResult SubmitAudit(AuditRequest request)
        {
            return Task.Run(() => SubmitAuditAsync(request)).Result;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Client for communicating with the GreenChainz REST API.
    /// </summary>
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient"/> class.
        /// </summary>
        /// <param name="baseUrl">The base URL of the API.</param>
        /// <param name="authToken">The JWT authentication token.</param>
        public ApiClient(string baseUrl, string authToken)
            : this(baseUrl, authToken, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient"/> class with a custom HttpClient.
        /// </summary>
        /// <param name="baseUrl">The base URL of the API.</param>
        /// <param name="authToken">The JWT authentication token.</param>
        /// <param name="httpClient">Optional HttpClient instance for testing.</param>
        internal ApiClient(string baseUrl, string authToken, HttpClient httpClient)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (string.IsNullOrWhiteSpace(authToken))
                throw new ArgumentNullException(nameof(authToken));

            _baseUrl = baseUrl.TrimEnd('/');
            
            if (httpClient != null)
            {
                _httpClient = httpClient;
            }
            else
            {
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(ApiConfig.TIMEOUT_SECONDS)
                };
            }

            // Set default headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        /// <summary>
        /// Gets materials from the GreenChainz API with optional filtering.
        /// </summary>
        /// <param name="category">Optional category filter.</param>
        /// <param name="search">Optional search term.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the materials response.</returns>
        /// <exception cref="ApiException">Thrown when the API request fails.</exception>
        public async Task<MaterialsResponse> GetMaterialsAsync(string category = null, string search = null)
        {
            // TODO: Add logging
            // _logger.LogDebug($"Getting materials with category={category}, search={search}");

            var uriBuilder = new UriBuilder($"{_baseUrl}/materials");
            var queryParams = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(category))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (queryParams.Count > 0)
                uriBuilder.Query = string.Join("&", queryParams);

            return await SendRequestAsync<MaterialsResponse>(HttpMethod.Get, uriBuilder.Uri.ToString(), null);
        }

        /// <summary>
        /// Submits a carbon audit request to the API.
        /// </summary>
        /// <param name="request">The audit request containing project and materials data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audit response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="ApiException">Thrown when the API request fails.</exception>
        public async Task<AuditResponse> SubmitAuditAsync(AuditRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // TODO: Add logging
            // _logger.LogInformation($"Submitting audit for project: {request.ProjectName}");

            string url = $"{_baseUrl}/audit/extract-materials";
            return await SendRequestAsync<AuditResponse>(HttpMethod.Post, url, request);
        }

        /// <summary>
        /// Submits an RFQ (Request for Quotation) to suppliers.
        /// </summary>
        /// <param name="request">The RFQ request containing project and contact information.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the RFQ response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="ApiException">Thrown when the API request fails.</exception>
        public async Task<RfqResponse> SubmitRfqAsync(RfqRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // TODO: Add logging
            // _logger.LogInformation($"Submitting RFQ for project: {request.ProjectName}");

            string url = $"{_baseUrl}/rfqs";
            return await SendRequestAsync<RfqResponse>(HttpMethod.Post, url, request);
        }

        /// <summary>
        /// Sends an HTTP request and deserializes the response.
        /// </summary>
        private async Task<T> SendRequestAsync<T>(HttpMethod method, string url, object body = null)
        {
            HttpResponseMessage response = null;
            string responseContent = null;

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(method, url);

                if (body != null)
                {
                    string json = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    // TODO: Add logging
                    // _logger.LogDebug($"Request body: {json}");
                }

                // TODO: Add logging
                // _logger.LogDebug($"Sending {method} request to {url}");

                response = await _httpClient.SendAsync(request);
                responseContent = await response.Content.ReadAsStringAsync();

                // TODO: Add logging
                // _logger.LogDebug($"Response status: {response.StatusCode}, body: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = GetErrorMessage(response.StatusCode, responseContent);
                    throw new ApiException(errorMessage, (int)response.StatusCode, responseContent);
                }

                T result = JsonConvert.DeserializeObject<T>(responseContent);
                return result;
            }
            catch (HttpRequestException ex)
            {
                // TODO: Add logging
                // _logger.LogError(ex, "HTTP request failed");

                string errorMessage = $"Network error occurred: {ex.Message}";
                if (response != null)
                {
                    throw new ApiException(errorMessage, (int)response.StatusCode, responseContent, ex);
                }
                throw new ApiException(errorMessage, ex);
            }
            catch (TaskCanceledException ex)
            {
                // TODO: Add logging
                // _logger.LogError(ex, "Request timeout");

                throw new ApiException($"Request timeout after {ApiConfig.TIMEOUT_SECONDS} seconds", ex);
            }
            catch (JsonException ex)
            {
                // TODO: Add logging
                // _logger.LogError(ex, "Failed to deserialize response");

                throw new ApiException($"Failed to parse API response: {ex.Message}", ex);
            }
            catch (ApiException)
            {
                // Re-throw ApiException as-is
                throw;
            }
            catch (Exception ex)
            {
                // TODO: Add logging
                // _logger.LogError(ex, "Unexpected error");

                throw new ApiException($"Unexpected error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates an error message based on the HTTP status code.
        /// </summary>
        private string GetErrorMessage(HttpStatusCode statusCode, string responseBody)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return "Authentication failed. Please check your API token.";

                case HttpStatusCode.Forbidden:
                    return "Access forbidden. You don't have permission to access this resource.";

                case HttpStatusCode.NotFound:
                    return "The requested resource was not found.";

                case HttpStatusCode.BadRequest:
                    return $"Bad request: {responseBody}";

                case HttpStatusCode.InternalServerError:
                    return $"Server error occurred: {responseBody}";

                case HttpStatusCode.ServiceUnavailable:
                    return "The API service is temporarily unavailable. Please try again later.";

                case HttpStatusCode.TooManyRequests:
                    return "Rate limit exceeded. Please wait before making more requests.";

                default:
                    return $"API request failed with status {(int)statusCode}: {responseBody}";
            }
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
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

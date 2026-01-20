using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Agent
{
    /// <summary>
    /// Client for communicating with the GreenChainz AI Agent.
    /// The agent runs as a Docker container or local Python service.
    /// </summary>
    public class AgentClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseAddress;
        private bool _disposed;

        /// <summary>
        /// Default agent URLs to try
        /// </summary>
        private static readonly string[] AGENT_URLS = new[]
        {
            "http://localhost:8000",           // Local development
            "http://host.docker.internal:8000", // Docker for Windows
            "http://127.0.0.1:8000"            // Fallback
        };

        public AgentClient(string baseAddress = null)
        {
            _baseAddress = baseAddress ?? AGENT_URLS[0];
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseAddress),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Check if the agent is running
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try to find a running agent on any of the default URLs
        /// </summary>
        public static async Task<AgentClient> FindRunningAgentAsync()
        {
            foreach (var url in AGENT_URLS)
            {
                try
                {
                    var client = new AgentClient(url);
                    if (await client.IsHealthyAsync())
                    {
                        return client;
                    }
                    client.Dispose();
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// Main inference endpoint - scores materials and returns actions
        /// </summary>
        public async Task<AgentResponse> InferAsync(AgentRequest request)
        {
            return await PostAsync<AgentResponse>("/agent/infer", request);
        }

        /// <summary>
        /// Score materials only
        /// </summary>
        public async Task<AgentResponse> ScoreAsync(AgentRequest request)
        {
            request.task = "score_only";
            return await PostAsync<AgentResponse>("/agent/score", request);
        }

        /// <summary>
        /// Get swap recommendations
        /// </summary>
        public async Task<AgentResponse> RecommendAsync(AgentRequest request)
        {
            request.task = "recommend_swaps";
            return await PostAsync<AgentResponse>("/agent/recommend", request);
        }

        /// <summary>
        /// Map materials to IFC property sets
        /// </summary>
        public async Task<AgentResponse> MapToIfcAsync(AgentRequest request)
        {
            request.task = "ifc_mapping";
            return await PostAsync<AgentResponse>("/agent/ifc-mapping", request);
        }

        /// <summary>
        /// Generic POST helper
        /// </summary>
        private async Task<T> PostAsync<T>(string endpoint, object request)
        {
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var response = await _httpClient.PostAsync(endpoint, content))
            {
                var respJson = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Agent error ({response.StatusCode}): {respJson}");
                }
                
                return JsonConvert.DeserializeObject<T>(respJson);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

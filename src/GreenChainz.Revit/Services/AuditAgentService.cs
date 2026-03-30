using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service to interact with the AI Audit Agent (Defensibility Agent)
    /// for verifying material specifications and preventing value engineering.
    /// </summary>
    public class AuditAgentService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger _logger;

        public AuditAgentService(ILogger logger = null)
        {
            _apiClient = new ApiClient();
            _logger = logger ?? new TelemetryLogger();
        }

        /// <summary>
        /// Runs a defensibility check on a material to verify its sustainability claims.
        /// </summary>
        public async Task<DefensibilityResult> CheckMaterialDefensibility(int materialId)
        {
            try
            {
                // Maps to the main app's agent triage or direct agent endpoint
                // For now, we use a specialized endpoint that wraps the DefensibilityAgent logic
                var result = await _apiClient.GetMaterialDefensibilityAsync(materialId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run defensibility check");
                return new DefensibilityResult 
                { 
                    IsDefensible = false, 
                    Recommendations = new List<string> { "Connection to Audit Agent failed. Check your network." } 
                };
            }
        }

        /// <summary>
        /// Compares an original material with a proposed substitute ("Or Equal" analysis).
        /// </summary>
        public async Task<ComparisonResult> CompareMaterials(int originalId, int substituteId)
        {
            try
            {
                var result = await _apiClient.CompareMaterialsAsync(originalId, substituteId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare materials");
                return null;
            }
        }
    }

    public class DefensibilityResult
    {
        public bool IsDefensible { get; set; }
        public double DefensibilityScore { get; set; }
        public List<string> Strengths { get; set; }
        public List<string> Vulnerabilities { get; set; }
        public List<string> Recommendations { get; set; }
        public List<string> LeedCredits { get; set; }
    }

    public partial class ApiClient
    {
        public async Task<DefensibilityResult> GetMaterialDefensibilityAsync(int materialId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/agents.checkDefensibility?input={{\"materialId\":{materialId}}}");
            var result = await SendRequestAsync<dynamic>(request);
            return JsonConvert.DeserializeObject<DefensibilityResult>(JsonConvert.SerializeObject(result.result.data));
        }

        public async Task<ComparisonResult> CompareMaterialsAsync(int originalId, int substituteId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/agents.compareMaterials?input={{\"originalId\":{originalId},\"substituteId\":{substituteId}}}");
            var result = await SendRequestAsync<dynamic>(request);
            return JsonConvert.DeserializeObject<ComparisonResult>(JsonConvert.SerializeObject(result.result.data));
        }
    }

    public class ComparisonResult
    {
        public bool IsEqualOrBetter { get; set; }
        public double CarbonDelta { get; set; }
        public List<string> Issues { get; set; }
        public string Recommendation { get; set; }
    }
}

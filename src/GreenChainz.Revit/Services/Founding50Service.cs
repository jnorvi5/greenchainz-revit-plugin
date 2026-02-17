using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service to manage the "Founding 50" campaign assets and supplier interactions
    /// directly within the Revit environment.
    /// </summary>
    public class Founding50Service
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger _logger;

        public Founding50Service(ILogger logger = null)
        {
            _apiClient = new ApiClient();
            _logger = logger ?? new TelemetryLogger();
        }

        /// <summary>
        /// Retrieves the list of "Founding 50" suppliers that match the current project context.
        /// </summary>
        public async Task<List<FoundingSupplier>> GetMatchedFoundingSuppliers(string materialCategory, string location = null)
        {
            try
            {
                // Maps to the main app's supplier matching logic for premium Founding 50 members
                var request = await _apiClient.GetFoundingSuppliersAsync(materialCategory, location);
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch founding suppliers");
                return new List<FoundingSupplier>();
            }
        }

        /// <summary>
        /// Submits a direct inquiry to a Founding 50 supplier from the Revit plugin.
        /// </summary>
        public async Task<bool> SubmitDirectInquiry(int supplierId, string materialId, string notes)
        {
            try
            {
                var body = new { supplierId, materialId, notes, source = "Revit Plugin" };
                var request = await _apiClient.SubmitDirectInquiryAsync(body);
                return request != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit direct inquiry");
                return false;
            }
        }
    }

    public class FoundingSupplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public double SustainabilityScore { get; set; }
        public List<string> Certifications { get; set; }
        public bool IsFoundingMember { get; set; } = true;
    }

    public partial class ApiClient
    {
        public async Task<List<FoundingSupplier>> GetFoundingSuppliersAsync(string category, string location)
        {
            var query = $"?input={{\"category\":\"{category}\",\"location\":\"{location}\",\"onlyFounding\":true}}";
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/trpc/supplier.list{query}");
            var result = await SendRequestAsync<dynamic>(request);
            return JsonConvert.DeserializeObject<List<FoundingSupplier>>(JsonConvert.SerializeObject(result.result.data.items));
        }

        public async Task<object> SubmitDirectInquiryAsync(object inquiryData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/trpc/rfqMarketplace.submitDirectInquiry")
            {
                Content = new StringContent(JsonConvert.SerializeObject(inquiryData), System.Text.Encoding.UTF8, "application/json")
            };
            return await SendRequestAsync<object>(request);
        }
    }
}

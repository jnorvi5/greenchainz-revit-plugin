using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class SdaConnectorService
    {
        private readonly HttpClient _httpClient;
        private readonly AutodeskAuthService _authService;
        private const string SDA_BASE_URL = "https://developer.api.autodesk.com/carbon/v1";

        public SdaConnectorService(AutodeskAuthService authService)
        {
            _httpClient = new HttpClient();
            _authService = authService;
        }

        public async Task<List<SdaMaterial>> GetMaterialsAsync(string category = null)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            string endpoint = $"{SDA_BASE_URL}/materials";
            if (!string.IsNullOrEmpty(category))
            {
                endpoint += $"?category={Uri.EscapeDataString(category)}";
            }

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<SdaMaterialsResponse>(jsonResponse);

            return new List<SdaMaterial>(data.Materials ?? Array.Empty<SdaMaterial>());
        }

        public async Task<SdaMaterialDetail> GetMaterialDetailAsync(string materialId)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            string endpoint = $"{SDA_BASE_URL}/materials/{materialId}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SdaMaterialDetail>(jsonResponse);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await GetMaterialsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

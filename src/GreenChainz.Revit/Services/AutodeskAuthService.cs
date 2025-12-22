using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class AutodeskAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public AutodeskAuthService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "data:read data:write bucket:read bucket:create")
            });

            var response = await _httpClient.PostAsync(
                "https://developer.api.autodesk.com/authentication/v2/token",
                formContent
            );

            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

            _cachedToken = tokenData.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn - 60);

            return _cachedToken;
        }

        public bool HasValidCredentials()
        {
            return !string.IsNullOrEmpty(_clientId) &&
                   !string.IsNullOrEmpty(_clientSecret) &&
                   _clientId != "YOUR_CLIENT_ID";
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }
    }
}

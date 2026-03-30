using System;
using System.Collections.Generic;
using System.Net.Http;
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

        // Public property to access the token if needed by other services
        public string CurrentToken => _cachedToken;

        public AutodeskAuthService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Return cached token if still valid (with 60s buffer)
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            if (!HasValidCredentials())
            {
                throw new InvalidOperationException("Autodesk credentials are not configured.");
            }

            try
            {
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

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Autodesk Auth Failed ({response.StatusCode}): {errorBody}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    throw new Exception("Received empty token from Autodesk");
                }

                _cachedToken = tokenData.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn - 60);

                return _cachedToken;
            }
            catch (Exception ex)
            {
                // Log exception here in a real app
                throw new Exception($"Failed to authenticate with Autodesk: {ex.Message}", ex);
            }
        }

        public bool HasValidCredentials()
        {
            return !string.IsNullOrEmpty(_clientId) &&
                   !string.IsNullOrEmpty(_clientSecret) &&
                   !_clientId.Contains("DEFAULT") && // Rudimentary check for placeholder
                   _clientId.Length > 10;
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

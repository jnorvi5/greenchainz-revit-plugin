using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit.Services
{
    public class AuthService
    {
        private static AuthService _instance;
        public static AuthService Instance => _instance ?? (_instance = new AuthService());

        public string Token { get; private set; }
        public string UserEmail { get; private set; }
        public int Credits { get; private set; }

        private const string ApiBaseUrl = "https://api.greenchainz.com";
        private readonly HttpClient _httpClient;

        private AuthService()
        {
            _httpClient = new HttpClient();
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new
                {
                    email = email,
                    password = password
                };

                string json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ApiBaseUrl + "/api/plugin/auth", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AuthResponse>(responseJson);

                    Token = result.Token;
                    Credits = result.Credits;
                    UserEmail = result.Email ?? email;

                    SecureStorage.SaveToken(Token);
                    SecureStorage.SaveUserInfo(UserEmail, Credits);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Logout()
        {
            Token = null;
            UserEmail = null;
            Credits = 0;
            SecureStorage.ClearToken();
            SecureStorage.SaveUserInfo(null, 0);
        }

        public bool AutoLogin()
        {
            string token = SecureStorage.LoadToken();
            if (!string.IsNullOrEmpty(token))
            {
                if (IsTokenExpired(token))
                {
                    Logout();
                    return false;
                }

                Token = token;
                UserEmail = SecureStorage.LoadUserEmail();
                Credits = SecureStorage.LoadUserCredits();
                return true;
            }
            return false;
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                string[] parts = token.Split('.');
                if (parts.Length != 3) return true;

                string payload = parts[1];
                string json = Base64UrlDecode(payload);
                var claims = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (claims.ContainsKey("exp"))
                {
                    long exp = Convert.ToInt64(claims["exp"]);
                    DateTime expirationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(exp);
                    return expirationTime < DateTime.UtcNow;
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        private string Base64UrlDecode(string input)
        {
            string output = input;
            output = output.Replace('-', '+');
            output = output.Replace('_', '/');
            switch (output.Length % 4)
            {
                case 0: break;
                case 2: output += "=="; break;
                case 3: output += "="; break;
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output);
            return Encoding.UTF8.GetString(converted);
        }

        private class AuthResponse
        {
            [JsonProperty("token")]
            public string Token { get; set; }
            
            [JsonProperty("credits")]
            public int Credits { get; set; }
            
            [JsonProperty("email")]
            public string Email { get; set; }
        }
    }
}

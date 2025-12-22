using System;
using System.Text;
using System.Threading.Tasks;
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

        private const string ApiBaseUrl = "https://api.greenchainz.com"; // Placeholder URL
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer;

        private AuthService()
        {
            _httpClient = new HttpClient();
            _serializer = new JavaScriptSerializer();
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

                string json = _serializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ApiBaseUrl + "/api/plugin/auth", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    var result = _serializer.Deserialize<AuthResponse>(responseJson);

                    Token = result.token;
                    Credits = result.credits;
                    UserEmail = result.email ?? email;

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
                var claims = _serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);

                if (claims.ContainsKey("exp"))
                {
                    // exp is seconds since epoch
                    long exp = Convert.ToInt64(claims["exp"]);
                    DateTime expirationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(exp);
                    return expirationTime < DateTime.UtcNow;
                }

                return false; // No exp claim, assume valid? Or invalid. Usually JWTs have exp.
            }
            catch
            {
                return true; // If parsing fails, treat as expired
            }
        }

        private string Base64UrlDecode(string input)
        {
            string output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return Encoding.UTF8.GetString(converted);
        }

        private class AuthResponse
        {
            public string token { get; set; }
            public int credits { get; set; }
            public string email { get; set; }
        }
    }
}

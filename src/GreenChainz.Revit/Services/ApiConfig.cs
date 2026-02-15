using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Configuration settings for the GreenChainz API.
    /// </summary>
    public static class ApiConfig
    {
        /// <summary>
        /// The base URL for the GreenChainz API.
        /// Points to the main app production endpoint.
        /// </summary>
        public const string BASE_URL = "https://green-sourcing-b2b-app.vercel.app";

        /// <summary>
        /// The default timeout for API requests in seconds.
        /// </summary>
        public const int TIMEOUT_SECONDS = 30;

        private const string TOKEN_KEY = "GreenChainzAuthToken";

        /// <summary>
        /// Securely saves the authentication token using Windows DPAPI.
        /// </summary>
        public static void SaveAuthToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            try
            {
                // Note: DPAPI is only available on Windows. 
                // In a cross-platform context, this would need a conditional implementation.
                byte[] data = Encoding.UTF8.GetBytes(token);
                byte[] encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                string base64Token = Convert.ToBase64String(encryptedData);

                // Store in user-scoped settings or a local file
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GreenChainz");
                Directory.CreateDirectory(appDataPath);
                File.WriteAllText(Path.Combine(appDataPath, "token.dat"), base64Token);
            }
            catch (Exception)
            {
                // Fallback to environment variable if DPAPI fails (e.g., non-Windows dev environment)
                Environment.SetEnvironmentVariable(TOKEN_KEY, token, EnvironmentVariableTarget.User);
            }
        }

        /// <summary>
        /// Loads the authentication token securely.
        /// </summary>
        public static string LoadAuthToken()
        {
            // 1. Try to load from DPAPI stored file
            try
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GreenChainz");
                string filePath = Path.Combine(appDataPath, "token.dat");
                
                if (File.Exists(filePath))
                {
                    string base64Token = File.ReadAllText(filePath);
                    byte[] encryptedData = Convert.FromBase64String(base64Token);
                    byte[] data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(data);
                }
            }
            catch { /* Ignore and try next method */ }

            // 2. Try to load from environment variable
            string token = Environment.GetEnvironmentVariable(TOKEN_KEY);

            if (string.IsNullOrEmpty(token))
            {
                // 3. Try to load from app configuration (legacy)
                try
                {
                    token = ConfigurationManager.AppSettings[TOKEN_KEY];
                }
                catch
                {
                    token = null;
                }
            }

            return token;
        }
    }
}

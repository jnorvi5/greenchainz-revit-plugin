using System;
using System.Configuration;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Configuration settings for the GreenChainz API.
    /// </summary>
    public static class ApiConfig
    {
        /// <summary>
        /// The base URL for the GreenChainz API.
        /// Defaults to the production API.
        /// </summary>
        public const string BASE_URL = "https://greenchainz.com";

        /// <summary>
        /// The default timeout for API requests in seconds.
        /// </summary>
        public const int TIMEOUT_SECONDS = 30;

        /// <summary>
        /// Loads the authentication token from configuration or environment variables.
        /// </summary>
        /// <returns>The authentication token, or null if not found.</returns>
        public static string LoadAuthToken()
        {
            // Try to load from environment variable first
            string token = Environment.GetEnvironmentVariable("GREENCHAINZ_AUTH_TOKEN");

            if (string.IsNullOrEmpty(token))
            {
                // Try to load from app configuration
                try
                {
                    token = ConfigurationManager.AppSettings["GreenChainzAuthToken"];
                }
                catch
                {
                    // Configuration not available, return null
                    token = null;
                }
            }

            return token;
        }
    }
}

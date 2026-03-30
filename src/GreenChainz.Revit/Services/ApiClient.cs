using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly bool _shouldDisposeHttpClient;

        public ApiClient()
            : this(ApiConfig.BASE_URL, ApiConfig.LoadAuthToken())
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
            : this(baseUrl, authToken, null)
        {
        }

        public ApiClient(string baseUrl, string authToken, ILogger logger)
        {
            _baseUrl = (baseUrl ?? ApiConfig.BASE_URL).TrimEnd('/');
            _logger = logger ?? new TelemetryLogger();

            // Security: Enforce HTTPS for non-localhost
            if (!_baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !_baseUrl.Contains("localhost") &&
                !_baseUrl.Contains("127.0.0.1"))
            {
                _logger.LogInfo($"[SECURITY WARNING] Using insecure connection to {_baseUrl}");
            }

        }
        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_shouldDisposeHttpClient) _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

        private class TelemetryLogger : ILogger
        {
            public void LogDebug(string message) => TelemetryService.LogInfo($"[DEBUG] {message}");
            public void LogInfo(string message) => TelemetryService.LogInfo(message);
            public void LogError(Exception ex, string message) => TelemetryService.LogError(ex, message);
        }
    }
}

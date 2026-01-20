using System;

namespace GreenChainz.Revit.Services
{
    public class FileLogger : ILogger
    {
        public void LogDebug(string message)
        {
            // TelemetryService might not have LogDebug exposed publicly or it maps to LogInfo
            // Assuming LogInfo is the safe fallback based on TelemetryLogger
             TelemetryService.LogInfo($"[DEBUG] {message}");
        }

        public void LogInfo(string message)
        {
            TelemetryService.LogInfo(message);
        }

        public void LogInformation(string message)
        {
            TelemetryService.LogInfo(message);
        }

        public void LogError(Exception ex, string message)
        {
            TelemetryService.LogError(ex, message);
        }

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                TelemetryService.LogError(ex, message);
            }
            else
            {
                TelemetryService.LogError(new Exception(message), "Error");
            }
        }
    }
}

using System;

namespace GreenChainz.Revit.Services
{
    public class TelemetryLogger : ILogger
    {
        public void LogDebug(string message)
        {
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
                TelemetryService.LogError(ex, message);
            else
                TelemetryService.LogInfo($"[ERROR] {message}");
        }
    }
}

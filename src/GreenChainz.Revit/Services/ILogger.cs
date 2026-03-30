using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogInformation(string message); // Alias for LogInfo often used
        void LogError(Exception ex, string message);
    }

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

        public void LogError(Exception ex, string message)
        {
            TelemetryService.LogError(ex, message);
        }
    }
}

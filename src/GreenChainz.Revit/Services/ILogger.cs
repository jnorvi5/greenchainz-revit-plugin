using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogError(Exception ex, string message);
        void LogInfo(string message);
        void LogInformation(string message);
        void LogError(string message, Exception ex = null);
    }
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(string message, Exception ex = null);
    }
        void LogError(Exception ex, string message);
        void LogError(string message, Exception ex = null);
    }

    public class TelemetryLogger : ILogger
    {
        public void LogError(Exception ex, string message)
        {
            TelemetryService.LogError(ex, message);
        public void LogDebug(string message)
        {
            // TelemetryService doesn't have LogDebug, map to LogInfo or ignore.
            // Mapping to LogInfo for visibility as requested by "LogDebug" usage in snippet.
            // Or maybe just ignore if strict debug. But user asked to uncomment it.
            // Let's assume for this environment we log it.
            TelemetryService.LogInfo($"[DEBUG] {message}");
        }

        public void LogInfo(string message)
        {
            TelemetryService.LogInfo(message);
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

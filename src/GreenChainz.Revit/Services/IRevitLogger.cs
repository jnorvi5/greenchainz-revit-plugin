using System;

namespace GreenChainz.Revit.Services
{
    public interface IRevitLogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(Exception ex, string message);
        void LogError(string message);
    }

    public class TelemetryLogger : IRevitLogger
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

        public void LogError(string message)
        {
            TelemetryService.LogError(new Exception(message), message);
        }
    }
}

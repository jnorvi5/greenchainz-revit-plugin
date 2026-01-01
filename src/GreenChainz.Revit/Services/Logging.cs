using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogError(Exception ex, string message);
        void LogInfo(string message);
    }

    public class TelemetryLogger : ILogger
    {
        public void LogError(Exception ex, string message)
        {
            TelemetryService.LogError(ex, message);
        }

        public void LogInfo(string message)
        {
            TelemetryService.LogInfo(message);
        }
    }
}

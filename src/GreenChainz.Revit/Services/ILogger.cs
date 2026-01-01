using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogError(Exception ex, string context);
        void LogInfo(string message);
    }

    public class TelemetryLogger : ILogger
    {
        public void LogError(Exception ex, string context)
        {
            TelemetryService.LogError(ex, context);
        }

        public void LogInfo(string message)
        {
            TelemetryService.LogInfo(message);
        }
    }
}

using System;

namespace GreenChainz.Revit.Services
{
    public class FileLogger : ILogger
    {
        public void LogDebug(string message)
        {
            TelemetryService.LogDebug(message);
        }

        public void LogInfo(string message)
        {
            TelemetryService.LogInfo(message);
        }

        public void LogError(Exception ex, string message)
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

using System;

namespace GreenChainz.Revit.Services
{
    public class TelemetryLogger : ILogger
    {
        public void LogInformation(string message)
        {
            TelemetryService.LogInfo(message);
        }

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                TelemetryService.LogError(ex, message);
            }
            else
            {
                TelemetryService.LogInfo($"ERROR: {message}");
            }
        }
    }
}

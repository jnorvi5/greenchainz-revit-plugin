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

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                TelemetryService.LogError(ex, message);
            }
            else
            {
                // If no exception object, just log the message as error
                // TelemetryService.LogError requires an exception, so we create a dummy one or adapt TelemetryService.
                // For now, let's treat it as Info with ERROR prefix or use a dummy exception.
                // Better approach: Since TelemetryService is simple, we can just write to file directly or adapt.
                // But let's assume TelemetryService.LogError is what we want.
                // We'll pass a generic exception if none provided to satisfy signature.
                TelemetryService.LogError(new Exception(message), "Error");
            }
        }
    }
}

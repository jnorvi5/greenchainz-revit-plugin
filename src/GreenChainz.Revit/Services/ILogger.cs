using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogInformation(string message); // Alias for LogInfo often used
        void LogError(Exception ex, string message);
        void LogError(string message, Exception ex = null);
    }
}

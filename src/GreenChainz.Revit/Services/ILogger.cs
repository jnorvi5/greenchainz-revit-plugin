using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogInformation(string message);
        void LogError(Exception ex, string message);
    }
}

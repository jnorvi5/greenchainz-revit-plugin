using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogError(string message, Exception ex = null);
    }
}

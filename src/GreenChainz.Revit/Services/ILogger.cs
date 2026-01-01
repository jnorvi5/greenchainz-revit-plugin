using System;

namespace GreenChainz.Revit.Services
{
    public interface ILogger
    {
        void LogInformation(string message);
        void LogError(string message, Exception ex = null);
    }
}

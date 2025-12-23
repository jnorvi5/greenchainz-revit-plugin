using System;
using System.IO;

namespace GreenChainz.Revit.Services
{
    public static class TelemetryService
    {
        public static void LogError(Exception ex, string context)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logDir = Path.Combine(appDataPath, "GreenChainz");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logFilePath = Path.Combine(logDir, "logs.txt");
                string message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] Context: {context}\nException: {ex}\n--------------------------------------------------\n";

                File.AppendAllText(logFilePath, message);
            }
            catch
            {
                // Fail silently if we can't log
            }
        }
    }
}

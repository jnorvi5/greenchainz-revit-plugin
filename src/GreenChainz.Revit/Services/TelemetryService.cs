using System;
using System.IO;

namespace GreenChainz.Revit.Services
{
    public static class TelemetryService
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GreenChainz", "logs.txt");

        public static void Initialize()
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        public static void LogError(Exception ex, string context)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now}] [ERROR] {context}: {ex.Message}\n{ex.StackTrace}\n"); } catch { }
        }

        public static void LogInfo(string message)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now}] [INFO] {message}\n"); } catch { }
        }

        public static void LogDebug(string message)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now}] [DEBUG] {message}\n"); } catch { }
        }
    }
}

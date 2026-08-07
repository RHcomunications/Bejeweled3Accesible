using System;
using System.IO;

namespace Bejeweled3Accessible.Engine
{
    // Shared persistence helper: resolves the data directory for the four
    // XML-backed stores (GameProgress, GameOptions, ProfileManager, BadgeManager)
    // so they cannot drift in folder naming or creation logic.
    public static class StoragePaths
    {
        public static string ResolveDataDirectory(string overrideDir)
        {
            string appData = !string.IsNullOrEmpty(overrideDir)
                ? overrideDir
                : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "Bejeweled3Accessible");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetPath(string overrideDir, string fileName)
        {
            return Path.Combine(ResolveDataDirectory(overrideDir), fileName);
        }
    }

    // Best-effort human-readable log of serialization/IO failures so that
    // persistence errors are visible even in Release builds (where the
    // Debug.WriteLine fallback is compiled out).
    public static class PersistenceLog
    {
        public static void Write(Exception ex, string fileName)
        {
            if (ex == null) return;
            try
            {
                string dir = StoragePaths.ResolveDataDirectory(GameProgress.OverrideDataDirectory);
                string logPath = Path.Combine(dir, "persistence_errors.log");
                File.AppendAllText(logPath,
                    string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}: {2}\r\n", DateTime.Now, fileName, ex.Message));
            }
            catch { }
        }
    }
}
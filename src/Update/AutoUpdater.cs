using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;

namespace Bejeweled3Accessible.Update
{
    // Auto-updater: detects the latest release on GitHub without opening the
    // browser, downloads the zip directly, replaces the game folder in place
    // (deleting the previous version so the user runs clean) and relaunches.
    // The swap is done by a hidden .cmd script that waits for this process to
    // end, because a running exe cannot overwrite itself.
    public static class AutoUpdater
    {
        public const string GitHubRepo = "RHcomunications/Bejeweled3Accessible";

        // Zip asset naming must match the release process: Bejeweled3Accesible-<tag>.zip
        public const string ZipAssetPrefix = "Bejeweled3Accesible-";

        static AutoUpdater()
        {
            try
            {
                // GitHub requires TLS 1.2; best effort on modern systems.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch { }
        }

        // Full version of the running assembly, e.g. "2026.8.9.1".
        public static string CurrentVersionString
        {
            get
            {
                try
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    if (v != null) return v.ToString();
                }
                catch { }
                return "0.0.0.0";
            }
        }

        // Parses a release tag ("v2026.8.9.1" or "2026.8.9.1") into a Version.
        public static Version ParseTagVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            string v = tag.Trim();
            if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase)) v = v.Substring(1);
            Version parsed;
            if (Version.TryParse(v, out parsed)) return parsed;
            return null;
        }

        // Compares two tags: >0 if tagA is newer, 0 if equal, <0 if older.
        public static int CompareTagVersions(string tagA, string tagB)
        {
            Version a = ParseTagVersion(tagA);
            Version b = ParseTagVersion(tagB);
            if (a == null || b == null) return 0;
            return a.CompareTo(b);
        }

        // Human-readable version for announcements ("2026.8.9.1").
        public static string DisplayVersion(string tag)
        {
            Version v = ParseTagVersion(tag);
            return v != null ? v.ToString() : (tag ?? "");
        }

        // True when the given tag is newer than the running assembly version.
        public static bool IsNewerThanCurrent(string latestTag)
        {
            Version latest = ParseTagVersion(latestTag);
            if (latest == null) return false;
            try
            {
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                return latest > current;
            }
            catch { return false; }
        }

        // Resolves the latest release tag by following the /releases/latest
        // redirect and reading its Location header (no page download needed).
        public static string GetLatestTag(int timeoutMs = 10000)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
                    "https://github.com/" + GitHubRepo + "/releases/latest");
                req.Method = "GET";
                req.AllowAutoRedirect = false;
                req.Timeout = timeoutMs;
                req.UserAgent = "Bejeweled3Accessible-Updater/" + CurrentVersionString;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    int code = (int)resp.StatusCode;
                    if (code >= 300 && code < 400)
                    {
                        string location = resp.Headers["Location"];
                        if (!string.IsNullOrEmpty(location))
                        {
                            int idx = location.LastIndexOf("/tag/", StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0) return location.Substring(idx + 5).TrimEnd('/');
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public class UpdateDownloadResult
        {
            public string Error;       // null when everything is ready
            public string ScriptPath;  // hidden .cmd to run before exiting the game
        }

        // Downloads the release zip for `tag` into %TEMP%, extracts it and
        // writes the updater script. Returns an UpdateDownloadResult; Error is
        // null when the update is ready to install.
        public static UpdateDownloadResult PrepareUpdate(string tag, string exeDir)
        {
            UpdateDownloadResult result = new UpdateDownloadResult();
            try
            {
                Version v = ParseTagVersion(tag);
                if (v == null) { result.Error = "etiqueta de version no valida: " + tag; return result; }
                if (string.IsNullOrEmpty(exeDir) || !Directory.Exists(exeDir))
                { result.Error = "carpeta del juego no encontrada"; return result; }

                string tmp = Path.GetTempPath();
                string safeTag = SanitizeTag(tag);
                string zipPath = Path.Combine(tmp, "B3A_Update_" + safeTag + ".zip");
                string extractDir = Path.Combine(tmp, "B3A_Update_" + safeTag);
                string cmdPath = Path.Combine(tmp, "B3A_Update_" + safeTag + ".cmd");

                if (!File.Exists(zipPath))
                {
                    string url = "https://github.com/" + GitHubRepo
                        + "/releases/download/" + tag + "/" + ZipAssetPrefix + tag + ".zip";
                    using (WebClient client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] =
                            "Bejeweled3Accessible-Updater/" + CurrentVersionString;
                        client.DownloadFile(url, zipPath);
                    }
                }

                if (!File.Exists(zipPath) || new FileInfo(zipPath).Length == 0)
                { result.Error = "descarga vacia o fallida"; return result; }

                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                if (!File.Exists(Path.Combine(extractDir, "Bejeweled3Accessible.exe")))
                { result.Error = "el paquete no contiene Bejeweled3Accessible.exe"; return result; }

                File.WriteAllText(cmdPath, BuildUpdateScript(exeDir, extractDir, zipPath), Encoding.ASCII);
                result.ScriptPath = cmdPath;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        // Only letters, digits, dots and hyphens survive the tag -> file name.
        private static string SanitizeTag(string tag)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in tag)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_') sb.Append(c);
            }
            return sb.ToString();
        }

        // The script waits for the game to close, wipes the game folder, copies
        // the new release over it, relaunches the game and cleans the temp
        // files. The working directory must leave the game folder before the
        // wipe, otherwise Windows refuses to delete a directory in use.
        private static string BuildUpdateScript(string exeDir, string extractDir, string zipPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal");
            sb.AppendLine("cd /d \"%TEMP%\"");
            sb.AppendLine("set \"DST=" + exeDir + "\"");
            sb.AppendLine("set \"SRC=" + extractDir + "\"");
            sb.AppendLine(":wait");
            sb.AppendLine("tasklist /fi \"IMAGENAME eq Bejeweled3Accessible.exe\" | find /i \"Bejeweled3Accessible.exe\" >nul");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("    ping 127.0.0.1 -n 2 >nul");
            sb.AppendLine("    goto wait");
            sb.AppendLine(")");
            sb.AppendLine("rd /s /q \"%DST%\" 2>nul");
            sb.AppendLine("mkdir \"%DST%\"");
            sb.AppendLine("xcopy /e /i /y \"%SRC%\" \"%DST%\" >nul");
            sb.AppendLine("start \"\" \"%DST%\\Bejeweled3Accessible.exe\"");
            sb.AppendLine("cd /d \"%TEMP%\"");
            sb.AppendLine("rd /s /q \"" + extractDir + "\" 2>nul");
            sb.AppendLine("del /f /q \"" + zipPath + "\" 2>nul");
            sb.AppendLine("(goto) 2>nul & del \"%~f0\"");
            return sb.ToString();
        }
    }
}

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
        public const string GitHubRepo = "RHcomunications/Bejeweled3Accesible";

        // Zip asset naming must match the release process: Bejeweled3Accesible-<version>.zip
        // (the tag keeps the "v" but the asset name does not).
        public const string ZipAssetPrefix = "Bejeweled3Accesible-";

        // e.g. BuildZipAssetName("v2026.8.9.1") -> "Bejeweled3Accesible-2026.8.9.1.zip"
        public static string BuildZipAssetName(string tag)
        {
            return ZipAssetPrefix + DisplayVersion(tag) + ".zip";
        }

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
            if (v.StartsWith("android-", StringComparison.OrdinalIgnoreCase)) v = v.Substring(8).Trim();
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
            ReleaseInfo release = GetLatestRelease(timeoutMs);
            return release != null ? release.Tag : null;
        }

        public class ReleaseInfo
        {
            public string Tag;    // e.g. "v2026.8.10.0" (null when unreachable)
            public string Notes;  // raw release body (may be null)
            public bool IsValid { get { return !string.IsNullOrEmpty(Tag); } }
        }

        // Queries the GitHub API for the latest release: returns the tag and
        // the raw release notes body. When the API is unavailable (rate limit,
        // network block) it falls back to following the /releases/latest
        // redirect, which needs no API. Best effort: never throws, and every
        // check leaves a diagnostic line in %TEMP%\B3A_update_check.log.
        public static ReleaseInfo GetLatestRelease(int timeoutMs = 10000)
        {
            ReleaseInfo info = new ReleaseInfo();
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
                    "https://api.github.com/repos/" + GitHubRepo + "/releases/latest");
                req.Method = "GET";
                req.Accept = "application/vnd.github+json";
                req.Timeout = timeoutMs;
                req.UserAgent = "Bejeweled3Accessible-Updater/" + CurrentVersionString;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    info.Tag = ReadJsonString(json, "tag_name");
                    info.Notes = ReadJsonString(json, "body");
                }
            }
            catch (Exception ex) { Log("api.github.com fallo: " + ex.Message); }

            if (!info.IsValid)
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
                        "https://github.com/" + GitHubRepo + "/releases/latest");
                    req.Method = "HEAD";
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
                                if (idx >= 0)
                                {
                                    info.Tag = location.Substring(idx + 5).TrimEnd('/');
                                    info.Notes = null;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log("redirect github.com fallo: " + ex.Message); }
            }

            Log("resultado: " + (info.IsValid ? info.Tag : "sin release (modo diagnostico)"));
            return info;
        }

        // One-line diagnostic log for support: %TEMP%\B3A_update_check.log
        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "B3A_update_check.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
            }
            catch { }
        }

        // Reads "key":"value" from a JSON object, decoding the standard escapes.
        private static string ReadJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string needle = "\"" + key + "\":\"";
            int start = json.IndexOf(needle, StringComparison.Ordinal);
            if (start < 0) return null;
            start += needle.Length;
            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (escaped)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            if (i + 4 < json.Length)
                            {
                                string hex = json.Substring(i + 1, 4);
                                try { sb.Append((char)Convert.ToInt32(hex, 16)); i += 4; }
                                catch { }
                            }
                            break;
                        default: sb.Append(c); break;
                    }
                    escaped = false;
                }
                else if (c == '\\') escaped = true;
                else if (c == '"') break;
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Notes are trimmed to keep screen-reader announcements short.
        public const int MaxNotesChars = 1400;

        // Returns the release notes section for the requested language. The
        // release body carries "#ES" and "#EN" marker lines (case insensitive)
        // and only the matching block is returned; when the markers are missing
        // the whole body is used as a fallback. Long notes are trimmed.
        public static string ExtractNotes(string body, bool spanish)
        {
            if (string.IsNullOrEmpty(body)) return "";
            string[] lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            StringBuilder sb = new StringBuilder();
            bool inBlock = false;
            foreach (string rawLine in lines)
            {
                string trimmed = rawLine.Trim();
                bool isMarker = trimmed.Length >= 3 && trimmed[0] == '#'
                    && (trimmed.Substring(1).Trim().ToUpperInvariant() == "ES"
                        || trimmed.Substring(1).Trim().ToUpperInvariant() == "EN");
                if (isMarker)
                {
                    inBlock = trimmed.Substring(1).Trim().ToUpperInvariant() == (spanish ? "ES" : "EN");
                    continue;
                }
                if (inBlock && trimmed.Length > 0)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(trimmed);
                }
            }
            if (sb.Length == 0)
            {
                foreach (string rawLine in lines)
                {
                    string t = rawLine.Trim();
                    if (t.Length == 0) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(t);
                }
            }
            string text = sb.ToString().Trim();
            if (text.Length > MaxNotesChars) text = text.Substring(0, MaxNotesChars).TrimEnd() + "...";
            return text;
        }

        // Human-readable byte count for announcements, e.g. "186 megabytes",
        // "1,9 gigabytes" (es) / "1.9 gigabytes" (en). Decimals use a comma in
        // Spanish and a dot in English, matching how each language is read.
        public static string FormatBytes(long bytes, bool spanish)
        {
            if (bytes < 0) bytes = 0;
            if (bytes >= 1073741824L)
            {
                double gb = bytes / 1073741824.0;
                if (Math.Round(gb, 1) == 1.0) return "1 " + (spanish ? "gigabyte" : "gigabyte");
                string s = gb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                if (spanish) s = s.Replace('.', ',');
                return s + " " + (spanish ? "gigabytes" : "gigabytes");
            }
            if (bytes >= 1048576L)
            {
                long mb = (long)Math.Round(bytes / 1048576.0);
                return mb + " " + (mb == 1 ? (spanish ? "megabyte" : "megabyte") : (spanish ? "megabytes" : "megabytes"));
            }
            if (bytes >= 1024L)
            {
                long kb = (long)Math.Round(bytes / 1024.0);
                return kb + " " + (kb == 1 ? (spanish ? "kilobyte" : "kilobyte") : (spanish ? "kilobytes" : "kilobytes"));
            }
            return bytes + " " + (bytes == 1 ? (spanish ? "byte" : "byte") : (spanish ? "bytes" : "bytes"));
        }

        // Download speed, e.g. "5 megabytes por segundo" or "1,5 megabytes por
        // segundo". One decimal below 10 of the current unit, integers above.
        public static string FormatSpeed(double bytesPerSecond, bool spanish)
        {
            if (bytesPerSecond < 0) bytesPerSecond = 0;
            string suffix = spanish ? "por segundo" : "per second";
            if (bytesPerSecond >= 1048576.0)
            {
                double mb = bytesPerSecond / 1048576.0;
                string unit = spanish ? "megabytes" : "megabytes";
                if (mb >= 10.0 || Math.Round(mb, 1) == Math.Floor(mb))
                    return (long)Math.Round(mb) + " " + unit + " " + suffix;
                string s = mb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                if (spanish) s = s.Replace('.', ',');
                return s + " " + unit + " " + suffix;
            }
            if (bytesPerSecond >= 1024.0)
            {
                double kb = bytesPerSecond / 1024.0;
                string unit = spanish ? "kilobytes" : "kilobytes";
                if (kb >= 10.0 || Math.Round(kb, 1) == Math.Floor(kb))
                    return (long)Math.Round(kb) + " " + unit + " " + suffix;
                string s = kb.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                if (spanish) s = s.Replace('.', ',');
                return s + " " + unit + " " + suffix;
            }
            return (long)bytesPerSecond + " " + (spanish ? "bytes" : "bytes") + " " + suffix;
        }

        // Remaining time, e.g. "45 segundos", "1 minuto y 15 segundos".
        public static string FormatDuration(double seconds, bool spanish)
        {
            if (seconds < 0) seconds = 0;
            if (seconds < 1.0) return spanish ? "menos de 1 segundo" : "less than 1 second";
            long total = (long)Math.Round(seconds);
            long min = total / 60;
            long sec = total % 60;
            if (min == 0)
                return sec + " " + (sec == 1 ? (spanish ? "segundo" : "second") : (spanish ? "segundos" : "seconds"));
            string minuteWord = min == 1 ? (spanish ? "minuto" : "minute") : (spanish ? "minutos" : "minutes");
            if (sec == 0) return min + " " + minuteWord;
            return min + " " + minuteWord + (spanish ? " y " : " and ") + sec + " "
                + (sec == 1 ? (spanish ? "segundo" : "second") : (spanish ? "segundos" : "seconds"));
        }

        // Downloads a file reporting progress. MUST use the async WebClient
        // API: the synchronous DownloadFile never raises DownloadProgressChanged
        // in .NET Framework, so the UI would not receive a single event.
        // Throws the original exception (unwrapped from AggregateException).
        public static void DownloadToFile(string url, string destPath,
            Action<DownloadProgressChangedEventArgs> progressCallback = null)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] =
                    "Bejeweled3Accessible-Updater/" + CurrentVersionString;
                if (progressCallback != null)
                    client.DownloadProgressChanged += (s, e) => progressCallback(e);
                try
                {
                    client.DownloadFileTaskAsync(url, destPath).Wait();
                }
                catch (AggregateException ag)
                {
                    Exception inner = ag.InnerException;
                    while (inner is AggregateException) inner = inner.InnerException;
                    throw inner ?? ag;
                }
            }
        }

        public class UpdateDownloadResult
        {
            public string Error;       // null when everything is ready
            public string ScriptPath;  // hidden .cmd to run before exiting the game
        }

        // Downloads the release zip for `tag` into %TEMP%, extracts it and
        // writes the updater script. Returns an UpdateDownloadResult; Error is
        // null when the update is ready to install. progressCallback (optional)
        // receives DownloadProgressChanged events while the zip is downloaded,
        // so the UI can announce progress to the user.
        public static UpdateDownloadResult PrepareUpdate(string tag, string exeDir,
            Action<DownloadProgressChangedEventArgs> progressCallback = null)
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
                        + "/releases/download/" + tag + "/" + BuildZipAssetName(tag);
                    Exception lastError = null;
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            DownloadToFile(url, zipPath, progressCallback);
                            lastError = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                            System.Threading.Thread.Sleep(400); // reintenta: el archivo puede estar en uso un instante
                        }
                    }
                    if (lastError != null)
                    {
                        try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                        result.Error = "descarga fallida: " + lastError.Message;
                        return result;
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
            sb.AppendLine("if errorlevel 1 goto continue");
            sb.AppendLine("    ping 127.0.0.1 -n 2 >nul");
            sb.AppendLine("    goto wait");
            sb.AppendLine(":continue");
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

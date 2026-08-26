using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Widget;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.AndroidApp.Accessibility;

namespace Bejeweled3Accessible.AndroidApp.Update
{
    public static class AndroidAutoUpdater
    {
        public const string GitHubRepo = "RHcomunications/Bejeweled3Accesible";
        public const string CurrentVersion = "2026.08.25.6";

        public class AndroidReleaseInfo
        {
            public string Tag;
            public string Notes;
            public string DownloadUrl;
            public bool IsNewer;
        }

        public static async Task<AndroidReleaseInfo> CheckForUpdatesAsync()
        {
            return await Task.Run(() =>
            {
                AndroidReleaseInfo info = new AndroidReleaseInfo();
                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                    // Buscamos especificamente la release de Android de mayor
                    // version, sin depender del marcador "Latest" del repositorio
                    // (que puede ser la release de Windows). Asi cada actualizador
                    // vive en su propia "rama" de plataforma.
                    AndroidReleaseInfo android = FetchLatestAndroidViaList();
                    if (android != null) return android;

                    // Fallback: lo que devuelva /releases/latest (aunque no sea Android).
                    AndroidReleaseInfo latest = FetchReleaseFromUrl(
                        "https://api.github.com/repos/" + GitHubRepo + "/releases/latest");
                    if (latest != null) return latest;
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("BejeweledUpdater", "Error al verificar actualizaciones: " + ex.Message);
                }

                return info;
            });
        }

        private static AndroidReleaseInfo ParseReleaseJson(string json)
        {
            AndroidReleaseInfo info = new AndroidReleaseInfo();
            if (string.IsNullOrEmpty(json)) return info;

            info.Tag = ReadJsonString(json, "tag_name");
            info.Notes = ReadJsonString(json, "body");

            int browserDownloadIdx = json.IndexOf("\"browser_download_url\":\"", StringComparison.Ordinal);
            while (browserDownloadIdx >= 0)
            {
                int urlStart = browserDownloadIdx + 24;
                int urlEnd = json.IndexOf("\"", urlStart, StringComparison.Ordinal);
                if (urlEnd > urlStart)
                {
                    string url = json.Substring(urlStart, urlEnd - urlStart);
                    if (url.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        info.DownloadUrl = url;
                        break;
                    }
                }
                browserDownloadIdx = json.IndexOf("\"browser_download_url\":\"", urlEnd, StringComparison.Ordinal);
            }

            if (!string.IsNullOrEmpty(info.Tag))
            {
                string cleanTag = info.Tag.TrimStart('v', 'V');
                if (cleanTag.StartsWith("android-", StringComparison.OrdinalIgnoreCase))
                    cleanTag = cleanTag.Substring(8).TrimStart('v', 'V');

                if (Version.TryParse(cleanTag, out Version latestVer) &&
                    Version.TryParse(CurrentVersion, out Version currentVer))
                {
                    info.IsNewer = latestVer > currentVer;
                }
            }
            return info;
        }

        private static AndroidReleaseInfo FetchReleaseFromUrl(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Accept = "application/vnd.github+json";
                req.Timeout = 10000;
                req.UserAgent = "Bejeweled3Accessible-AndroidUpdater/" + CurrentVersion;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    return ParseReleaseJson(reader.ReadToEnd());
                }
            }
            catch { return null; }
        }

        // Enumera las releases y devuelve la de Android (tag android-...) de mayor
        // version, consultando sus notas y el .apk con un segundo request puntual.
        private static AndroidReleaseInfo FetchLatestAndroidViaList()
        {
            string bestTag = null;
            Version bestVer = null;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
                    "https://api.github.com/repos/" + GitHubRepo + "/releases?per_page=50");
                req.Method = "GET";
                req.Accept = "application/vnd.github+json";
                req.Timeout = 10000;
                req.UserAgent = "Bejeweled3Accessible-AndroidUpdater/" + CurrentVersion;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    int i = 0;
                    while ((i = json.IndexOf("\"tag_name\"", i, StringComparison.Ordinal)) >= 0)
                    {
                        int colon = json.IndexOf(':', i);
                        if (colon < 0) break;
                        int q1 = json.IndexOf('"', colon + 1);
                        if (q1 < 0) break;
                        int q2 = json.IndexOf('"', q1 + 1);
                        if (q2 < 0) break;
                        string tag = json.Substring(q1 + 1, q2 - q1 - 1);
                        i = q2 + 1;

                        string clean = tag.TrimStart('v', 'V');
                        if (!clean.StartsWith("android-", StringComparison.OrdinalIgnoreCase)) continue;
                        string verStr = clean.Substring(8).TrimStart('v', 'V');
                        if (!Version.TryParse(verStr, out Version v)) continue;
                        if (bestVer == null || v > bestVer)
                        {
                            bestVer = v;
                            bestTag = tag;
                        }
                    }
                }
            }
            catch { return null; }

            return bestTag == null ? null : FetchReleaseFromUrl(
                "https://api.github.com/repos/" + GitHubRepo + "/releases/tags/" + bestTag);
        }

        public static void OpenDownloadOrRelease(Context context, AndroidReleaseInfo info)
        {
            try
            {
                string targetUrl;
                if (!string.IsNullOrEmpty(info.DownloadUrl))
                    targetUrl = info.DownloadUrl;
                else if (!string.IsNullOrEmpty(info.Tag))
                    targetUrl = "https://github.com/" + GitHubRepo + "/releases/tag/" + info.Tag;
                else
                    targetUrl = "https://github.com/" + GitHubRepo + "/releases";

                Intent browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(targetUrl));
                browserIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(browserIntent);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("BejeweledUpdater", "Error abriendo navegador: " + ex.Message);
            }
        }

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
                        case '"': sb.Append('\"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        default: sb.Append(c); break;
                    }
                    escaped = false;
                }
                else if (c == '\\') escaped = true;
                else if (c == '\"') break;
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}

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
        public const string CurrentVersion = "2026.08.25.0";

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
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
                        "https://api.github.com/repos/" + GitHubRepo + "/releases/latest");
                    req.Method = "GET";
                    req.Accept = "application/vnd.github+json";
                    req.Timeout = 10000;
                    req.UserAgent = "Bejeweled3Accessible-AndroidUpdater/" + CurrentVersion;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
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
                    }

                    if (!string.IsNullOrEmpty(info.Tag))
                    {
                        string cleanTag = info.Tag.TrimStart('v', 'V');
                        if (cleanTag.StartsWith("android-", StringComparison.OrdinalIgnoreCase))
                        {
                            cleanTag = cleanTag.Substring(8).TrimStart('v', 'V');
                        }

                        if (Version.TryParse(cleanTag, out Version latestVer) &&
                            Version.TryParse(CurrentVersion, out Version currentVer))
                        {
                            info.IsNewer = latestVer > currentVer;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("BejeweledUpdater", "Error al verificar actualizaciones: " + ex.Message);
                }

                return info;
            });
        }

        public static void OpenDownloadOrRelease(Context context, AndroidReleaseInfo info)
        {
            try
            {
                string targetUrl = !string.IsNullOrEmpty(info.DownloadUrl) 
                    ? info.DownloadUrl 
                    : ("https://github.com/" + GitHubRepo + "/releases/latest");

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

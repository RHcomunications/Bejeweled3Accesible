using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Audio
{
    public class PacReader
    {
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public PacReader(string pacFilePath)
        {
            if (!File.Exists(pacFilePath)) return;

            try
            {
                using (FileStream fs = new FileStream(pacFilePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    char[] magic = reader.ReadChars(4);
                    if (new string(magic) != "PAC1") return;

                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();
                        byte[] encryptedData = reader.ReadBytes(length);

                        // Decrypt data with XOR Key
                        PacCipher.Xor(encryptedData);

                        _files[fileName] = encryptedData;
                        _files[fileName.ToLower()] = encryptedData;
                        string justName = Path.GetFileName(fileName);
                        _files[justName] = encryptedData;
                        _files[justName.ToLower()] = encryptedData;
                        string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                        _files[nameNoExt] = encryptedData;
                        _files[nameNoExt.ToLower()] = encryptedData;
                    }
                }
            }
            catch { }
        }

        public byte[] GetFileBytes(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            if (_files.ContainsKey(resourceName)) return _files[resourceName];
            string lower = resourceName.ToLower();
            if (_files.ContainsKey(lower)) return _files[lower];
            string clean = Path.GetFileNameWithoutExtension(resourceName).ToLower();
            if (_files.ContainsKey(clean)) return _files[clean];
            return null;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        public string ExtractToTempFile(string resourceName)
        {
            byte[] bytes = GetFileBytes(resourceName);
            if (bytes == null) return null;

            string safeName = Path.GetFileName(resourceName);
            string appTempDir = Path.Combine(Path.GetTempPath(), "Bejeweled3Audio");
            if (!Directory.Exists(appTempDir)) Directory.CreateDirectory(appTempDir);

            string tempPath = Path.Combine(appTempDir, safeName);
            try
            {
                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length != bytes.Length)
                {
                    File.WriteAllBytes(tempPath, bytes);
                }

                // Convert to Short 8.3 Path for MCI compatibility on all Windows machines
                StringBuilder shortPathBuilder = new StringBuilder(260);
                uint result = GetShortPathName(tempPath, shortPathBuilder, (uint)shortPathBuilder.Capacity);
                if (result > 0)
                {
                    return shortPathBuilder.ToString();
                }
                return tempPath;
            }
            catch
            {
                string fallbackPath = Path.Combine(Path.GetTempPath(), "bj3_" + Guid.NewGuid().ToString("N") + "_" + safeName);
                File.WriteAllBytes(fallbackPath, bytes);
                return fallbackPath;
            }
        }
    }
}

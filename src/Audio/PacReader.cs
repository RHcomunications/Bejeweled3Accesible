using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Audio
{
    // Lazy PAC reader: the constructor only reads the index (file name +
    // offset + length), so building the engine does NOT decompress the whole
    // archive (~180 MB) into RAM. Bytes are decrypted on first request and
    // cached from then on, which keeps the audio used by the playing modes
    // resident but avoids the startup cost/memory of decoding everything.
    public class PacReader : IDisposable
    {
        private sealed class PacEntry
        {
            public long Offset;
            public int Length;
            public byte[] Data;
        }

        private readonly Dictionary<string, PacEntry> _files =
            new Dictionary<string, PacEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly string _pacFilePath;
        private readonly object _sync = new object();

        public PacReader(string pacFilePath)
        {
            _pacFilePath = pacFilePath;
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
                        long offset = reader.BaseStream.Position;

                        PacEntry entry = new PacEntry { Offset = offset, Length = length };
                        _files[fileName] = entry;
                        _files[fileName.ToLower()] = entry;
                        string justName = Path.GetFileName(fileName);
                        _files[justName] = entry;
                        _files[justName.ToLower()] = entry;
                        string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                        _files[nameNoExt] = entry;
                        _files[nameNoExt.ToLower()] = entry;

                        // Skip the payload; it is decoded on demand.
                        reader.BaseStream.Seek(length, SeekOrigin.Current);
                    }
                }
            }
            catch { }
        }

        public byte[] GetFileBytes(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;

            lock (_sync)
            {
                PacEntry entry;
                if (_files.TryGetValue(resourceName, out entry))
                    return LoadEntry(entry);
                string lower = resourceName.ToLower();
                if (_files.TryGetValue(lower, out entry))
                    return LoadEntry(entry);
                string clean = Path.GetFileNameWithoutExtension(resourceName).ToLower();
                if (_files.TryGetValue(clean, out entry))
                    return LoadEntry(entry);
                return null;
            }
        }

        private byte[] LoadEntry(PacEntry entry)
        {
            if (entry.Data != null) return entry.Data;

            // Read per request (no persistent handle) so the archive can be
            // replaced/rebuilt meanwhile and temp copies are never locked.
            byte[] encrypted = new byte[entry.Length];
            using (FileStream fs = new FileStream(_pacFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(entry.Offset, SeekOrigin.Begin);
                int read = 0;
                while (read < entry.Length)
                {
                    int n = fs.Read(encrypted, read, entry.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
            }

            // Decrypt data with XOR Key
            PacCipher.Xor(encrypted);

            entry.Data = encrypted;
            return entry.Data;
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

        public void Dispose()
        {
            // Lazy reader keeps no persistent file handle; just drop the cached
            // decoded buffers so the engine frees the audio memory on shutdown.
            lock (_sync)
            {
                foreach (PacEntry entry in _files.Values)
                {
                    entry.Data = null;
                }
                _files.Clear();
            }
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Audio
{
    public class PacPacker
    {
        public static void PackDirectoriesToSinglePac(string baseDir, string outputPacFile, params string[] subDirectories)
        {
            List<string> allFiles = new List<string>();
            foreach (var sub in subDirectories)
            {
                string targetDir = Path.Combine(baseDir, sub);
                if (Directory.Exists(targetDir))
                {
                    allFiles.AddRange(Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories));
                }
            }

            if (allFiles.Count == 0) return;

            using (FileStream fs = new FileStream(outputPacFile, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Header magic: 'P', 'A', 'C', '1'
                writer.Write(new char[] { 'P', 'A', 'C', '1' });
                writer.Write(allFiles.Count);

                foreach (var file in allFiles)
                {
                    // Las pistas 01-23 de musica son offsets del modulo
                    // Bejeweled3_suite.mo3 y no se reproducen como ficheros
                    // sueltos: no las empaquetamos para no inflar el PAC
                    // (~150 MB de MP3 muertos). Se mantienen el modulo .mo3 y
                    // las pistas ambientales 24-29 (ficheros independientes).
                    if (IsRedundantModuleMp3(file, baseDir)) continue;

                    string relativePath = file.Substring(baseDir.Length).TrimStart('\\', '/');
                    byte[] fileData = File.ReadAllBytes(file);

                    // Encrypt file bytes with XOR key
                    PacCipher.Xor(fileData);

                    writer.Write(relativePath);
                    writer.Write(fileData.Length);
                    writer.Write(fileData);
                }
            }
        }

        // Una pista de musica es redundante (no se empaqueta) si es un .mp3
        // dentro de music\ cuyo nombre corresponde a un offset del modulo
        // real (pistas 01-23). Las ambientales 24-29 y el propio .mo3 se
        // conservan.
        private static bool IsRedundantModuleMp3(string file, string baseDir)
        {
            if (!file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return false;
            string rel = file.Substring(baseDir.Length).Replace('/', '\\').TrimStart('\\');
            if (!rel.StartsWith("music\\", StringComparison.OrdinalIgnoreCase)) return false;
            string name = Path.GetFileNameWithoutExtension(file);
            return MusicMap.OrderForTrack(name) >= 0;
        }
    }
}

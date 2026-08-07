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
    }
}

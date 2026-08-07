using System;
using System.IO;
using System.Windows.Forms;
using Bejeweled3Accessible.UI;

namespace Bejeweled3Accessible
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[] cmdArgs = Environment.GetCommandLineArgs();
            bool repackOnly = cmdArgs.Length > 1 &&
                string.Equals(cmdArgs[1], "--pack-audio", StringComparison.OrdinalIgnoreCase);

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string audioPac = Path.Combine(baseDir, "audio.pac");

                if (repackOnly || !File.Exists(audioPac))
                {
                    Audio.PacPacker.PackDirectoriesToSinglePac(baseDir, audioPac, "sounds", "music");
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pack_error.txt"), ex.ToString());
            }

            if (repackOnly) return;

            Application.Run(new MainWindow());
        }
    }
}

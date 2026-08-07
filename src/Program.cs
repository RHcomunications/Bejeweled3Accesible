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
                // Never start in silent mode: audio is the backbone of the
                // accessible experience, so tell the user what happened instead
                // of silently launching without sound.
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pack_error.txt"), ex.ToString());
                }
                catch { }
                MessageBox.Show(
                    "No se pudo generar el paquete de audio (audio.pac). El juego necesita el audio para su accesibilidad y se cerrará.\r\n\r\n" +
                    "Could not generate the audio package (audio.pac). The game needs audio for accessibility and will close.\r\n\r\n" +
                    "Detalles / Details: pack_error.txt",
                    "Bejeweled 3 Accesible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (repackOnly) return;

            Application.Run(new MainWindow());
        }
    }
}

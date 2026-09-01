using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Bejeweled3Accessible.UI;

namespace Bejeweled3Accessible
{
    static class Program
    {
        // Mutex global para impedir varias instancias del juego en la misma
        // maquina (una sola copia a la vez, en cualquier sesion de usuario).
        private const string SingleInstanceMutexName = @"Global\Bejeweled3Accessible-SingleInstance";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string[] cmdArgs = Environment.GetCommandLineArgs();
            bool repackOnly = cmdArgs.Length > 1 &&
                string.Equals(cmdArgs[1], "--pack-audio", StringComparison.OrdinalIgnoreCase);

            // Instancia unica: si ya hay otra corriendo, traemos su ventana al
            // frente y salimos. Esto protege de lanzamientos accidentales
            // multiples (p. ej. doble clic repetido en el acceso directo).
            Mutex singleInstance;
            bool owned;
            try
            {
                singleInstance = new Mutex(true, SingleInstanceMutexName, out owned);
            }
            catch
            {
                // Si no se puede crear el mutex (permisos), degradamos: el juego
                // arranca igualmente, sin la guarda de instancia unica.
                singleInstance = null;
                owned = true;
            }

            if (singleInstance != null && !owned)
            {
                if (!repackOnly)
                    BringExistingInstanceToFront();
                return;
            }

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

            // Manejador global: ninguna excepcion debe cerrar el juego en silencio.
            // En el hilo de UI (WinForms) la capturamos y el juego sigue vivo tras
            // avisar; en cualquier otro hilo registramos el motivo antes de morir.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, ev) => ReportFatal("UI", ev.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => ReportFatal("Dominio", ev.ExceptionObject as Exception);

            Application.Run(new MainWindow());
        }

        // Activa la ventana de la instancia ya en ejecucion (la trae al frente
        // y la restaura si esta minimizada) para que el usuario la vea en lugar
        // de lanzar una copia nueva.
        private static void BringExistingInstanceToFront()
        {
            string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            IntPtr found = IntPtr.Zero;
            uint pid;
            EnumWindows((hWnd, lparam) =>
            {
                GetWindowThreadProcessId(hWnd, out pid);
                if (pid == (uint)Process.GetCurrentProcess().Id)
                    return true;
                try
                {
                    using (Process proc = Process.GetProcessById((int)pid))
                    {
                        if (!proc.ProcessName.Equals(exeName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { return true; }

                int len = GetWindowTextLength(hWnd);
                if (len <= 0) return true;
                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().IndexOf("Bejeweled 3 Acc", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                if (IsIconic(found))
                    ShowWindow(found, SW_RESTORE);
                SetForegroundWindow(found);
            }
        }

        // Registra cualquier excepcion fatal en un log y la muestra al usuario,
        // para que el juego no desaparezca sin explicacion (facilita el diagnostico).
        private static void ReportFatal(string source, Exception ex)
        {
            try
            {
                string log = Path.Combine(Path.GetTempPath(), "B3A_crash.log");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  [" + source + "]");
                sb.AppendLine(ex != null ? ex.ToString() : "(sin excepcion)");
                sb.AppendLine(new string('-', 60));
                File.AppendAllText(log, sb.ToString());
            }
            catch { }
            try
            {
                MessageBox.Show(
                    "Se produjo un error inesperado (registrado en B3A_crash.log):\r\n\r\n" +
                    (ex != null ? ex.GetType().Name + ": " + ex.Message : "(sin detalles)"),
                    "Bejeweled 3 Accesible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch { }
        }

        private const int SW_RESTORE = 9;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}

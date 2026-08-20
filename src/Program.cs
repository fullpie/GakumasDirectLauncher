using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GakumasSmartLauncher
{
    internal static class Program
    {
        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [STAThread]
        private static int Main(string[] args)
        {
            var normalized = args.Select(value => value.ToLowerInvariant()).ToArray();
            var environment = LauncherEnvironment.CreateDefault();
            var uiText = UiStrings.Detect();

            if (normalized.Contains("--status"))
            {
                AttachConsole(AttachParentProcess);
                Console.WriteLine(environment.GetStatusJson());
                return 0;
            }

            if (normalized.Contains("--sync"))
            {
                AttachConsole(AttachParentProcess);
                try
                {
                    environment.SyncLatest(null);
                    Console.WriteLine(environment.GetStatusJson());
                    return 0;
                }
                catch (LauncherException ex)
                {
                    Console.Error.WriteLine(SafeLogger.Sanitize(ex.Message));
                    return 2;
                }
            }

            var autoLaunch = normalized.Contains("--launch") && !normalized.Contains("--manage");
            var skipElevation = normalized.Contains("--no-elevation");

            bool ownsMutex;
            using (var mutex = new Mutex(true, "Local\\GakumasSmartLauncher.Gui", out ownsMutex))
            {
                if (!ownsMutex)
                {
                    MessageBox.Show(
                        uiText.AnotherInstance,
                        uiText.WindowTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate
                {
                    MessageBox.Show(
                        uiText.UnexpectedError,
                        uiText.WindowTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                };
                Application.Run(new MainForm(environment, autoLaunch, skipElevation));
            }

            return 0;
        }
    }
}

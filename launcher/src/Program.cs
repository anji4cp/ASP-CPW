using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AspCpwLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EnableModernBrowserEngine();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (LaunchOriginalLauncherWhenIntegrated(args)) return;
            if (args.Length == 1 && args[0] == "--smoke-test")
            {
                using (LauncherForm form = new LauncherForm()) form.CreateControl();
                return;
            }
            if (args.Length == 2 && args[0] == "--render-test")
            {
                LauncherForm preview = new LauncherForm();
                Timer capture = new Timer { Interval = 5000 };
                capture.Tick += delegate
                {
                    capture.Stop();
                    using (Bitmap image = new Bitmap(preview.Width, preview.Height))
                    {
                        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.Size));
                        image.Save(args[1], ImageFormat.Png);
                    }
                    preview.Close();
                };
                capture.Start();
                Application.Run(preview);
                return;
            }
            if (args.Length == 3 && args[0] == "--native-news-render-test")
            {
                NewsOverlayForm preview = new NewsOverlayForm(args[1]);
                preview.Size = new Size(464, 221);
                Timer capture = new Timer { Interval = 3000 };
                capture.Tick += delegate
                {
                    capture.Stop();
                    using (Bitmap image = new Bitmap(preview.Width, preview.Height))
                    {
                        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.Size));
                        image.Save(args[2], ImageFormat.Png);
                    }
                    preview.Close();
                };
                capture.Start();
                Application.Run(preview);
                return;
            }
            if (args.Length == 1 && args[0] == "--manager")
                Application.Run(new LauncherForm());
            else if (args.Length == 2 && args[0] == "--client")
                Application.Run(new NativeOverlayContext(args[1], true));
            else
                Application.Run(new NativeOverlayContext());
        }

        private static bool LaunchOriginalLauncherWhenIntegrated(string[] args)
        {
            try
            {
                string executable = Process.GetCurrentProcess().MainModule.FileName;
                string directory = Path.GetDirectoryName(executable);
                if (!Path.GetFileName(executable).Equals("Launcher.exe", StringComparison.OrdinalIgnoreCase) ||
                    !new DirectoryInfo(directory).Name.Equals("launcher", StringComparison.OrdinalIgnoreCase)) return false;
                string core = Path.Combine(directory, "Launcher-core.exe");
                if (!File.Exists(core)) return false;
                Process.Start(new ProcessStartInfo(core, JoinArguments(args)) {
                    WorkingDirectory = directory, UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("The original Launcher could not be started.\r\n\r\n" + ex.Message,
                    "ASP Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        private static string JoinArguments(string[] args)
        {
            if (args == null || args.Length == 0) return "";
            string[] escaped = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                escaped[i] = "\"" + (args[i] ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            return String.Join(" ", escaped);
        }

        private static void EnableModernBrowserEngine()
        {
            try
            {
                string executable = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null) key.SetValue(executable, 11001, RegistryValueKind.DWord);
                }
            }
            catch { }
        }
    }
}

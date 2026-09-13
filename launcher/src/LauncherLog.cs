using System;
using System.IO;
using System.Text;

namespace AspCpwLauncher
{
    internal static class LauncherLog
    {
        internal static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ASP-Launcher.log");

        internal static void Write(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                try
                {
                    string fallback = Path.Combine(Path.GetDirectoryName(LauncherSettings.FilePath), "launcher.log");
                    Directory.CreateDirectory(Path.GetDirectoryName(fallback));
                    File.AppendAllText(fallback, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}

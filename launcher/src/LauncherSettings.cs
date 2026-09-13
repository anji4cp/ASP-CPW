using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AspCpwLauncher
{
    internal sealed class LauncherSettings
    {
        public string ClientPath = "";
        public string NewsUrl = "http://127.0.0.1:8081/launcher-news";
        public string RegisterUrl = "http://127.0.0.1:8081/#register";
        public string HomeUrl = "http://127.0.0.1:8081/";
        public string SupportUrl = "http://127.0.0.1:8081/guide";
        public string ForumUrl = "http://127.0.0.1:8081/";

        public static string FilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ASP Editor Studio", "CPW Desktop", "settings.ini");
            }
        }

        public static LauncherSettings Load()
        {
            LauncherSettings result = new LauncherSettings();
            if (!File.Exists(FilePath)) return result;
            try
            {
                Dictionary<string, string> entries = ReadEntries();
                string value;
                if (entries.TryGetValue("ClientPath", out value)) result.ClientPath = value;
                if (entries.TryGetValue("NewsUrl", out value)) result.NewsUrl = value;
                if (entries.TryGetValue("RegisterUrl", out value)) result.RegisterUrl = value;
                if (entries.TryGetValue("HomeUrl", out value)) result.HomeUrl = value;
                if (entries.TryGetValue("SupportUrl", out value)) result.SupportUrl = value;
                if (entries.TryGetValue("ForumUrl", out value)) result.ForumUrl = value;
            }
            catch { }
            return result;
        }

        public static void SaveClientPath(string clientPath)
        {
            Dictionary<string, string> entries = ReadEntries();
            entries["ClientPath"] = clientPath;
            string directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> item in entries)
                lines.Add(item.Key + "=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Value ?? "")));
            File.WriteAllLines(FilePath, lines.ToArray(), Encoding.UTF8);
        }

        private static Dictionary<string, string> ReadEntries()
        {
            Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(FilePath)) return entries;
            foreach (string line in File.ReadAllLines(FilePath))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                try
                {
                    entries[line.Substring(0, separator)] = Encoding.UTF8.GetString(
                        Convert.FromBase64String(line.Substring(separator + 1)));
                }
                catch { }
            }
            return entries;
        }
    }
}

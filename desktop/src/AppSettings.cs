using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AspCpwDesktop
{
    internal sealed class AppSettings
    {
        public string ClientPath = "";
        public string Server = "127.0.0.1";
        public string Port = "2223";
        public string User = "pwadmin";
        public string PatchUrl = "http://127.0.0.1:8082/patch/";
        public string GameAddress = "127.0.0.1";
        public string GamePort = "29001";
        public bool ServerInstalled;
        public bool ClientPrepared;

        private static string SettingsDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ASP Editor Studio", "CPW Desktop");
            }
        }

        private static string SettingsFile { get { return Path.Combine(SettingsDirectory, "settings.ini"); } }

        public static AppSettings Load()
        {
            AppSettings value = new AppSettings();
            if (!File.Exists(SettingsFile)) return value;
            try
            {
                Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in File.ReadAllLines(SettingsFile))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    entries[line.Substring(0, separator)] = Decode(line.Substring(separator + 1));
                }
                string item;
                if (entries.TryGetValue("ClientPath", out item)) value.ClientPath = item;
                if (entries.TryGetValue("Server", out item)) value.Server = item;
                if (entries.TryGetValue("Port", out item)) value.Port = item;
                if (entries.TryGetValue("User", out item)) value.User = item;
                if (entries.TryGetValue("PatchUrl", out item)) value.PatchUrl = item;
                if (entries.TryGetValue("GameAddress", out item)) value.GameAddress = item;
                if (entries.TryGetValue("GamePort", out item)) value.GamePort = item;
                if (entries.TryGetValue("ServerInstalled", out item)) value.ServerInstalled = item == "1";
                if (entries.TryGetValue("ClientPrepared", out item)) value.ClientPrepared = item == "1";
            }
            catch { }
            return value;
        }

        public void Save()
        {
            Directory.CreateDirectory(SettingsDirectory);
            string[] lines = {
                "ClientPath=" + Encode(ClientPath),
                "Server=" + Encode(Server),
                "Port=" + Encode(Port),
                "User=" + Encode(User),
                "PatchUrl=" + Encode(PatchUrl),
                "GameAddress=" + Encode(GameAddress),
                "GamePort=" + Encode(GamePort),
                "ServerInstalled=" + Encode(ServerInstalled ? "1" : "0"),
                "ClientPrepared=" + Encode(ClientPrepared ? "1" : "0")
            };
            File.WriteAllLines(SettingsFile, lines, Encoding.UTF8);
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}

using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace SlyvekLauncherCore
{
    public static class LauncherCore
    {
        private static string LocalVersionFile = Environment.CurrentDirectory + @"\launcherVersion.txt";
        public static Configuration Conf;
        public static string ConfPath;

        static LauncherCore()
        {
            ConfPath = Environment.CurrentDirectory + @"\config.json";
            if (!ConfigExist())
            {
                CreateDefaultConfig();
                SaveConfig();
            }
            LoadConfig();
        }

        public static bool CheckLauncherUpdate()
        {
            try
            {
                return GetRemoteVersion().isHigher(GetLocalVersion());
            }
            catch
            {
                return false;
            }
        }

        public static bool ConfigExist()
        {
            return File.Exists(ConfPath);
        }

        public static void LoadConfig()
        {
            Conf = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfPath));
        }

        public static void SaveConfig()
        {
            File.WriteAllText(ConfPath, JsonConvert.SerializeObject(Conf));
        }

        public static void CreateDefaultConfig()
        {
            Conf = new Configuration()
            {
                BaseURI = "https://www.vrdtmstudio.com/DFL",
                LauncherRemoteVersion = "https://www.vrdtmstudio.com/DFL/Launcher/launcherVersion.txt",
                LauncherName = "DodgeFromLight_Launcher.exe",
                LauncherRemoteVersionFiles = "https://www.vrdtmstudio.com/DFL/Launcher/"
            };
        }

        public static Version GetRemoteVersion()
        {
            try
            {
                WebClient Client = new WebClient();
                string remoteVersionString = Client.DownloadString(Conf.LauncherRemoteVersion);
                return new Version(remoteVersionString);
            }
            catch
            {
                return null;
            }
        }

        public static Version GetLocalVersion()
        {
            return new Version(File.ReadAllText(LocalVersionFile));
        }
    }

    public class Configuration
    {
        public string LauncherRemoteVersion { get; set; }
        public string BaseURI { get; set; }
        public string LauncherName { get; set; }
        public string LauncherRemoteVersionFiles { get; set; }
    }
}

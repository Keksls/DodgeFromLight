using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic
{
    public class Version
    {
        public int Major, Minor, Release, Fix;

        public Version(string v)
        {
            Major = int.Parse(v.Split('.')[0]);
            Minor = int.Parse(v.Split('.')[1]);
            Release = int.Parse(v.Split('.')[2]);
            Fix = int.Parse(v.Split('.')[3]);
        }

        public override string ToString()
        {
            return Major + "." + Minor + "." + Release + "." + Fix;
        }

        public bool isHigher(Version v)
        {
            bool can = true;

            if (Major > v.Major)
                return true && can;
            else if (Major < v.Major)
                can = false;

            if (Minor > v.Minor)
                return true && can;
            else if (Minor < v.Minor)
                can = false;

            if (Release > v.Release)
                return true && can;
            else if (Release < v.Release)
                can = false;

            if (Fix > v.Fix)
                return true && can;
            else if (Fix < v.Fix)
                can = false;

            return false;
        }
    }

    public static class ConfigurationManager
    {
        public static Configuration Conf;
        public static string ConfPath;

        static ConfigurationManager()
        {
            ConfPath = Environment.CurrentDirectory + @"\config.json";
            if(!ConfigExist())
            {
                CreateDefaultConfig();
                SaveConfig();
            }
            LoadConfig();
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
    }

    public class Configuration
    {
        public string LauncherRemoteVersion { get; set; }
        public string BaseURI { get; set; }
        public string LauncherName { get; set; }
        public string LauncherRemoteVersionFiles { get; set; }
    }
}

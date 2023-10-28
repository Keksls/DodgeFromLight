using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace Magic
{
    class Program
    {
        private static string LocalVersionFile = Environment.CurrentDirectory + @"\launcherVersion.txt";
        private static string LocalTempDataPath = Environment.CurrentDirectory + @"\launcherTemp.zip";
        private static WebClient Client;
        static Version CurrentVersion;
        static Version RemoteVersion;

        static void Main(string[] args)
        {
            try
            {
                // init vars
                Client = new WebClient();
                // get the current version
                WriteLine("Getting new Version...");
                CurrentVersion = new Version(File.ReadAllText(LocalVersionFile));
                // get remove version
                if (GetRemoteVersion().isHigher(CurrentVersion))
                {
                    WriteLine("New version found !", ConsoleColor.Cyan);
                    WriteLine("Downloading new version", ConsoleColor.DarkCyan);
                    // downmload new version zip
                    Client.DownloadFile(new Uri(ConfigurationManager.Conf.LauncherRemoteVersionFiles + "/" + RemoteVersion.ToString() + ".zip"), LocalTempDataPath);

                    // extract zip
                    WriteLine("Installing new version", ConsoleColor.DarkCyan);
                    using (var zip = new ZipFile(LocalTempDataPath))
                    {
                        zip.ExtractAll(Environment.CurrentDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                    CurrentVersion = RemoteVersion;
                    File.WriteAllText(LocalVersionFile, CurrentVersion.ToString());
                    File.Delete(LocalTempDataPath);
                    WriteLine("Success !", ConsoleColor.Cyan);
                }
                else
                    WriteLine("Already up to date", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                WriteLine(ex.Message, ConsoleColor.Red);
            }
            // start launcher
            StartLauncher();
        }

        static void StartLauncher()
        {
            WriteLine("Starting Launcher", ConsoleColor.Gray);
            Process.Start(Environment.CurrentDirectory + @"\" + ConfigurationManager.Conf.LauncherName);
        }

        public static Version GetRemoteVersion()
        {
            try
            {
                string remoteVersionString = Client.DownloadString(ConfigurationManager.Conf.LauncherRemoteVersion);
                RemoteVersion = new Version(remoteVersionString);
                return RemoteVersion;
            }
            catch
            {
                return CurrentVersion;
            }
        }

        static void Write(string text, ConsoleColor col = ConsoleColor.Gray)
        {
            Console.ForegroundColor = col;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void WriteLine(string text, ConsoleColor col = ConsoleColor.Gray)
        {
            Console.ForegroundColor = col;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Win32;
using DFLServer.Utils;
using DFLServer;
using DFL.Utils;
using DFLNetwork.Protocole.Compression;
using DFLNetwork.Protocole.Serialization;
using DFLNetwork.Protocole;

class Program
{
    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int nStdHandle);

    static void Main(string[] args)
    {
        if (Debugger.IsAttached)
            Routine();
        else
        {
            Loop:
            try
            {
                Routine();
            }
            catch (Exception ex)
            {
                Writer.Write(ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace, ConsoleColor.Red);
                goto Loop;
            }
        }
    }

    private static void Routine()
    {
        DrawHeader("v" + Assembly.GetAssembly(typeof(Program)).GetName().Version);
        Writer.Write_Server("Loading Configuration...", ConsoleColor.DarkYellow, false);
        if (File.Exists(Environment.CurrentDirectory + @"\config.json"))
        {
            Server.config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(Environment.CurrentDirectory + @"\config.json"));
            Server.config.PersistancePath = Server.config.PersistancePath.Replace("[current]", Environment.CurrentDirectory);
            Server.config.DatabaseFolder = Server.config.DatabaseFolder.Replace("[current]", Environment.CurrentDirectory);
            Server.config.BlackListFilePath = Server.config.BlackListFilePath.Replace("[current]", Environment.CurrentDirectory);
            if (Server.config.LockConsole)
            {
                try
                {
                    const uint ENABLE_QUICK_EDIT = 0x0040;
                    IntPtr consoleHandle = GetStdHandle(-10);
                    UInt32 consoleMode;
                    GetConsoleMode(consoleHandle, out consoleMode);
                    consoleMode &= ~ENABLE_QUICK_EDIT;
                    SetConsoleMode(consoleHandle, consoleMode);
                }
                catch { Writer.Write("Fail to set Console unselectable. Don't worry, everything is OK.", ConsoleColor.DarkGray); }
            }

            Writer.Write("OK", ConsoleColor.Green);
            Writer.Write(Server.config.ToString(), ConsoleColor.Yellow, false);
            
            Database.OpenDatabase();
            PhysicalPersistance.LoadPersistance(Server.config);
            BlackListManager.Initialize();
            Server.Initialize();
            if (Server.Start(Server.config.Port))
            {
                Writer.Write_Server("Processing Message Queue...", ConsoleColor.DarkYellow, false);
                Writer.Write("Started", ConsoleColor.Green);
                Server.processMessagesQueue(Server.config.ProcessOffsetTime);
                Database.AutoSaveDatabase();
            }
            else
            {
                Writer.Write("ERROR : Can't Start Server...", ConsoleColor.Red);
            }
        }
        else
        {
            Server.config = new Configuration();
            File.WriteAllText(Environment.CurrentDirectory + @"\config.json", JsonConvert.SerializeObject(Server.config));
            Writer.Write("No configuration file finded.", ConsoleColor.Red);
            Writer.Write("A new configuration file have been created. Please restart the server.", ConsoleColor.DarkYellow);
        }
    }

    static void DrawHeader(string version)
    {
        Console.Title = "DFL Server " + version;
        Writer.Write("\n");
        Writer.Write(@"  ________  ___________.____     ", ConsoleColor.Cyan);
        Writer.Write(@"  \______ \ \_   _____/|    |     ", ConsoleColor.Magenta);
        Writer.Write(@"   |    |  \ |    __)  |    |            ___  ___ _ ____   _____ _ __  ", ConsoleColor.Cyan);
        Writer.Write(@"   |    `   \|     \   |    |___        / __|/ _ \ '__\ \ / / _ \ '__| ", ConsoleColor.Magenta);
        Writer.Write(@"  /_______  /\___  /   |_______ \       \__ \  __/ |   \ V /  __/ |    ", ConsoleColor.Cyan);
        Writer.Write(@"          \/     \/            \/       |___/\___|_|    \_/ \___|_|    ", ConsoleColor.Magenta);
        Writer.Write(@"                          by ", ConsoleColor.Cyan, false);
        Writer.Write(@"Keks                                     ", ConsoleColor.Magenta, false);
        Writer.Write(version + "\n\n", ConsoleColor.Cyan);
    }
}
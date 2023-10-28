using Newtonsoft.Json;
using DFL.Utils;
using DFLServer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using DFLCommonNetwork.GameEngine;

namespace DFLServer
{
    public static class PhysicalPersistance
    {
        public static HashSet<int> Admins = new HashSet<int>();
        private static string PersistancePath;

        public static void LoadPersistance(Configuration config)
        {
            PersistancePath = config.PersistancePath;

            // Load Admins
            Writer.Write_Physical("Loading Admins...", ConsoleColor.DarkYellow, false);
            string adminPath = config.PersistancePath + @"\Admins.json";
            if (!File.Exists(adminPath))
            {
                Admins = new HashSet<int>() { 1 };
                File.WriteAllText(adminPath, JsonConvert.SerializeObject(Admins));
            }
            Admins = JsonConvert.DeserializeObject<HashSet<int>>(File.ReadAllText(adminPath));
            Writer.Write(Admins.Count.ToString(), ConsoleColor.Green);

        }

        public static PlayerSave GetFullyUnlockedSave()
        {
            string path = PersistancePath + @"\fullyUnlockedSave.json";
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PlayerSave>(json);
        }
    }
}
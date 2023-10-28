using DFLCommonNetwork.GameEngine;
using DFLNetwork.Protocole;
using DFLServer;
using DFLServer.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Test
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            PlayerSave s = JsonConvert.DeserializeObject<PlayerSave>(File.ReadAllText(Environment.CurrentDirectory + @"\fullyUnlockedSave.json"));
            NetworkMessage msg = new NetworkMessage(HeadActions.None).SetObject(s);
            byte[] data = msg.Serialize();
            byte[] dataWithoutSize = new byte[data.Length - 4];
            Array.Copy(data, 4, dataWithoutSize, 0, dataWithoutSize.Length);
            NetworkMessage networkMessage2 = new NetworkMessage(dataWithoutSize);
            PlayerSave save = null;
            networkMessage2.GetObject<PlayerSave>(ref save);
            Console.ReadKey();

            //List<int> tmp = new List<int>();
            //for(int i = 0; i < 72; i++)
            //    tmp.Add(i);
            //string json = JsonConvert.SerializeObject(tmp, Formatting.Indented);
            //Clipboard.SetText(json);
        }
    }
}
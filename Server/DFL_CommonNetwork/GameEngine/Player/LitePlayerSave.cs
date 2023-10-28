using System;
using System.Collections.Generic;

namespace DFLCommonNetwork.GameEngine
{
    [Serializable]
    public class LitePlayerSave
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int XP { get; set; }
        public Dictionary<byte, Int16> Equiped { get; set; }
        public CellPos Pos { get; set; }

        public LitePlayerSave() { }

        public LitePlayerSave(GameClient client, CellPos pos)
        {
            ID = client.ID;
            Name = client.Name;
            XP = client.PlayerSave.XP;
            Equiped = client.PlayerSave.Equiped;
            Pos = pos;
        }

        public LitePlayerSave(GameClient client)
        {
            ID = client.ID;
            Name = client.Name;
            XP = client.PlayerSave.XP;
            Equiped = client.PlayerSave.Equiped;
            Pos = new CellPos();
        }

        public LitePlayerSave(PlayerSave save)
        {
            ID = -1;
            Name = "";
            XP = save.XP;
            Equiped = save.Equiped;
            Pos = new CellPos();
        }

        public int GetCurrentPart(SkinType type)
        {
            if (Equiped.ContainsKey((byte)type))
                return Equiped[(byte)type];
            return -1;
        }

        public void SetCurrentPart(SkinType type, int ID)
        {
            Equiped[(byte)type] = (Int16)ID;
        }
    }
}
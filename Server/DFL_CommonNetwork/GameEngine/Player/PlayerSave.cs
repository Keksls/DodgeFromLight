using System;
using System.Collections.Generic;

namespace DFLCommonNetwork.GameEngine
{
    [Serializable]
    public class PlayerSave
    {
        public Dictionary<byte, short> Equiped { get; set; }
        public Dictionary<byte, List<short>> Unlocked { get; set; }
        public short OnlineLastReward { get; set; }
        public int XP { get; set; }
        public short CampaignIndex { get; set; }
        public short CampaignLastReward { get; set; }

        public PlayerSave()
        {
            Equiped = new Dictionary<byte, short>();
            Unlocked = new Dictionary<byte, List<short>>();
        }

        public int GetCurrentPart(SkinType type)
        {
            if (Equiped.ContainsKey((byte)type))
                return Equiped[(byte)type];
            return -1;
        }

        public void SetCurrentPart(SkinType type, short ID)
        {
            Equiped[(byte)type] = ID;
        }

        public void Unlock(SkinType type, short ID)
        {
            if (!Unlocked[(byte)type].Contains(ID))
                Unlocked[(byte)type].Add(ID);
        }

        public bool IsUnlocked(SkinType type, short ID)
        {
            return Unlocked[(byte)type].Contains(ID);
        }

        public List<short> GetUnlocked(SkinType type)
        {
            return Unlocked[(byte)type];
        }

        public int GetNbUnlockedPart(SkinType type)
        {
            if (Unlocked.ContainsKey((byte)type))
                return Unlocked[(byte)type].Count;
            return 0;
        }
    }

    public enum SkinType
    {
        Belt = 0,
        Boot = 1,
        Chest = 2,
        Glove = 3,
        Helm = 4,
        Plant = 5,
        Shoulder = 6,
        Hat = 7,
        Hand = 8,
        Eye = 9,
        Face = 10,
        Hair = 11,
        Wand = 13,
        Sword = 14,
        Shield = 15,
        Ornament = 16,
        Pet = 17,
        Hammer = 18,
        CustomHelmet = 19,
        Axe = 20,
        Wings = 21,
        Dagger = 22,
        Aureol = 23
    }
}
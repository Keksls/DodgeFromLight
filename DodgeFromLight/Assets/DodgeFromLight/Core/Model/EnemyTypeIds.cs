namespace DodgeFromLight.Core.Model
{
    public static class EnemyTypeIds
    {
        public const string Cyclops = "enemy.cyclops";
        public const string Gloutton = "enemy.gloutton";
        public const string Keeper = "enemy.keeper";
        public const string Knight = "enemy.knight";
        public const string Scout = "enemy.scout";
        public const string Trap = "enemy.trap";
        public const string Lizardman = "enemy.lizardman";
        public const string Thunderbird = "enemy.thunderbird";
        public const string Axe = "enemy.axe";
        public const string Hammer = "enemy.hammer";
        public const string Catapult = "enemy.catapult";
        public const string Pillar = "enemy.pillar";

        /// <summary>
        /// Reports whether an ID belongs to the complete supported enemy catalogue.
        /// </summary>
        public static bool IsKnown(string enemyId)
        {
            // Keep catalogue validation independent of legacy numeric enum values.
            switch (enemyId)
            {
                case Cyclops:
                case Gloutton:
                case Keeper:
                case Knight:
                case Scout:
                case Trap:
                case Lizardman:
                case Thunderbird:
                case Axe:
                case Hammer:
                case Catapult:
                case Pillar:
                    return true;
                default:
                    return false;
            }
        }
    }
}

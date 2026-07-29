namespace DodgeFromLight.Core.Model
{
    public static class GearTypeIds
    {
        public const string Bomb = "gear.bomb";
        public const string DiscoBall = "gear.disco_ball";
        public const string Grapple = "gear.grapple";
        public const string InvisibilityCloak = "gear.invisibility_cloak";
        public const string Mirror = "gear.mirror";
        public const string PowderOfDarkness = "gear.powder_of_darkness";
        public const string RemoteControl = "gear.remote_control";
        public const string SpeedBoots = "gear.speed_boots";
        public const string TimeController = "gear.time_controller";

        public const string PlacedDiscoBall = "object.disco_ball";
        public const string PlacedMirror = "object.mirror";
        public const string DarknessCloud = "object.darkness_cloud";

        /// <summary>
        /// Reports whether an ID belongs to the complete supported gear catalogue.
        /// </summary>
        public static bool IsKnown(string gearId)
        {
            // Keep catalogue validation independent of legacy numeric enum values.
            switch (gearId)
            {
                case Bomb:
                case DiscoBall:
                case Grapple:
                case InvisibilityCloak:
                case Mirror:
                case PowderOfDarkness:
                case RemoteControl:
                case SpeedBoots:
                case TimeController:
                    return true;
                default:
                    return false;
            }
        }
    }
}

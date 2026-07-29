namespace DodgeFromLight.Core.Model
{
    public static class CellTypeIds
    {
        public const string Floor = "cell.floor";
        public const string Void = "cell.void";
        public const string Teleporter = "cell.teleporter";
        public const string Collapse = "cell.collapse";
        public const string SpeedBoost = "cell.speed_boost";
        public const string Checkpoint = "cell.checkpoint";
        public const string Spring = "cell.spring";
        public const string TimedBomb = "cell.timed_bomb";
        public const string Needles = "cell.needles";
        public const string SwordSocket = "cell.sword_socket";

        /// <summary>
        /// Reports whether an ID belongs to the complete supported cell catalogue.
        /// </summary>
        public static bool IsKnown(string cellTypeId)
        {
            // Keep map validation independent of legacy numeric enum values.
            switch (cellTypeId)
            {
                case Floor:
                case Void:
                case Teleporter:
                case Collapse:
                case SpeedBoost:
                case Checkpoint:
                case Spring:
                case TimedBomb:
                case Needles:
                case SwordSocket:
                    return true;
                default:
                    return false;
            }
        }
    }
}

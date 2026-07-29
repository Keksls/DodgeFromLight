namespace DodgeFromLight.Application.Game
{
    /// <summary>
    /// Exposes the immutable part of a puzzle attempt required by UI consumers.
    /// </summary>
    public sealed class GameSessionReadModel
    {
        public string MapId { get; }
        public string MapTitle { get; }
        public int TurnIndex { get; }
        public GameSessionStatus Status { get; }
        public bool CanUndo { get; }
        public bool CanCreateReplay { get; }
        public string GearSlotOne { get; }
        public string GearSlotTwo { get; }
        public int SpeedBoostTurns { get; }
        public int InvisibilityTurns { get; }
        public int TimeStopTurns { get; }
        public bool HasCheckpoint { get; }
        public bool CanPickUpGear { get; }
        public bool CanActivateCheckpoint { get; }

        /// <summary>
        /// Creates one stable UI projection of an authoritative session state.
        /// </summary>
        public GameSessionReadModel(
            string mapId,
            string mapTitle,
            int turnIndex,
            GameSessionStatus status,
            bool canUndo,
            bool canCreateReplay,
            string gearSlotOne,
            string gearSlotTwo,
            int speedBoostTurns,
            int invisibilityTurns,
            int timeStopTurns,
            bool hasCheckpoint,
            bool canPickUpGear,
            bool canActivateCheckpoint)
        {
            // Normalize optional content so presentation code never handles null strings.
            MapId = mapId ?? string.Empty;
            MapTitle = mapTitle ?? string.Empty;
            TurnIndex = turnIndex;
            Status = status;
            CanUndo = canUndo;
            CanCreateReplay = canCreateReplay;
            GearSlotOne = gearSlotOne ?? string.Empty;
            GearSlotTwo = gearSlotTwo ?? string.Empty;
            SpeedBoostTurns = speedBoostTurns;
            InvisibilityTurns = invisibilityTurns;
            TimeStopTurns = timeStopTurns;
            HasCheckpoint = hasCheckpoint;
            CanPickUpGear = canPickUpGear;
            CanActivateCheckpoint = canActivateCheckpoint;
        }
    }
}

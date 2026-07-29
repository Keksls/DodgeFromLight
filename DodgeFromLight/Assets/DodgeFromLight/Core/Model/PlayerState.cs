using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class PlayerState
    {
        public GridPosition Position { get; }
        public Direction Direction { get; }
        public string GearSlotOne { get; }
        public string GearSlotTwo { get; }
        public int SpeedBoostTurns { get; }
        public int InvisibilityTurns { get; }
        public int TimeStopTurns { get; }
        public GridPosition? AnchorPosition { get; }
        public GridPosition? CheckpointPosition { get; }
        public bool HasUsedCheckpoint { get; }

        /// <summary>
        /// Creates the immutable logical state of the player.
        /// </summary>
        public PlayerState(GridPosition position, Direction direction)
            : this(
                position,
                direction,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                null,
                null,
                false)
        {
            // Keep the minimal constructor available for simple maps and existing callers.
        }

        /// <summary>
        /// Creates the complete immutable logical state of the player.
        /// </summary>
        public PlayerState(
            GridPosition position,
            Direction direction,
            string gearSlotOne,
            string gearSlotTwo,
            int speedBoostTurns,
            int invisibilityTurns,
            int timeStopTurns,
            GridPosition? anchorPosition,
            GridPosition? checkpointPosition,
            bool hasUsedCheckpoint)
        {
            // Validate all counters because they are authoritative replay state.
            if (direction == Direction.None || !Enum.IsDefined(typeof(Direction), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (speedBoostTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(speedBoostTurns));
            if (invisibilityTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(invisibilityTurns));
            if (timeStopTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(timeStopTurns));

            Position = position;
            Direction = direction;
            GearSlotOne = gearSlotOne ?? string.Empty;
            GearSlotTwo = gearSlotTwo ?? string.Empty;
            SpeedBoostTurns = speedBoostTurns;
            InvisibilityTurns = invisibilityTurns;
            TimeStopTurns = timeStopTurns;
            AnchorPosition = anchorPosition;
            CheckpointPosition = checkpointPosition;
            HasUsedCheckpoint = hasUsedCheckpoint;
        }

        /// <summary>
        /// Returns the gear ID stored in a zero-based inventory slot.
        /// </summary>
        public string GetGear(int slotIndex)
        {
            // Expose only the two supported inventory slots.
            switch (slotIndex)
            {
                case 0:
                    return GearSlotOne;
                case 1:
                    return GearSlotTwo;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }
    }
}

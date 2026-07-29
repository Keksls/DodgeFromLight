using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class TimedBombState
    {
        public GridPosition Position { get; }
        public int RemainingTurns { get; }
        public bool HasExploded => RemainingTurns == 0;

        /// <summary>
        /// Creates the immutable runtime state of one timed-bomb cell.
        /// </summary>
        public TimedBombState(GridPosition position, int remainingTurns)
        {
            // Zero represents an exploded bomb whose cell has already been destroyed.
            if (remainingTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingTurns));

            Position = position;
            RemainingTurns = remainingTurns;
        }

        /// <summary>
        /// Advances the countdown by one authoritative turn.
        /// </summary>
        public TimedBombState Tick()
        {
            // Exploded bombs remain stable in later snapshots.
            return HasExploded
                ? this
                : new TimedBombState(Position, RemainingTurns - 1);
        }
    }
}

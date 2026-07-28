using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class PlayerState
    {
        public GridPosition Position { get; }
        public Direction Direction { get; }

        /// <summary>
        /// Creates the immutable logical state of the player.
        /// </summary>
        public PlayerState(GridPosition position, Direction direction)
        {
            // Store only gameplay state; presentation data belongs outside the core.
            if (direction == Direction.None || !Enum.IsDefined(typeof(Direction), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));

            Position = position;
            Direction = direction;
        }
    }
}

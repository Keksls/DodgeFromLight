using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class MovingPlatformDefinition
    {
        public GridPosition StartPosition { get; }
        public Direction StartDirection { get; }
        public IReadOnlyList<GridPosition> Waypoints { get; }
        public int ActionsPerTurn { get; }

        /// <summary>
        /// Creates an immutable moving-platform placement for a map revision.
        /// </summary>
        public MovingPlatformDefinition(
            GridPosition startPosition,
            Direction startDirection,
            IEnumerable<GridPosition> waypoints,
            int actionsPerTurn)
        {
            // Copy the route so editor changes cannot mutate an active simulation.
            if (startDirection == Direction.None ||
                !Enum.IsDefined(typeof(Direction), startDirection))
                throw new ArgumentOutOfRangeException(nameof(startDirection));
            if (actionsPerTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(actionsPerTurn));

            StartPosition = startPosition;
            StartDirection = startDirection;
            Waypoints = new ReadOnlyCollection<GridPosition>(
                new List<GridPosition>(waypoints ?? Array.Empty<GridPosition>()));
            ActionsPerTurn = actionsPerTurn;
        }
    }
}

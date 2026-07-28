using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class EnemyDefinition
    {
        public string EnemyId { get; }
        public GridPosition StartPosition { get; }
        public Direction StartDirection { get; }
        public IReadOnlyList<GridPosition> Waypoints { get; }
        public int SightRange { get; }
        public int ActionsPerTurn { get; }
        public string Arguments { get; }

        /// <summary>
        /// Creates an immutable enemy placement for a map revision.
        /// </summary>
        public EnemyDefinition(
            string enemyId,
            GridPosition startPosition,
            Direction startDirection,
            IEnumerable<GridPosition> waypoints,
            int sightRange,
            int actionsPerTurn,
            string arguments)
        {
            // Copy authored values so callers cannot mutate a published map.
            if (string.IsNullOrWhiteSpace(enemyId))
                throw new ArgumentException("An enemy ID is required.", nameof(enemyId));
            if (startDirection == Direction.None ||
                !Enum.IsDefined(typeof(Direction), startDirection))
                throw new ArgumentOutOfRangeException(nameof(startDirection));
            if (sightRange < 0)
                throw new ArgumentOutOfRangeException(nameof(sightRange));
            if (actionsPerTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(actionsPerTurn));

            EnemyId = enemyId;
            StartPosition = startPosition;
            StartDirection = startDirection;
            Waypoints = new ReadOnlyCollection<GridPosition>(
                new List<GridPosition>(waypoints ?? Array.Empty<GridPosition>()));
            SightRange = sightRange;
            ActionsPerTurn = actionsPerTurn;
            Arguments = arguments ?? string.Empty;
        }
    }
}

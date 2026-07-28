using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class PatrolRouteBuilder
    {
        /// <summary>
        /// Expands axis-aligned waypoints into the cyclic one-cell patrol used by enemies.
        /// </summary>
        internal static bool TryBuild(
            IReadOnlyList<GridPosition> waypoints,
            out IReadOnlyList<GridPosition> route,
            out string error)
        {
            // Match the legacy cyclic route while rejecting diagonal segments explicitly.
            var positions = new List<GridPosition>();
            if (waypoints == null || waypoints.Count < 2)
            {
                route = new ReadOnlyCollection<GridPosition>(positions);
                error = string.Empty;
                return true;
            }

            for (var index = 0; index < waypoints.Count; index++)
            {
                var start = waypoints[index];
                var end = waypoints[(index + 1) % waypoints.Count];
                var deltaX = end.X - start.X;
                var deltaY = end.Y - start.Y;
                if (deltaX != 0 && deltaY != 0)
                {
                    route = new ReadOnlyCollection<GridPosition>(positions);
                    error = "Patrol segments must be axis-aligned.";
                    return false;
                }

                var stepX = Math.Sign(deltaX);
                var stepY = Math.Sign(deltaY);
                var current = start;
                while (current != end)
                {
                    positions.Add(current);
                    current = new GridPosition(current.X + stepX, current.Y + stepY);
                }
            }

            route = new ReadOnlyCollection<GridPosition>(positions);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Returns the cardinal direction from one adjacent route position to the next.
        /// </summary>
        internal static Direction GetDirection(GridPosition from, GridPosition to)
        {
            // Resolve only one-cell route transitions produced by the route builder.
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            if (deltaX == 1 && deltaY == 0)
                return Direction.Right;
            if (deltaX == -1 && deltaY == 0)
                return Direction.Left;
            if (deltaX == 0 && deltaY == 1)
                return Direction.Up;
            if (deltaX == 0 && deltaY == -1)
                return Direction.Down;
            throw new InvalidOperationException("Patrol route positions must be cardinally adjacent.");
        }

        /// <summary>
        /// Returns the cardinal direction opposite to the supplied direction.
        /// </summary>
        internal static Direction GetOppositeDirection(Direction direction)
        {
            // Keep reversal logic independent of presentation rotations.
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}

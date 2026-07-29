using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class DirectionUtility
    {
        /// <summary>
        /// Returns the cardinal direction opposite to the supplied direction.
        /// </summary>
        internal static Direction Opposite(Direction direction)
        {
            // Keep directional rules independent of presentation rotations.
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

        /// <summary>
        /// Returns the direction ninety degrees clockwise from a cardinal direction.
        /// </summary>
        internal static Direction Right(Direction direction)
        {
            // Explicit mapping avoids relying on enum arithmetic.
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Up;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Returns the direction ninety degrees counter-clockwise from a cardinal direction.
        /// </summary>
        internal static Direction Left(Direction direction)
        {
            // The left turn is the inverse of the clockwise mapping.
            return Opposite(Right(direction));
        }

        /// <summary>
        /// Translates a position by an exact cardinal distance.
        /// </summary>
        internal static GridPosition Translate(
            GridPosition position,
            Direction direction,
            int distance)
        {
            // Apply one-cell translation repeatedly to share the canonical direction mapping.
            if (distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance));
            for (var index = 0; index < distance; index++)
                position = position.Translate(direction);
            return position;
        }
    }
}

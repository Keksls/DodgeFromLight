using System;

namespace DodgeFromLight.Core.Model
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Creates an immutable position on the logical grid.
        /// </summary>
        public GridPosition(int x, int y)
        {
            // Store integer coordinates so simulation positions cannot drift.
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns the adjacent position in the requested cardinal direction.
        /// </summary>
        public GridPosition Translate(Direction direction)
        {
            // Resolve movement without relying on Unity vectors.
            switch (direction)
            {
                case Direction.Up:
                    return new GridPosition(X, Y + 1);
                case Direction.Right:
                    return new GridPosition(X + 1, Y);
                case Direction.Down:
                    return new GridPosition(X, Y - 1);
                case Direction.Left:
                    return new GridPosition(X - 1, Y);
                default:
                    return this;
            }
        }

        /// <summary>
        /// Compares this position with another logical grid position.
        /// </summary>
        public bool Equals(GridPosition other)
        {
            // Compare both axes because a grid position has no other state.
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Compares this position with an arbitrary object.
        /// </summary>
        public override bool Equals(object obj)
        {
            // Delegate typed comparisons to the strongly typed overload.
            return obj is GridPosition other && Equals(other);
        }

        /// <summary>
        /// Produces a stable hash code for dictionary and set lookups.
        /// </summary>
        public override int GetHashCode()
        {
            // Use fixed arithmetic to remain compatible with Unity's runtime profile.
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        /// <summary>
        /// Formats the position for diagnostics.
        /// </summary>
        public override string ToString()
        {
            // Keep the representation compact and independent of the current culture.
            return X + "," + Y;
        }

        /// <summary>
        /// Tests whether two positions are equal.
        /// </summary>
        public static bool operator ==(GridPosition left, GridPosition right)
        {
            // Reuse the typed equality implementation.
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two positions are different.
        /// </summary>
        public static bool operator !=(GridPosition left, GridPosition right)
        {
            // Negate the typed equality implementation.
            return !left.Equals(right);
        }
    }
}

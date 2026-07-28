using System;

/// <summary>
/// Identifies one cell on the puzzle grid.
/// </summary>
[Serializable]
public struct CellPos : IEquatable<CellPos>
{
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>
    /// Creates a grid position from its two axes.
    /// </summary>
    public CellPos(int x, int y)
    {
        // Store the authored integer coordinates without normalization.
        X = x;
        Y = y;
    }

    /// <summary>
    /// Determines whether another position uses the same coordinates.
    /// </summary>
    public bool Equals(CellPos other)
    {
        // Grid positions are equal only when both axes match.
        return X == other.X && Y == other.Y;
    }

    /// <summary>
    /// Determines whether the position matches explicit coordinates.
    /// </summary>
    public bool Equals(int x, int y)
    {
        // Compare the supplied axes directly to avoid temporary allocations.
        return X == x && Y == y;
    }

    /// <summary>
    /// Determines whether an object represents the same grid position.
    /// </summary>
    public override bool Equals(object obj)
    {
        // Restrict equality to strongly typed grid positions.
        return obj is CellPos other && Equals(other);
    }

    /// <summary>
    /// Returns a stable hash for dictionary and set lookups.
    /// </summary>
    public override int GetHashCode()
    {
        // Combine both axes so equal positions share the same hash.
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    /// <summary>
    /// Formats the position for legacy map identifiers and diagnostics.
    /// </summary>
    public override string ToString()
    {
        // Keep the established format used by authored map data.
        return X + "_" + Y;
    }

    /// <summary>
    /// Determines whether another position shares one grid axis.
    /// </summary>
    public bool IsInLine(CellPos other)
    {
        // Orthogonal lines share either their X or their Y coordinate.
        return X == other.X || Y == other.Y;
    }

    /// <summary>
    /// Determines whether another position lies in the requested authored direction.
    /// </summary>
    public bool IsInOrientation(CellPos other, Orientation orientation)
    {
        // Preserve the coordinate convention used by the existing puzzle content.
        switch (orientation)
        {
            case Orientation.Down:
                return other.X == X && other.Y > Y;
            case Orientation.Up:
                return other.X == X && other.Y < Y;
            case Orientation.Right:
                return other.Y == Y && other.X < X;
            case Orientation.Left:
                return other.Y == Y && other.X > X;
            default:
                return false;
        }
    }

    /// <summary>
    /// Compares two positions for coordinate equality.
    /// </summary>
    public static bool operator ==(CellPos left, CellPos right)
    {
        // Delegate to the typed equality implementation.
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two positions for coordinate inequality.
    /// </summary>
    public static bool operator !=(CellPos left, CellPos right)
    {
        // Negate the typed equality result.
        return !left.Equals(right);
    }
}

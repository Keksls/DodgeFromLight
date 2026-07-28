using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Simulation
{
    public static class MapDefinitionHasher
    {
        private static readonly ConditionalWeakTable<MapDefinition, string> CachedHashes =
            new ConditionalWeakTable<MapDefinition, string>();

        /// <summary>
        /// Computes a canonical content hash for an immutable map definition.
        /// </summary>
        public static string Compute(MapDefinition map)
        {
            // Cache by immutable map instance so state hashing does not traverse the grid every turn.
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            return CachedHashes.GetValue(map, ComputeUncached);
        }

        /// <summary>
        /// Computes a canonical content hash for a map not already present in the cache.
        /// </summary>
        private static string ComputeUncached(MapDefinition map)
        {
            // Serialize logical values in a fixed order before hashing them.
            var builder = new StringBuilder();
            AppendInteger(builder, map.SchemaVersion);
            AppendInteger(builder, map.RulesetVersion);
            AppendString(builder, map.MapId);
            AppendString(builder, map.RevisionId);
            AppendString(builder, map.Title);
            AppendString(builder, map.Author);
            AppendInteger(builder, map.Width);
            AppendInteger(builder, map.Height);
            AppendPosition(builder, map.StartPosition);
            AppendPosition(builder, map.ExitPosition);
            AppendInteger(builder, map.Cells.Count);

            foreach (var cell in map.Cells.OrderBy(cell => cell.Position.Y).ThenBy(cell => cell.Position.X))
                AppendCell(builder, cell);

            AppendInteger(builder, map.Enemies.Count);
            foreach (var enemy in map.Enemies
                         .OrderBy(enemy => enemy.StartPosition.Y)
                         .ThenBy(enemy => enemy.StartPosition.X)
                         .ThenBy(enemy => enemy.EnemyId, StringComparer.Ordinal))
            {
                AppendEnemy(builder, enemy);
            }

            AppendInteger(builder, map.MovingPlatforms.Count);
            foreach (var platform in map.MovingPlatforms
                         .OrderBy(platform => platform.StartPosition.Y)
                         .ThenBy(platform => platform.StartPosition.X))
            {
                AppendPlatform(builder, platform);
            }

            return HashUtility.ComputeSha256(builder.ToString());
        }

        /// <summary>
        /// Appends one cell in canonical property order.
        /// </summary>
        private static void AppendCell(StringBuilder builder, CellDefinition cell)
        {
            // Include logical, placement, and asset-reference data in the revision hash.
            AppendPosition(builder, cell.Position);
            AppendString(builder, cell.TypeId);
            AppendBoolean(builder, cell.IsWalkable);
            AppendString(builder, cell.FloorId);
            AppendString(builder, cell.ElementId);
            AppendFloat(builder, cell.ElementOrientationDegrees);
            AppendFloat(builder, cell.ElementScale);
            AppendBoolean(builder, cell.BlocksMovement);
            AppendString(builder, cell.GearId);
            AppendInteger(builder, cell.Parameters.Count);

            foreach (var parameter in cell.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                AppendString(builder, parameter.Key);
                AppendString(builder, parameter.Value);
            }
        }

        /// <summary>
        /// Appends one enemy in canonical property order.
        /// </summary>
        private static void AppendEnemy(StringBuilder builder, EnemyDefinition enemy)
        {
            // Preserve placement, authored stats, arguments, and route ordering.
            AppendString(builder, enemy.EnemyId);
            AppendPosition(builder, enemy.StartPosition);
            AppendInteger(builder, (int)enemy.StartDirection);
            AppendInteger(builder, enemy.SightRange);
            AppendInteger(builder, enemy.ActionsPerTurn);
            AppendString(builder, enemy.Arguments);
            AppendInteger(builder, enemy.Waypoints.Count);
            foreach (var waypoint in enemy.Waypoints)
                AppendPosition(builder, waypoint);
        }

        /// <summary>
        /// Appends one moving platform in canonical property order.
        /// </summary>
        private static void AppendPlatform(StringBuilder builder, MovingPlatformDefinition platform)
        {
            // Preserve the initial state and authored route ordering.
            AppendPosition(builder, platform.StartPosition);
            AppendInteger(builder, (int)platform.StartDirection);
            AppendInteger(builder, platform.ActionsPerTurn);
            AppendInteger(builder, platform.Waypoints.Count);
            foreach (var waypoint in platform.Waypoints)
                AppendPosition(builder, waypoint);
        }

        /// <summary>
        /// Appends a grid position in canonical form.
        /// </summary>
        private static void AppendPosition(StringBuilder builder, GridPosition position)
        {
            // Coordinates are appended independently to avoid ambiguous text pairs.
            AppendInteger(builder, position.X);
            AppendInteger(builder, position.Y);
        }

        /// <summary>
        /// Appends a length-prefixed string in canonical form.
        /// </summary>
        private static void AppendString(StringBuilder builder, string value)
        {
            // Length prefixes prevent separator collisions between adjacent values.
            var safeValue = value ?? string.Empty;
            builder.Append(safeValue.Length).Append(':').Append(safeValue).Append('|');
        }

        /// <summary>
        /// Appends an integer in culture-independent canonical form.
        /// </summary>
        private static void AppendInteger(StringBuilder builder, int value)
        {
            // Invariant formatting keeps hashes identical across locales.
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        /// <summary>
        /// Appends a floating-point value in round-trip canonical form.
        /// </summary>
        private static void AppendFloat(StringBuilder builder, float value)
        {
            // Round-trip formatting preserves the exact authored single-precision value.
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('|');
        }

        /// <summary>
        /// Appends a boolean in canonical numeric form.
        /// </summary>
        private static void AppendBoolean(StringBuilder builder, bool value)
        {
            // Numeric booleans avoid casing differences.
            builder.Append(value ? '1' : '0').Append('|');
        }
    }
}

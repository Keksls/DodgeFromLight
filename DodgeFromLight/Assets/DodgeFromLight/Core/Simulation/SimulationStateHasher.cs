using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Simulation
{
    public static class SimulationStateHasher
    {
        /// <summary>
        /// Computes a canonical hash for the current authoritative simulation state.
        /// </summary>
        public static string Compute(SimulationState state)
        {
            // Hash only authoritative logical values and the immutable map revision.
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var builder = new StringBuilder();
            AppendString(builder, state.Map.MapId);
            AppendString(builder, state.Map.RevisionId);
            AppendInteger(builder, state.Map.RulesetVersion);
            AppendString(builder, MapDefinitionHasher.Compute(state.Map));
            AppendInteger(builder, state.TurnIndex);
            AppendInteger(builder, (int)state.Status);
            AppendInteger(builder, state.Player.Position.X);
            AppendInteger(builder, state.Player.Position.Y);
            AppendInteger(builder, (int)state.Player.Direction);
            AppendString(builder, state.Player.GearSlotOne);
            AppendString(builder, state.Player.GearSlotTwo);
            AppendInteger(builder, state.Player.SpeedBoostTurns);
            AppendInteger(builder, state.Player.InvisibilityTurns);
            AppendInteger(builder, state.Player.TimeStopTurns);
            AppendNullablePosition(builder, state.Player.AnchorPosition);
            AppendNullablePosition(builder, state.Player.CheckpointPosition);
            AppendBoolean(builder, state.Player.HasUsedCheckpoint);
            AppendInteger(builder, state.Board.CollapsedCells.Count);
            foreach (var position in state.Board.CollapsedCells
                         .OrderBy(position => position.Y)
                         .ThenBy(position => position.X))
            {
                AppendInteger(builder, position.X);
                AppendInteger(builder, position.Y);
            }

            AppendInteger(builder, state.Board.RemovedGearCells.Count);
            foreach (var position in state.Board.RemovedGearCells
                         .OrderBy(position => position.Y)
                         .ThenBy(position => position.X))
            {
                AppendInteger(builder, position.X);
                AppendInteger(builder, position.Y);
            }

            AppendInteger(builder, state.Board.DestroyedElementCells.Count);
            foreach (var position in state.Board.DestroyedElementCells
                         .OrderBy(position => position.Y)
                         .ThenBy(position => position.X))
            {
                AppendInteger(builder, position.X);
                AppendInteger(builder, position.Y);
            }

            AppendInteger(builder, state.Board.TimedBombs.Count);
            foreach (var timedBomb in state.Board.TimedBombs
                         .OrderBy(item => item.Position.Y)
                         .ThenBy(item => item.Position.X))
            {
                AppendInteger(builder, timedBomb.Position.X);
                AppendInteger(builder, timedBomb.Position.Y);
                AppendInteger(builder, timedBomb.RemainingTurns);
            }

            AppendInteger(builder, state.Board.Needles.Count);
            foreach (var needle in state.Board.Needles
                         .OrderBy(item => item.Position.Y)
                         .ThenBy(item => item.Position.X))
            {
                AppendInteger(builder, needle.Position.X);
                AppendInteger(builder, needle.Position.Y);
                AppendBoolean(builder, needle.IsActive);
            }

            AppendInteger(builder, state.Board.PlacedObjects.Count);
            foreach (var placedObject in state.Board.PlacedObjects
                         .OrderBy(item => item.Position.Y)
                         .ThenBy(item => item.Position.X)
                         .ThenBy(item => item.ObjectId, StringComparer.Ordinal))
            {
                AppendString(builder, placedObject.ObjectId);
                AppendInteger(builder, placedObject.Position.X);
                AppendInteger(builder, placedObject.Position.Y);
                AppendInteger(builder, placedObject.RemainingTurns);
            }

            AppendInteger(builder, state.Enemies.Count);
            foreach (var enemy in state.Enemies)
            {
                AppendInteger(builder, enemy.DefinitionIndex);
                AppendInteger(builder, enemy.Position.X);
                AppendInteger(builder, enemy.Position.Y);
                AppendInteger(builder, (int)enemy.Direction);
                AppendInteger(builder, enemy.PatrolIndex);
                AppendBoolean(builder, enemy.IsPatrolReversed);
                AppendInteger(builder, enemy.PhaseIndex);
                AppendInteger(builder, enemy.StunnedTurns);
            }

            AppendInteger(builder, state.MovingPlatforms.Count);
            foreach (var platform in state.MovingPlatforms)
            {
                AppendInteger(builder, platform.DefinitionIndex);
                AppendInteger(builder, platform.Position.X);
                AppendInteger(builder, platform.Position.Y);
                AppendInteger(builder, (int)platform.Direction);
                AppendInteger(builder, platform.PatrolIndex);
                AppendBoolean(builder, platform.IsPatrolReversed);
            }

            AppendUnsignedInteger(builder, state.Random.Value);
            return HashUtility.ComputeSha256(builder.ToString());
        }

        /// <summary>
        /// Appends an optional grid position in canonical form.
        /// </summary>
        private static void AppendNullablePosition(
            StringBuilder builder,
            GridPosition? position)
        {
            // A presence marker distinguishes null from the valid zero coordinate.
            AppendBoolean(builder, position.HasValue);
            if (!position.HasValue)
                return;
            AppendInteger(builder, position.Value.X);
            AppendInteger(builder, position.Value.Y);
        }

        /// <summary>
        /// Appends a length-prefixed string in canonical form.
        /// </summary>
        private static void AppendString(StringBuilder builder, string value)
        {
            // Length prefixes remove ambiguity between adjacent text values.
            var safeValue = value ?? string.Empty;
            builder.Append(safeValue.Length).Append(':').Append(safeValue).Append('|');
        }

        /// <summary>
        /// Appends an integer in culture-independent canonical form.
        /// </summary>
        private static void AppendInteger(StringBuilder builder, int value)
        {
            // Invariant formatting keeps state hashes stable across locales.
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        /// <summary>
        /// Appends an unsigned integer in culture-independent canonical form.
        /// </summary>
        private static void AppendUnsignedInteger(StringBuilder builder, uint value)
        {
            // Preserve all pseudo-random generator bits without a signed conversion.
            builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        /// <summary>
        /// Appends a boolean in canonical numeric form.
        /// </summary>
        private static void AppendBoolean(StringBuilder builder, bool value)
        {
            // Numeric booleans avoid platform-specific casing.
            builder.Append(value ? '1' : '0').Append('|');
        }
    }
}

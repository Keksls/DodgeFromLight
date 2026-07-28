using System;
using System.Globalization;
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
            AppendInteger(builder, state.Enemies.Count);
            foreach (var enemy in state.Enemies)
            {
                AppendInteger(builder, enemy.DefinitionIndex);
                AppendInteger(builder, enemy.Position.X);
                AppendInteger(builder, enemy.Position.Y);
                AppendInteger(builder, (int)enemy.Direction);
                AppendInteger(builder, enemy.PatrolIndex);
                AppendBoolean(builder, enemy.IsPatrolReversed);
            }

            return HashUtility.ComputeSha256(builder.ToString());
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
        /// Appends a boolean in canonical numeric form.
        /// </summary>
        private static void AppendBoolean(StringBuilder builder, bool value)
        {
            // Numeric booleans avoid platform-specific casing.
            builder.Append(value ? '1' : '0').Append('|');
        }
    }
}

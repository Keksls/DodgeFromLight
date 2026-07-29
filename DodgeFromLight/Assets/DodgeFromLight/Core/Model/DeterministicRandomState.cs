using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class DeterministicRandomState
    {
        public uint Value { get; }

        /// <summary>
        /// Creates a deterministic pseudo-random generator snapshot.
        /// </summary>
        public DeterministicRandomState(uint value)
        {
            // Xorshift cannot advance from zero, so normalize that single invalid state.
            Value = value == 0 ? 0x9E3779B9u : value;
        }

        /// <summary>
        /// Creates a stable initial generator state from map revision identifiers.
        /// </summary>
        public static DeterministicRandomState CreateInitial(string mapId, string revisionId)
        {
            // FNV-1a produces the same seed on every runtime and platform.
            var text = (mapId ?? string.Empty) + "\n" + (revisionId ?? string.Empty);
            var hash = 2166136261u;
            for (var index = 0; index < text.Length; index++)
            {
                hash ^= text[index];
                hash *= 16777619u;
            }

            return new DeterministicRandomState(hash);
        }

        /// <summary>
        /// Draws an integer in an exclusive range and returns the advanced generator state.
        /// </summary>
        public int Next(int exclusiveMaximum, out DeterministicRandomState nextState)
        {
            // Advance xorshift32 exactly once for every gameplay draw.
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            var nextValue = Value;
            nextValue ^= nextValue << 13;
            nextValue ^= nextValue >> 17;
            nextValue ^= nextValue << 5;
            nextState = new DeterministicRandomState(nextValue);
            return (int)(nextValue % (uint)exclusiveMaximum);
        }

        /// <summary>
        /// Draws an integer inside a minimum-inclusive, maximum-exclusive range.
        /// </summary>
        public int Next(
            int inclusiveMinimum,
            int exclusiveMaximum,
            out DeterministicRandomState nextState)
        {
            // Preserve the legacy range convention while making the draw replayable.
            if (exclusiveMaximum <= inclusiveMinimum)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            return inclusiveMinimum +
                   Next(exclusiveMaximum - inclusiveMinimum, out nextState);
        }
    }
}

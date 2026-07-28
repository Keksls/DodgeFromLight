using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Replays
{
    public sealed class ReplayResult
    {
        public bool IsSuccessful { get; }
        public SimulationState FinalState { get; }
        public string FinalStateHash { get; }
        public string Error { get; }

        /// <summary>
        /// Creates the immutable outcome of a replay validation.
        /// </summary>
        public ReplayResult(
            bool isSuccessful,
            SimulationState finalState,
            string finalStateHash,
            string error)
        {
            // Keep the final state available for diagnostics even on a hash mismatch.
            FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
            IsSuccessful = isSuccessful;
            FinalStateHash = finalStateHash ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }
}

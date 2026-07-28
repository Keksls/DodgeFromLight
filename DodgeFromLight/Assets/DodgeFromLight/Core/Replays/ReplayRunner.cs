using System;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Simulation;

namespace DodgeFromLight.Core.Replays
{
    public sealed class ReplayRunner
    {
        private readonly SimulationEngine _engine;

        /// <summary>
        /// Creates a replay runner using the supplied deterministic engine.
        /// </summary>
        public ReplayRunner(SimulationEngine engine)
        {
            // Reuse the same engine contract used by live sessions.
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Replays every command and validates the final state hash.
        /// </summary>
        public ReplayResult Run(MapDefinition map, ReplayDefinition replay)
        {
            // Validate immutable identifiers before executing any replay command.
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (replay == null)
                throw new ArgumentNullException(nameof(replay));

            var state = SimulationState.CreateInitial(map);
            if (map.MapId != replay.MapId ||
                map.RevisionId != replay.RevisionId ||
                map.RulesetVersion != replay.RulesetVersion ||
                !string.Equals(
                    MapDefinitionHasher.Compute(map),
                    replay.MapContentHash,
                    StringComparison.Ordinal))
            {
                return new ReplayResult(
                    false,
                    state,
                    SimulationStateHasher.Compute(state),
                    "The replay does not target this exact map revision, content, and ruleset.");
            }

            for (var index = 0; index < replay.Commands.Count; index++)
            {
                var result = _engine.Step(state, replay.Commands[index]);
                if (!result.IsAccepted)
                {
                    return new ReplayResult(
                        false,
                        state,
                        SimulationStateHasher.Compute(state),
                        "Command " + index + " was rejected: " + result.RejectionReason);
                }

                state = result.State;
            }

            var finalHash = SimulationStateHasher.Compute(state);
            if (!string.Equals(finalHash, replay.FinalStateHash, StringComparison.Ordinal))
            {
                return new ReplayResult(
                    false,
                    state,
                    finalHash,
                    "The replay final state hash does not match.");
            }

            return new ReplayResult(true, state, finalHash, string.Empty);
        }
    }
}

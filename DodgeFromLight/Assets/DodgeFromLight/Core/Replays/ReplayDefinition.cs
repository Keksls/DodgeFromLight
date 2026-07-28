using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Commands;

namespace DodgeFromLight.Core.Replays
{
    public sealed class ReplayDefinition
    {
        public string MapId { get; }
        public string RevisionId { get; }
        public int RulesetVersion { get; }
        public string MapContentHash { get; }
        public IReadOnlyList<PlayerCommand> Commands { get; }
        public string FinalStateHash { get; }

        /// <summary>
        /// Creates an immutable replay tied to one map and ruleset revision.
        /// </summary>
        public ReplayDefinition(
            string mapId,
            string revisionId,
            int rulesetVersion,
            string mapContentHash,
            IEnumerable<PlayerCommand> commands,
            string finalStateHash)
        {
            // Copy commands so the replay cannot change after validation.
            if (string.IsNullOrWhiteSpace(mapId))
                throw new ArgumentException("A replay map ID is required.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(revisionId))
                throw new ArgumentException("A replay revision ID is required.", nameof(revisionId));
            if (rulesetVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(rulesetVersion));
            if (string.IsNullOrWhiteSpace(mapContentHash))
                throw new ArgumentException("A replay map content hash is required.", nameof(mapContentHash));
            if (string.IsNullOrWhiteSpace(finalStateHash))
                throw new ArgumentException("A replay final state hash is required.", nameof(finalStateHash));

            MapId = mapId;
            RevisionId = revisionId;
            RulesetVersion = rulesetVersion;
            MapContentHash = mapContentHash;
            var copiedCommands = new List<PlayerCommand>(commands ?? Array.Empty<PlayerCommand>());
            if (copiedCommands.Exists(command => command == null))
                throw new ArgumentException("A replay cannot contain a null command.", nameof(commands));

            Commands = new ReadOnlyCollection<PlayerCommand>(copiedCommands);
            FinalStateHash = finalStateHash;
        }
    }
}

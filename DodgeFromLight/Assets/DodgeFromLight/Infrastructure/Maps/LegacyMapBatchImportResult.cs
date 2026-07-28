using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapBatchImportResult
    {
        public IReadOnlyList<ImportedLegacyMap> Maps { get; }
        public IReadOnlyList<LegacyMapImportConflict> Conflicts { get; }
        public IReadOnlyList<LegacyMapImportIssue> Issues { get; }
        public bool IsSuccessful =>
            Conflicts.Count == 0 &&
            Issues.All(issue => issue.Severity != LegacyMapImportIssueSeverity.Error);

        /// <summary>
        /// Creates an immutable report for a complete legacy map batch.
        /// </summary>
        public LegacyMapBatchImportResult(
            IEnumerable<ImportedLegacyMap> maps,
            IEnumerable<LegacyMapImportConflict> conflicts,
            IEnumerable<LegacyMapImportIssue> issues)
        {
            // Snapshot every collection so the final verdict remains deterministic.
            Maps = new ReadOnlyCollection<ImportedLegacyMap>(
                new List<ImportedLegacyMap>(maps ?? Array.Empty<ImportedLegacyMap>()));
            Conflicts = new ReadOnlyCollection<LegacyMapImportConflict>(
                new List<LegacyMapImportConflict>(
                    conflicts ?? Array.Empty<LegacyMapImportConflict>()));
            Issues = new ReadOnlyCollection<LegacyMapImportIssue>(
                new List<LegacyMapImportIssue>(
                    issues ?? Array.Empty<LegacyMapImportIssue>()));
        }
    }
}

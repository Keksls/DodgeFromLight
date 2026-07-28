using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapImportResult
    {
        public MapPackage Package { get; }
        public IReadOnlyList<LegacyMapImportIssue> Issues { get; }
        public bool IsSuccessful =>
            Package != null && Issues.All(issue => issue.Severity != LegacyMapImportIssueSeverity.Error);

        /// <summary>
        /// Creates an immutable legacy-map import result.
        /// </summary>
        public LegacyMapImportResult(
            MapPackage package,
            IEnumerable<LegacyMapImportIssue> issues)
        {
            // Copy diagnostics so later callers cannot change an import verdict.
            Package = package;
            Issues = new ReadOnlyCollection<LegacyMapImportIssue>(
                new List<LegacyMapImportIssue>(issues ?? Array.Empty<LegacyMapImportIssue>()));
        }
    }
}

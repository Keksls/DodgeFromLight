using System;
using System.Collections.Generic;
using System.Linq;
using DodgeFromLight.Core.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapBatchImporter
    {
        private readonly LegacyMapImporter importer;

        /// <summary>
        /// Creates a batch importer using the established single-map conversion boundary.
        /// </summary>
        public LegacyMapBatchImporter()
            : this(new LegacyMapImporter())
        {
            // Keep the default construction convenient for production migration tools.
        }

        /// <summary>
        /// Creates a batch importer with an explicit single-map importer.
        /// </summary>
        public LegacyMapBatchImporter(LegacyMapImporter importer)
        {
            // Accept dependency injection without adding a framework to the migration layer.
            this.importer = importer ?? throw new ArgumentNullException(nameof(importer));
        }

        /// <summary>
        /// Imports, deduplicates, and reports a stable sequence of legacy map sources.
        /// </summary>
        public LegacyMapBatchImportResult Import(IEnumerable<LegacyMapSource> sources)
        {
            // Preserve first-seen ordering while indexing map IDs for deterministic comparisons.
            var maps = new List<ImportedLegacyMap>();
            var conflicts = new List<LegacyMapImportConflict>();
            var issues = new List<LegacyMapImportIssue>();
            var mapIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var source in sources ?? Array.Empty<LegacyMapSource>())
            {
                if (source == null)
                {
                    issues.Add(new LegacyMapImportIssue(
                        LegacyMapImportIssueSeverity.Error,
                        "legacy.null-source",
                        "The batch contains a null legacy map source."));
                    continue;
                }

                var importResult = importer.Import(
                    source.GridJson,
                    source.MetadataJson,
                    source.SourceLabel);
                AppendSourceIssues(importResult.Issues, source.SourceLabel, issues);
                if (!importResult.IsSuccessful)
                    continue;

                var candidate = new ImportedLegacyMap(
                    importResult.Package,
                    source.PreviewBytes,
                    new[] { source.SourceLabel });
                var mapId = candidate.Package.Map.MapId;
                if (!mapIndexes.TryGetValue(mapId, out var existingIndex))
                {
                    mapIndexes.Add(mapId, maps.Count);
                    maps.Add(candidate);
                    continue;
                }

                var existingMap = maps[existingIndex];
                var difference = FindDifference(existingMap, candidate);
                if (difference == LegacyMapConflictDifference.None)
                {
                    maps[existingIndex] = MergeEquivalentSources(existingMap, candidate);
                    continue;
                }

                conflicts.Add(new LegacyMapImportConflict(
                    mapId,
                    existingMap,
                    candidate,
                    difference));
                issues.Add(new LegacyMapImportIssue(
                    LegacyMapImportIssueSeverity.Error,
                    "legacy.duplicate-conflict",
                    "Map ID '" + mapId + "' differs from its first imported source in: " +
                    difference + ".",
                    source.SourceLabel));
            }

            return new LegacyMapBatchImportResult(maps, conflicts, issues);
        }

        /// <summary>
        /// Adds a source label to diagnostics emitted by the single-map importer.
        /// </summary>
        private static void AppendSourceIssues(
            IEnumerable<LegacyMapImportIssue> sourceIssues,
            string sourceLabel,
            ICollection<LegacyMapImportIssue> targetIssues)
        {
            // Retain original codes and messages while making every batch diagnostic traceable.
            foreach (var issue in sourceIssues ?? Array.Empty<LegacyMapImportIssue>())
            {
                targetIssues.Add(new LegacyMapImportIssue(
                    issue.Severity,
                    issue.Code,
                    issue.Message,
                    sourceLabel));
            }
        }

        /// <summary>
        /// Identifies every content layer that differs between candidates sharing a map ID.
        /// </summary>
        private static LegacyMapConflictDifference FindDifference(
            ImportedLegacyMap existingMap,
            ImportedLegacyMap candidate)
        {
            // Compare gameplay, visual environment, and preview independently for useful reports.
            var difference = LegacyMapConflictDifference.None;
            if (!string.Equals(
                    MapDefinitionHasher.Compute(existingMap.Package.Map),
                    MapDefinitionHasher.Compute(candidate.Package.Map),
                    StringComparison.Ordinal))
            {
                difference |= LegacyMapConflictDifference.Gameplay;
            }

            if (!EnvironmentsEqual(
                    existingMap.Package.EnvironmentJson,
                    candidate.Package.EnvironmentJson))
            {
                difference |= LegacyMapConflictDifference.Environment;
            }

            if (!existingMap.PreviewBytes.SequenceEqual(candidate.PreviewBytes))
                difference |= LegacyMapConflictDifference.Preview;

            return difference;
        }

        /// <summary>
        /// Combines provenance for two semantically equivalent imports.
        /// </summary>
        private static ImportedLegacyMap MergeEquivalentSources(
            ImportedLegacyMap existingMap,
            ImportedLegacyMap candidate)
        {
            // Keep one package while retaining each unique source label in encounter order.
            var labels = existingMap.SourceLabels
                .Concat(candidate.SourceLabels)
                .Distinct(StringComparer.Ordinal);
            return new ImportedLegacyMap(
                existingMap.Package,
                existingMap.PreviewBytes,
                labels);
        }

        /// <summary>
        /// Compares environment JSON by value instead of source property order.
        /// </summary>
        private static bool EnvironmentsEqual(string leftJson, string rightJson)
        {
            // Canonicalize object properties while preserving array ordering and scalar values.
            var left = CanonicalizeToken(JToken.Parse(leftJson ?? "{}"));
            var right = CanonicalizeToken(JToken.Parse(rightJson ?? "{}"));
            return string.Equals(
                left.ToString(Formatting.None),
                right.ToString(Formatting.None),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Recursively orders JSON object properties into a deterministic representation.
        /// </summary>
        private static JToken CanonicalizeToken(JToken token)
        {
            // Sort unordered objects but retain authored array order because it can be meaningful.
            if (token is JObject objectToken)
            {
                var canonicalObject = new JObject();
                foreach (var property in objectToken.Properties()
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    canonicalObject.Add(
                        property.Name,
                        CanonicalizeToken(property.Value));
                }

                return canonicalObject;
            }

            if (token is JArray arrayToken)
                return new JArray(arrayToken.Select(CanonicalizeToken));

            return token.DeepClone();
        }
    }
}

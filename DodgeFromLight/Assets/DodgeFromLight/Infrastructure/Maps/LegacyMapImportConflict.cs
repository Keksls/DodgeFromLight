using System;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapImportConflict
    {
        public string MapId { get; }
        public ImportedLegacyMap ExistingMap { get; }
        public ImportedLegacyMap ConflictingMap { get; }
        public LegacyMapConflictDifference Difference { get; }

        /// <summary>
        /// Preserves both candidates when one map ID resolves to different imported content.
        /// </summary>
        public LegacyMapImportConflict(
            string mapId,
            ImportedLegacyMap existingMap,
            ImportedLegacyMap conflictingMap,
            LegacyMapConflictDifference difference)
        {
            // Reject incomplete conflicts so neither source can disappear from the report.
            if (string.IsNullOrWhiteSpace(mapId))
                throw new ArgumentException("A conflicting map ID is required.", nameof(mapId));
            if (difference == LegacyMapConflictDifference.None)
                throw new ArgumentException("A conflict must contain at least one difference.", nameof(difference));

            ExistingMap = existingMap ?? throw new ArgumentNullException(nameof(existingMap));
            ConflictingMap = conflictingMap ?? throw new ArgumentNullException(nameof(conflictingMap));
            if (!string.Equals(ExistingMap.Package.Map.MapId, mapId, StringComparison.Ordinal) ||
                !string.Equals(ConflictingMap.Package.Map.MapId, mapId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Both conflict candidates must use the reported map ID.",
                    nameof(mapId));
            }

            MapId = mapId;
            Difference = difference;
        }
    }
}

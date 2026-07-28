using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class LegacyMapSource
    {
        public string SourceLabel { get; }
        public string GridJson { get; }
        public string MetadataJson { get; }
        public IReadOnlyList<byte> PreviewBytes { get; }

        /// <summary>
        /// Captures one complete legacy map source without retaining mutable input buffers.
        /// </summary>
        public LegacyMapSource(
            string sourceLabel,
            string gridJson,
            string metadataJson,
            IEnumerable<byte> previewBytes)
        {
            // Copy all boundary values so a batch import observes a stable source snapshot.
            if (string.IsNullOrWhiteSpace(sourceLabel))
                throw new ArgumentException("A legacy source label is required.", nameof(sourceLabel));

            SourceLabel = sourceLabel;
            GridJson = gridJson ?? string.Empty;
            MetadataJson = metadataJson ?? string.Empty;
            PreviewBytes = new ReadOnlyCollection<byte>(
                new List<byte>(previewBytes ?? Array.Empty<byte>()));
        }
    }
}

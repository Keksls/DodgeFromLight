using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class ImportedLegacyMap
    {
        public MapPackage Package { get; }
        public IReadOnlyList<byte> PreviewBytes { get; }
        public IReadOnlyList<string> SourceLabels { get; }

        /// <summary>
        /// Groups one exact imported package with its preview and every equivalent source.
        /// </summary>
        public ImportedLegacyMap(
            MapPackage package,
            IEnumerable<byte> previewBytes,
            IEnumerable<string> sourceLabels)
        {
            // Own immutable copies so deduplication cannot lose or later mutate source evidence.
            Package = package ?? throw new ArgumentNullException(nameof(package));
            PreviewBytes = new ReadOnlyCollection<byte>(
                new List<byte>(previewBytes ?? Array.Empty<byte>()));

            var labels = new List<string>(sourceLabels ?? Array.Empty<string>());
            if (labels.Count == 0)
                throw new ArgumentException("At least one source label is required.", nameof(sourceLabels));
            foreach (var label in labels)
            {
                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new ArgumentException(
                        "Source labels cannot be empty.",
                        nameof(sourceLabels));
                }
            }

            SourceLabels = new ReadOnlyCollection<string>(labels);
        }
    }
}

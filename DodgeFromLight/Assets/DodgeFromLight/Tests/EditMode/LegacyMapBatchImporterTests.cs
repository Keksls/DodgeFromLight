using System.Linq;
using DodgeFromLight.Infrastructure.Maps;
using NUnit.Framework;

namespace DodgeFromLight.Tests
{
    public sealed class LegacyMapBatchImporterTests
    {
        /// <summary>
        /// Verifies that exact duplicates merge without losing their source provenance.
        /// </summary>
        [Test]
        public void ImportMergesEquivalentSources()
        {
            // Reorder environment properties to prove equality is semantic rather than textual.
            var result = new LegacyMapBatchImporter().Import(new[]
            {
                CreateSource("Source A", "{\"B\":2,\"A\":1}", "FloorA", new byte[] { 1, 2 }),
                CreateSource("Source B", "{\"A\":1,\"B\":2}", "FloorA", new byte[] { 1, 2 })
            });

            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Maps, Has.Count.EqualTo(1));
            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(
                result.Maps[0].SourceLabels,
                Is.EqualTo(new[] { "Source A", "Source B" }));
            Assert.That(result.Maps[0].PreviewBytes, Is.EqualTo(new byte[] { 1, 2 }));
        }

        /// <summary>
        /// Verifies that a visual environment variant remains available in a conflict report.
        /// </summary>
        [Test]
        public void ImportPreservesEnvironmentConflict()
        {
            // Use one map ID with identical gameplay and two intentionally different environments.
            var result = new LegacyMapBatchImporter().Import(new[]
            {
                CreateSource("Root copy", "{\"Seed\":1}", "FloorA", new byte[] { 1 }),
                CreateSource("Tutorial copy", "{\"Seed\":2}", "FloorA", new byte[] { 1 })
            });

            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Conflicts, Has.Count.EqualTo(1));
            Assert.That(
                result.Conflicts[0].Difference,
                Is.EqualTo(LegacyMapConflictDifference.Environment));
            Assert.That(
                result.Conflicts[0].ExistingMap.Package.EnvironmentJson,
                Does.Contain("\"Seed\":1"));
            Assert.That(
                result.Conflicts[0].ConflictingMap.Package.EnvironmentJson,
                Does.Contain("\"Seed\":2"));
        }

        /// <summary>
        /// Verifies that gameplay changes under one map ID cannot be silently deduplicated.
        /// </summary>
        [Test]
        public void ImportReportsGameplayConflict()
        {
            // Change a preserved floor reference while keeping visuals and preview unchanged.
            var result = new LegacyMapBatchImporter().Import(new[]
            {
                CreateSource("Source A", "{}", "FloorA", new byte[] { 1 }),
                CreateSource("Source B", "{}", "FloorB", new byte[] { 1 })
            });

            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(
                result.Conflicts[0].Difference,
                Is.EqualTo(LegacyMapConflictDifference.Gameplay));
        }

        /// <summary>
        /// Verifies that differing previews are treated as authored content conflicts.
        /// </summary>
        [Test]
        public void ImportReportsPreviewConflict()
        {
            // Keep both packages equal and vary only the source preview bytes.
            var result = new LegacyMapBatchImporter().Import(new[]
            {
                CreateSource("Source A", "{}", "FloorA", new byte[] { 1 }),
                CreateSource("Source B", "{}", "FloorA", new byte[] { 2 })
            });

            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(
                result.Conflicts[0].Difference,
                Is.EqualTo(LegacyMapConflictDifference.Preview));
            Assert.That(
                result.Conflicts[0].ConflictingMap.PreviewBytes,
                Is.EqualTo(new byte[] { 2 }));
        }

        /// <summary>
        /// Verifies that batch diagnostics identify the source that emitted them.
        /// </summary>
        [Test]
        public void ImportLabelsSingleMapDiagnostics()
        {
            // Mark the sample as a lobby map to produce a non-fatal importer warning.
            var source = new LegacyMapSource(
                "Lobby source",
                CreateGridJson("{}", "FloorA", true),
                CreateMetadataJson(),
                new byte[0]);

            var result = new LegacyMapBatchImporter().Import(new[] { source });
            var lobbyIssue = result.Issues.Single(issue => issue.Code == "legacy.lobby-map");

            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(lobbyIssue.SourceLabel, Is.EqualTo("Lobby source"));
        }

        /// <summary>
        /// Creates one complete in-memory legacy source for batch behavior tests.
        /// </summary>
        private static LegacyMapSource CreateSource(
            string sourceLabel,
            string environmentJson,
            string floorId,
            byte[] previewBytes)
        {
            // Reuse a fixed map ID so each test controls the exact deduplication dimension.
            return new LegacyMapSource(
                sourceLabel,
                CreateGridJson(environmentJson, floorId, false),
                CreateMetadataJson(),
                previewBytes);
        }

        /// <summary>
        /// Creates a minimal valid legacy grid with selectable visual and floor content.
        /// </summary>
        private static string CreateGridJson(
            string environmentJson,
            string floorId,
            bool isLobbyMap)
        {
            // Include every field consumed by the importer while keeping the fixture readable.
            return
                "{\"LobbyMap\":" + (isLobbyMap ? "true" : "false") + "," +
                "\"Cells\":[[{\"X\":0,\"Y\":0,\"Walkable\":true,\"Type\":0," +
                "\"FloorID\":\"" + floorId + "\",\"ElementID\":\"\"," +
                "\"ElementOrientation\":0.0,\"ElementScale\":0.0,\"HasElement\":false," +
                "\"FloorOrientation\":1,\"GearType\":0,\"Arg1\":0}]]," +
                "\"Width\":1,\"Height\":1,\"StartCell\":{\"X\":0,\"Y\":0}," +
                "\"EndCell\":{\"X\":0,\"Y\":0},\"Environment\":" + environmentJson + "," +
                "\"Ennemies\":[],\"MovingPlatforms\":[]}";
        }

        /// <summary>
        /// Creates stable metadata shared by the in-memory duplicate candidates.
        /// </summary>
        private static string CreateMetadataJson()
        {
            // Keep metadata equal so each test isolates its intended conflict layer.
            return
                "{\"ID\":\"batch-map\",\"Name\":\"Batch map\",\"Author\":\"Tests\"," +
                "\"Width\":\"1\",\"Height\":\"1\"}";
        }
    }
}

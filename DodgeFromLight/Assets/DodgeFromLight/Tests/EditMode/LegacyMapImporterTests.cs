using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Simulation;
using DodgeFromLight.Infrastructure.Maps;
using NUnit.Framework;

namespace DodgeFromLight.Tests
{
    public sealed class LegacyMapImporterTests
    {
        /// <summary>
        /// Verifies that known legacy IDs survive import and target-package round trips.
        /// </summary>
        [Test]
        public void ImportPreservesKnownGameplayDefinitions()
        {
            // Import an in-memory legacy sample so the test never modifies source files.
            const string gridJson =
                "{\"LobbyMap\":false,\"Cells\":[[" +
                "{\"X\":0,\"Y\":0,\"Walkable\":true,\"Type\":0,\"FloorID\":\"EmptyTile4\"," +
                "\"ElementID\":\"\",\"ElementOrientation\":0.0,\"ElementScale\":0.0," +
                "\"HasElement\":false,\"FloorOrientation\":1,\"GearType\":0,\"Arg1\":0}" +
                "]],\"Width\":1,\"Height\":1," +
                "\"StartCell\":{\"X\":0,\"Y\":0},\"EndCell\":{\"X\":0,\"Y\":0}," +
                "\"Environment\":{\"Seed\":1.5}," +
                "\"Ennemies\":[{\"Type\":4,\"StartOrientation\":2," +
                "\"StartPos\":{\"X\":0,\"Y\":0},\"WayPoints\":[],\"PO\":3,\"TA\":1,\"Args\":null}]," +
                "\"MovingPlatforms\":[]}";
            const string metadataJson =
                "{\"ID\":\"legacy-test\",\"Name\":\"Legacy test\",\"Author\":\"Tests\"," +
                "\"Width\":\"1\",\"Height\":\"1\"}";

            var importResult = new LegacyMapImporter().Import(
                gridJson,
                metadataJson,
                "In-memory test");

            Assert.That(importResult.IsSuccessful, Is.True);
            Assert.That(importResult.Package.Map.Cells[0].TypeId, Is.EqualTo(CellTypeIds.Floor));
            Assert.That(importResult.Package.Map.Enemies[0].EnemyId, Is.EqualTo("enemy.scout"));

            var serializer = new MapPackageSerializer();
            var restoredPackage = serializer.Deserialize(serializer.Serialize(importResult.Package));
            Assert.That(
                MapDefinitionHasher.Compute(restoredPackage.Map),
                Is.EqualTo(MapDefinitionHasher.Compute(importResult.Package.Map)));
            Assert.That(restoredPackage.EnvironmentJson, Does.Contain("\"Seed\":1.5"));
        }
    }
}

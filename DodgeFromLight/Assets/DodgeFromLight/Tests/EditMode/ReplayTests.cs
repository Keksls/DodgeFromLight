using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Replays;
using DodgeFromLight.Core.Simulation;
using NUnit.Framework;

namespace DodgeFromLight.Tests
{
    public sealed class ReplayTests
    {
        /// <summary>
        /// Verifies that undo restores the exact previously hashed state.
        /// </summary>
        [Test]
        public void UndoRestoresPreviousStateHash()
        {
            // Record an initial hash, execute a command, then restore the snapshot.
            var engine = new SimulationEngine();
            var history = new SimulationHistory(CreateMap(), engine);
            var initialHash = SimulationStateHasher.Compute(history.CurrentState);

            history.Execute(PlayerCommand.Wait());
            var didUndo = history.TryUndo(out var restoredState);

            Assert.That(didUndo, Is.True);
            Assert.That(SimulationStateHasher.Compute(restoredState), Is.EqualTo(initialHash));
            Assert.That(history.Commands, Is.Empty);
        }

        /// <summary>
        /// Verifies that live execution and replay produce the same final state hash.
        /// </summary>
        [Test]
        public void ReplayMatchesLiveSession()
        {
            // Complete a map, export its command stream, and validate it from the start.
            var map = CreateMap();
            var engine = new SimulationEngine();
            var history = new SimulationHistory(map, engine);
            history.Execute(PlayerCommand.Move(Direction.Right));
            history.Execute(PlayerCommand.Move(Direction.Right));

            var replay = history.CreateReplay();
            var replayResult = new ReplayRunner(engine).Run(map, replay);

            Assert.That(replayResult.IsSuccessful, Is.True);
            Assert.That(replayResult.FinalState.Status, Is.EqualTo(SimulationStatus.Won));
            Assert.That(
                replayResult.FinalStateHash,
                Is.EqualTo(SimulationStateHasher.Compute(history.CurrentState)));
        }

        /// <summary>
        /// Verifies that a replay cannot target altered content hidden behind the same IDs.
        /// </summary>
        [Test]
        public void ReplayRejectsDifferentMapContent()
        {
            // Export a valid run, then substitute a map with matching IDs but different content.
            var map = CreateMap();
            var engine = new SimulationEngine();
            var history = new SimulationHistory(map, engine);
            history.Execute(PlayerCommand.Move(Direction.Right));
            history.Execute(PlayerCommand.Move(Direction.Right));

            var replay = history.CreateReplay();
            var alteredMap = CreateMap("decor.altered_floor");
            var replayResult = new ReplayRunner(engine).Run(alteredMap, replay);

            Assert.That(replayResult.IsSuccessful, Is.False);
            Assert.That(replayResult.Error, Does.Contain("exact map"));
        }

        /// <summary>
        /// Creates a three-cell map used by replay and undo tests.
        /// </summary>
        private static MapDefinition CreateMap(string floorId = "decor.test_floor")
        {
            // Keep the fixture free of systems outside the first simulation slice.
            var cells = new List<CellDefinition>();
            for (var x = 0; x < 3; x++)
            {
                cells.Add(new CellDefinition(
                    new GridPosition(x, 0),
                    CellTypeIds.Floor,
                    true,
                    floorId,
                    string.Empty,
                    0f,
                    0f,
                    false,
                    string.Empty));
            }

            return new MapDefinition(
                2,
                1,
                "test.replay",
                "test.replay.1",
                "Replay test",
                "Tests",
                3,
                1,
                new GridPosition(0, 0),
                new GridPosition(2, 0),
                cells,
                Array.Empty<EnemyDefinition>(),
                Array.Empty<MovingPlatformDefinition>());
        }
    }
}

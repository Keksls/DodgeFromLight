using System;
using System.Collections.Generic;
using System.Linq;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Replays;
using DodgeFromLight.Core.Simulation;
using NUnit.Framework;

namespace DodgeFromLight.Tests
{
    public sealed class ScoutSystemTests
    {
        /// <summary>
        /// Verifies that one Scout action advances one cell on its cyclic patrol.
        /// </summary>
        [Test]
        public void ScoutMovesOneCellPerAuthoredAction()
        {
            // Keep the player on another row so the test isolates patrol movement.
            var scout = CreateScout(
                new GridPosition(4, 1),
                Direction.Left,
                3,
                1,
                new GridPosition(4, 1),
                new GridPosition(0, 1));
            var map = CreateMap(
                5,
                2,
                new GridPosition(0, 0),
                new GridPosition(4, 0),
                scout);

            var result = new SimulationEngine().Step(
                SimulationState.CreateInitial(map),
                PlayerCommand.Wait());

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Enemies[0].Position, Is.EqualTo(new GridPosition(3, 1)));
            Assert.That(result.State.Enemies[0].Direction, Is.EqualTo(Direction.Left));
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.EnemyMoved),
                Is.True);
        }

        /// <summary>
        /// Verifies that a stationary Scout detects the player in its authored range.
        /// </summary>
        [Test]
        public void StationaryScoutDetectsPlayer()
        {
            // Use zero actions and no route to characterize a static authored Scout.
            var scout = CreateScout(
                new GridPosition(0, 0),
                Direction.Right,
                3,
                0);
            var map = CreateMap(
                4,
                1,
                new GridPosition(2, 0),
                new GridPosition(3, 0),
                scout);

            var result = new SimulationEngine().Step(
                SimulationState.CreateInitial(map),
                PlayerCommand.Wait());

            Assert.That(result.State.Status, Is.EqualTo(SimulationStatus.Lost));
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.PlayerDetected),
                Is.True);
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.GameLost),
                Is.True);
        }

        /// <summary>
        /// Verifies that non-traversable terrain blocks the Scout's line of sight.
        /// </summary>
        [Test]
        public void VoidCellBlocksScoutVision()
        {
            // Place a void cell between the stationary Scout and the player.
            var scout = CreateScout(
                new GridPosition(0, 0),
                Direction.Right,
                3,
                0);
            var map = CreateMap(
                4,
                1,
                new GridPosition(2, 0),
                new GridPosition(3, 0),
                scout,
                new GridPosition(1, 0));

            var result = new SimulationEngine().Step(
                SimulationState.CreateInitial(map),
                PlayerCommand.Wait());

            Assert.That(result.State.Status, Is.EqualTo(SimulationStatus.Running));
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.PlayerDetected),
                Is.False);
        }

        /// <summary>
        /// Verifies that detection is resolved before victory when the exit is illuminated.
        /// </summary>
        [Test]
        public void DetectionOnExitCausesLoss()
        {
            // Enter an exit that remains inside a stationary Scout's vision.
            var scout = CreateScout(
                new GridPosition(0, 0),
                Direction.Right,
                3,
                0);
            var map = CreateMap(
                4,
                1,
                new GridPosition(1, 0),
                new GridPosition(2, 0),
                scout);

            var result = new SimulationEngine().Step(
                SimulationState.CreateInitial(map),
                PlayerCommand.Move(Direction.Right));

            Assert.That(result.State.Status, Is.EqualTo(SimulationStatus.Lost));
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.GameWon),
                Is.False);
        }

        /// <summary>
        /// Verifies that Scout state participates in undo, replay, and final-state hashing.
        /// </summary>
        [Test]
        public void ScoutStateReplaysAndUndoesExactly()
        {
            // Move a Scout, export the session, then compare replay and restored snapshots.
            var scout = CreateScout(
                new GridPosition(4, 1),
                Direction.Left,
                3,
                1,
                new GridPosition(4, 1),
                new GridPosition(0, 1));
            var map = CreateMap(
                5,
                2,
                new GridPosition(0, 0),
                new GridPosition(4, 0),
                scout);
            var engine = new SimulationEngine();
            var history = new SimulationHistory(map, engine);
            var initialHash = SimulationStateHasher.Compute(history.CurrentState);

            history.Execute(PlayerCommand.Wait());
            var movedHash = SimulationStateHasher.Compute(history.CurrentState);
            var replay = history.CreateReplay();
            var replayResult = new ReplayRunner(engine).Run(map, replay);
            var didUndo = history.TryUndo(out var restoredState);

            Assert.That(movedHash, Is.Not.EqualTo(initialHash));
            Assert.That(replayResult.IsSuccessful, Is.True);
            Assert.That(replayResult.FinalStateHash, Is.EqualTo(movedHash));
            Assert.That(didUndo, Is.True);
            Assert.That(SimulationStateHasher.Compute(restoredState), Is.EqualTo(initialHash));
        }

        /// <summary>
        /// Creates one immutable Scout definition for isolated system tests.
        /// </summary>
        private static EnemyDefinition CreateScout(
            GridPosition start,
            Direction direction,
            int sightRange,
            int actionsPerTurn,
            params GridPosition[] waypoints)
        {
            // Keep test definitions explicit and independent of legacy runtime classes.
            return new EnemyDefinition(
                "enemy.scout",
                start,
                direction,
                waypoints ?? Array.Empty<GridPosition>(),
                sightRange,
                actionsPerTurn,
                string.Empty);
        }

        /// <summary>
        /// Creates a rectangular floor map with one optional vision-blocking void cell.
        /// </summary>
        private static MapDefinition CreateMap(
            int width,
            int height,
            GridPosition start,
            GridPosition exit,
            EnemyDefinition scout,
            GridPosition? voidPosition = null)
        {
            // Build every logical cell so the fixture satisfies map invariants.
            var cells = new List<CellDefinition>(width * height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new GridPosition(x, y);
                    var isVoid = voidPosition.HasValue && voidPosition.Value == position;
                    cells.Add(new CellDefinition(
                        position,
                        isVoid ? SimulationEngine.VoidCellId : SimulationEngine.FloorCellId,
                        !isVoid,
                        "decor.test_floor",
                        string.Empty,
                        0f,
                        0f,
                        false,
                        string.Empty));
                }
            }

            return new MapDefinition(
                2,
                1,
                "test.scout",
                "test.scout.1",
                "Scout test",
                "Tests",
                width,
                height,
                start,
                exit,
                cells,
                new[] { scout },
                Array.Empty<MovingPlatformDefinition>());
        }
    }
}

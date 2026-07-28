using System;
using System.Collections.Generic;
using System.Linq;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Simulation;
using NUnit.Framework;

namespace DodgeFromLight.Tests
{
    public sealed class SimulationEngineTests
    {
        /// <summary>
        /// Verifies that entering the exit wins and emits resolved presentation events.
        /// </summary>
        [Test]
        public void MoveIntoExitWins()
        {
            // Execute the smallest complete deterministic map.
            var map = CreateLinearMap(2, 0, 1);
            var engine = new SimulationEngine();
            var result = engine.Step(
                SimulationState.CreateInitial(map),
                PlayerCommand.Move(Direction.Right));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.Status, Is.EqualTo(SimulationStatus.Won));
            Assert.That(result.State.TurnIndex, Is.EqualTo(1));
            Assert.That(
                result.Events.Any(item => item.Type == SimulationEventType.GameWon),
                Is.True);
        }

        /// <summary>
        /// Verifies that an invalid movement changes neither state nor turn index.
        /// </summary>
        [Test]
        public void BlockedMoveDoesNotConsumeTurn()
        {
            // Move outside the map to exercise command rejection.
            var map = CreateLinearMap(2, 0, 1);
            var engine = new SimulationEngine();
            var initialState = SimulationState.CreateInitial(map);
            var result = engine.Step(
                initialState,
                PlayerCommand.Move(Direction.Left));

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.State, Is.SameAs(initialState));
            Assert.That(result.State.TurnIndex, Is.EqualTo(0));
            Assert.That(result.Events, Is.Empty);
        }

        /// <summary>
        /// Verifies that waiting advances the world without moving the player.
        /// </summary>
        [Test]
        public void WaitAdvancesOneTurn()
        {
            // Compare immutable state values before and after an explicit wait.
            var map = CreateLinearMap(2, 0, 1);
            var engine = new SimulationEngine();
            var initialState = SimulationState.CreateInitial(map);
            var result = engine.Step(initialState, PlayerCommand.Wait());

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.State.TurnIndex, Is.EqualTo(1));
            Assert.That(result.State.Player.Position, Is.EqualTo(initialState.Player.Position));
            Assert.That(result.StateHash, Is.Not.EqualTo(SimulationStateHasher.Compute(initialState)));
        }

        /// <summary>
        /// Creates a walkable horizontal map for isolated simulation tests.
        /// </summary>
        private static MapDefinition CreateLinearMap(int width, int startX, int exitX)
        {
            // Build every cell explicitly to satisfy map revision invariants.
            var cells = new List<CellDefinition>();
            for (var x = 0; x < width; x++)
            {
                cells.Add(new CellDefinition(
                    new GridPosition(x, 0),
                    SimulationEngine.FloorCellId,
                    true,
                    "decor.test_floor",
                    string.Empty,
                    0f,
                    0f,
                    false,
                    string.Empty));
            }

            return new MapDefinition(
                2,
                1,
                "test.linear",
                "test.linear.1",
                "Linear test",
                "Tests",
                width,
                1,
                new GridPosition(startX, 0),
                new GridPosition(exitX, 0),
                cells,
                Array.Empty<EnemyDefinition>(),
                Array.Empty<MovingPlatformDefinition>());
        }
    }
}

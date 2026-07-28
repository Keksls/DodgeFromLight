using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class SimulationState
    {
        public MapDefinition Map { get; }
        public PlayerState Player { get; }
        public IReadOnlyList<EnemyState> Enemies { get; }
        public int TurnIndex { get; }
        public SimulationStatus Status { get; }

        /// <summary>
        /// Creates an immutable simulation state with explicit enemy runtime states.
        /// </summary>
        public SimulationState(
            MapDefinition map,
            PlayerState player,
            IEnumerable<EnemyState> enemies,
            int turnIndex,
            SimulationStatus status)
        {
            // Keep all authoritative state explicit for undo and replay.
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            if (turnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            if (!Enum.IsDefined(typeof(SimulationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (!Map.IsInside(Player.Position))
                throw new ArgumentException("The player must be inside the map.", nameof(player));

            var copiedEnemies = new List<EnemyState>(
                enemies ?? throw new ArgumentNullException(nameof(enemies)));
            if (copiedEnemies.Count != Map.Enemies.Count)
            {
                throw new ArgumentException(
                    "The simulation must contain one state per enemy definition.",
                    nameof(enemies));
            }

            for (var index = 0; index < copiedEnemies.Count; index++)
            {
                var enemy = copiedEnemies[index];
                if (enemy == null || enemy.DefinitionIndex != index)
                {
                    throw new ArgumentException(
                        "Enemy states must follow map-definition order.",
                        nameof(enemies));
                }

                if (!Map.IsInside(enemy.Position))
                {
                    throw new ArgumentException(
                        "Every enemy state must remain inside the map.",
                        nameof(enemies));
                }
            }

            Enemies = new ReadOnlyCollection<EnemyState>(copiedEnemies);
            TurnIndex = turnIndex;
            Status = status;
        }

        /// <summary>
        /// Creates the initial state for a map revision.
        /// </summary>
        public static SimulationState CreateInitial(MapDefinition map)
        {
            // Spawn the player at the authored start position facing upward.
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var status = map.StartPosition == map.ExitPosition
                ? SimulationStatus.Won
                : SimulationStatus.Running;
            return new SimulationState(
                map,
                new PlayerState(map.StartPosition, Direction.Up),
                CreateInitialEnemyStates(map),
                0,
                status);
        }

        /// <summary>
        /// Creates the initial runtime state for every authored enemy placement.
        /// </summary>
        private static IReadOnlyList<EnemyState> CreateInitialEnemyStates(MapDefinition map)
        {
            // Preserve definition order so events and replays use stable enemy indices.
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var enemies = new List<EnemyState>(map.Enemies.Count);
            for (var index = 0; index < map.Enemies.Count; index++)
            {
                var definition = map.Enemies[index];
                enemies.Add(new EnemyState(
                    index,
                    definition.StartPosition,
                    definition.StartDirection,
                    0,
                    false));
            }

            return new ReadOnlyCollection<EnemyState>(enemies);
        }
    }
}

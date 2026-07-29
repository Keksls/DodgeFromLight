using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class SimulationState
    {
        public MapDefinition Map { get; }
        public PlayerState Player { get; }
        public BoardState Board { get; }
        public IReadOnlyList<EnemyState> Enemies { get; }
        public IReadOnlyList<MovingPlatformState> MovingPlatforms { get; }
        public DeterministicRandomState Random { get; }
        public int TurnIndex { get; }
        public SimulationStatus Status { get; }

        /// <summary>
        /// Creates an immutable simulation state with explicit enemy runtime states.
        /// </summary>
        public SimulationState(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            int turnIndex,
            SimulationStatus status)
            : this(
                map,
                player,
                board,
                enemies,
                CreateInitialPlatformStates(map),
                DeterministicRandomState.CreateInitial(map?.MapId, map?.RevisionId),
                turnIndex,
                status)
        {
            // Keep the original constructor available for maps without platform state.
        }

        /// <summary>
        /// Creates an immutable simulation state with every authoritative subsystem.
        /// </summary>
        public SimulationState(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            IEnumerable<MovingPlatformState> movingPlatforms,
            DeterministicRandomState random,
            int turnIndex,
            SimulationStatus status)
        {
            // Keep all authoritative state explicit for undo and replay.
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            if (turnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            if (!Enum.IsDefined(typeof(SimulationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (!Map.IsInside(Player.Position))
                throw new ArgumentException("The player must be inside the map.", nameof(player));
            if (Player.AnchorPosition.HasValue && !Map.IsInside(Player.AnchorPosition.Value))
                throw new ArgumentException("The player anchor must be inside the map.", nameof(player));
            if (Player.CheckpointPosition.HasValue &&
                !Map.IsInside(Player.CheckpointPosition.Value))
            {
                throw new ArgumentException(
                    "The active checkpoint must be inside the map.",
                    nameof(player));
            }

            foreach (var position in Board.CollapsedCells)
            {
                if (!Map.TryGetCell(position, out var cell) ||
                    cell.TypeId != CellTypeIds.Collapse)
                {
                    throw new ArgumentException(
                        "Collapsed state must reference an authored collapse cell.",
                        nameof(board));
                }
            }

            foreach (var timedBomb in Board.TimedBombs)
            {
                if (!Map.TryGetCell(timedBomb.Position, out var cell) ||
                    cell.TypeId != CellTypeIds.TimedBomb)
                {
                    throw new ArgumentException(
                        "Timed-bomb state must reference an authored timed-bomb cell.",
                        nameof(board));
                }
            }

            foreach (var needle in Board.Needles)
            {
                if (!Map.TryGetCell(needle.Position, out var cell) ||
                    cell.TypeId != CellTypeIds.Needles)
                {
                    throw new ArgumentException(
                        "Needle state must reference an authored needle cell.",
                        nameof(board));
                }
            }

            foreach (var position in Board.RemovedGearCells)
            {
                if (!Map.TryGetCell(position, out var cell) ||
                    string.IsNullOrEmpty(cell.GearId))
                {
                    throw new ArgumentException(
                        "Removed-gear state must reference an authored gear cell.",
                        nameof(board));
                }
            }

            foreach (var position in Board.DestroyedElementCells)
            {
                if (!Map.TryGetCell(position, out var cell) ||
                    string.IsNullOrEmpty(cell.ElementId))
                {
                    throw new ArgumentException(
                        "Destroyed-element state must reference an authored element.",
                        nameof(board));
                }
            }

            foreach (var placedObject in Board.PlacedObjects)
            {
                if (!Map.IsInside(placedObject.Position))
                {
                    throw new ArgumentException(
                        "Every placed object must remain inside the map.",
                        nameof(board));
                }
            }

            var authoredTimedBombCount = 0;
            var authoredNeedleCount = 0;
            foreach (var cell in Map.Cells)
            {
                if (cell.TypeId == CellTypeIds.TimedBomb)
                    authoredTimedBombCount++;
                else if (cell.TypeId == CellTypeIds.Needles)
                    authoredNeedleCount++;
            }

            if (Board.TimedBombs.Count != authoredTimedBombCount)
            {
                throw new ArgumentException(
                    "The board must contain one state per authored timed-bomb cell.",
                    nameof(board));
            }

            if (Board.Needles.Count != authoredNeedleCount)
            {
                throw new ArgumentException(
                    "The board must contain one state per authored needle cell.",
                    nameof(board));
            }

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

            var copiedPlatforms = new List<MovingPlatformState>(
                movingPlatforms ?? throw new ArgumentNullException(nameof(movingPlatforms)));
            if (copiedPlatforms.Count != Map.MovingPlatforms.Count)
            {
                throw new ArgumentException(
                    "The simulation must contain one state per moving-platform definition.",
                    nameof(movingPlatforms));
            }

            for (var index = 0; index < copiedPlatforms.Count; index++)
            {
                var platform = copiedPlatforms[index];
                if (platform == null || platform.DefinitionIndex != index)
                {
                    throw new ArgumentException(
                        "Moving-platform states must follow map-definition order.",
                        nameof(movingPlatforms));
                }

                if (!Map.IsInside(platform.Position))
                {
                    throw new ArgumentException(
                        "Every moving platform must remain inside the map.",
                        nameof(movingPlatforms));
                }
            }

            MovingPlatforms = new ReadOnlyCollection<MovingPlatformState>(copiedPlatforms);
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
                BoardState.CreateInitial(map),
                CreateInitialEnemyStates(map),
                CreateInitialPlatformStates(map),
                DeterministicRandomState.CreateInitial(map.MapId, map.RevisionId),
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

        /// <summary>
        /// Creates the initial runtime state for every authored moving platform.
        /// </summary>
        private static IReadOnlyList<MovingPlatformState> CreateInitialPlatformStates(
            MapDefinition map)
        {
            // Preserve definition order so remote control and events use stable indices.
            if (map == null)
                return Array.Empty<MovingPlatformState>();

            var platforms = new List<MovingPlatformState>(map.MovingPlatforms.Count);
            for (var index = 0; index < map.MovingPlatforms.Count; index++)
            {
                var definition = map.MovingPlatforms[index];
                platforms.Add(new MovingPlatformState(
                    index,
                    definition.StartPosition,
                    definition.StartDirection,
                    0,
                    false));
            }

            return new ReadOnlyCollection<MovingPlatformState>(platforms);
        }
    }
}

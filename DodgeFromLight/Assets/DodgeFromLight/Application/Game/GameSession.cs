using System.Collections.Generic;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Replays;
using DodgeFromLight.Core.Simulation;

namespace DodgeFromLight.Application.Game
{
    /// <summary>
    /// Orchestrates one deterministic puzzle attempt for presentation and UI consumers.
    /// </summary>
    public sealed class GameSession
    {
        private readonly SimulationEngine _engine;
        private readonly SimulationHistory _history;

        public SimulationState CurrentState => _history.CurrentState;
        public bool CanUndo => _history.CanUndo;
        public bool CanCreateReplay => _history.CanCreateReplay;
        public int CommandCount => _history.Commands.Count;

        /// <summary>
        /// Creates a replayable attempt from the authored initial map state.
        /// </summary>
        public GameSession(MapDefinition map)
        {
            // Live execution and history share one deterministic engine instance.
            _engine = new SimulationEngine();
            _history = new SimulationHistory(map, _engine);
        }

        /// <summary>
        /// Creates a non-competitive attempt from an explicit restored state.
        /// </summary>
        public GameSession(SimulationState restoredState)
        {
            // Checkpoint attempts support undo but cannot claim a full-map replay.
            _engine = new SimulationEngine();
            _history = new SimulationHistory(restoredState, _engine);
        }

        /// <summary>
        /// Executes and records one accepted player command.
        /// </summary>
        public SimulationStepResult Execute(PlayerCommand command)
        {
            // SimulationHistory records only commands accepted by the Core.
            return _history.Execute(command);
        }

        /// <summary>
        /// Restores the exact state preceding the latest accepted command.
        /// </summary>
        public bool TryUndo(out SimulationState restoredState)
        {
            // Immutable snapshots make undo independent from Unity presentation state.
            return _history.TryUndo(out restoredState);
        }

        /// <summary>
        /// Creates a replay bound to the exact map revision and final state hash.
        /// </summary>
        public ReplayDefinition CreateReplay()
        {
            // SimulationHistory rejects export for attempts resumed from checkpoints.
            return _history.CreateReplay();
        }

        /// <summary>
        /// Creates an immutable read model for HUD and result-screen consumers.
        /// </summary>
        public GameSessionReadModel GetReadModel()
        {
            // UI-specific prompts are derived from the same authoritative snapshot.
            var state = CurrentState;
            var player = state.Player;
            var canPickUpGear = false;
            var canActivateCheckpoint = false;
            if (state.Map.TryGetCell(player.Position, out var cell))
            {
                canPickUpGear =
                    !string.IsNullOrEmpty(cell.GearId) &&
                    state.Board.IsGearAvailable(player.Position);
                canActivateCheckpoint =
                    cell.TypeId == CellTypeIds.Checkpoint &&
                    player.CheckpointPosition != player.Position;
            }

            return new GameSessionReadModel(
                state.Map.MapId,
                state.Map.Title,
                state.TurnIndex,
                ToSessionStatus(state.Status),
                CanUndo,
                CanCreateReplay,
                player.GearSlotOne,
                player.GearSlotTwo,
                player.SpeedBoostTurns,
                player.InvisibilityTurns,
                player.TimeStopTurns,
                player.CheckpointPosition.HasValue,
                canPickUpGear,
                canActivateCheckpoint);
        }

        /// <summary>
        /// Converts the Core lifecycle value into the application UI contract.
        /// </summary>
        private static GameSessionStatus ToSessionStatus(SimulationStatus status)
        {
            // Application consumers do not depend on Core status types.
            switch (status)
            {
                case SimulationStatus.Won:
                    return GameSessionStatus.Won;
                case SimulationStatus.Lost:
                    return GameSessionStatus.Lost;
                default:
                    return GameSessionStatus.Running;
            }
        }

        /// <summary>
        /// Gets the lethal danger cells of one enemy in the current state.
        /// </summary>
        public IReadOnlyList<GridPosition> GetEnemyDangerCells(int enemyIndex)
        {
            // Presentation consumes the same pure query used by Core detection.
            return _engine.GetEnemyDangerCells(CurrentState, enemyIndex);
        }

        /// <summary>
        /// Gets the non-lethal secondary effect cells of one enemy in the current state.
        /// </summary>
        public IReadOnlyList<GridPosition> GetEnemySecondaryEffectCells(
            int enemyIndex)
        {
            // Gloutton attraction remains visually distinct from lethal danger.
            return _engine.GetEnemySecondaryEffectCells(
                CurrentState,
                enemyIndex);
        }
    }
}

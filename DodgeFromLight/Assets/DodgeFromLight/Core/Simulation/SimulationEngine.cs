using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Systems;
using CorePlayerState = DodgeFromLight.Core.Model.PlayerState;

namespace DodgeFromLight.Core.Simulation
{
    public sealed class SimulationEngine
    {
        public const string FloorCellId = "cell.floor";
        public const string VoidCellId = "cell.void";

        /// <summary>
        /// Resolves one player command against an immutable simulation state.
        /// </summary>
        public SimulationStepResult Step(SimulationState state, PlayerCommand command)
        {
            // Validate the session and feature scope before applying any mutation.
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var currentHash = SimulationStateHasher.Compute(state);
            if (state.Status != SimulationStatus.Running)
                return SimulationStepResult.CreateRejected(
                    state,
                    "The simulation has already ended.",
                    currentHash);

            if (!SupportsMap(state.Map, out var unsupportedReason))
                return SimulationStepResult.CreateRejected(state, unsupportedReason, currentHash);

            switch (command.Type)
            {
                case PlayerCommandType.Move:
                    return ResolveMove(state, command.Direction);
                case PlayerCommandType.Wait:
                    return ResolveWait(state);
                default:
                    return SimulationStepResult.CreateRejected(
                        state,
                        "The command type is not supported.",
                        currentHash);
            }
        }

        /// <summary>
        /// Resolves a one-cell cardinal player movement.
        /// </summary>
        private static SimulationStepResult ResolveMove(SimulationState state, Direction direction)
        {
            // Reject blocked moves without consuming a turn.
            var targetPosition = state.Player.Position.Translate(direction);
            if (!state.Map.TryGetCell(targetPosition, out var targetCell) || !targetCell.IsTraversable)
            {
                return SimulationStepResult.CreateRejected(
                    state,
                    "The target cell is not traversable.",
                    SimulationStateHasher.Compute(state));
            }

            var nextTurn = state.TurnIndex + 1;
            var events = new List<SimulationEvent>
            {
                new SimulationEvent(
                    SimulationEventType.PlayerMoved,
                    nextTurn,
                    state.Player.Position,
                    targetPosition,
                    direction)
            };

            return ResolveAcceptedTurn(
                state,
                new CorePlayerState(targetPosition, direction),
                nextTurn,
                events);
        }

        /// <summary>
        /// Resolves an explicit wait command.
        /// </summary>
        private static SimulationStepResult ResolveWait(SimulationState state)
        {
            // Advance the turn while preserving player position and orientation.
            var nextTurn = state.TurnIndex + 1;
            var events = new List<SimulationEvent>
            {
                new SimulationEvent(
                    SimulationEventType.PlayerWaited,
                    nextTurn,
                    state.Player.Position,
                    state.Player.Position,
                    state.Player.Direction)
            };

            return ResolveAcceptedTurn(
                state,
                state.Player,
                nextTurn,
                events);
        }

        /// <summary>
        /// Resolves enemies, detection, objective state, and completion for an accepted player action.
        /// </summary>
        private static SimulationStepResult ResolveAcceptedTurn(
            SimulationState previousState,
            CorePlayerState player,
            int nextTurn,
            List<SimulationEvent> events)
        {
            // Resolve danger before the exit, matching the characterized legacy turn order.
            var enemyStates = ScoutSystem.ResolveTurn(
                previousState.Map,
                previousState.Enemies,
                player.Position,
                nextTurn,
                events,
                out var detectingEnemyIndex);
            var status = detectingEnemyIndex >= 0
                ? SimulationStatus.Lost
                : player.Position == previousState.Map.ExitPosition
                    ? SimulationStatus.Won
                    : SimulationStatus.Running;

            if (status == SimulationStatus.Lost)
            {
                var detectingEnemy = enemyStates[detectingEnemyIndex];
                events.Add(new SimulationEvent(
                    SimulationEventType.GameLost,
                    nextTurn,
                    detectingEnemy.Position,
                    player.Position,
                    detectingEnemy.Direction,
                    detectingEnemyIndex));
            }
            else if (status == SimulationStatus.Won)
            {
                events.Add(new SimulationEvent(
                    SimulationEventType.GameWon,
                    nextTurn,
                    player.Position,
                    player.Position,
                    player.Direction));
            }

            events.Add(new SimulationEvent(
                SimulationEventType.TurnCompleted,
                nextTurn,
                player.Position,
                player.Position,
                player.Direction));

            var nextState = new SimulationState(
                previousState.Map,
                player,
                enemyStates,
                nextTurn,
                status);
            return SimulationStepResult.CreateAccepted(
                previousState,
                nextState,
                events,
                SimulationStateHasher.Compute(nextState));
        }

        /// <summary>
        /// Reports whether every authored map feature is supported by this first simulation slice.
        /// </summary>
        private static bool SupportsMap(MapDefinition map, out string rejectionReason)
        {
            // Refuse incomplete feature parity instead of silently ignoring authored content.
            if (map.MovingPlatforms.Count > 0)
            {
                rejectionReason = "Moving-platform simulation is not implemented in the current ruleset slice.";
                return false;
            }

            foreach (var cell in map.Cells)
            {
                if (cell.TypeId != FloorCellId && cell.TypeId != VoidCellId)
                {
                    rejectionReason = "Cell type '" + cell.TypeId + "' is not implemented in the current ruleset slice.";
                    return false;
                }

                if (!string.IsNullOrEmpty(cell.GearId))
                {
                    rejectionReason = "Placed gears are not implemented in the current ruleset slice.";
                    return false;
                }
            }

            if (!ScoutSystem.Supports(map, out rejectionReason))
                return false;

            rejectionReason = string.Empty;
            return true;
        }
    }
}

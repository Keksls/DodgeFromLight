using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class MovingPlatformSystem
    {
        /// <summary>
        /// Reports whether every authored moving-platform route is structurally valid.
        /// </summary>
        internal static bool Supports(
            MapDefinition map,
            out string rejectionReason)
        {
            // Platforms may cross holes, but their routes must remain cardinal and in bounds.
            foreach (var platform in map.MovingPlatforms)
            {
                if (!PatrolRouteBuilder.TryBuild(
                        platform.Waypoints,
                        out var route,
                        out rejectionReason))
                {
                    return false;
                }
                if (route.Count > 0 && route[0] != platform.StartPosition)
                {
                    rejectionReason =
                        "Moving-platform routes must begin at their authored start position.";
                    return false;
                }
                foreach (var position in route)
                {
                    if (!map.IsInside(position))
                    {
                        rejectionReason =
                            "Moving-platform routes must remain inside map bounds.";
                        return false;
                    }
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Resolves every moving platform and carries a player standing on it.
        /// </summary>
        internal static PlatformTurnResult ResolveTurn(
            MapDefinition map,
            IReadOnlyList<MovingPlatformState> currentStates,
            PlayerState player,
            bool isTimeStopped,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Resolve platforms in stable definition order for deterministic collisions.
            var states = new List<MovingPlatformState>(currentStates);
            var currentPlayer = player;
            if (isTimeStopped)
                return new PlatformTurnResult(currentPlayer, states);

            for (var definitionIndex = 0;
                 definitionIndex < map.MovingPlatforms.Count;
                 definitionIndex++)
            {
                var definition = map.MovingPlatforms[definitionIndex];
                var state = states[definitionIndex];
                if (!PatrolRouteBuilder.TryBuild(
                        definition.Waypoints,
                        out var route,
                        out var routeError))
                {
                    throw new InvalidOperationException(routeError);
                }

                for (var actionIndex = 0;
                     actionIndex < definition.ActionsPerTurn && route.Count > 1;
                     actionIndex++)
                {
                    // Platforms translate directly; orientation changes are presentation only.
                    var nextPatrolIndex = state.IsPatrolReversed
                        ? (state.PatrolIndex - 1 + route.Count) % route.Count
                        : (state.PatrolIndex + 1) % route.Count;
                    var target = route[nextPatrolIndex];
                    if (IsOccupiedByPlatform(target, definitionIndex, states))
                    {
                        state = CopyPlatform(
                            state,
                            direction: DirectionUtility.Opposite(state.Direction),
                            isPatrolReversed: !state.IsPatrolReversed);
                        states[definitionIndex] = state;
                        events.Add(new SimulationEvent(
                            SimulationEventType.PlatformReversed,
                            turnIndex,
                            state.Position,
                            state.Position,
                            state.Direction,
                            definitionIndex));
                        continue;
                    }

                    var direction = PatrolRouteBuilder.GetDirection(state.Position, target);
                    var carriesPlayer = currentPlayer.Position == state.Position;
                    var previousPosition = state.Position;
                    state = CopyPlatform(
                        state,
                        position: target,
                        direction: direction,
                        patrolIndex: nextPatrolIndex);
                    states[definitionIndex] = state;
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlatformMoved,
                        turnIndex,
                        previousPosition,
                        target,
                        direction,
                        definitionIndex));

                    if (!carriesPlayer)
                        continue;

                    currentPlayer = CopyPlayer(currentPlayer, target);
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerForcedMoved,
                        turnIndex,
                        previousPosition,
                        target,
                        direction,
                        definitionIndex,
                        "object.moving_platform"));
                }
            }

            return new PlatformTurnResult(currentPlayer, states);
        }

        /// <summary>
        /// Tests whether any moving platform currently supports a logical position.
        /// </summary>
        internal static bool HasPlatformAt(
            IReadOnlyList<MovingPlatformState> states,
            GridPosition position)
        {
            // Platform support makes an otherwise void cell safe for the player.
            foreach (var state in states)
            {
                if (state.Position == position)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Tests whether another platform already occupies a route target.
        /// </summary>
        private static bool IsOccupiedByPlatform(
            GridPosition position,
            int ignoredDefinitionIndex,
            IReadOnlyList<MovingPlatformState> states)
        {
            // Earlier definition indices win deterministic simultaneous collisions.
            foreach (var state in states)
            {
                if (state.DefinitionIndex != ignoredDefinitionIndex &&
                    state.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies platform state while replacing selected values.
        /// </summary>
        private static MovingPlatformState CopyPlatform(
            MovingPlatformState source,
            GridPosition? position = null,
            Direction? direction = null,
            int? patrolIndex = null,
            bool? isPatrolReversed = null)
        {
            // Preserve every untouched route value explicitly.
            return new MovingPlatformState(
                source.DefinitionIndex,
                position ?? source.Position,
                direction ?? source.Direction,
                patrolIndex ?? source.PatrolIndex,
                isPatrolReversed ?? source.IsPatrolReversed);
        }

        /// <summary>
        /// Copies player state after a platform carries it.
        /// </summary>
        private static PlayerState CopyPlayer(
            PlayerState source,
            GridPosition position)
        {
            // Carrying changes only logical position.
            return new PlayerState(
                position,
                source.Direction,
                source.GearSlotOne,
                source.GearSlotTwo,
                source.SpeedBoostTurns,
                source.InvisibilityTurns,
                source.TimeStopTurns,
                source.AnchorPosition,
                source.CheckpointPosition,
                source.HasUsedCheckpoint);
        }
    }
}

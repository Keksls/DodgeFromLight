using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class ScoutSystem
    {
        internal const string EnemyId = "enemy.scout";

        /// <summary>
        /// Reports whether the current Scout slice can resolve every enemy on a map.
        /// </summary>
        internal static bool Supports(MapDefinition map, out string rejectionReason)
        {
            // Reject unsupported enemy identities and unresolved multi-enemy collisions.
            var routeOwners = new Dictionary<GridPosition, int>();
            for (var definitionIndex = 0; definitionIndex < map.Enemies.Count; definitionIndex++)
            {
                var definition = map.Enemies[definitionIndex];
                if (!string.Equals(definition.EnemyId, EnemyId, StringComparison.Ordinal))
                {
                    rejectionReason =
                        "Enemy type '" + definition.EnemyId + "' is not implemented in the current ruleset slice.";
                    return false;
                }

                if (!PatrolRouteBuilder.TryBuild(
                        definition.Waypoints,
                        out var route,
                        out rejectionReason))
                {
                    return false;
                }

                if (!map.TryGetCell(definition.StartPosition, out var startCell) ||
                    !startCell.IsTraversable)
                {
                    rejectionReason = "A Scout must start on a traversable cell.";
                    return false;
                }

                if (route.Count > 0 && route[0] != definition.StartPosition)
                {
                    rejectionReason = "A Scout patrol must begin at its authored start position.";
                    return false;
                }

                IReadOnlyList<GridPosition> occupiedPositions;
                if (route.Count > 0)
                    occupiedPositions = route;
                else
                    occupiedPositions = new[] { definition.StartPosition };
                foreach (var position in occupiedPositions)
                {
                    if (routeOwners.TryGetValue(position, out var otherDefinitionIndex) &&
                        otherDefinitionIndex != definitionIndex)
                    {
                        rejectionReason =
                            "Overlapping enemy patrol routes require a later collision rule.";
                        return false;
                    }

                    routeOwners[position] = definitionIndex;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Resolves all Scout micro-actions in stable map-definition order.
        /// </summary>
        internal static IReadOnlyList<EnemyState> ResolveTurn(
            MapDefinition map,
            IReadOnlyList<EnemyState> currentStates,
            GridPosition playerPosition,
            int turnIndex,
            ICollection<SimulationEvent> events,
            out int detectingEnemyIndex)
        {
            // Work on a private list and expose a new immutable enemy-state snapshot.
            var nextStates = new List<EnemyState>(currentStates);
            detectingEnemyIndex = -1;

            for (var definitionIndex = 0;
                 definitionIndex < map.Enemies.Count;
                 definitionIndex++)
            {
                var definition = map.Enemies[definitionIndex];
                var state = nextStates[definitionIndex];
                if (!PatrolRouteBuilder.TryBuild(
                        definition.Waypoints,
                        out var route,
                        out var routeError))
                {
                    throw new InvalidOperationException(routeError);
                }

                if (route.Count > 0 && state.PatrolIndex >= route.Count)
                    throw new InvalidOperationException("The Scout patrol index is outside its route.");

                var performedAction = false;
                for (var actionIndex = 0;
                     actionIndex < definition.ActionsPerTurn && route.Count > 0;
                     actionIndex++)
                {
                    // A rotation, reversal, or translation consumes exactly one enemy action.
                    performedAction = true;
                    state = ResolveMicroAction(
                        map,
                        definitionIndex,
                        state,
                        route,
                        nextStates,
                        turnIndex,
                        events);
                    nextStates[definitionIndex] = state;

                    if (CanDetectPlayer(map, state, nextStates, playerPosition))
                    {
                        detectingEnemyIndex = definitionIndex;
                        events.Add(new SimulationEvent(
                            SimulationEventType.PlayerDetected,
                            turnIndex,
                            state.Position,
                            playerPosition,
                            state.Direction,
                            definitionIndex));
                        return new ReadOnlyCollection<EnemyState>(nextStates);
                    }
                }

                if (!performedAction &&
                    CanDetectPlayer(map, state, nextStates, playerPosition))
                {
                    detectingEnemyIndex = definitionIndex;
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerDetected,
                        turnIndex,
                        state.Position,
                        playerPosition,
                        state.Direction,
                        definitionIndex));
                    return new ReadOnlyCollection<EnemyState>(nextStates);
                }
            }

            return new ReadOnlyCollection<EnemyState>(nextStates);
        }

        /// <summary>
        /// Resolves one movement, rotation, or blocked-route reversal for a Scout.
        /// </summary>
        private static EnemyState ResolveMicroAction(
            MapDefinition map,
            int definitionIndex,
            EnemyState state,
            IReadOnlyList<GridPosition> route,
            IReadOnlyList<EnemyState> allStates,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Select the next cyclic route position according to the current traversal direction.
            var nextPatrolIndex = state.IsPatrolReversed
                ? (state.PatrolIndex - 1 + route.Count) % route.Count
                : (state.PatrolIndex + 1) % route.Count;
            var targetPosition = route[nextPatrolIndex];
            var targetDirection = PatrolRouteBuilder.GetDirection(
                state.Position,
                targetPosition);

            if (state.Direction != targetDirection)
            {
                var turnedState = new EnemyState(
                    definitionIndex,
                    state.Position,
                    targetDirection,
                    state.PatrolIndex,
                    state.IsPatrolReversed);
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyTurned,
                    turnIndex,
                    state.Position,
                    state.Position,
                    targetDirection,
                    definitionIndex));
                return turnedState;
            }

            if (!map.TryGetCell(targetPosition, out var targetCell) ||
                !targetCell.IsTraversable ||
                IsOccupiedByEnemy(targetPosition, definitionIndex, allStates))
            {
                var reversedDirection =
                    PatrolRouteBuilder.GetOppositeDirection(state.Direction);
                var reversedState = new EnemyState(
                    definitionIndex,
                    state.Position,
                    reversedDirection,
                    state.PatrolIndex,
                    !state.IsPatrolReversed);
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyTurned,
                    turnIndex,
                    state.Position,
                    state.Position,
                    reversedDirection,
                    definitionIndex));
                return reversedState;
            }

            var movedState = new EnemyState(
                definitionIndex,
                targetPosition,
                state.Direction,
                nextPatrolIndex,
                state.IsPatrolReversed);
            events.Add(new SimulationEvent(
                SimulationEventType.EnemyMoved,
                turnIndex,
                state.Position,
                targetPosition,
                state.Direction,
                definitionIndex));
            return movedState;
        }

        /// <summary>
        /// Tests whether a Scout currently sees or occupies the player's cell.
        /// </summary>
        private static bool CanDetectPlayer(
            MapDefinition map,
            EnemyState scout,
            IReadOnlyList<EnemyState> allStates,
            GridPosition playerPosition)
        {
            // The Scout sees straight ahead until range, bounds, terrain, or another enemy blocks it.
            if (scout.Position == playerPosition)
                return true;

            var position = scout.Position;
            var definition = map.Enemies[scout.DefinitionIndex];
            for (var distance = 1; distance <= definition.SightRange; distance++)
            {
                position = position.Translate(scout.Direction);
                if (!map.TryGetCell(position, out var cell) || !cell.IsTraversable)
                    return false;
                if (IsOccupiedByEnemy(position, scout.DefinitionIndex, allStates))
                    return false;
                if (position == playerPosition)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Tests whether another enemy currently occupies a logical position.
        /// </summary>
        private static bool IsOccupiedByEnemy(
            GridPosition position,
            int ignoredDefinitionIndex,
            IReadOnlyList<EnemyState> states)
        {
            // Ignore the querying Scout and compare authoritative logical positions only.
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
    }
}

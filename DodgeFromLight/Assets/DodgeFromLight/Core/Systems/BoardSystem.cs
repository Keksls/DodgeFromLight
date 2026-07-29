using System;
using System.Collections.Generic;
using System.Globalization;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class BoardSystem
    {
        /// <summary>
        /// Reports whether every authored cell can be resolved by the current board slice.
        /// </summary>
        internal static bool Supports(MapDefinition map, out string rejectionReason)
        {
            // Reject unknown behavior instead of silently treating special cells as floor.
            foreach (var cell in map.Cells)
            {
                switch (cell.TypeId)
                {
                    case CellTypeIds.Floor:
                        break;
                    case CellTypeIds.Void:
                        if (cell.IsWalkable)
                        {
                            rejectionReason =
                                "Void cell " + cell.Position + " cannot be walkable.";
                            return false;
                        }

                        break;
                    case CellTypeIds.Teleporter:
                    case CellTypeIds.SpeedBoost:
                    case CellTypeIds.Checkpoint:
                    case CellTypeIds.SwordSocket:
                        if (!cell.IsWalkable)
                        {
                            rejectionReason =
                                "Special cell " + cell.Position + " must be walkable.";
                            return false;
                        }

                        break;
                    case CellTypeIds.Collapse:
                        if (!cell.IsTraversable)
                        {
                            rejectionReason =
                                "Collapse cell " + cell.Position + " must start traversable.";
                            return false;
                        }

                        break;
                    case CellTypeIds.Spring:
                        if (!cell.IsTraversable ||
                            !BoardState.TryReadPositiveInteger(
                                cell,
                                CellParameterIds.SpringDistance,
                                out _) ||
                            !TryReadSpringDirection(cell, out _))
                        {
                            rejectionReason =
                                "Spring cell " + cell.Position +
                                " requires traversable terrain, a positive distance, and a cardinal direction.";
                            return false;
                        }

                        break;
                    case CellTypeIds.TimedBomb:
                        if (!cell.IsTraversable ||
                            !BoardState.TryReadPositiveInteger(
                                cell,
                                CellParameterIds.TimedBombTurns,
                                out _))
                        {
                            rejectionReason =
                                "Timed-bomb cell " + cell.Position +
                                " requires traversable terrain and a positive countdown.";
                            return false;
                        }

                        break;
                    case CellTypeIds.Needles:
                        if (!cell.IsTraversable ||
                            !BoardState.TryReadNeedleInitialState(cell, out _))
                        {
                            rejectionReason =
                                "Needle cell " + cell.Position +
                                " requires traversable terrain and an initial phase of 0 or 1.";
                            return false;
                        }

                        break;
                    default:
                        rejectionReason =
                            "Cell type '" + cell.TypeId +
                            "' is not implemented in the current ruleset slice.";
                        return false;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Advances cell phases that resolve at the beginning of an accepted turn.
        /// </summary>
        internal static BoardState BeginTurn(
            BoardState state,
            bool isTimeStopped,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Needles alternate before the player's action, matching the characterized rule.
            if (isTimeStopped || state.Needles.Count == 0)
                return state;

            var nextNeedles = new List<NeedleState>(state.Needles.Count);
            foreach (var needle in state.Needles)
            {
                var nextNeedle = needle.Toggle();
                nextNeedles.Add(nextNeedle);
                events.Add(new SimulationEvent(
                    nextNeedle.IsActive
                        ? SimulationEventType.NeedlesActivated
                        : SimulationEventType.NeedlesDeactivated,
                    turnIndex,
                    needle.Position,
                    needle.Position,
                    Direction.None));
            }

            return new BoardState(
                state.CollapsedCells,
                state.RemovedGearCells,
                state.DestroyedElementCells,
                state.TimedBombs,
                nextNeedles,
                state.PlacedObjects);
        }

        /// <summary>
        /// Collapses a fragile cell after the player leaves it.
        /// </summary>
        internal static BoardState ResolvePlayerDeparture(
            MapDefinition map,
            BoardState state,
            GridPosition from,
            GridPosition to,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Waiting in place never collapses the supporting cell.
            if (from == to ||
                !map.TryGetCell(from, out var departedCell) ||
                departedCell.TypeId != CellTypeIds.Collapse ||
                state.IsCellDestroyed(from))
            {
                return state;
            }

            var collapsedCells = new List<GridPosition>(state.CollapsedCells)
            {
                from
            };
            events.Add(new SimulationEvent(
                SimulationEventType.CellCollapsed,
                turnIndex,
                from,
                from,
                Direction.None));
            return new BoardState(
                collapsedCells,
                state.RemovedGearCells,
                state.DestroyedElementCells,
                state.TimedBombs,
                state.Needles,
                state.PlacedObjects);
        }

        /// <summary>
        /// Advances timed bombs and reports an explosion on the player's current cell.
        /// </summary>
        internal static BoardState EndTurn(
            BoardState state,
            GridPosition playerPosition,
            bool isTimeStopped,
            int turnIndex,
            ICollection<SimulationEvent> events,
            out GridPosition? lethalExplosionPosition)
        {
            // Bombs tick after movement and enemy resolution, then destroy their own cells.
            lethalExplosionPosition = null;
            if (isTimeStopped)
                return state;

            var nextTimedBombs = new List<TimedBombState>(state.TimedBombs.Count);
            foreach (var timedBomb in state.TimedBombs)
            {
                var nextTimedBomb = timedBomb.Tick();
                nextTimedBombs.Add(nextTimedBomb);
                if (timedBomb.HasExploded)
                    continue;

                events.Add(new SimulationEvent(
                    SimulationEventType.TimedBombTicked,
                    turnIndex,
                    timedBomb.Position,
                    timedBomb.Position,
                    Direction.None));
                if (!nextTimedBomb.HasExploded)
                    continue;

                events.Add(new SimulationEvent(
                    SimulationEventType.TimedBombExploded,
                    turnIndex,
                    timedBomb.Position,
                    timedBomb.Position,
                    Direction.None));
                if (timedBomb.Position == playerPosition)
                    lethalExplosionPosition = timedBomb.Position;
            }

            var nextPlacedObjects = new List<PlacedObjectState>(state.PlacedObjects.Count);
            foreach (var placedObject in state.PlacedObjects)
            {
                var nextPlacedObject = placedObject.Tick();
                if (nextPlacedObject != null)
                {
                    nextPlacedObjects.Add(nextPlacedObject);
                    continue;
                }

                events.Add(new SimulationEvent(
                    SimulationEventType.ObjectExpired,
                    turnIndex,
                    placedObject.Position,
                    placedObject.Position,
                    Direction.None,
                    contentId: placedObject.ObjectId));
            }

            return new BoardState(
                state.CollapsedCells,
                state.RemovedGearCells,
                state.DestroyedElementCells,
                nextTimedBombs,
                state.Needles,
                nextPlacedObjects);
        }

        /// <summary>
        /// Tests whether the player's current cell contains active needles.
        /// </summary>
        internal static bool IsPlayerOnActiveNeedles(
            BoardState state,
            GridPosition playerPosition)
        {
            // Only the resolved active phase is lethal.
            return state.TryGetNeedle(playerPosition, out var needle) &&
                   needle.IsActive;
        }

        /// <summary>
        /// Tests whether a cell is currently traversable in the dynamic board snapshot.
        /// </summary>
        internal static bool IsTraversable(
            MapDefinition map,
            BoardState state,
            GridPosition position)
        {
            // Combine immutable authored terrain with runtime destruction.
            return map.TryGetCell(position, out var cell) &&
                   cell.IsWalkable &&
                   (!cell.BlocksMovement || state.IsElementDestroyed(position)) &&
                   !state.IsCellDestroyed(position) &&
                   !IsBlockedByPlacedObject(state, position);
        }

        /// <summary>
        /// Tests whether authored terrain can support a player after a jump or grapple.
        /// </summary>
        internal static bool IsLandingCell(
            MapDefinition map,
            BoardState state,
            GridPosition position)
        {
            // Landing uses the same terrain rules as normal traversal.
            return IsTraversable(map, state, position);
        }

        /// <summary>
        /// Tests whether a cell blocks enemy vision in the current board snapshot.
        /// </summary>
        internal static bool BlocksVision(
            MapDefinition map,
            BoardState state,
            GridPosition position)
        {
            // Holes, remaining elements, mirrors, and darkness interrupt light.
            if (!map.TryGetCell(position, out var cell) ||
                state.IsCellDestroyed(position) ||
                !cell.IsWalkable ||
                cell.BlocksMovement && !state.IsElementDestroyed(position))
            {
                return true;
            }

            if (state.TryGetPlacedObject(position, out var placedObject) &&
                placedObject.ObjectId == GearTypeIds.PlacedMirror)
            {
                return true;
            }

            return IsInsideDarkness(state, position);
        }

        /// <summary>
        /// Tests whether a position lies inside an active powder-of-darkness cloud.
        /// </summary>
        internal static bool IsInsideDarkness(
            BoardState state,
            GridPosition position)
        {
            // Darkness uses the approved Manhattan radius of two cells.
            foreach (var placedObject in state.PlacedObjects)
            {
                if (placedObject.ObjectId != GearTypeIds.DarknessCloud)
                    continue;
                var distance =
                    Math.Abs(placedObject.Position.X - position.X) +
                    Math.Abs(placedObject.Position.Y - position.Y);
                if (distance <= 2)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the farthest valid landing cell reached by one authored spring.
        /// </summary>
        internal static GridPosition GetSpringDestination(
            MapDefinition map,
            BoardState state,
            GridPosition springPosition,
            Direction direction,
            int distance,
            ISet<GridPosition> enemyPositions)
        {
            // Springs cross obstacles and holes but retain the last valid empty landing cell.
            var current = springPosition;
            var lastGood = springPosition;
            for (var step = 0; step < distance; step++)
            {
                current = current.Translate(direction);
                if (!map.IsInside(current))
                    continue;
                if (IsLandingCell(map, state, current) &&
                    !enemyPositions.Contains(current) &&
                    !HasIntactElement(map, state, current))
                {
                    lastGood = current;
                }
            }

            return lastGood;
        }

        /// <summary>
        /// Tries to read the authored cardinal direction of a spring cell.
        /// </summary>
        internal static bool TryReadSpringDirection(
            CellDefinition cell,
            out Direction direction)
        {
            // The importer stores the stable direction name instead of a numeric enum.
            direction = Direction.None;
            return cell != null &&
                   cell.Parameters.TryGetValue(
                       CellParameterIds.FloorDirection,
                       out var serializedValue) &&
                   Enum.TryParse(serializedValue, true, out direction) &&
                   direction != Direction.None;
        }

        /// <summary>
        /// Tests whether a cell still contains its authored decorative element.
        /// </summary>
        internal static bool HasIntactElement(
            MapDefinition map,
            BoardState state,
            GridPosition position)
        {
            // Destruction removes both movement and vision blocking from the element.
            return map.TryGetCell(position, out var cell) &&
                   !string.IsNullOrEmpty(cell.ElementId) &&
                   !state.IsElementDestroyed(position);
        }

        /// <summary>
        /// Tests whether a temporary placed object prevents entering a cell.
        /// </summary>
        internal static bool IsBlockedByPlacedObject(
            BoardState state,
            GridPosition position)
        {
            // Mirrors are scenery while disco balls and darkness remain non-solid.
            return state.TryGetPlacedObject(position, out var placedObject) &&
                   placedObject.ObjectId == GearTypeIds.PlacedMirror;
        }

        /// <summary>
        /// Tries to read an integer parameter without accepting malformed values.
        /// </summary>
        internal static bool TryReadInteger(
            CellDefinition cell,
            string parameterId,
            out int value)
        {
            // Parameter parsing is centralized to keep cell systems culture independent.
            value = 0;
            return cell != null &&
                   cell.Parameters.TryGetValue(parameterId, out var serializedValue) &&
                   int.TryParse(
                       serializedValue,
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out value);
        }
    }
}

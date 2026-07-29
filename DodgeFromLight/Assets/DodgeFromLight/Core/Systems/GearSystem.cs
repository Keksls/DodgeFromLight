using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class GearSystem
    {
        private const int DiscoBallDuration = 5;
        private const int MirrorDuration = 3;
        private const int DarknessDuration = 3;
        private const int InvisibilityDuration = 2;
        private const int SpeedBoostDuration = 2;
        private const int TimeStopDuration = 3;
        private const int GrappleRange = 5;

        /// <summary>
        /// Reports whether every authored placed gear belongs to the supported catalogue.
        /// </summary>
        internal static bool Supports(
            MapDefinition map,
            out string rejectionReason)
        {
            // Empty cells need no gear validation.
            foreach (var cell in map.Cells)
            {
                if (!string.IsNullOrEmpty(cell.GearId) &&
                    !GearTypeIds.IsKnown(cell.GearId))
                {
                    rejectionReason =
                        "Gear type '" + cell.GearId + "' is not supported.";
                    return false;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Resolves pickup of the current-cell gear into one of two inventory slots.
        /// </summary>
        internal static GearCommandResult Pickup(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int slotIndex,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Reject empty or already-consumed pickup cells without advancing a turn.
            if (!map.TryGetCell(player.Position, out var cell) ||
                string.IsNullOrEmpty(cell.GearId) ||
                !board.IsGearAvailable(player.Position))
            {
                return Reject(
                    "There is no available gear on the player's current cell.",
                    player,
                    board,
                    enemies,
                    platforms);
            }

            var builder = new BoardStateBuilder(board);
            builder.RemoveAuthoredGear(player.Position);
            var nextPlayer = CopyPlayerWithGear(player, slotIndex, cell.GearId);
            events.Add(new SimulationEvent(
                SimulationEventType.GearPickedUp,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction,
                slotIndex,
                cell.GearId));
            return Accept(nextPlayer, builder.Build(), enemies, platforms);
        }

        /// <summary>
        /// Resolves use of one inventory gear and consumes it on success.
        /// </summary>
        internal static GearCommandResult Use(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int slotIndex,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Dispatch the stable gear ID while preserving snapshots for rejection.
            var gearId = player.GetGear(slotIndex);
            if (string.IsNullOrEmpty(gearId))
            {
                return Reject(
                    "The selected inventory slot is empty.",
                    player,
                    board,
                    enemies,
                    platforms);
            }

            GearCommandResult result;
            switch (gearId)
            {
                case GearTypeIds.Bomb:
                    result = UseBomb(
                        map,
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.DiscoBall:
                    result = PlaceForwardObject(
                        map,
                        player,
                        board,
                        enemies,
                        platforms,
                        GearTypeIds.PlacedDiscoBall,
                        DiscoBallDuration,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.Grapple:
                    result = UseGrapple(
                        map,
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.InvisibilityCloak:
                    result = ApplyInvisibility(
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.Mirror:
                    result = PlaceForwardObject(
                        map,
                        player,
                        board,
                        enemies,
                        platforms,
                        GearTypeIds.PlacedMirror,
                        MirrorDuration,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.PowderOfDarkness:
                    result = ApplyDarkness(
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.RemoteControl:
                    result = UseRemoteControl(
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.SpeedBoots:
                    result = ApplySpeedBoost(
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                case GearTypeIds.TimeController:
                    result = ApplyTimeStop(
                        player,
                        board,
                        enemies,
                        platforms,
                        turnIndex,
                        events);
                    break;
                default:
                    return Reject(
                        "The selected gear type is unsupported.",
                        player,
                        board,
                        enemies,
                        platforms);
            }

            if (!result.IsAccepted)
                return result;

            var consumedPlayer = CopyPlayerWithGear(result.Player, slotIndex, string.Empty);
            events.Add(new SimulationEvent(
                SimulationEventType.GearUsed,
                turnIndex,
                player.Position,
                consumedPlayer.Position,
                player.Direction,
                slotIndex,
                gearId));
            events.Add(new SimulationEvent(
                SimulationEventType.GearConsumed,
                turnIndex,
                consumedPlayer.Position,
                consumedPlayer.Position,
                consumedPlayer.Direction,
                slotIndex,
                gearId));
            return Accept(
                consumedPlayer,
                result.Board,
                result.Enemies,
                result.Platforms);
        }

        /// <summary>
        /// Destroys intact decorative elements on four cells around the player.
        /// </summary>
        private static GearCommandResult UseBomb(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // The bomb affects scenery only and never destroys floor cells.
            var builder = new BoardStateBuilder(board);
            foreach (Direction direction in new[]
                     {
                         Direction.Up,
                         Direction.Right,
                         Direction.Down,
                         Direction.Left
                     })
            {
                var position = player.Position.Translate(direction);
                if (!BoardSystem.HasIntactElement(map, board, position))
                    continue;
                builder.DestroyElement(position);
                events.Add(new SimulationEvent(
                    SimulationEventType.ElementDestroyed,
                    turnIndex,
                    player.Position,
                    position,
                    direction,
                    contentId: GearTypeIds.Bomb));
            }

            return Accept(player, builder.Build(), enemies, platforms);
        }

        /// <summary>
        /// Places a temporary mirror or disco ball on the cell in front of the player.
        /// </summary>
        private static GearCommandResult PlaceForwardObject(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            string objectId,
            int duration,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Placed objects require a traversable cell not occupied by an actor or object.
            var target = player.Position.Translate(player.Direction);
            if (!BoardSystem.IsTraversable(map, board, target) ||
                board.TryGetPlacedObject(target, out _) ||
                IsOccupiedByEnemy(enemies, target) ||
                MovingPlatformSystem.HasPlatformAt(platforms, target))
            {
                return Reject(
                    "The cell in front of the player cannot receive this object.",
                    player,
                    board,
                    enemies,
                    platforms);
            }

            var builder = new BoardStateBuilder(board);
            builder.PlaceObject(new PlacedObjectState(objectId, target, duration));
            events.Add(new SimulationEvent(
                SimulationEventType.ObjectPlaced,
                turnIndex,
                player.Position,
                target,
                player.Direction,
                contentId: objectId,
                value: duration));
            return Accept(player, builder.Build(), enemies, platforms);
        }

        /// <summary>
        /// Pulls the player to the farthest valid grappling destination within five cells.
        /// </summary>
        private static GearCommandResult UseGrapple(
            MapDefinition map,
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Grapple crosses holes but stops before scenery, enemies, mirrors, or platforms.
            var current = player.Position;
            var candidates = new List<GridPosition>();
            for (var distance = 0; distance < GrappleRange; distance++)
            {
                var candidate = current.Translate(player.Direction);
                if (!map.IsInside(candidate) ||
                    BoardSystem.HasIntactElement(map, board, candidate) ||
                    BoardSystem.IsBlockedByPlacedObject(board, candidate) ||
                    IsOccupiedByEnemy(enemies, candidate) ||
                    MovingPlatformSystem.HasPlatformAt(platforms, candidate))
                {
                    break;
                }

                candidates.Add(candidate);
                current = candidate;
            }

            var destination = player.Position;
            for (var index = candidates.Count - 1; index >= 0; index--)
            {
                if (BoardSystem.IsLandingCell(map, board, candidates[index]))
                {
                    destination = candidates[index];
                    break;
                }
            }

            if (destination == player.Position)
            {
                return Reject(
                    "The grapple has no valid destination.",
                    player,
                    board,
                    enemies,
                    platforms);
            }

            events.Add(new SimulationEvent(
                SimulationEventType.PlayerForcedMoved,
                turnIndex,
                player.Position,
                destination,
                player.Direction,
                contentId: GearTypeIds.Grapple));
            return Accept(
                CopyPlayer(player, position: destination),
                board,
                enemies,
                platforms);
        }

        /// <summary>
        /// Applies the approved two-turn invisibility status.
        /// </summary>
        private static GearCommandResult ApplyInvisibility(
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Invisibility affects light-based detection while physical traps remain lethal.
            var nextPlayer = CopyPlayer(
                player,
                invisibilityTurns: InvisibilityDuration);
            events.Add(new SimulationEvent(
                SimulationEventType.InvisibilityApplied,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction,
                contentId: GearTypeIds.InvisibilityCloak,
                value: InvisibilityDuration));
            return Accept(nextPlayer, board, enemies, platforms);
        }

        /// <summary>
        /// Creates the approved radius-two darkness cloud for three turns.
        /// </summary>
        private static GearCommandResult ApplyDarkness(
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // One logical object represents the centered Manhattan-radius cloud.
            if (board.TryGetPlacedObject(player.Position, out _))
            {
                return Reject(
                    "A temporary object already occupies the darkness origin.",
                    player,
                    board,
                    enemies,
                    platforms);
            }

            var builder = new BoardStateBuilder(board);
            builder.PlaceObject(new PlacedObjectState(
                GearTypeIds.DarknessCloud,
                player.Position,
                DarknessDuration));
            events.Add(new SimulationEvent(
                SimulationEventType.DarknessCreated,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction,
                contentId: GearTypeIds.PowderOfDarkness,
                value: DarknessDuration));
            return Accept(player, builder.Build(), enemies, platforms);
        }

        /// <summary>
        /// Reverses every enemy and moving-platform patrol immediately.
        /// </summary>
        private static GearCommandResult UseRemoteControl(
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Reverse route traversal and facing while preserving current route indices.
            var reversedEnemies = new List<EnemyState>(enemies.Count);
            foreach (var enemy in enemies)
            {
                var reversed = new EnemyState(
                    enemy.DefinitionIndex,
                    enemy.Position,
                    DirectionUtility.Opposite(enemy.Direction),
                    enemy.PatrolIndex,
                    !enemy.IsPatrolReversed,
                    enemy.PhaseIndex,
                    enemy.StunnedTurns);
                reversedEnemies.Add(reversed);
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyReversed,
                    turnIndex,
                    enemy.Position,
                    enemy.Position,
                    reversed.Direction,
                    enemy.DefinitionIndex,
                    GearTypeIds.RemoteControl));
            }

            var reversedPlatforms = new List<MovingPlatformState>(platforms.Count);
            foreach (var platform in platforms)
            {
                var reversed = new MovingPlatformState(
                    platform.DefinitionIndex,
                    platform.Position,
                    DirectionUtility.Opposite(platform.Direction),
                    platform.PatrolIndex,
                    !platform.IsPatrolReversed);
                reversedPlatforms.Add(reversed);
                events.Add(new SimulationEvent(
                    SimulationEventType.PlatformReversed,
                    turnIndex,
                    platform.Position,
                    platform.Position,
                    reversed.Direction,
                    platform.DefinitionIndex,
                    GearTypeIds.RemoteControl));
            }

            return Accept(player, board, reversedEnemies, reversedPlatforms);
        }

        /// <summary>
        /// Applies the approved two-turn two-cell movement boost.
        /// </summary>
        private static GearCommandResult ApplySpeedBoost(
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Refreshing the boost restores its full duration rather than stacking range.
            var nextPlayer = CopyPlayer(player, speedBoostTurns: SpeedBoostDuration);
            events.Add(new SimulationEvent(
                SimulationEventType.SpeedBoostApplied,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction,
                contentId: GearTypeIds.SpeedBoots,
                value: SpeedBoostDuration));
            return Accept(nextPlayer, board, enemies, platforms);
        }

        /// <summary>
        /// Applies a three-turn freeze while preserving active enemy vision.
        /// </summary>
        private static GearCommandResult ApplyTimeStop(
            PlayerState player,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // The engine reads this status before phases, movement, and board timers.
            var nextPlayer = CopyPlayer(player, timeStopTurns: TimeStopDuration);
            events.Add(new SimulationEvent(
                SimulationEventType.TimeStopped,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction,
                contentId: GearTypeIds.TimeController,
                value: TimeStopDuration));
            return Accept(nextPlayer, board, enemies, platforms);
        }

        /// <summary>
        /// Tests whether an enemy currently occupies a logical position.
        /// </summary>
        private static bool IsOccupiedByEnemy(
            IReadOnlyList<EnemyState> enemies,
            GridPosition position)
        {
            // Actor occupancy blocks placement and grappling lines.
            foreach (var enemy in enemies)
            {
                if (enemy.Position == position)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Copies player state while replacing selected runtime values.
        /// </summary>
        private static PlayerState CopyPlayer(
            PlayerState source,
            GridPosition? position = null,
            int? speedBoostTurns = null,
            int? invisibilityTurns = null,
            int? timeStopTurns = null)
        {
            // Preserve every inventory, anchor, and checkpoint value not explicitly changed.
            return new PlayerState(
                position ?? source.Position,
                source.Direction,
                source.GearSlotOne,
                source.GearSlotTwo,
                speedBoostTurns ?? source.SpeedBoostTurns,
                invisibilityTurns ?? source.InvisibilityTurns,
                timeStopTurns ?? source.TimeStopTurns,
                source.AnchorPosition,
                source.CheckpointPosition,
                source.HasUsedCheckpoint);
        }

        /// <summary>
        /// Copies player state while replacing one inventory slot.
        /// </summary>
        private static PlayerState CopyPlayerWithGear(
            PlayerState source,
            int slotIndex,
            string gearId)
        {
            // Replacing a slot discards its prior one-use gear exactly once.
            return new PlayerState(
                source.Position,
                source.Direction,
                slotIndex == 0 ? gearId : source.GearSlotOne,
                slotIndex == 1 ? gearId : source.GearSlotTwo,
                source.SpeedBoostTurns,
                source.InvisibilityTurns,
                source.TimeStopTurns,
                source.AnchorPosition,
                source.CheckpointPosition,
                source.HasUsedCheckpoint);
        }

        /// <summary>
        /// Creates an accepted inventory-command result.
        /// </summary>
        private static GearCommandResult Accept(
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            IEnumerable<MovingPlatformState> platforms)
        {
            // Accepted results contain no rejection text.
            return new GearCommandResult(
                true,
                string.Empty,
                player,
                board,
                enemies,
                platforms);
        }

        /// <summary>
        /// Creates a rejected inventory-command result without changing state.
        /// </summary>
        private static GearCommandResult Reject(
            string reason,
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            IEnumerable<MovingPlatformState> platforms)
        {
            // Rejections preserve every authoritative snapshot.
            return new GearCommandResult(
                false,
                reason,
                player,
                board,
                enemies,
                platforms);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class EnemySystem
    {
        private const int DiscoBallStunTurns = 2;

        /// <summary>
        /// Reports whether every authored enemy can be resolved by the complete ruleset.
        /// </summary>
        internal static bool Supports(
            MapDefinition map,
            BoardState board,
            out string rejectionReason)
        {
            // Validate identities, routes, starts, and identity-specific arguments up front.
            foreach (var definition in map.Enemies)
            {
                if (!EnemyTypeIds.IsKnown(definition.EnemyId))
                {
                    rejectionReason =
                        "Enemy type '" + definition.EnemyId + "' is not supported.";
                    return false;
                }

                if (!PatrolRouteBuilder.TryBuild(
                        definition.Waypoints,
                        out var route,
                        out rejectionReason))
                {
                    return false;
                }

                if (!BoardSystem.IsTraversable(map, board, definition.StartPosition))
                {
                    rejectionReason =
                        "Enemy '" + definition.EnemyId +
                        "' must start on traversable terrain.";
                    return false;
                }

                if (route.Count > 0 && route[0] != definition.StartPosition)
                {
                    rejectionReason =
                        "Enemy patrols must begin at their authored start position.";
                    return false;
                }

                if (definition.EnemyId == EnemyTypeIds.Trap &&
                    !TryReadTrapRanges(
                        definition.Arguments,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    rejectionReason =
                        "Oculte Trap arguments must contain four valid min/max integers.";
                    return false;
                }
            }

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Resolves all enemy phases, patrol actions, effects, and detection in stable order.
        /// </summary>
        internal static EnemyTurnResult ResolveTurn(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> currentStates,
            PlayerState player,
            DeterministicRandomState random,
            bool isTimeStopped,
            bool playerIsJumping,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Work on private snapshots so the previous turn remains valid for undo.
            var states = new List<EnemyState>(currentStates);
            var currentBoard = board;
            var currentPlayer = player;
            var currentRandom = random;

            for (var definitionIndex = 0;
                 definitionIndex < map.Enemies.Count;
                 definitionIndex++)
            {
                var definition = map.Enemies[definitionIndex];
                var state = states[definitionIndex];
                if (!isTimeStopped && HasBeginningTurnPhase(definition.EnemyId))
                {
                    state = CopyEnemy(state, phaseIndex: state.PhaseIndex + 1);
                    states[definitionIndex] = state;
                    events.Add(new SimulationEvent(
                        SimulationEventType.EnemyPhaseChanged,
                        turnIndex,
                        state.Position,
                        state.Position,
                        state.Direction,
                        definitionIndex,
                        definition.EnemyId,
                        state.PhaseIndex));
                }

                if (state.StunnedTurns > 0)
                {
                    // Stun removes movement and sight, then loses one remaining turn.
                    states[definitionIndex] = CopyEnemy(
                        state,
                        stunnedTurns: state.StunnedTurns - 1);
                    continue;
                }

                if (!PatrolRouteBuilder.TryBuild(
                        definition.Waypoints,
                        out var route,
                        out var routeError))
                {
                    throw new InvalidOperationException(routeError);
                }

                var actionCount = isTimeStopped
                    ? 0
                    : GetActionsPerTurn(definition);
                var resolvedInteraction = false;
                var stopCurrentEnemy = false;
                for (var actionIndex = 0;
                     actionIndex < actionCount &&
                     route.Count > 1 &&
                     !stopCurrentEnemy;
                     actionIndex++)
                {
                    // Rotation, reversal, and translation each consume one enemy action.
                    state = ResolveMicroAction(
                        map,
                        currentBoard,
                        definitionIndex,
                        state,
                        route,
                        states,
                        turnIndex,
                        events);
                    states[definitionIndex] = state;
                    resolvedInteraction = true;

                    var interaction = ResolveInteraction(
                        map,
                        currentBoard,
                        states,
                        currentPlayer,
                        currentRandom,
                        definitionIndex,
                        playerIsJumping,
                        turnIndex,
                        events);
                    currentBoard = interaction.Board;
                    currentPlayer = interaction.Player;
                    currentRandom = interaction.Random;
                    state = interaction.Enemy;
                    states[definitionIndex] = state;
                    if (interaction.DetectsPlayer)
                    {
                        return new EnemyTurnResult(
                            currentPlayer,
                            currentBoard,
                            states,
                            currentRandom,
                            definitionIndex);
                    }

                    stopCurrentEnemy = interaction.StopsEnemy;
                    if (definition.EnemyId == EnemyTypeIds.Cyclops)
                    {
                        state = CopyEnemy(state, phaseIndex: state.PhaseIndex + 1);
                        states[definitionIndex] = state;
                    }
                }

                if (resolvedInteraction)
                    continue;

                var staticInteraction = ResolveInteraction(
                    map,
                    currentBoard,
                    states,
                    currentPlayer,
                    currentRandom,
                    definitionIndex,
                    playerIsJumping,
                    turnIndex,
                    events);
                currentBoard = staticInteraction.Board;
                currentPlayer = staticInteraction.Player;
                currentRandom = staticInteraction.Random;
                states[definitionIndex] = staticInteraction.Enemy;
                if (staticInteraction.DetectsPlayer)
                {
                    return new EnemyTurnResult(
                        currentPlayer,
                        currentBoard,
                        states,
                        currentRandom,
                        definitionIndex);
                }
            }

            return new EnemyTurnResult(
                currentPlayer,
                currentBoard,
                states,
                currentRandom,
                -1);
        }

        /// <summary>
        /// Tests whether a non-walkable enemy body currently blocks player movement.
        /// </summary>
        internal static bool HasSolidEnemyAt(
            MapDefinition map,
            IReadOnlyList<EnemyState> states,
            GridPosition position)
        {
            // Preserve the four legacy identities whose bodies explicitly rejected entry.
            foreach (var state in states)
            {
                if (state.Position != position)
                    continue;
                var enemyId = map.Enemies[state.DefinitionIndex].EnemyId;
                return enemyId == EnemyTypeIds.Trap ||
                       enemyId == EnemyTypeIds.Hammer ||
                       enemyId == EnemyTypeIds.Catapult ||
                       enemyId == EnemyTypeIds.Pillar;
            }

            return false;
        }

        /// <summary>
        /// Resolves one patrol movement, rotation, or blocked-route reversal.
        /// </summary>
        private static EnemyState ResolveMicroAction(
            MapDefinition map,
            BoardState board,
            int definitionIndex,
            EnemyState state,
            IReadOnlyList<GridPosition> route,
            IReadOnlyList<EnemyState> allStates,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Select the next cyclic route position according to the current traversal direction.
            if (state.PatrolIndex >= route.Count)
                throw new InvalidOperationException("The enemy patrol index is outside its route.");

            var nextPatrolIndex = state.IsPatrolReversed
                ? (state.PatrolIndex - 1 + route.Count) % route.Count
                : (state.PatrolIndex + 1) % route.Count;
            var targetPosition = route[nextPatrolIndex];
            var targetDirection = PatrolRouteBuilder.GetDirection(
                state.Position,
                targetPosition);

            if (state.Direction != targetDirection)
            {
                var turned = CopyEnemy(state, direction: targetDirection);
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyTurned,
                    turnIndex,
                    state.Position,
                    state.Position,
                    targetDirection,
                    definitionIndex,
                    map.Enemies[definitionIndex].EnemyId));
                return turned;
            }

            if (!BoardSystem.IsTraversable(map, board, targetPosition) ||
                IsOccupiedByEnemy(targetPosition, definitionIndex, allStates))
            {
                var reversedDirection = DirectionUtility.Opposite(state.Direction);
                var reversed = CopyEnemy(
                    state,
                    direction: reversedDirection,
                    isPatrolReversed: !state.IsPatrolReversed);
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyReversed,
                    turnIndex,
                    state.Position,
                    state.Position,
                    reversedDirection,
                    definitionIndex,
                    map.Enemies[definitionIndex].EnemyId));
                return reversed;
            }

            var moved = CopyEnemy(
                state,
                position: targetPosition,
                patrolIndex: nextPatrolIndex);
            events.Add(new SimulationEvent(
                SimulationEventType.EnemyMoved,
                turnIndex,
                state.Position,
                targetPosition,
                state.Direction,
                definitionIndex,
                map.Enemies[definitionIndex].EnemyId));
            return moved;
        }

        /// <summary>
        /// Resolves disco-ball distraction, lethal vision, and identity-specific force effects.
        /// </summary>
        private static EnemyInteractionResult ResolveInteraction(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> allStates,
            PlayerState player,
            DeterministicRandomState random,
            int definitionIndex,
            bool playerIsJumping,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // A visible disco ball distracts the enemy before it can target the player.
            var enemy = allStates[definitionIndex];
            if (EnemyVisionSystem.TryFindVisibleDiscoBall(
                    map,
                    board,
                    enemy,
                    allStates,
                    out var discoBallPosition))
            {
                var builder = new BoardStateBuilder(board);
                builder.RemoveObject(discoBallPosition, out _);
                var stunned = CopyEnemy(enemy, stunnedTurns: DiscoBallStunTurns);
                events.Add(new SimulationEvent(
                    SimulationEventType.DiscoBallDestroyed,
                    turnIndex,
                    discoBallPosition,
                    discoBallPosition,
                    Direction.None,
                    definitionIndex,
                    GearTypeIds.DiscoBall));
                events.Add(new SimulationEvent(
                    SimulationEventType.EnemyStunned,
                    turnIndex,
                    enemy.Position,
                    enemy.Position,
                    enemy.Direction,
                    definitionIndex,
                    map.Enemies[definitionIndex].EnemyId,
                    DiscoBallStunTurns));
                return new EnemyInteractionResult(
                    player,
                    builder.Build(),
                    stunned,
                    random,
                    false,
                    true);
            }

            if (!playerIsJumping &&
                EnemyVisionSystem.CanDetectPlayer(
                    map,
                    board,
                    enemy,
                    allStates,
                    player))
            {
                events.Add(new SimulationEvent(
                    SimulationEventType.PlayerDetected,
                    turnIndex,
                    enemy.Position,
                    player.Position,
                    enemy.Direction,
                    definitionIndex,
                    map.Enemies[definitionIndex].EnemyId));
                return new EnemyInteractionResult(
                    player,
                    board,
                    enemy,
                    random,
                    true,
                    true);
            }

            if (playerIsJumping)
            {
                return new EnemyInteractionResult(
                    player,
                    board,
                    enemy,
                    random,
                    false,
                    false);
            }

            var enemyId = map.Enemies[definitionIndex].EnemyId;
            if (enemyId == EnemyTypeIds.Gloutton &&
                TryResolveGlouttonAttraction(
                    map,
                    board,
                    allStates,
                    enemy,
                    player,
                    turnIndex,
                    events,
                    out var attractedPlayer))
            {
                // Resolve lethal sight again at the landing cell before the player can act next turn.
                var detectsAttractedPlayer = EnemyVisionSystem.CanDetectPlayer(
                    map,
                    board,
                    enemy,
                    allStates,
                    attractedPlayer);
                if (detectsAttractedPlayer)
                {
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerDetected,
                        turnIndex,
                        enemy.Position,
                        attractedPlayer.Position,
                        enemy.Direction,
                        definitionIndex,
                        enemyId));
                }

                return new EnemyInteractionResult(
                    attractedPlayer,
                    board,
                    enemy,
                    random,
                    detectsAttractedPlayer,
                    true);
            }

            if (enemyId == EnemyTypeIds.Trap &&
                TryResolveTrapEffect(
                    map,
                    board,
                    allStates,
                    enemy,
                    player,
                    random,
                    turnIndex,
                    events,
                    out var displacedPlayer,
                    out var nextRandom))
            {
                return new EnemyInteractionResult(
                    displacedPlayer,
                    board,
                    enemy,
                    nextRandom,
                    false,
                    true);
            }

            return new EnemyInteractionResult(
                player,
                board,
                enemy,
                random,
                false,
                false);
        }

        /// <summary>
        /// Pulls a player in the Gloutton attraction line one cell toward the enemy.
        /// </summary>
        private static bool TryResolveGlouttonAttraction(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> allStates,
            EnemyState enemy,
            PlayerState player,
            int turnIndex,
            ICollection<SimulationEvent> events,
            out PlayerState attractedPlayer)
        {
            // Attraction begins immediately after lethal range and ends at four cells.
            var lethalRange = map.Enemies[enemy.DefinitionIndex].SightRange > 0
                ? map.Enemies[enemy.DefinitionIndex].SightRange
                : 1;
            if (!IsOnForwardLine(
                    enemy.Position,
                    enemy.Direction,
                    player.Position,
                    lethalRange + 1,
                    4) ||
                !HasClearCardinalPath(
                    map,
                    board,
                    allStates,
                    enemy,
                    player.Position))
            {
                attractedPlayer = player;
                return false;
            }

            var direction = DirectionUtility.Opposite(enemy.Direction);
            var destination = GetForcedDestination(
                map,
                board,
                allStates,
                player.Position,
                direction,
                1);
            attractedPlayer = CopyPlayer(player, position: destination);
            events.Add(new SimulationEvent(
                SimulationEventType.PlayerForcedMoved,
                turnIndex,
                player.Position,
                destination,
                direction,
                enemy.DefinitionIndex,
                EnemyTypeIds.Gloutton));
            return true;
        }

        /// <summary>
        /// Resolves deterministic attraction and push distances for the pacifist Oculte Trap.
        /// </summary>
        private static bool TryResolveTrapEffect(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> allStates,
            EnemyState enemy,
            PlayerState player,
            DeterministicRandomState random,
            int turnIndex,
            ICollection<SimulationEvent> events,
            out PlayerState displacedPlayer,
            out DeterministicRandomState nextRandom)
        {
            // The effect applies only while the player is visible in the trap's forward line.
            var dangerCells = EnemyVisionSystem.GetDangerCells(
                map,
                board,
                enemy,
                allStates);
            var containsPlayer = false;
            foreach (var position in dangerCells)
            {
                if (position == player.Position)
                {
                    containsPlayer = true;
                    break;
                }
            }

            if (!containsPlayer)
            {
                displacedPlayer = player;
                nextRandom = random;
                return false;
            }

            TryReadTrapRanges(
                map.Enemies[enemy.DefinitionIndex].Arguments,
                out var minimumAttraction,
                out var maximumAttraction,
                out var minimumPush,
                out var maximumPush);
            var attraction = DrawRange(
                random,
                minimumAttraction,
                maximumAttraction,
                out var afterAttraction);
            var push = DrawRange(
                afterAttraction,
                minimumPush,
                maximumPush,
                out nextRandom);

            var attractedPosition = GetForcedDestination(
                map,
                board,
                allStates,
                player.Position,
                DirectionUtility.Opposite(enemy.Direction),
                attraction);
            var pushedPosition = GetForcedDestination(
                map,
                board,
                allStates,
                attractedPosition,
                enemy.Direction,
                push);
            displacedPlayer = CopyPlayer(player, position: pushedPosition);
            events.Add(new SimulationEvent(
                SimulationEventType.PlayerForcedMoved,
                turnIndex,
                player.Position,
                pushedPosition,
                enemy.Direction,
                enemy.DefinitionIndex,
                EnemyTypeIds.Trap,
                attraction * 1000 + push));
            return true;
        }

        /// <summary>
        /// Draws a legacy minimum-inclusive, maximum-exclusive range without invalid zero-width calls.
        /// </summary>
        private static int DrawRange(
            DeterministicRandomState random,
            int inclusiveMinimum,
            int exclusiveMaximum,
            out DeterministicRandomState nextRandom)
        {
            // A one-value or malformed historical range resolves to its minimum without a draw.
            if (exclusiveMaximum <= inclusiveMinimum + 1)
            {
                nextRandom = random;
                return inclusiveMinimum;
            }

            return random.Next(inclusiveMinimum, exclusiveMaximum, out nextRandom);
        }

        /// <summary>
        /// Advances along traversable cells until a forced movement reaches its distance or an obstacle.
        /// </summary>
        private static GridPosition GetForcedDestination(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> allStates,
            GridPosition start,
            Direction direction,
            int distance)
        {
            // Forced movement never crosses solid terrain, mirrors, or enemy bodies.
            var current = start;
            for (var index = 0; index < distance; index++)
            {
                var candidate = current.Translate(direction);
                if (!BoardSystem.IsTraversable(map, board, candidate) ||
                    IsOccupiedByEnemy(candidate, -1, allStates))
                {
                    break;
                }

                current = candidate;
            }

            return current;
        }

        /// <summary>
        /// Tests whether a target lies on a bounded forward cardinal line.
        /// </summary>
        private static bool IsOnForwardLine(
            GridPosition origin,
            Direction direction,
            GridPosition target,
            int minimumDistance,
            int maximumDistance)
        {
            // Compare exact translated cells to avoid floating-point direction tests.
            for (var distance = minimumDistance;
                 distance <= maximumDistance;
                 distance++)
            {
                if (DirectionUtility.Translate(origin, direction, distance) == target)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Tests whether every intermediate cell of a cardinal effect remains unobstructed.
        /// </summary>
        private static bool HasClearCardinalPath(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> allStates,
            EnemyState source,
            GridPosition target)
        {
            // Walk the source orientation until reaching the target or a blocker.
            var current = source.Position;
            while (current != target)
            {
                current = current.Translate(source.Direction);
                if (current == target)
                    return !BoardSystem.BlocksVision(map, board, current);
                if (BoardSystem.BlocksVision(map, board, current) ||
                    IsOccupiedByEnemy(current, source.DefinitionIndex, allStates))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses optional trap ranges or returns the legacy defaults.
        /// </summary>
        private static bool TryReadTrapRanges(
            string arguments,
            out int minimumAttraction,
            out int maximumAttraction,
            out int minimumPush,
            out int maximumPush)
        {
            // Empty arguments use the four legacy defaults.
            minimumAttraction = 1;
            maximumAttraction = 4;
            minimumPush = 1;
            maximumPush = 4;
            if (string.IsNullOrEmpty(arguments))
                return true;

            var values = arguments.Split(';');
            return values.Length == 4 &&
                   int.TryParse(
                       values[0],
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out minimumAttraction) &&
                   int.TryParse(
                       values[1],
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out maximumAttraction) &&
                   int.TryParse(
                       values[2],
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out minimumPush) &&
                   int.TryParse(
                       values[3],
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out maximumPush) &&
                   minimumAttraction >= 0 &&
                   minimumPush >= 0 &&
                   maximumAttraction > minimumAttraction &&
                   maximumPush > minimumPush;
        }

        /// <summary>
        /// Returns the effective action count including identity-specific legacy defaults.
        /// </summary>
        private static int GetActionsPerTurn(EnemyDefinition definition)
        {
            // Static identities stay static while mobile identities default to one action.
            if (definition.ActionsPerTurn > 0)
                return definition.ActionsPerTurn;
            switch (definition.EnemyId)
            {
                case EnemyTypeIds.Keeper:
                case EnemyTypeIds.Trap:
                case EnemyTypeIds.Axe:
                case EnemyTypeIds.Hammer:
                case EnemyTypeIds.Catapult:
                case EnemyTypeIds.Pillar:
                    return 0;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Reports whether an enemy advances a phase at the beginning of accepted turns.
        /// </summary>
        private static bool HasBeginningTurnPhase(string enemyId)
        {
            // Cyclops advances per micro-action rather than per player turn.
            return enemyId == EnemyTypeIds.Thunderbird ||
                   enemyId == EnemyTypeIds.Axe ||
                   enemyId == EnemyTypeIds.Hammer ||
                   enemyId == EnemyTypeIds.Catapult ||
                   enemyId == EnemyTypeIds.Pillar;
        }

        /// <summary>
        /// Tests whether another enemy occupies one position.
        /// </summary>
        private static bool IsOccupiedByEnemy(
            GridPosition position,
            int ignoredDefinitionIndex,
            IReadOnlyList<EnemyState> states)
        {
            // Stable definition order resolves simultaneous route conflicts predictably.
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
        /// Copies an enemy state while replacing selected values.
        /// </summary>
        private static EnemyState CopyEnemy(
            EnemyState source,
            GridPosition? position = null,
            Direction? direction = null,
            int? patrolIndex = null,
            bool? isPatrolReversed = null,
            int? phaseIndex = null,
            int? stunnedTurns = null)
        {
            // Named optional replacements keep every untouched authoritative value explicit.
            return new EnemyState(
                source.DefinitionIndex,
                position ?? source.Position,
                direction ?? source.Direction,
                patrolIndex ?? source.PatrolIndex,
                isPatrolReversed ?? source.IsPatrolReversed,
                phaseIndex ?? source.PhaseIndex,
                stunnedTurns ?? source.StunnedTurns);
        }

        /// <summary>
        /// Copies player state while replacing its logical position.
        /// </summary>
        private static PlayerState CopyPlayer(
            PlayerState source,
            GridPosition? position = null)
        {
            // Forced enemy movement preserves inventory, timers, checkpoint, and anchor.
            return new PlayerState(
                position ?? source.Position,
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

using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Systems;

namespace DodgeFromLight.Core.Simulation
{
    public sealed class SimulationEngine
    {
        /// <summary>
        /// Returns the resolved danger cells of one enemy in the supplied simulation state.
        /// </summary>
        public IReadOnlyList<GridPosition> GetEnemyDangerCells(
            SimulationState state,
            int enemyIndex)
        {
            // Presentation reads the same pure danger calculation used for lethal detection.
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (enemyIndex < 0 || enemyIndex >= state.Enemies.Count)
                throw new ArgumentOutOfRangeException(nameof(enemyIndex));
            return EnemyVisionSystem.GetDangerCells(
                state.Map,
                state.Board,
                state.Enemies[enemyIndex],
                state.Enemies);
        }

        /// <summary>
        /// Returns the resolved non-lethal effect cells of one enemy.
        /// </summary>
        public IReadOnlyList<GridPosition> GetEnemySecondaryEffectCells(
            SimulationState state,
            int enemyIndex)
        {
            // Presentation uses this for the distinct Gloutton attraction overlay.
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (enemyIndex < 0 || enemyIndex >= state.Enemies.Count)
                throw new ArgumentOutOfRangeException(nameof(enemyIndex));
            return EnemyVisionSystem.GetSecondaryEffectCells(
                state.Map,
                state.Board,
                state.Enemies[enemyIndex],
                state.Enemies);
        }

        /// <summary>
        /// Resolves one player command against an immutable simulation state.
        /// </summary>
        public SimulationStepResult Step(
            SimulationState state,
            PlayerCommand command)
        {
            // Validate the session and complete content contract before any world mutation.
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var currentHash = SimulationStateHasher.Compute(state);
            if (state.Status != SimulationStatus.Running)
            {
                return SimulationStepResult.CreateRejected(
                    state,
                    "The simulation has already ended.",
                    currentHash);
            }
            if (!SupportsMap(state, out var unsupportedReason))
            {
                return SimulationStepResult.CreateRejected(
                    state,
                    unsupportedReason,
                    currentHash);
            }

            var nextTurn = state.TurnIndex + 1;
            var events = new List<SimulationEvent>();
            var commandResolution = ResolvePlayerCommand(
                state,
                command,
                nextTurn,
                events);
            if (!commandResolution.IsAccepted)
            {
                return SimulationStepResult.CreateRejected(
                    state,
                    commandResolution.RejectionReason,
                    currentHash);
            }

            return ResolveAcceptedTurn(
                state,
                commandResolution,
                nextTurn,
                events);
        }

        /// <summary>
        /// Resolves the immediate player intent without advancing autonomous systems.
        /// </summary>
        private static PlayerCommandResolution ResolvePlayerCommand(
            SimulationState state,
            PlayerCommand command,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Each explicit command has one validation and mutation path.
            switch (command.Type)
            {
                case PlayerCommandType.Move:
                    return ResolveMove(state, command.Direction, turnIndex, events);
                case PlayerCommandType.Wait:
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerWaited,
                        turnIndex,
                        state.Player.Position,
                        state.Player.Position,
                        state.Player.Direction));
                    return AcceptCommand(state, state.Player);
                case PlayerCommandType.PickupGear:
                    return ResolvePickup(state, command.SlotIndex, turnIndex, events);
                case PlayerCommandType.UseGear:
                    return ResolveGearUse(state, command.SlotIndex, turnIndex, events);
                case PlayerCommandType.ActivateCheckpoint:
                    return ResolveCheckpoint(state, turnIndex, events);
                case PlayerCommandType.PlantOrSwapAnchor:
                    return ResolveAnchor(state, turnIndex, events);
                case PlayerCommandType.PushAnchor:
                    return ResolveAnchorPush(state, turnIndex, events);
                case PlayerCommandType.Jump:
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerJumped,
                        turnIndex,
                        state.Player.Position,
                        state.Player.Position,
                        state.Player.Direction));
                    return AcceptCommand(state, state.Player, isJumping: true);
                default:
                    return RejectCommand(state, "The command type is not supported.");
            }
        }

        /// <summary>
        /// Resolves a cardinal player movement or an explicit blocked-direction rotation.
        /// </summary>
        private static PlayerCommandResolution ResolveMove(
            SimulationState state,
            Direction direction,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Speed boost examines two cells but triggers entry behavior only on the destination.
            var maximumDistance = state.Player.SpeedBoostTurns > 0 ? 2 : 1;
            var current = state.Player.Position;
            var destination = current;
            for (var distance = 0; distance < maximumDistance; distance++)
            {
                var candidate = current.Translate(direction);
                var hasTerrain = BoardSystem.IsTraversable(
                    state.Map,
                    state.Board,
                    candidate);
                var hasPlatform = MovingPlatformSystem.HasPlatformAt(
                    state.MovingPlatforms,
                    candidate);
                if ((!hasTerrain && !hasPlatform) ||
                    EnemySystem.HasSolidEnemyAt(
                        state.Map,
                        state.Enemies,
                        candidate))
                {
                    break;
                }

                destination = candidate;
                current = candidate;
            }

            if (destination == state.Player.Position)
            {
                if (state.Player.Direction == direction)
                {
                    return RejectCommand(
                        state,
                        "The target direction is blocked.");
                }

                var turnedPlayer = CopyPlayer(state.Player, direction: direction);
                events.Add(new SimulationEvent(
                    SimulationEventType.PlayerTurned,
                    turnIndex,
                    state.Player.Position,
                    state.Player.Position,
                    direction));
                return AcceptCommand(state, turnedPlayer);
            }

            var movedPlayer = CopyPlayer(
                state.Player,
                position: destination,
                direction: direction);
            events.Add(new SimulationEvent(
                SimulationEventType.PlayerMoved,
                turnIndex,
                state.Player.Position,
                destination,
                direction));
            return AcceptCommand(state, movedPlayer);
        }

        /// <summary>
        /// Resolves a current-cell gear pickup.
        /// </summary>
        private static PlayerCommandResolution ResolvePickup(
            SimulationState state,
            int slotIndex,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Delegate inventory rules to the complete gear catalogue system.
            var result = GearSystem.Pickup(
                state.Map,
                state.Player,
                state.Board,
                state.Enemies,
                state.MovingPlatforms,
                slotIndex,
                turnIndex,
                events);
            return FromGearResult(state, result);
        }

        /// <summary>
        /// Resolves use of one inventory gear.
        /// </summary>
        private static PlayerCommandResolution ResolveGearUse(
            SimulationState state,
            int slotIndex,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // GearSystem returns every actor snapshot affected by remote control or movement.
            var result = GearSystem.Use(
                state.Map,
                state.Player,
                state.Board,
                state.Enemies,
                state.MovingPlatforms,
                slotIndex,
                turnIndex,
                events);
            return FromGearResult(state, result);
        }

        /// <summary>
        /// Activates the checkpoint under the player and marks the attempt unranked.
        /// </summary>
        private static PlayerCommandResolution ResolveCheckpoint(
            SimulationState state,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // Only an authored checkpoint cell can become the local restart position.
            if (!state.Map.TryGetCell(state.Player.Position, out var cell) ||
                cell.TypeId != CellTypeIds.Checkpoint)
            {
                return RejectCommand(
                    state,
                    "The player is not standing on a checkpoint.");
            }
            if (state.Player.CheckpointPosition == state.Player.Position)
            {
                return RejectCommand(
                    state,
                    "This checkpoint is already active.");
            }

            var player = CopyPlayer(
                state.Player,
                checkpointPosition: state.Player.Position,
                hasUsedCheckpoint: true);
            events.Add(new SimulationEvent(
                SimulationEventType.CheckpointActivated,
                turnIndex,
                state.Player.Position,
                state.Player.Position,
                state.Player.Direction));
            return AcceptCommand(state, player);
        }

        /// <summary>
        /// Plants the weapon anchor or swaps the player with its current position.
        /// </summary>
        private static PlayerCommandResolution ResolveAnchor(
            SimulationState state,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // The first action saves a position; later actions exchange both positions.
            if (!state.Player.AnchorPosition.HasValue)
            {
                var planted = CopyPlayer(
                    state.Player,
                    anchorPosition: state.Player.Position);
                events.Add(new SimulationEvent(
                    SimulationEventType.AnchorPlanted,
                    turnIndex,
                    state.Player.Position,
                    state.Player.Position,
                    state.Player.Direction));
                return AcceptCommand(state, planted);
            }

            var anchorPosition = state.Player.AnchorPosition.Value;
            var swapped = CopyPlayer(
                state.Player,
                position: anchorPosition,
                anchorPosition: state.Player.Position);
            events.Add(new SimulationEvent(
                SimulationEventType.AnchorSwapped,
                turnIndex,
                state.Player.Position,
                anchorPosition,
                state.Player.Direction));
            return AcceptCommand(state, swapped);
        }

        /// <summary>
        /// Pushes the planted anchor up to four traversable cells.
        /// </summary>
        private static PlayerCommandResolution ResolveAnchorPush(
            SimulationState state,
            int turnIndex,
            ICollection<SimulationEvent> events)
        {
            // A failed push still consumes a turn, matching the historical shield strike.
            if (!state.Player.AnchorPosition.HasValue)
            {
                return RejectCommand(
                    state,
                    "No weapon anchor has been planted.");
            }

            var current = state.Player.AnchorPosition.Value;
            for (var distance = 0; distance < 4; distance++)
            {
                var candidate = current.Translate(state.Player.Direction);
                if (!BoardSystem.IsTraversable(
                        state.Map,
                        state.Board,
                        candidate) ||
                    BoardSystem.HasIntactElement(
                        state.Map,
                        state.Board,
                        candidate) ||
                    EnemySystem.HasSolidEnemyAt(
                        state.Map,
                        state.Enemies,
                        candidate))
                {
                    break;
                }

                current = candidate;
            }

            var pushed = CopyPlayer(state.Player, anchorPosition: current);
            events.Add(new SimulationEvent(
                SimulationEventType.AnchorPushed,
                turnIndex,
                state.Player.AnchorPosition.Value,
                current,
                state.Player.Direction));
            return AcceptCommand(state, pushed);
        }

        /// <summary>
        /// Resolves cell entry, platforms, enemies, timers, defeat, and victory for one accepted command.
        /// </summary>
        private static SimulationStepResult ResolveAcceptedTurn(
            SimulationState previousState,
            PlayerCommandResolution command,
            int nextTurn,
            List<SimulationEvent> events)
        {
            // A time controller used by this command freezes every affected subsystem immediately.
            var player = command.Player;
            var boardAfterCommand = command.Board;
            var enemies = command.Enemies;
            var platforms = command.Platforms;
            var random = command.Random;
            var isTimeStopped = player.TimeStopTurns > 0;
            var board = BoardSystem.BeginTurn(
                boardAfterCommand,
                isTimeStopped,
                nextTurn,
                events);

            var lethalCellPosition = default(GridPosition?);
            if (player.Position != previousState.Player.Position)
            {
                board = BoardSystem.ResolvePlayerDeparture(
                    previousState.Map,
                    board,
                    previousState.Player.Position,
                    player.Position,
                    nextTurn,
                    events);
                ResolveLandingEffects(
                    previousState.Map,
                    ref player,
                    ref board,
                    enemies,
                    platforms,
                    ref random,
                    nextTurn,
                    events,
                    out lethalCellPosition);
            }

            if (!command.IsJumping &&
                BoardSystem.IsPlayerOnActiveNeedles(board, player.Position))
            {
                lethalCellPosition = player.Position;
            }

            var platformResult = MovingPlatformSystem.ResolveTurn(
                previousState.Map,
                platforms,
                player,
                isTimeStopped,
                nextTurn,
                events);
            player = platformResult.Player;
            platforms = platformResult.Platforms;

            var detectingEnemyIndex = -1;
            if (!lethalCellPosition.HasValue)
            {
                var enemyResult = EnemySystem.ResolveTurn(
                    previousState.Map,
                    board,
                    enemies,
                    player,
                    random,
                    isTimeStopped,
                    command.IsJumping,
                    nextTurn,
                    events);
                player = enemyResult.Player;
                board = enemyResult.Board;
                enemies = enemyResult.Enemies;
                random = enemyResult.Random;
                detectingEnemyIndex = enemyResult.DetectingEnemyIndex;
            }

            board = BoardSystem.EndTurn(
                board,
                player.Position,
                isTimeStopped,
                nextTurn,
                events,
                out var lethalExplosionPosition);
            if (!lethalCellPosition.HasValue &&
                detectingEnemyIndex < 0 &&
                lethalExplosionPosition.HasValue)
            {
                lethalCellPosition = lethalExplosionPosition;
            }

            if (!lethalCellPosition.HasValue &&
                detectingEnemyIndex < 0 &&
                !BoardSystem.IsTraversable(
                    previousState.Map,
                    board,
                    player.Position) &&
                !MovingPlatformSystem.HasPlatformAt(platforms, player.Position))
            {
                lethalCellPosition = player.Position;
            }

            player = DecrementExistingStatuses(previousState.Player, player);
            var status = detectingEnemyIndex >= 0 || lethalCellPosition.HasValue
                ? SimulationStatus.Lost
                : player.Position == previousState.Map.ExitPosition
                    ? SimulationStatus.Won
                    : SimulationStatus.Running;
            AddCompletionEvents(
                events,
                nextTurn,
                status,
                player,
                enemies,
                detectingEnemyIndex,
                lethalCellPosition);

            var nextState = new SimulationState(
                previousState.Map,
                player,
                board,
                enemies,
                platforms,
                random,
                nextTurn,
                status);
            return SimulationStepResult.CreateAccepted(
                previousState,
                nextState,
                events,
                SimulationStateHasher.Compute(nextState));
        }

        /// <summary>
        /// Resolves chained speed, teleport, spring, and needle effects after a player landing.
        /// </summary>
        private static void ResolveLandingEffects(
            MapDefinition map,
            ref PlayerState player,
            ref BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            ref DeterministicRandomState random,
            int turnIndex,
            ICollection<SimulationEvent> events,
            out GridPosition? lethalCellPosition)
        {
            // A visited set prevents authored spring or teleporter cycles from hanging a run.
            lethalCellPosition = null;
            var visitedSpecialCells = new HashSet<GridPosition>();
            var maximumEffects = map.Cells.Count * 2;
            for (var effectIndex = 0; effectIndex < maximumEffects; effectIndex++)
            {
                if (!map.TryGetCell(player.Position, out var cell))
                    return;
                if (cell.TypeId == CellTypeIds.Needles &&
                    BoardSystem.IsPlayerOnActiveNeedles(board, player.Position))
                {
                    lethalCellPosition = player.Position;
                    return;
                }

                if (cell.TypeId == CellTypeIds.SpeedBoost)
                {
                    player = CopyPlayer(player, speedBoostTurns: 2);
                    events.Add(new SimulationEvent(
                        SimulationEventType.SpeedBoostApplied,
                        turnIndex,
                        player.Position,
                        player.Position,
                        player.Direction,
                        contentId: CellTypeIds.SpeedBoost,
                        value: 2));
                    return;
                }

                if (cell.TypeId == CellTypeIds.Teleporter)
                {
                    if (!visitedSpecialCells.Add(player.Position))
                        return;
                    var candidates = GetTeleportCandidates(
                        map,
                        board,
                        enemies,
                        platforms,
                        player.Position);
                    if (candidates.Count == 0)
                        return;

                    var candidateIndex = random.Next(
                        candidates.Count,
                        out var nextRandom);
                    random = nextRandom;
                    var origin = player.Position;
                    var destination = candidates[candidateIndex];
                    player = CopyPlayer(player, position: destination);
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerTeleported,
                        turnIndex,
                        origin,
                        destination,
                        player.Direction));
                    board = BoardSystem.ResolvePlayerDeparture(
                        map,
                        board,
                        origin,
                        destination,
                        turnIndex,
                        events);
                    continue;
                }

                if (cell.TypeId == CellTypeIds.Spring)
                {
                    if (!visitedSpecialCells.Add(player.Position) ||
                        !BoardState.TryReadPositiveInteger(
                            cell,
                            CellParameterIds.SpringDistance,
                            out var distance) ||
                        !BoardSystem.TryReadSpringDirection(cell, out var direction))
                    {
                        return;
                    }

                    var enemyPositions = new HashSet<GridPosition>();
                    foreach (var enemy in enemies)
                        enemyPositions.Add(enemy.Position);
                    var origin = player.Position;
                    var destination = BoardSystem.GetSpringDestination(
                        map,
                        board,
                        origin,
                        direction,
                        distance,
                        enemyPositions);
                    player = CopyPlayer(
                        player,
                        position: destination,
                        direction: direction);
                    events.Add(new SimulationEvent(
                        SimulationEventType.PlayerSpringLaunched,
                        turnIndex,
                        origin,
                        destination,
                        direction,
                        value: distance));
                    if (destination == origin)
                        return;
                    board = BoardSystem.ResolvePlayerDeparture(
                        map,
                        board,
                        origin,
                        destination,
                        turnIndex,
                        events);
                    continue;
                }

                return;
            }
        }

        /// <summary>
        /// Builds the stable candidate list used by deterministic teleportation.
        /// </summary>
        private static IReadOnlyList<GridPosition> GetTeleportCandidates(
            MapDefinition map,
            BoardState board,
            IReadOnlyList<EnemyState> enemies,
            IReadOnlyList<MovingPlatformState> platforms,
            GridPosition currentPlayerPosition)
        {
            // Preserve legacy exclusions for start, exit, occupied, and non-walkable cells.
            var result = new List<GridPosition>();
            foreach (var cell in map.Cells)
            {
                var position = cell.Position;
                if (position == currentPlayerPosition ||
                    position == map.StartPosition ||
                    position == map.ExitPosition ||
                    !BoardSystem.IsTraversable(map, board, position) ||
                    MovingPlatformSystem.HasPlatformAt(platforms, position))
                {
                    continue;
                }

                var occupied = false;
                foreach (var enemy in enemies)
                {
                    if (enemy.Position == position)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied)
                    result.Add(position);
            }

            result.Sort(ComparePositions);
            return result;
        }

        /// <summary>
        /// Orders grid positions canonically for deterministic random candidate indices.
        /// </summary>
        private static int ComparePositions(
            GridPosition left,
            GridPosition right)
        {
            // Legacy iteration visited X first and Y second.
            var xComparison = left.X.CompareTo(right.X);
            return xComparison != 0
                ? xComparison
                : left.Y.CompareTo(right.Y);
        }

        /// <summary>
        /// Decrements statuses that already existed at the beginning of the accepted command.
        /// </summary>
        private static PlayerState DecrementExistingStatuses(
            PlayerState previousPlayer,
            PlayerState currentPlayer)
        {
            // Speed begins on future movement; invisibility and time stop include the use turn.
            return CopyPlayer(
                currentPlayer,
                speedBoostTurns: previousPlayer.SpeedBoostTurns > 0
                    ? Math.Max(0, currentPlayer.SpeedBoostTurns - 1)
                    : currentPlayer.SpeedBoostTurns,
                invisibilityTurns: Math.Max(0, currentPlayer.InvisibilityTurns - 1),
                timeStopTurns: Math.Max(0, currentPlayer.TimeStopTurns - 1));
        }

        /// <summary>
        /// Adds deterministic loss, win, and turn-completion events.
        /// </summary>
        private static void AddCompletionEvents(
            ICollection<SimulationEvent> events,
            int turnIndex,
            SimulationStatus status,
            PlayerState player,
            IReadOnlyList<EnemyState> enemies,
            int detectingEnemyIndex,
            GridPosition? lethalCellPosition)
        {
            // Loss events name their resolved source while wins use the exit position.
            if (status == SimulationStatus.Lost)
            {
                if (detectingEnemyIndex >= 0)
                {
                    var enemy = enemies[detectingEnemyIndex];
                    events.Add(new SimulationEvent(
                        SimulationEventType.GameLost,
                        turnIndex,
                        enemy.Position,
                        player.Position,
                        enemy.Direction,
                        detectingEnemyIndex));
                }
                else
                {
                    var source = lethalCellPosition ?? player.Position;
                    events.Add(new SimulationEvent(
                        SimulationEventType.GameLost,
                        turnIndex,
                        source,
                        player.Position,
                        Direction.None));
                }
            }
            else if (status == SimulationStatus.Won)
            {
                events.Add(new SimulationEvent(
                    SimulationEventType.GameWon,
                    turnIndex,
                    player.Position,
                    player.Position,
                    player.Direction));
            }

            events.Add(new SimulationEvent(
                SimulationEventType.TurnCompleted,
                turnIndex,
                player.Position,
                player.Position,
                player.Direction));
        }

        /// <summary>
        /// Reports whether every authored map feature belongs to the complete ruleset.
        /// </summary>
        private static bool SupportsMap(
            SimulationState state,
            out string rejectionReason)
        {
            // Refuse unknown authored content instead of treating it as harmless floor.
            if (!BoardSystem.Supports(state.Map, out rejectionReason))
                return false;
            if (!GearSystem.Supports(state.Map, out rejectionReason))
                return false;
            if (!EnemySystem.Supports(state.Map, state.Board, out rejectionReason))
                return false;
            if (!MovingPlatformSystem.Supports(state.Map, out rejectionReason))
                return false;

            rejectionReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Converts a gear-system result to the common command-resolution type.
        /// </summary>
        private static PlayerCommandResolution FromGearResult(
            SimulationState state,
            GearCommandResult result)
        {
            // The deterministic random generator is unchanged by all nine gear uses.
            return new PlayerCommandResolution(
                result.IsAccepted,
                result.RejectionReason,
                result.Player,
                result.Board,
                result.Enemies,
                result.Platforms,
                state.Random,
                false);
        }

        /// <summary>
        /// Creates an accepted command resolution from an existing simulation state.
        /// </summary>
        private static PlayerCommandResolution AcceptCommand(
            SimulationState state,
            PlayerState player,
            bool isJumping = false)
        {
            // Commands not touching collections reuse their immutable snapshots.
            return new PlayerCommandResolution(
                true,
                string.Empty,
                player,
                state.Board,
                state.Enemies,
                state.MovingPlatforms,
                state.Random,
                isJumping);
        }

        /// <summary>
        /// Creates a rejected command resolution without changing authoritative state.
        /// </summary>
        private static PlayerCommandResolution RejectCommand(
            SimulationState state,
            string reason)
        {
            // Rejected commands never advance autonomous systems.
            return new PlayerCommandResolution(
                false,
                reason,
                state.Player,
                state.Board,
                state.Enemies,
                state.MovingPlatforms,
                state.Random,
                false);
        }

        /// <summary>
        /// Copies player state while replacing selected authoritative values.
        /// </summary>
        private static PlayerState CopyPlayer(
            PlayerState source,
            GridPosition? position = null,
            Direction? direction = null,
            int? speedBoostTurns = null,
            int? invisibilityTurns = null,
            int? timeStopTurns = null,
            GridPosition? anchorPosition = null,
            bool replaceAnchor = false,
            GridPosition? checkpointPosition = null,
            bool replaceCheckpoint = false,
            bool? hasUsedCheckpoint = null)
        {
            // Replacement flags distinguish clearing a nullable position from preserving it.
            return new PlayerState(
                position ?? source.Position,
                direction ?? source.Direction,
                source.GearSlotOne,
                source.GearSlotTwo,
                speedBoostTurns ?? source.SpeedBoostTurns,
                invisibilityTurns ?? source.InvisibilityTurns,
                timeStopTurns ?? source.TimeStopTurns,
                replaceAnchor ? anchorPosition : anchorPosition ?? source.AnchorPosition,
                replaceCheckpoint
                    ? checkpointPosition
                    : checkpointPosition ?? source.CheckpointPosition,
                hasUsedCheckpoint ?? source.HasUsedCheckpoint);
        }
    }
}

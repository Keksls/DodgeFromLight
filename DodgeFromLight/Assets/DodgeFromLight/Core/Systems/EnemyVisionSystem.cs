using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal static class EnemyVisionSystem
    {
        /// <summary>
        /// Computes the current resolved danger cells for one enemy.
        /// </summary>
        internal static IReadOnlyList<GridPosition> GetDangerCells(
            MapDefinition map,
            BoardState board,
            EnemyState enemy,
            IReadOnlyList<EnemyState> allEnemies)
        {
            // Build the authored shape first, then apply terrain and line-of-sight rules.
            var definition = map.Enemies[enemy.DefinitionIndex];
            var candidates = BuildRawShape(definition, enemy);
            var result = new List<GridPosition>();
            var unique = new HashSet<GridPosition>();
            var ignoresLineOfSight = IsPhysicalTrap(definition.EnemyId);

            foreach (var candidate in candidates)
            {
                if (!unique.Add(candidate) || !map.IsInside(candidate))
                    continue;
                if (!IsVisibleTerrain(map, board, candidate))
                    continue;
                if (IsOccupiedByOtherEnemy(candidate, enemy.DefinitionIndex, allEnemies))
                    continue;
                if (!ignoresLineOfSight &&
                    !HasClearLine(map, board, enemy, candidate, allEnemies))
                {
                    continue;
                }

                result.Add(candidate);
            }

            return new ReadOnlyCollection<GridPosition>(result);
        }

        /// <summary>
        /// Tests whether one enemy currently kills the player.
        /// </summary>
        internal static bool CanDetectPlayer(
            MapDefinition map,
            BoardState board,
            EnemyState enemy,
            IReadOnlyList<EnemyState> allEnemies,
            PlayerState player)
        {
            // Direct contact remains lethal except for the explicitly pacifist Oculte Trap.
            var enemyId = map.Enemies[enemy.DefinitionIndex].EnemyId;
            if (enemy.Position == player.Position)
                return enemyId != EnemyTypeIds.Trap;
            if (enemyId == EnemyTypeIds.Trap)
                return false;
            if (player.InvisibilityTurns > 0 && !IsPhysicalTrap(enemyId))
                return false;

            var dangerCells = GetDangerCells(map, board, enemy, allEnemies);
            foreach (var position in dangerCells)
            {
                if (position == player.Position)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the first active disco ball visible to an enemy.
        /// </summary>
        internal static bool TryFindVisibleDiscoBall(
            MapDefinition map,
            BoardState board,
            EnemyState enemy,
            IReadOnlyList<EnemyState> allEnemies,
            out GridPosition position)
        {
            // A disco ball distracts through the same resolved view shape as the player.
            var dangerCells = GetDangerCells(map, board, enemy, allEnemies);
            foreach (var candidate in dangerCells)
            {
                if (board.TryGetPlacedObject(candidate, out var placedObject) &&
                    placedObject.ObjectId == GearTypeIds.PlacedDiscoBall)
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// Computes non-lethal secondary effect cells such as the Gloutton attraction ray.
        /// </summary>
        internal static IReadOnlyList<GridPosition> GetSecondaryEffectCells(
            MapDefinition map,
            BoardState board,
            EnemyState enemy,
            IReadOnlyList<EnemyState> allEnemies)
        {
            // Only the Gloutton owns a separately rendered non-lethal effect zone.
            var definition = map.Enemies[enemy.DefinitionIndex];
            var result = new List<GridPosition>();
            if (definition.EnemyId != EnemyTypeIds.Gloutton)
                return new ReadOnlyCollection<GridPosition>(result);

            var lethalRange = GetSightRange(definition, 1);
            for (var distance = lethalRange + 1; distance <= 4; distance++)
            {
                var position = DirectionUtility.Translate(
                    enemy.Position,
                    enemy.Direction,
                    distance);
                if (!map.IsInside(position) ||
                    BoardSystem.BlocksVision(map, board, position) ||
                    IsOccupiedByOtherEnemy(
                        position,
                        enemy.DefinitionIndex,
                        allEnemies))
                {
                    break;
                }

                result.Add(position);
            }

            return new ReadOnlyCollection<GridPosition>(result);
        }

        /// <summary>
        /// Reports whether an enemy identity is a physical area trap unaffected by invisibility.
        /// </summary>
        internal static bool IsPhysicalTrap(string enemyId)
        {
            // These four identities attack cells physically without a light ray.
            return enemyId == EnemyTypeIds.Axe ||
                   enemyId == EnemyTypeIds.Hammer ||
                   enemyId == EnemyTypeIds.Catapult ||
                   enemyId == EnemyTypeIds.Pillar;
        }

        /// <summary>
        /// Builds the unfiltered authored danger shape of one enemy identity.
        /// </summary>
        private static IReadOnlyList<GridPosition> BuildRawShape(
            EnemyDefinition definition,
            EnemyState enemy)
        {
            // Dispatch stable content IDs to their exact characterized puzzle shapes.
            var result = new List<GridPosition>();
            switch (definition.EnemyId)
            {
                case EnemyTypeIds.Cyclops:
                    AddLine(
                        result,
                        enemy.Position,
                        enemy.PhaseIndex % 2 == 0
                            ? enemy.Direction
                            : DirectionUtility.Opposite(enemy.Direction),
                        GetSightRange(definition, 3),
                        1);
                    break;
                case EnemyTypeIds.Gloutton:
                case EnemyTypeIds.Scout:
                case EnemyTypeIds.Trap:
                    AddLine(
                        result,
                        enemy.Position,
                        enemy.Direction,
                        GetSightRange(
                            definition,
                            definition.EnemyId == EnemyTypeIds.Scout ? 3 :
                            definition.EnemyId == EnemyTypeIds.Trap ? 4 : 1),
                        1);
                    break;
                case EnemyTypeIds.Keeper:
                    AddDiamond(
                        result,
                        enemy.Position,
                        GetSightRange(definition, 2));
                    break;
                case EnemyTypeIds.Knight:
                    AddKnight(
                        result,
                        enemy.Position,
                        enemy.Direction,
                        GetSightRange(definition, 1));
                    break;
                case EnemyTypeIds.Lizardman:
                    AddDiagonals(
                        result,
                        enemy.Position,
                        GetSightRange(definition, 1));
                    break;
                case EnemyTypeIds.Thunderbird:
                    AddThunderbird(
                        result,
                        enemy.Position,
                        enemy.Direction,
                        GetSightRange(definition, 1),
                        enemy.PhaseIndex % 4 >= 2);
                    break;
                case EnemyTypeIds.Axe:
                    AddAxe(result, enemy);
                    break;
                case EnemyTypeIds.Hammer:
                    AddHammer(result, enemy);
                    break;
                case EnemyTypeIds.Catapult:
                    AddCatapult(result, enemy);
                    break;
                case EnemyTypeIds.Pillar:
                    AddPillar(result, enemy);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns authored sight range or one enemy-specific legacy default.
        /// </summary>
        private static int GetSightRange(EnemyDefinition definition, int fallback)
        {
            // Legacy components replaced zero with their identity-specific defaults.
            return definition.SightRange > 0 ? definition.SightRange : fallback;
        }

        /// <summary>
        /// Adds a cardinal line starting at an arbitrary positive distance.
        /// </summary>
        private static void AddLine(
            ICollection<GridPosition> result,
            GridPosition origin,
            Direction direction,
            int count,
            int startingDistance)
        {
            // Generate each distance independently so gaps remain possible.
            for (var index = 0; index < count; index++)
            {
                result.Add(DirectionUtility.Translate(
                    origin,
                    direction,
                    startingDistance + index));
            }
        }

        /// <summary>
        /// Adds a Manhattan diamond including the enemy cell.
        /// </summary>
        private static void AddDiamond(
            ICollection<GridPosition> result,
            GridPosition origin,
            int range)
        {
            // Enumerate all offsets whose Manhattan distance fits the configured range.
            for (var y = -range; y <= range; y++)
            {
                var horizontalRange = range - Math.Abs(y);
                for (var x = -horizontalRange; x <= horizontalRange; x++)
                    result.Add(new GridPosition(origin.X + x, origin.Y + y));
            }
        }

        /// <summary>
        /// Adds the Knight's front diagonals and every cardinal line except directly behind.
        /// </summary>
        private static void AddKnight(
            ICollection<GridPosition> result,
            GridPosition origin,
            Direction direction,
            int range)
        {
            // Front diagonal cells stay one cell away exactly as in the legacy shape.
            var front = origin.Translate(direction);
            result.Add(front.Translate(DirectionUtility.Left(direction)));
            result.Add(front.Translate(DirectionUtility.Right(direction)));

            var behind = DirectionUtility.Opposite(direction);
            foreach (Direction cardinal in new[]
                     {
                         Direction.Up,
                         Direction.Right,
                         Direction.Down,
                         Direction.Left
                     })
            {
                if (cardinal != behind)
                    AddLine(result, origin, cardinal, range, 1);
            }
        }

        /// <summary>
        /// Adds four diagonal rays around a Lizardman.
        /// </summary>
        private static void AddDiagonals(
            ICollection<GridPosition> result,
            GridPosition origin,
            int range)
        {
            // Each ray advances both axes by the same distance.
            for (var distance = 1; distance <= range; distance++)
            {
                result.Add(new GridPosition(origin.X + distance, origin.Y + distance));
                result.Add(new GridPosition(origin.X - distance, origin.Y + distance));
                result.Add(new GridPosition(origin.X - distance, origin.Y - distance));
                result.Add(new GridPosition(origin.X + distance, origin.Y - distance));
            }
        }

        /// <summary>
        /// Adds the Thunderbird's low or raised danger pattern.
        /// </summary>
        private static void AddThunderbird(
            ICollection<GridPosition> result,
            GridPosition origin,
            Direction direction,
            int range,
            bool isRaised)
        {
            // Low flight attacks near cells while raised flight attacks separated rays.
            if (!isRaised)
            {
                AddLine(result, origin, direction, 2, 1);
                result.Add(origin.Translate(DirectionUtility.Left(direction)));
                result.Add(origin.Translate(DirectionUtility.Right(direction)));
                return;
            }

            AddLine(result, origin, direction, range, 3);
            AddLine(result, origin, DirectionUtility.Left(direction), range + 1, 2);
            AddLine(result, origin, DirectionUtility.Right(direction), range + 1, 2);
        }

        /// <summary>
        /// Adds the alternating two-cell side sweep of an Axe.
        /// </summary>
        private static void AddAxe(
            ICollection<GridPosition> result,
            EnemyState enemy)
        {
            // Even phases strike right and odd phases strike left.
            result.Add(enemy.Position);
            var side = enemy.PhaseIndex % 2 == 0
                ? DirectionUtility.Right(enemy.Direction)
                : DirectionUtility.Left(enemy.Direction);
            AddLine(result, enemy.Position, side, 2, 1);
        }

        /// <summary>
        /// Adds the alternating cross impact of a Hammer.
        /// </summary>
        private static void AddHammer(
            ICollection<GridPosition> result,
            EnemyState enemy)
        {
            // The impact center is two cells to the active side.
            var side = enemy.PhaseIndex % 2 == 0
                ? DirectionUtility.Right(enemy.Direction)
                : DirectionUtility.Left(enemy.Direction);
            AddCross(result, DirectionUtility.Translate(enemy.Position, side, 2));
        }

        /// <summary>
        /// Adds the Catapult reload danger or its five-cell impact cross.
        /// </summary>
        private static void AddCatapult(
            ICollection<GridPosition> result,
            EnemyState enemy)
        {
            // Even phases expose two cells behind; odd phases resolve the projectile impact.
            if (enemy.PhaseIndex % 2 == 0)
            {
                AddLine(
                    result,
                    enemy.Position,
                    DirectionUtility.Opposite(enemy.Direction),
                    2,
                    1);
                return;
            }

            AddCross(
                result,
                DirectionUtility.Translate(enemy.Position, enemy.Direction, 5));
        }

        /// <summary>
        /// Adds the Pillar's current pair of symmetric cross impacts.
        /// </summary>
        private static void AddPillar(
            ICollection<GridPosition> result,
            EnemyState enemy)
        {
            // Horizontal starting orientations offset the four-phase legacy cycle by one.
            var phase = enemy.PhaseIndex % 4;
            if (enemy.Direction == Direction.Left || enemy.Direction == Direction.Right)
                phase = (phase + 1) % 4;

            var horizontal = phase == 0 || phase == 2;
            var distance = phase < 2 ? 4 : 2;
            var firstDirection = horizontal ? Direction.Right : Direction.Up;
            AddCross(
                result,
                DirectionUtility.Translate(enemy.Position, firstDirection, distance));
            AddCross(
                result,
                DirectionUtility.Translate(
                    enemy.Position,
                    DirectionUtility.Opposite(firstDirection),
                    distance));
        }

        /// <summary>
        /// Adds an impact center and its four cardinal neighbors.
        /// </summary>
        private static void AddCross(
            ICollection<GridPosition> result,
            GridPosition center)
        {
            // Impact shapes are not clipped here; board filtering handles map bounds.
            result.Add(center);
            result.Add(center.Translate(Direction.Up));
            result.Add(center.Translate(Direction.Right));
            result.Add(center.Translate(Direction.Down));
            result.Add(center.Translate(Direction.Left));
        }

        /// <summary>
        /// Tests whether a candidate danger cell can contain resolved light or impact.
        /// </summary>
        private static bool IsVisibleTerrain(
            MapDefinition map,
            BoardState board,
            GridPosition position)
        {
            // Legacy danger rendering ignored holes and cells blocked by intact scenery.
            return map.TryGetCell(position, out var cell) &&
                   cell.IsWalkable &&
                   !board.IsCellDestroyed(position) &&
                   (!cell.BlocksMovement || board.IsElementDestroyed(position)) &&
                   !BoardSystem.IsBlockedByPlacedObject(board, position);
        }

        /// <summary>
        /// Tests whether terrain, darkness, or another enemy blocks a ray to a target.
        /// </summary>
        private static bool HasClearLine(
            MapDefinition map,
            BoardState board,
            EnemyState source,
            GridPosition target,
            IReadOnlyList<EnemyState> allEnemies)
        {
            // Evaluate every intermediate grid cell with an integer Bresenham traversal.
            if (BoardSystem.IsInsideDarkness(board, source.Position) ||
                BoardSystem.IsInsideDarkness(board, target))
            {
                return false;
            }

            var x = source.Position.X;
            var y = source.Position.Y;
            var deltaX = Math.Abs(target.X - x);
            var deltaY = Math.Abs(target.Y - y);
            var stepX = x < target.X ? 1 : -1;
            var stepY = y < target.Y ? 1 : -1;
            var error = deltaX - deltaY;

            while (x != target.X || y != target.Y)
            {
                var doubledError = error * 2;
                if (doubledError > -deltaY)
                {
                    error -= deltaY;
                    x += stepX;
                }
                if (doubledError < deltaX)
                {
                    error += deltaX;
                    y += stepY;
                }

                var current = new GridPosition(x, y);
                if (current == target)
                    break;
                if (BoardSystem.BlocksVision(map, board, current) ||
                    IsOccupiedByOtherEnemy(
                        current,
                        source.DefinitionIndex,
                        allEnemies))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests whether a different enemy occupies a logical position.
        /// </summary>
        private static bool IsOccupiedByOtherEnemy(
            GridPosition position,
            int ignoredDefinitionIndex,
            IReadOnlyList<EnemyState> allEnemies)
        {
            // Enemy bodies block both patrol movement and light behind them.
            foreach (var enemy in allEnemies)
            {
                if (enemy.DefinitionIndex != ignoredDefinitionIndex &&
                    enemy.Position == position)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

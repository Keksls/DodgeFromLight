using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class MapDefinition
    {
        private readonly IReadOnlyDictionary<GridPosition, CellDefinition> _cellsByPosition;

        public int SchemaVersion { get; }
        public int RulesetVersion { get; }
        public string MapId { get; }
        public string RevisionId { get; }
        public string Title { get; }
        public string Author { get; }
        public int Width { get; }
        public int Height { get; }
        public GridPosition StartPosition { get; }
        public GridPosition ExitPosition { get; }
        public IReadOnlyList<CellDefinition> Cells { get; }
        public IReadOnlyList<EnemyDefinition> Enemies { get; }
        public IReadOnlyList<MovingPlatformDefinition> MovingPlatforms { get; }

        /// <summary>
        /// Creates and validates an immutable map revision.
        /// </summary>
        public MapDefinition(
            int schemaVersion,
            int rulesetVersion,
            string mapId,
            string revisionId,
            string title,
            string author,
            int width,
            int height,
            GridPosition startPosition,
            GridPosition exitPosition,
            IEnumerable<CellDefinition> cells,
            IEnumerable<EnemyDefinition> enemies,
            IEnumerable<MovingPlatformDefinition> movingPlatforms)
        {
            // Copy every collection before validation to make the revision immutable.
            if (string.IsNullOrWhiteSpace(mapId))
                throw new ArgumentException("A map ID is required.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(revisionId))
                throw new ArgumentException("A revision ID is required.", nameof(revisionId));
            if (schemaVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (rulesetVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(rulesetVersion));
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            SchemaVersion = schemaVersion;
            RulesetVersion = rulesetVersion;
            MapId = mapId;
            RevisionId = revisionId;
            Title = title ?? string.Empty;
            Author = author ?? string.Empty;
            Width = width;
            Height = height;
            StartPosition = startPosition;
            ExitPosition = exitPosition;

            var copiedCells = new List<CellDefinition>(cells ?? Array.Empty<CellDefinition>());
            var copiedEnemies = new List<EnemyDefinition>(enemies ?? Array.Empty<EnemyDefinition>());
            var copiedPlatforms = new List<MovingPlatformDefinition>(
                movingPlatforms ?? Array.Empty<MovingPlatformDefinition>());

            Cells = new ReadOnlyCollection<CellDefinition>(copiedCells);
            Enemies = new ReadOnlyCollection<EnemyDefinition>(copiedEnemies);
            MovingPlatforms = new ReadOnlyCollection<MovingPlatformDefinition>(copiedPlatforms);

            var cellsByPosition = new Dictionary<GridPosition, CellDefinition>();
            foreach (var cell in copiedCells)
            {
                if (cell == null)
                    throw new ArgumentException("A map cannot contain a null cell.", nameof(cells));
                if (cellsByPosition.ContainsKey(cell.Position))
                    throw new ArgumentException("A map cannot contain duplicate cell positions.", nameof(cells));
                cellsByPosition.Add(cell.Position, cell);
            }

            _cellsByPosition = new ReadOnlyDictionary<GridPosition, CellDefinition>(cellsByPosition);
            Validate();
        }

        /// <summary>
        /// Tests whether a position belongs to the map bounds.
        /// </summary>
        public bool IsInside(GridPosition position)
        {
            // Bounds checks use the same inclusive-exclusive convention everywhere.
            return position.X >= 0 &&
                   position.Y >= 0 &&
                   position.X < Width &&
                   position.Y < Height;
        }

        /// <summary>
        /// Tries to retrieve the authored cell at a logical position.
        /// </summary>
        public bool TryGetCell(GridPosition position, out CellDefinition cell)
        {
            // Use the immutable lookup created with the map revision.
            return _cellsByPosition.TryGetValue(position, out cell);
        }

        /// <summary>
        /// Validates structural invariants required by every simulation system.
        /// </summary>
        private void Validate()
        {
            // Reject incomplete revisions before they can enter a session.
            if (Cells.Count != (long)Width * Height)
                throw new ArgumentException("A map must define exactly one cell for every grid position.");
            foreach (var cell in Cells)
            {
                if (!IsInside(cell.Position))
                    throw new ArgumentException("Every cell must be inside the map bounds.");
            }

            if (!IsInside(StartPosition))
                throw new ArgumentException("The start position must be inside the map.");
            if (!IsInside(ExitPosition))
                throw new ArgumentException("The exit position must be inside the map.");

            if (!TryGetCell(StartPosition, out var startCell) || !startCell.IsTraversable)
                throw new ArgumentException("The start position must reference a traversable cell.");
            if (!TryGetCell(ExitPosition, out var exitCell) || !exitCell.IsTraversable)
                throw new ArgumentException("The exit position must reference a traversable cell.");

            foreach (var enemy in Enemies)
            {
                if (enemy == null || !IsInside(enemy.StartPosition))
                    throw new ArgumentException("Every enemy must start inside the map.");
                foreach (var waypoint in enemy.Waypoints)
                {
                    if (!IsInside(waypoint))
                        throw new ArgumentException("Every enemy waypoint must be inside the map.");
                }
            }

            foreach (var platform in MovingPlatforms)
            {
                if (platform == null || !IsInside(platform.StartPosition))
                    throw new ArgumentException("Every moving platform must start inside the map.");
                foreach (var waypoint in platform.Waypoints)
                {
                    if (!IsInside(waypoint))
                        throw new ArgumentException("Every moving-platform waypoint must be inside the map.");
                }
            }
        }
    }
}

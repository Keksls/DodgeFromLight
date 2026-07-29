using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DodgeFromLight.Core.Model
{
    public sealed class BoardState
    {
        private readonly HashSet<GridPosition> _collapsedPositions;
        private readonly HashSet<GridPosition> _removedGearPositions;
        private readonly HashSet<GridPosition> _destroyedElementPositions;
        private readonly IReadOnlyDictionary<GridPosition, TimedBombState> _timedBombsByPosition;
        private readonly IReadOnlyDictionary<GridPosition, NeedleState> _needlesByPosition;
        private readonly IReadOnlyDictionary<GridPosition, PlacedObjectState> _placedObjectsByPosition;

        public IReadOnlyList<GridPosition> CollapsedCells { get; }
        public IReadOnlyList<GridPosition> RemovedGearCells { get; }
        public IReadOnlyList<GridPosition> DestroyedElementCells { get; }
        public IReadOnlyList<TimedBombState> TimedBombs { get; }
        public IReadOnlyList<NeedleState> Needles { get; }
        public IReadOnlyList<PlacedObjectState> PlacedObjects { get; }

        /// <summary>
        /// Creates an immutable snapshot of every dynamic board-cell state.
        /// </summary>
        public BoardState(
            IEnumerable<GridPosition> collapsedCells,
            IEnumerable<TimedBombState> timedBombs,
            IEnumerable<NeedleState> needles)
            : this(
                collapsedCells,
                Array.Empty<GridPosition>(),
                Array.Empty<GridPosition>(),
                timedBombs,
                needles,
                Array.Empty<PlacedObjectState>())
        {
            // Keep the original constructor available for cell-only simulations.
        }

        /// <summary>
        /// Creates a complete immutable snapshot of dynamic board state.
        /// </summary>
        public BoardState(
            IEnumerable<GridPosition> collapsedCells,
            IEnumerable<GridPosition> removedGearCells,
            IEnumerable<GridPosition> destroyedElementCells,
            IEnumerable<TimedBombState> timedBombs,
            IEnumerable<NeedleState> needles,
            IEnumerable<PlacedObjectState> placedObjects)
        {
            // Copy and index every collection so snapshots cannot be mutated by callers.
            var copiedCollapsedCells = new List<GridPosition>(
                collapsedCells ?? throw new ArgumentNullException(nameof(collapsedCells)));
            var copiedRemovedGearCells = new List<GridPosition>(
                removedGearCells ?? throw new ArgumentNullException(nameof(removedGearCells)));
            var copiedDestroyedElementCells = new List<GridPosition>(
                destroyedElementCells ??
                throw new ArgumentNullException(nameof(destroyedElementCells)));
            var copiedTimedBombs = new List<TimedBombState>(
                timedBombs ?? throw new ArgumentNullException(nameof(timedBombs)));
            var copiedNeedles = new List<NeedleState>(
                needles ?? throw new ArgumentNullException(nameof(needles)));
            var copiedPlacedObjects = new List<PlacedObjectState>(
                placedObjects ?? throw new ArgumentNullException(nameof(placedObjects)));

            _collapsedPositions = new HashSet<GridPosition>();
            foreach (var position in copiedCollapsedCells)
            {
                if (!_collapsedPositions.Add(position))
                    throw new ArgumentException(
                        "Collapsed-cell positions must be unique.",
                        nameof(collapsedCells));
            }

            _removedGearPositions = CopyUniquePositions(
                copiedRemovedGearCells,
                nameof(removedGearCells));
            _destroyedElementPositions = CopyUniquePositions(
                copiedDestroyedElementCells,
                nameof(destroyedElementCells));

            var timedBombsByPosition = new Dictionary<GridPosition, TimedBombState>();
            foreach (var timedBomb in copiedTimedBombs)
            {
                if (timedBomb == null)
                    throw new ArgumentException(
                        "A board cannot contain a null timed-bomb state.",
                        nameof(timedBombs));
                if (timedBombsByPosition.ContainsKey(timedBomb.Position))
                    throw new ArgumentException(
                        "Timed-bomb positions must be unique.",
                        nameof(timedBombs));
                timedBombsByPosition.Add(timedBomb.Position, timedBomb);
            }

            var needlesByPosition = new Dictionary<GridPosition, NeedleState>();
            foreach (var needle in copiedNeedles)
            {
                if (needle == null)
                    throw new ArgumentException(
                        "A board cannot contain a null needle state.",
                        nameof(needles));
                if (needlesByPosition.ContainsKey(needle.Position))
                    throw new ArgumentException(
                        "Needle positions must be unique.",
                        nameof(needles));
                needlesByPosition.Add(needle.Position, needle);
            }

            var placedObjectsByPosition = new Dictionary<GridPosition, PlacedObjectState>();
            foreach (var placedObject in copiedPlacedObjects)
            {
                if (placedObject == null)
                    throw new ArgumentException(
                        "A board cannot contain a null placed-object state.",
                        nameof(placedObjects));
                if (placedObjectsByPosition.ContainsKey(placedObject.Position))
                    throw new ArgumentException(
                        "Placed-object positions must be unique.",
                        nameof(placedObjects));
                placedObjectsByPosition.Add(placedObject.Position, placedObject);
            }

            CollapsedCells = new ReadOnlyCollection<GridPosition>(copiedCollapsedCells);
            RemovedGearCells = new ReadOnlyCollection<GridPosition>(copiedRemovedGearCells);
            DestroyedElementCells =
                new ReadOnlyCollection<GridPosition>(copiedDestroyedElementCells);
            TimedBombs = new ReadOnlyCollection<TimedBombState>(copiedTimedBombs);
            Needles = new ReadOnlyCollection<NeedleState>(copiedNeedles);
            PlacedObjects = new ReadOnlyCollection<PlacedObjectState>(copiedPlacedObjects);
            _timedBombsByPosition =
                new ReadOnlyDictionary<GridPosition, TimedBombState>(timedBombsByPosition);
            _needlesByPosition =
                new ReadOnlyDictionary<GridPosition, NeedleState>(needlesByPosition);
            _placedObjectsByPosition =
                new ReadOnlyDictionary<GridPosition, PlacedObjectState>(placedObjectsByPosition);
        }

        /// <summary>
        /// Creates the initial dynamic board state from authored cell parameters.
        /// </summary>
        public static BoardState CreateInitial(MapDefinition map)
        {
            // Materialize only cell types that own mutable runtime state.
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var timedBombs = new List<TimedBombState>();
            var needles = new List<NeedleState>();
            foreach (var cell in map.Cells)
            {
                if (cell.TypeId == CellTypeIds.TimedBomb)
                {
                    if (!TryReadPositiveInteger(
                            cell,
                            CellParameterIds.TimedBombTurns,
                            out var remainingTurns))
                    {
                        throw new ArgumentException(
                            "Timed-bomb cell " + cell.Position +
                            " requires a positive '" +
                            CellParameterIds.TimedBombTurns + "' parameter.",
                            nameof(map));
                    }

                    timedBombs.Add(new TimedBombState(cell.Position, remainingTurns));
                }
                else if (cell.TypeId == CellTypeIds.Needles)
                {
                    if (!TryReadNeedleInitialState(cell, out var isActive))
                    {
                        throw new ArgumentException(
                            "Needle cell " + cell.Position +
                            " requires an initial phase of 0 or 1.",
                            nameof(map));
                    }

                    needles.Add(new NeedleState(cell.Position, isActive));
                }
            }

            return new BoardState(
                Array.Empty<GridPosition>(),
                Array.Empty<GridPosition>(),
                Array.Empty<GridPosition>(),
                timedBombs,
                needles,
                Array.Empty<PlacedObjectState>());
        }

        /// <summary>
        /// Tests whether a cell has been destroyed by a collapse or timed bomb.
        /// </summary>
        public bool IsCellDestroyed(GridPosition position)
        {
            // Both destruction mechanisms make the authored cell non-traversable.
            return _collapsedPositions.Contains(position) ||
                   _timedBombsByPosition.TryGetValue(position, out var timedBomb) &&
                   timedBomb.HasExploded;
        }

        /// <summary>
        /// Tries to retrieve the current state of a timed-bomb cell.
        /// </summary>
        public bool TryGetTimedBomb(GridPosition position, out TimedBombState timedBomb)
        {
            // Use the immutable position index created with the snapshot.
            return _timedBombsByPosition.TryGetValue(position, out timedBomb);
        }

        /// <summary>
        /// Tries to retrieve the current state of a needle cell.
        /// </summary>
        public bool TryGetNeedle(GridPosition position, out NeedleState needle)
        {
            // Use the immutable position index created with the snapshot.
            return _needlesByPosition.TryGetValue(position, out needle);
        }

        /// <summary>
        /// Tests whether an authored gear is still present on its cell.
        /// </summary>
        public bool IsGearAvailable(GridPosition position)
        {
            // A gear is available until a pickup records its source cell as consumed.
            return !_removedGearPositions.Contains(position);
        }

        /// <summary>
        /// Tests whether an authored decorative obstacle has been destroyed.
        /// </summary>
        public bool IsElementDestroyed(GridPosition position)
        {
            // Bomb destruction persists for the complete simulation attempt.
            return _destroyedElementPositions.Contains(position);
        }

        /// <summary>
        /// Tries to retrieve a temporary object currently occupying a cell.
        /// </summary>
        public bool TryGetPlacedObject(
            GridPosition position,
            out PlacedObjectState placedObject)
        {
            // Use the immutable position index created with the snapshot.
            return _placedObjectsByPosition.TryGetValue(position, out placedObject);
        }

        /// <summary>
        /// Copies a position collection while rejecting duplicate entries.
        /// </summary>
        private static HashSet<GridPosition> CopyUniquePositions(
            IEnumerable<GridPosition> positions,
            string parameterName)
        {
            // Duplicate state entries would make serialization and hashing ambiguous.
            var result = new HashSet<GridPosition>();
            foreach (var position in positions)
            {
                if (!result.Add(position))
                    throw new ArgumentException(
                        "Dynamic board positions must be unique.",
                        parameterName);
            }

            return result;
        }

        /// <summary>
        /// Tries to read a strictly positive integer cell parameter.
        /// </summary>
        internal static bool TryReadPositiveInteger(
            CellDefinition cell,
            string parameterId,
            out int value)
        {
            // Parse authored numeric parameters with invariant culture.
            value = 0;
            return cell != null &&
                   cell.Parameters.TryGetValue(parameterId, out var serializedValue) &&
                   int.TryParse(
                       serializedValue,
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   value > 0;
        }

        /// <summary>
        /// Tries to convert the authored needle phase into its initial active state.
        /// </summary>
        internal static bool TryReadNeedleInitialState(
            CellDefinition cell,
            out bool isActive)
        {
            // Legacy phase zero starts active while phase one starts inactive.
            isActive = false;
            if (cell == null ||
                !cell.Parameters.TryGetValue(
                    CellParameterIds.NeedleInitialPhase,
                    out var serializedValue) ||
                !int.TryParse(
                    serializedValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var phase) ||
                (phase != 0 && phase != 1))
            {
                return false;
            }

            isActive = phase == 0;
            return true;
        }
    }
}

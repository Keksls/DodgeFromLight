using System;
using System.Collections.Generic;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal sealed class BoardStateBuilder
    {
        private readonly List<GridPosition> _collapsedCells;
        private readonly List<GridPosition> _removedGearCells;
        private readonly List<GridPosition> _destroyedElementCells;
        private readonly List<TimedBombState> _timedBombs;
        private readonly List<NeedleState> _needles;
        private readonly List<PlacedObjectState> _placedObjects;

        /// <summary>
        /// Creates a private mutable builder from one immutable board snapshot.
        /// </summary>
        internal BoardStateBuilder(BoardState state)
        {
            // Copy every collection so system mutations never affect prior undo states.
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            _collapsedCells = new List<GridPosition>(state.CollapsedCells);
            _removedGearCells = new List<GridPosition>(state.RemovedGearCells);
            _destroyedElementCells = new List<GridPosition>(state.DestroyedElementCells);
            _timedBombs = new List<TimedBombState>(state.TimedBombs);
            _needles = new List<NeedleState>(state.Needles);
            _placedObjects = new List<PlacedObjectState>(state.PlacedObjects);
        }

        /// <summary>
        /// Records that an authored gear has been removed from its cell.
        /// </summary>
        internal void RemoveAuthoredGear(GridPosition position)
        {
            // A pickup source cell can be consumed only once.
            if (!_removedGearCells.Contains(position))
                _removedGearCells.Add(position);
        }

        /// <summary>
        /// Records permanent destruction of one authored decorative element.
        /// </summary>
        internal void DestroyElement(GridPosition position)
        {
            // Element destruction is idempotent across overlapping bomb effects.
            if (!_destroyedElementCells.Contains(position))
                _destroyedElementCells.Add(position);
        }

        /// <summary>
        /// Adds a temporary object to an otherwise free logical cell.
        /// </summary>
        internal void PlaceObject(PlacedObjectState placedObject)
        {
            // Reject overlapping objects because occupancy and expiry would be ambiguous.
            if (placedObject == null)
                throw new ArgumentNullException(nameof(placedObject));
            if (_placedObjects.Exists(item => item.Position == placedObject.Position))
                throw new InvalidOperationException("A placed object already occupies this cell.");
            _placedObjects.Add(placedObject);
        }

        /// <summary>
        /// Removes the temporary object at a logical position.
        /// </summary>
        internal bool RemoveObject(GridPosition position, out PlacedObjectState removedObject)
        {
            // Locate by authoritative logical position rather than presentation identity.
            var index = _placedObjects.FindIndex(item => item.Position == position);
            if (index < 0)
            {
                removedObject = null;
                return false;
            }

            removedObject = _placedObjects[index];
            _placedObjects.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Creates the next immutable board snapshot.
        /// </summary>
        internal BoardState Build()
        {
            // BoardState performs final uniqueness validation on every collection.
            return new BoardState(
                _collapsedCells,
                _removedGearCells,
                _destroyedElementCells,
                _timedBombs,
                _needles,
                _placedObjects);
        }
    }
}

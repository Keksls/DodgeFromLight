using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DodgeFromLight.Core.Model
{
    public sealed class CellDefinition
    {
        public GridPosition Position { get; }
        public string TypeId { get; }
        public bool IsWalkable { get; }
        public string FloorId { get; }
        public string ElementId { get; }
        public float ElementOrientationDegrees { get; }
        public float ElementScale { get; }
        public bool BlocksMovement { get; }
        public string GearId { get; }
        public IReadOnlyDictionary<string, string> Parameters { get; }
        public bool IsTraversable => IsWalkable && !BlocksMovement;

        /// <summary>
        /// Creates the immutable logical and visual description of one map cell.
        /// </summary>
        public CellDefinition(
            GridPosition position,
            string typeId,
            bool isWalkable,
            string floorId,
            string elementId,
            float elementOrientationDegrees,
            float elementScale,
            bool blocksMovement,
            string gearId,
            IDictionary<string, string> parameters = null)
        {
            // Validate stable IDs at the data boundary so runtime systems stay simple.
            if (string.IsNullOrWhiteSpace(typeId))
                throw new ArgumentException("A cell type ID is required.", nameof(typeId));

            Position = position;
            TypeId = typeId;
            IsWalkable = isWalkable;
            FloorId = floorId ?? string.Empty;
            ElementId = elementId ?? string.Empty;
            ElementOrientationDegrees = elementOrientationDegrees;
            ElementScale = elementScale;
            BlocksMovement = blocksMovement;
            GearId = gearId ?? string.Empty;

            var copiedParameters = parameters == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(parameters, StringComparer.Ordinal);
            foreach (var parameter in copiedParameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Key))
                    throw new ArgumentException("A cell parameter key cannot be empty.", nameof(parameters));
                if (parameter.Value == null)
                    throw new ArgumentException("A cell parameter value cannot be null.", nameof(parameters));
            }

            Parameters = new ReadOnlyDictionary<string, string>(copiedParameters);
        }
    }
}

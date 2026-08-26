namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Provides the inspector with an immutable description of the current board selection.
    /// </summary>
    public sealed class MapCreatorSelectionState
    {
        public bool HasSelection { get; }
        public int X { get; }
        public int Y { get; }
        public string CellType { get; }
        public string FloorId { get; }
        public string ElementId { get; }
        public string GearId { get; }
        public string EnemyId { get; }
        public string Orientation { get; }
        public bool IsMovingPlatform { get; }
        public bool CanAssignGear { get; }
        public bool CanRotateElement { get; }
        public bool CanScaleElement { get; }
        public bool CanEditPath { get; }
        public bool IsEditingPath { get; }
        public int PathPointCount { get; }
        public int ArgumentValue { get; }
        public float ElementScale { get; }

        /// <summary>
        /// Creates one complete selection snapshot for a single render frame.
        /// </summary>
        public MapCreatorSelectionState(
            bool hasSelection,
            int x,
            int y,
            string cellType,
            string floorId,
            string elementId,
            string gearId,
            string enemyId,
            string orientation,
            bool isMovingPlatform,
            bool canAssignGear,
            bool canRotateElement,
            bool canScaleElement,
            bool canEditPath,
            bool isEditingPath,
            int pathPointCount,
            int argumentValue,
            float elementScale)
        {
            // The snapshot avoids presentation-side access to mutable puzzle entities.
            HasSelection = hasSelection;
            X = x;
            Y = y;
            CellType = cellType ?? string.Empty;
            FloorId = floorId ?? string.Empty;
            ElementId = elementId ?? string.Empty;
            GearId = gearId ?? string.Empty;
            EnemyId = enemyId ?? string.Empty;
            Orientation = orientation ?? string.Empty;
            IsMovingPlatform = isMovingPlatform;
            CanAssignGear = canAssignGear;
            CanRotateElement = canRotateElement;
            CanScaleElement = canScaleElement;
            CanEditPath = canEditPath;
            IsEditingPath = isEditingPath;
            PathPointCount = pathPointCount;
            ArgumentValue = argumentValue;
            ElementScale = elementScale;
        }
    }
}

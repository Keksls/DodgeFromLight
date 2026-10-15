using System.Collections.Generic;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Describes the camera mode, active gesture, and projected authoring feedback.
    /// </summary>
    public sealed class MapCreatorViewportState
    {
        public bool IsPlanView { get; }
        public Quaternion CameraRotation { get; }
        public bool IsTransitioning { get; }
        public bool ShowEnvironment { get; }
        public bool ShowGrid { get; }
        public string BrushMode { get; }
        public int Orientation { get; }
        public bool HoverValid { get; }
        public string Hint { get; }
        public IReadOnlyList<MapCreatorBoardCell> Cells { get; }
        public IReadOnlyList<Vector2> Path { get; }

        /// <summary>
        /// Creates an immutable workbench viewport snapshot.
        /// </summary>
        public MapCreatorViewportState(bool isPlanView, bool showGrid, string brushMode, int orientation, bool hoverValid, string hint, IReadOnlyList<MapCreatorBoardCell> cells, IReadOnlyList<Vector2> path, bool showEnvironment, Quaternion cameraRotation, bool isTransitioning)
        {
            CameraRotation = cameraRotation;
            IsTransitioning = isTransitioning;
            ShowEnvironment = showEnvironment;
            // Only screen-space geometry crosses into the Fugui presentation assembly.
            IsPlanView = isPlanView;
            ShowGrid = showGrid;
            BrushMode = brushMode;
            Orientation = orientation;
            HoverValid = hoverValid;
            Hint = hint;
            Cells = cells;
            Path = path;
        }
    }
}

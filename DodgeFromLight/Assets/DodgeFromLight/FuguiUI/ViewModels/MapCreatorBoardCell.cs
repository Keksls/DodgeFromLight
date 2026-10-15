using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Describes a projected board tile without exposing mutable Unity gameplay objects.
    /// </summary>
    public sealed class MapCreatorBoardCell
    {
        public int X { get; }
        public int Y { get; }
        public Vector2[] Corners { get; }
        public string AssetId { get; }
        public string Label { get; }
        public int Orientation { get; }
        public bool Walkable { get; }
        public bool Selected { get; }
        public bool Preview { get; }
        public bool HoverAnchor { get; }
        public bool IsStart { get; }
        public bool IsExit { get; }

        /// <summary>
        /// Captures the visible content and interaction feedback of one tile.
        /// </summary>
        public MapCreatorBoardCell(int x, int y, Vector2[] corners, string assetId, string label, int orientation, bool walkable, bool selected, bool preview, bool isStart, bool isExit, bool hoverAnchor)
        {
            // Projection is performed by the Unity bridge, leaving drawing independent of scene entities.
            X = x;
            Y = y;
            Corners = corners;
            AssetId = assetId;
            Label = label;
            Orientation = orientation;
            Walkable = walkable;
            Selected = selected;
            Preview = preview;
            HoverAnchor = hoverAnchor;
            IsStart = isStart;
            IsExit = isExit;
        }
    }
}

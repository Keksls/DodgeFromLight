using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Owns wheel, track paging, and captured thumb dragging for one independent editor scroll region.
    /// </summary>
    internal sealed class MapCreatorScrollRegion
    {
        private int _wheelFrame = -1;
        private int _pressFrame = -1;
        private bool _dragging;
        private float _grabOffset;
        public float Offset { get; set; }

        /// <summary>
        /// Updates one scroll offset before content hit-testing, without consuming another region's wheel.
        /// </summary>
        public void Update(Rect viewport, float contentHeight, bool enabled)
        {
            float maximum = Mathf.Max(0f, contentHeight - viewport.height);
            Offset = Mathf.Clamp(Offset, 0f, maximum);
            if (!Input.GetMouseButton(0) || !enabled) _dragging = false;
            if (!enabled || maximum <= 0f)
            {
                _dragging = false;
                return;
            }
            Vector2 pointer = Fugui.DefaultContainer.Mouse.Position;
            float scale = DrawListUi.GetScale();
            // The foreground container does not populate Mouse.Wheel; Unity's frame delta is authoritative here.
            if (viewport.Contains(pointer) && _wheelFrame != Time.frameCount)
            {
                Offset = Mathf.Clamp(Offset - Input.mouseScrollDelta.y * 56f * scale, 0f, maximum);
                _wheelFrame = Time.frameCount;
            }
            Rect track = Track(viewport, scale);
            Rect thumb = Thumb(track, contentHeight, viewport.height, maximum);
            if (Input.GetMouseButtonDown(0) && track.Contains(pointer) && _pressFrame != Time.frameCount)
            {
                _pressFrame = Time.frameCount;
                _dragging = true;
                _grabOffset = thumb.Contains(pointer) ? pointer.y - thumb.y : thumb.height * 0.5f;
            }
            if (_dragging)
                Offset = Mathf.Clamp01((pointer.y - track.y - _grabOffset) / Mathf.Max(1f, track.height - thumb.height)) * maximum;
        }

        /// <summary>
        /// Draws the track and draggable thumb outside the clipped content bounds.
        /// </summary>
        public void Draw(FuDrawList drawList, Rect viewport, float contentHeight)
        {
            float maximum = Mathf.Max(0f, contentHeight - viewport.height);
            if (maximum <= 0f) return;
            float scale = DrawListUi.GetScale();
            Rect track = Track(viewport, scale);
            Rect thumb = Thumb(track, contentHeight, viewport.height, maximum);
            drawList.AddRectFilled(track.min, track.max, new Color(0.015f, 0.012f, 0.01f, 0.9f), 4f * scale);
            Color color = _dragging || track.Contains(Fugui.DefaultContainer.Mouse.Position) ? DrawListUi.AccentBright : new Color(0.52f, 0.42f, 0.28f);
            drawList.AddRectFilled(thumb.min, thumb.max, color, 4f * scale);
        }

        /// <summary>
        /// Allocates a comfortably draggable track inside the region's reserved right-hand gutter.
        /// </summary>
        private static Rect Track(Rect viewport, float scale)
        {
            return new Rect(viewport.xMax - 13f * scale, viewport.y + 2f * scale, 11f * scale, Mathf.Max(1f, viewport.height - 4f * scale));
        }

        /// <summary>
        /// Converts document offset into a proportional thumb with a minimum mouse target height.
        /// </summary>
        private Rect Thumb(Rect track, float contentHeight, float visibleHeight, float maximum)
        {
            float height = Mathf.Clamp(track.height * visibleHeight / Mathf.Max(1f, contentHeight), Mathf.Min(track.height, 28f * DrawListUi.GetScale()), track.height);
            return new Rect(track.x, track.y + (track.height - height) * Offset / maximum, track.width, height);
        }
    }
}

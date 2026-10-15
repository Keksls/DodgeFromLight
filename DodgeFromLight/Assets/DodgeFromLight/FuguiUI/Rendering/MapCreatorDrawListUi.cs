using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Draws compact editor-specific controls using only Fugui DrawList primitives.
    /// </summary>
    internal static class MapCreatorDrawListUi
    {
        private static bool _interactionsEnabled = true;
        private static Vector2 _pressOrigin;
        private static int _pressFrame = -1;
        private static int _clickFrame = -1;
        private static Rect? _interactionClip;
        private static bool PointerInClip => !_interactionClip.HasValue || _interactionClip.Value.Contains(Fugui.DefaultContainer.Mouse.Position);

        /// <summary>
        /// Aligns immediate-mode hit tests with the current visual content clip.
        /// </summary>
        public static void SetInteractionClip(Rect? clip)
        {
            _interactionClip = clip;
        }

        /// <summary>
        /// Reports an available hit target without creating a visual button.
        /// </summary>
        public static bool IsHovered(Rect rect)
        {
            return _interactionsEnabled && PointerInClip && rect.Contains(Fugui.DefaultContainer.Mouse.Position);
        }

        /// <summary>
        /// Activates a target only when the pointer gesture began and ended inside its visible bounds.
        /// </summary>
        public static bool WasClicked(Rect rect)
        {
            if (_clickFrame == Time.frameCount || !IsHovered(rect) || !rect.Contains(_pressOrigin) ||
                (_interactionClip.HasValue && !_interactionClip.Value.Contains(_pressOrigin)) || !Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left))
                return false;
            _clickFrame = Time.frameCount;
            return true;
        }

        /// <summary>
        /// Draws an obvious expandable section header with a directional chevron and optional summary.
        /// </summary>
        public static bool DrawDisclosure(FuDrawList drawList, Rect rect, string label, bool expanded, string summary = "")
        {
            float scale = DrawListUi.GetScale();
            Color ink = IsHovered(rect) ? DrawListUi.AccentBright : DrawListUi.TextPrimary;
            drawList.AddRectFilled(rect.min, rect.max, new Color(0.11f, 0.08f, 0.05f, IsHovered(rect) ? 1f : 0.65f), 3f * scale);
            Vector2 center = new Vector2(rect.x + 13f * scale, rect.center.y);
            if (expanded)
                drawList.AddTriangleFilled(center + new Vector2(-4f, -2f) * scale, center + new Vector2(4f, -2f) * scale, center + new Vector2(0f, 4f) * scale, Fugui.GetColorU32(ink));
            else
                drawList.AddTriangleFilled(center + new Vector2(-2f, -4f) * scale, center + new Vector2(-2f, 4f) * scale, center + new Vector2(4f, 0f) * scale, Fugui.GetColorU32(ink));
            DrawListUi.DrawText(drawList, new Vector2(rect.x + 29f * scale, rect.center.y - DrawListUi.MeasureText(label, 14).y * 0.5f), label, ink, 14);
            if (!string.IsNullOrEmpty(summary))
            {
                Vector2 measured = DrawListUi.MeasureText(summary, 12);
                DrawListUi.DrawText(drawList, new Vector2(rect.xMax - measured.x - 9f * scale, rect.center.y - measured.y * 0.5f), summary, DrawListUi.TextSecondary, 12);
            }
            return WasClicked(rect);
        }

        /// <summary>
        /// Enables or suppresses editor-control hit-testing for the current DrawList pass.
        /// </summary>
        public static void SetInteractionsEnabled(bool enabled)
        {
            // Presenters suppress background workbench actions while a modal owns input.
            _interactionsEnabled = enabled;
            // Remember where a gesture began so releasing a paint stroke over a button never activates it.
            if (Fugui.DefaultContainer.Mouse.IsDown(FuMouseButton.Left) && _pressFrame != Time.frameCount)
            {
                _pressOrigin = Fugui.DefaultContainer.Mouse.Position;
                _pressFrame = Time.frameCount;
            }
        }

        /// <summary>
        /// Draws illustrated tool art or a labelled view selector with a forged active state.
        /// </summary>
        public static bool DrawIconControl(FuDrawList drawList, Rect rect, string icon, string caption, bool selected, bool enabled = true)
        {
            // Each texture is an authored illustration; secondary controls retain readable labels.
            bool hovered = _interactionsEnabled && PointerInClip && enabled && rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            float scale = DrawListUi.GetScale();
            Color ink = !enabled ? new Color(0.40f, 0.36f, 0.30f) : selected ? DrawListUi.AccentBright : DrawListUi.TextPrimary;
            MapCreatorTheme.Surface(drawList, rect, !enabled ? 4 : selected ? 2 : hovered ? 3 : 1);
            Texture2D texture = MapCreatorTheme.GetTool(icon);
            float size = Mathf.Min(46f * scale, rect.height - 5f * scale);
            bool hasCaption = !string.IsNullOrEmpty(caption);
            float left = hasCaption ? rect.x + 10f * scale : rect.center.x - size * 0.5f;
            if (texture != null)
            {
                Vector2 top = new Vector2(left, rect.center.y - size * 0.5f - (hovered ? scale : 0f));
                drawList.AddImage(texture, top, top + Vector2.one * size, new Vector4(1f, 1f, 1f, enabled ? 1f : 0.30f));
            }
            if (hasCaption)
            {
                Vector2 measured = DrawListUi.MeasureText(caption, 14);
                float textX = texture != null ? left + size + 8f * scale : rect.center.x - measured.x * 0.5f;
                DrawListUi.DrawText(drawList, new Vector2(textX, rect.center.y - measured.y * 0.5f), caption, ink, 14);
            }
            return enabled && WasClicked(rect);
        }

        /// <summary>
        /// Draws a restrained document command with regular-weight text and tactile hover feedback.
        /// </summary>
        public static bool DrawCompactButton(FuDrawList drawList, Rect rect, string label, Color accent, bool enabled = true, bool selected = false)
        {
            bool hovered = _interactionsEnabled && PointerInClip && enabled && rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool pressed = hovered && Fugui.DefaultContainer.Mouse.IsPressed(FuMouseButton.Left);
            MapCreatorTheme.Surface(drawList, rect, !enabled ? 4 : selected || pressed ? 2 : hovered ? 3 : 1);
            Color ink = enabled ? selected ? DrawListUi.AccentBright : DrawListUi.TextPrimary : new Color(0.40f, 0.36f, 0.30f);
            Vector2 measured = DrawListUi.MeasureText(label, 14);
            DrawListUi.DrawText(drawList, rect.center - measured * 0.5f, label, ink, 14);
            return enabled && WasClicked(rect);
        }

        /// <summary>
        /// Draws a content family as an illustrated labelled tab rather than an ambiguous text-only switch.
        /// </summary>
        public static bool DrawLibraryTab(FuDrawList drawList, Rect rect, string icon, string label, bool selected)
        {
            float scale = DrawListUi.GetScale();
            MapCreatorTheme.Surface(drawList, rect, selected ? 2 : IsHovered(rect) ? 3 : 1);
            Texture2D texture = MapCreatorTheme.GetTool(icon);
            float size = 34f * scale;
            if (texture != null)
                drawList.AddImage(texture, new Vector2(rect.x + 3f * scale, rect.center.y - size * 0.5f), new Vector2(rect.x + 3f * scale + size, rect.center.y + size * 0.5f), Vector4.one);
            Vector2 measured = DrawListUi.MeasureText(label, 12);
            DrawListUi.DrawText(drawList, new Vector2(rect.x + 42f * scale, rect.center.y - measured.y * 0.5f), label, selected ? DrawListUi.AccentBright : DrawListUi.TextPrimary, 12);
            return WasClicked(rect);
        }

        /// <summary>
        /// Draws one palette tab and returns whether it was activated.
        /// </summary>
        public static bool DrawTab(FuDrawList drawList, Rect rect, string label, bool selected)
        {
            // Tabs use a bright lower edge to establish hierarchy without enclosing every label.
            bool hovered =
                _interactionsEnabled && PointerInClip &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked = WasClicked(rect);
            Color background = selected
                ? new Color(0.12f, 0.075f, 0.033f, 0.96f)
                : hovered
                    ? new Color(0.075f, 0.057f, 0.044f, 0.94f)
                    : new Color(0.032f, 0.029f, 0.029f, 0.9f);
            drawList.AddRectFilled(rect.min, rect.max, background, 3f);
            if (selected)
            {
                drawList.AddRectFilled(
                    new Vector2(rect.x + 5f, rect.yMax - 3f),
                    new Vector2(rect.xMax - 5f, rect.yMax),
                    DrawListUi.AccentBright,
                    2f);
            }

            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(rect.x, rect.center.y - DrawListUi.MeasureText(label, 14).y * 0.5f),
                rect.width,
                label,
                selected || hovered
                    ? DrawListUi.TextPrimary
                    : DrawListUi.TextSecondary,
                14,
                FontType.Regular);
            return clicked;
        }

        /// <summary>
        /// Draws an authored-content card with optional preview image and hover information.
        /// </summary>
        public static bool DrawPaletteCard(FuDrawList drawList, Rect rect, string label, string category, Texture2D preview, Color accent, bool selected, out bool hovered)
        {
            // Large click targets make rapid mouse authoring reliable while retaining visual previews.
            hovered =
                _interactionsEnabled && PointerInClip &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked = WasClicked(rect);
            float scale = DrawListUi.GetScale();
            Color background = selected
                ? new Color(0.18f, 0.12f, 0.065f, 1f)
                : hovered
                    ? new Color(0.115f, 0.095f, 0.075f, 1f)
                    : new Color(0.065f, 0.055f, 0.047f, 1f);
            Color border = new Color(
                accent.r,
                accent.g,
                accent.b,
                selected || hovered ? 0.88f : 0.28f);

            drawList.AddRectFilled(
                rect.min,
                rect.max,
                background,
                5f * scale);
            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                5f * scale,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (selected || hovered ? 1.5f : 0.8f) * scale));

            float previewSize =
                Mathf.Min(rect.height - 15f * scale, 58f * scale);
            Rect previewRect = new Rect(
                rect.x + 8f * scale,
                rect.center.y - previewSize * 0.5f,
                previewSize,
                previewSize);
            drawList.AddRectFilled(
                previewRect.min,
                previewRect.max,
                new Color(0.012f, 0.011f, 0.012f, 0.88f),
                4f * scale);
            if (preview != null)
            {
                drawList.AddImage(
                    preview,
                    previewRect.min + Vector2.one * 3f * scale,
                    previewRect.max - Vector2.one * 3f * scale,
                    Vector4.one);
            }
            else
            {
                DrawListUi.DrawRuneEmblem(
                    drawList,
                    previewRect.center,
                    previewRect.width * 0.23f,
                    accent);
            }

            float textX = previewRect.xMax + 10f * scale;
            float textWidth = rect.xMax - textX - 7f * scale;
            string safeLabel =
                TruncateText(label, textWidth, 14, FontType.Regular);
            DrawListUi.DrawText(
                drawList,
                new Vector2(textX, rect.y + 14f * scale),
                safeLabel,
                DrawListUi.TextPrimary,
                14,
                FontType.Regular);
            if (!string.IsNullOrWhiteSpace(category))
            {
                string safeCategory =
                    TruncateText(category, textWidth, 12, FontType.Regular);
                DrawListUi.DrawText(
                    drawList,
                    new Vector2(textX, rect.y + 39f * scale),
                    safeCategory,
                    new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.72f),
                    12,
                    FontType.Regular);
            }

            if (selected)
            {
                drawList.AddCircleFilled(
                    new Vector2(rect.xMax - 10f * scale, rect.y + 10f * scale),
                    3f * scale,
                    DrawListUi.AccentBright);
            }

            return clicked;
        }

        /// <summary>
        /// Draws a manual text-entry field and returns whether it gained focus.
        /// </summary>
        public static bool DrawTextField(FuDrawList drawList, Rect rect, string value, string placeholder, bool focused)
        {
            // Text input is collected by the presenter while the DrawList owns only focus hit-testing.
            bool hovered =
                _interactionsEnabled && PointerInClip &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked = WasClicked(rect);
            float scale = DrawListUi.GetScale();
            Color accent = focused
                ? DrawListUi.AccentBright
                : DrawListUi.Steel;
            drawList.AddRectFilled(
                rect.min,
                rect.max,
                new Color(0.018f, 0.017f, 0.018f, 0.98f),
                4f * scale);
            drawList.AddRect(
                rect.min,
                rect.max,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    focused || hovered ? 0.78f : 0.3f),
                4f * scale,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (focused ? 1.5f : 0.9f) * scale));

            string displayed = string.IsNullOrEmpty(value)
                ? placeholder
                : value;
            Color color = string.IsNullOrEmpty(value)
                ? new Color(
                    DrawListUi.TextSecondary.r,
                    DrawListUi.TextSecondary.g,
                    DrawListUi.TextSecondary.b,
                    0.6f)
                : DrawListUi.TextPrimary;
            float available = rect.width - 22f * scale;
            displayed =
                TruncateText(displayed, available, 14, FontType.Regular);
            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x + 10f * scale, rect.y + 10f * scale),
                displayed,
                color,
                14);

            if (focused &&
                Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f)
            {
                Vector2 textSize =
                    DrawListUi.MeasureText(displayed, 11);
                float cursorX =
                    Mathf.Min(
                        rect.xMax - 8f * scale,
                        rect.x + 10f * scale + textSize.x + 2f * scale);
                drawList.AddLine(
                    new Vector2(cursorX, rect.y + 9f * scale),
                    new Vector2(cursorX, rect.yMax - 9f * scale),
                    DrawListUi.AccentBright,
                    Mathf.Max(1f, scale));
            }

            return clicked;
        }

        /// <summary>
        /// Draws a bounded horizontal slider and reports a changed pointer-driven value.
        /// </summary>
        public static bool DrawSlider(FuDrawList drawList, Rect rect, string label, float value, float minimum, float maximum, string format, Color accent, out float changedValue)
        {
            // DrawList sliders use the public Fugui pointer state and remain independent from ImGui widgets.
            float scale = DrawListUi.GetScale();
            Vector2 pointer =
                Fugui.DefaultContainer.Mouse.Position;
            bool hovered = IsHovered(rect);
            bool changing =
                _interactionsEnabled && rect.Contains(_pressOrigin) &&
                (!_interactionClip.HasValue || _interactionClip.Value.Contains(_pressOrigin)) && Input.GetMouseButton(0);
            changedValue = value;
            if (changing && maximum > minimum)
            {
                changedValue =
                    Mathf.Lerp(
                        minimum,
                        maximum,
                        Mathf.Clamp01(
                            (pointer.x - rect.x) /
                            Mathf.Max(1f, rect.width)));
            }

            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x, rect.y - 19f * scale),
                label,
                DrawListUi.TextSecondary,
                12,
                FontType.Regular);
            string valueLabel =
                value.ToString(
                    string.IsNullOrWhiteSpace(format)
                        ? "0.00"
                        : format);
            Vector2 valueSize =
                DrawListUi.MeasureText(
                    valueLabel,
                    12,
                    FontType.Regular);
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    rect.xMax - valueSize.x,
                    rect.y - 19f * scale),
                valueLabel,
                DrawListUi.TextPrimary,
                12,
                FontType.Regular);

            float railHeight = 7f * scale;
            Rect rail = new Rect(
                rect.x,
                rect.center.y - railHeight * 0.5f,
                rect.width,
                railHeight);
            drawList.AddRectFilled(
                rail.min,
                rail.max,
                new Color(0.014f, 0.013f, 0.014f, 0.98f),
                railHeight);
            float normalized =
                maximum > minimum
                    ? Mathf.InverseLerp(minimum, maximum, changing ? changedValue : value)
                    : 0f;
            float handleX =
                Mathf.Lerp(rail.x, rail.xMax, normalized);
            drawList.AddRectFilled(
                rail.min,
                new Vector2(handleX, rail.yMax),
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hovered ? 0.92f : 0.66f),
                railHeight);
            drawList.AddCircleFilled(
                new Vector2(handleX, rail.center.y),
                (hovered ? 7f : 5.5f) * scale,
                hovered
                    ? DrawListUi.AccentBright
                    : accent);
            return changing &&
                   !Mathf.Approximately(changedValue, value);
        }

        /// <summary>
        /// Draws one environment color swatch and reports activation.
        /// </summary>
        public static bool DrawColorSwatch(FuDrawList drawList, Rect rect, string label, Color value, Color accent)
        {
            // A large swatch communicates the actual authored color before opening channel controls.
            bool hovered =
                _interactionsEnabled && PointerInClip &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked = WasClicked(rect);
            float scale = DrawListUi.GetScale();
            DrawListUi.DrawPlate(
                drawList,
                rect,
                hovered ? accent : DrawListUi.Steel,
                hovered);
            Rect swatch = new Rect(
                rect.x + 7f * scale,
                rect.y + 7f * scale,
                32f * scale,
                rect.height - 14f * scale);
            drawList.AddRectFilled(
                swatch.min,
                swatch.max,
                value,
                3f * scale);
            drawList.AddRect(
                swatch.min,
                swatch.max,
                new Color(1f, 1f, 1f, 0.34f),
                3f * scale,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, scale));
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    swatch.xMax + 9f * scale,
                    rect.y + 12f * scale),
                TruncateText(label, rect.width - 57f * scale, 12, FontType.Regular),
                DrawListUi.TextPrimary,
                12,
                FontType.Regular);
            return clicked;
        }


        /// <summary>
        /// Truncates text to a measured width and appends a single ellipsis when needed.
        /// </summary>
        public static string TruncateText(string value, float maxWidth, int fontSize, FontType fontType)
        {
            // Measurement-based truncation remains correct for variable-width Fugui fonts.
            string text = value ?? string.Empty;
            if (DrawListUi.MeasureText(text, fontSize, fontType).x <= maxWidth)
                return text;

            const string ellipsis = "…";
            while (text.Length > 0 &&
                   DrawListUi.MeasureText(
                       text + ellipsis,
                       fontSize,
                       fontType).x > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text + ellipsis;
        }
    }
}

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

        /// <summary>
        /// Enables or suppresses editor-control hit-testing for the current DrawList pass.
        /// </summary>
        public static void SetInteractionsEnabled(bool enabled)
        {
            // Presenters suppress background workbench actions while a modal owns input.
            _interactionsEnabled = enabled;
        }

        /// <summary>
        /// Draws a compact action button suited to dense creator toolbars.
        /// </summary>
        public static bool DrawCompactButton(
            FuDrawList drawList,
            Rect rect,
            string label,
            Color accent,
            bool enabled = true,
            bool selected = false)
        {
            // Dense controls reuse the forged material language without the main-menu rune spacing.
            bool hovered =
                _interactionsEnabled &&
                enabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool pressed =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsPressed(FuMouseButton.Left);
            bool clicked =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);
            float scale = DrawListUi.GetScale();
            Color background = !enabled
                ? new Color(0.025f, 0.024f, 0.025f, 0.84f)
                : pressed
                    ? new Color(0.19f, 0.11f, 0.045f, 0.98f)
                    : hovered || selected
                        ? new Color(0.12f, 0.077f, 0.038f, 0.98f)
                        : new Color(0.045f, 0.039f, 0.037f, 0.96f);
            Color border = !enabled
                ? new Color(0.26f, 0.25f, 0.24f, 0.3f)
                : new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hovered || selected ? 0.9f : 0.4f);

            drawList.AddRectFilled(
                rect.min,
                rect.max,
                background,
                4f * scale);
            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                4f * scale,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (hovered || selected ? 1.6f : 1f) * scale));
            if (selected)
            {
                drawList.AddRectFilled(
                    rect.min,
                    new Vector2(rect.x + 4f * scale, rect.yMax),
                    accent,
                    3f * scale,
                    FuDrawFlags.RoundCornersLeft);
            }

            Vector2 textSize =
                DrawListUi.MeasureText(label, 11, FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                rect.center - textSize * 0.5f,
                label,
                enabled
                    ? DrawListUi.TextPrimary
                    : new Color(0.38f, 0.36f, 0.34f, 1f),
                11,
                FontType.Bold);
            return clicked;
        }

        /// <summary>
        /// Draws one palette tab and returns whether it was activated.
        /// </summary>
        public static bool DrawTab(
            FuDrawList drawList,
            Rect rect,
            string label,
            bool selected)
        {
            // Tabs use a bright lower edge to establish hierarchy without enclosing every label.
            bool hovered =
                _interactionsEnabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);
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
                new Vector2(rect.x, rect.y + 11f * DrawListUi.GetScale()),
                rect.width,
                label,
                selected || hovered
                    ? DrawListUi.TextPrimary
                    : DrawListUi.TextSecondary,
                10,
                FontType.Bold);
            return clicked;
        }

        /// <summary>
        /// Draws an authored-content card with optional preview image and hover information.
        /// </summary>
        public static bool DrawPaletteCard(
            FuDrawList drawList,
            Rect rect,
            string label,
            string category,
            Texture2D preview,
            Color accent,
            bool selected,
            out bool hovered)
        {
            // Large click targets make rapid mouse authoring reliable while retaining visual previews.
            hovered =
                _interactionsEnabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);
            float scale = DrawListUi.GetScale();
            Color background = selected
                ? new Color(0.12f, 0.073f, 0.031f, 0.98f)
                : hovered
                    ? new Color(0.078f, 0.058f, 0.044f, 0.98f)
                    : new Color(0.032f, 0.029f, 0.029f, 0.96f);
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
                TruncateText(label, textWidth, 11, FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(textX, rect.y + 14f * scale),
                safeLabel,
                DrawListUi.TextPrimary,
                11,
                FontType.Bold);
            if (!string.IsNullOrWhiteSpace(category))
            {
                string safeCategory =
                    TruncateText(category.ToUpperInvariant(), textWidth, 8, FontType.Bold);
                DrawListUi.DrawText(
                    drawList,
                    new Vector2(textX, rect.y + 39f * scale),
                    safeCategory,
                    new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.72f),
                    8,
                    FontType.Bold);
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
        public static bool DrawTextField(
            FuDrawList drawList,
            Rect rect,
            string value,
            string placeholder,
            bool focused)
        {
            // Text input is collected by the presenter while the DrawList owns only focus hit-testing.
            bool hovered =
                _interactionsEnabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);
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
                TruncateText(displayed, available, 11, FontType.Regular);
            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x + 10f * scale, rect.y + 10f * scale),
                displayed,
                color,
                11);

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
        public static bool DrawSlider(
            FuDrawList drawList,
            Rect rect,
            string label,
            float value,
            float minimum,
            float maximum,
            string format,
            Color accent,
            out float changedValue)
        {
            // DrawList sliders use the public Fugui pointer state and remain independent from ImGui widgets.
            float scale = DrawListUi.GetScale();
            Vector2 pointer =
                Fugui.DefaultContainer.Mouse.Position;
            bool hovered = rect.Contains(pointer);
            hovered = _interactionsEnabled && hovered;
            bool changing =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsPressed(FuMouseButton.Left);
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
                label.ToUpperInvariant(),
                DrawListUi.TextSecondary,
                8,
                FontType.Bold);
            string valueLabel =
                value.ToString(
                    string.IsNullOrWhiteSpace(format)
                        ? "0.00"
                        : format);
            Vector2 valueSize =
                DrawListUi.MeasureText(
                    valueLabel,
                    9,
                    FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    rect.xMax - valueSize.x,
                    rect.y - 19f * scale),
                valueLabel,
                DrawListUi.TextPrimary,
                9,
                FontType.Bold);

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
        public static bool DrawColorSwatch(
            FuDrawList drawList,
            Rect rect,
            string label,
            Color value,
            Color accent)
        {
            // A large swatch communicates the actual authored color before opening channel controls.
            bool hovered =
                _interactionsEnabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool clicked =
                hovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);
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
                label,
                DrawListUi.TextPrimary,
                9,
                FontType.Bold);
            return clicked;
        }

        /// <summary>
        /// Draws one compact inspector label and value row.
        /// </summary>
        public static void DrawValueRow(
            FuDrawList drawList,
            Rect rect,
            string label,
            string value,
            Color accent)
        {
            // Alternating plate depth gives dense metadata a scannable label/value rhythm.
            DrawListUi.DrawPlate(drawList, rect, accent);
            float scale = DrawListUi.GetScale();
            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x + 10f * scale, rect.y + 9f * scale),
                label.ToUpperInvariant(),
                DrawListUi.TextSecondary,
                9,
                FontType.Bold);
            string safeValue =
                TruncateText(
                    value,
                    rect.width * 0.56f,
                    10,
                    FontType.Bold);
            Vector2 valueSize =
                DrawListUi.MeasureText(safeValue, 10, FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    rect.xMax - 10f * scale - valueSize.x,
                    rect.y + 9f * scale),
                safeValue,
                DrawListUi.TextPrimary,
                10,
                FontType.Bold);
        }

        /// <summary>
        /// Truncates text to a measured width and appends a single ellipsis when needed.
        /// </summary>
        private static string TruncateText(
            string value,
            float maxWidth,
            int fontSize,
            FontType fontType)
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

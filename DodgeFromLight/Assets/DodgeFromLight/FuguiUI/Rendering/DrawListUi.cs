using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Provides the shared visual language and interaction helpers for runtime DrawList screens.
    /// </summary>
    internal static class DrawListUi
    {
        public static readonly Color Accent = new Color(0.96f, 0.78f, 0.18f, 1f);
        public static readonly Color Danger = new Color(0.96f, 0.3f, 0.24f, 1f);
        public static readonly Color Success = new Color(0.34f, 0.94f, 0.64f, 1f);
        public static readonly Color TextPrimary = new Color(0.96f, 0.97f, 0.98f, 1f);
        public static readonly Color TextSecondary = new Color(0.66f, 0.71f, 0.76f, 1f);
        public static readonly Color Surface = new Color(0.035f, 0.045f, 0.055f, 0.94f);
        public static readonly Color SurfaceLight = new Color(0.075f, 0.09f, 0.105f, 0.96f);

        #region Geometry

        /// <summary>
        /// Gets the active Fugui display size in screen pixels.
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            // DrawList coordinates use the same top-left origin as the ImGui display.
            return Fugui.DefaultContainer.Size;
        }

        /// <summary>
        /// Gets the current container-aware Fugui layout scale.
        /// </summary>
        public static float GetScale()
        {
            // The controller already scales against its 1920x1080 reference resolution.
            return Fugui.CurrentContext == null
                ? 1f
                : Mathf.Max(0.5f, Fugui.CurrentContext.Scale);
        }

        /// <summary>
        /// Creates a screen-centered rectangle with the requested size.
        /// </summary>
        public static Rect CenterRect(Vector2 screenSize, Vector2 size)
        {
            // Central overlays remain stable across aspect ratios.
            return new Rect((screenSize - size) * 0.5f, size);
        }

        /// <summary>
        /// Draws a translucent full-screen interaction scrim.
        /// </summary>
        public static void DrawScrim(FuDrawList drawList, Vector2 screenSize, float opacity)
        {
            // A scrim separates terminal and pause states from the active board.
            drawList.AddRectFilled(
                Vector2.zero,
                screenSize,
                new Color(0.01f, 0.015f, 0.02f, Mathf.Clamp01(opacity)));
        }

        /// <summary>
        /// Draws a rounded surface with a soft shadow, border, and top accent.
        /// </summary>
        public static void DrawPanel(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            bool emphasized = false)
        {
            // Layered primitives keep the panel readable without requiring a retained Unity Canvas.
            float scale = GetScale();
            float rounding = 12f * scale;
            float shadowOffset = 8f * scale;
            var shadowRect = new Rect(
                rect.x + shadowOffset,
                rect.y + shadowOffset,
                rect.width,
                rect.height);
            drawList.AddRectFilled(
                shadowRect.min,
                shadowRect.max,
                new Color(0f, 0f, 0f, emphasized ? 0.5f : 0.34f),
                rounding);
            drawList.AddRectFilled(
                rect.min,
                rect.max,
                emphasized ? SurfaceLight : Surface,
                rounding);
            drawList.AddRect(
                rect.min,
                rect.max,
                new Color(accent.r, accent.g, accent.b, emphasized ? 0.72f : 0.34f),
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, scale));

            float accentHeight = Mathf.Max(2f, 3f * scale);
            drawList.AddRectFilledMultiColor(
                new Vector2(rect.x + rounding, rect.y),
                new Vector2(rect.xMax - rounding, rect.y + accentHeight),
                Fugui.GetColorU32(new Color(accent.r, accent.g, accent.b, 0.12f)),
                Fugui.GetColorU32(accent),
                Fugui.GetColorU32(accent),
                Fugui.GetColorU32(new Color(accent.r, accent.g, accent.b, 0.12f)));
        }

        #endregion

        #region Typography

        /// <summary>
        /// Draws text with an explicit configured Fugui font size.
        /// </summary>
        public static void DrawText(
            FuDrawList drawList,
            Vector2 position,
            string text,
            Color color,
            int fontSize = 14,
            FontType fontType = FontType.Regular)
        {
            // Font pushes affect the DrawList text command without introducing a Fugui layout item.
            if (string.IsNullOrEmpty(text))
                return;

            Fugui.PushFont(fontSize, fontType);
            try
            {
                drawList.AddText(position, Fugui.GetColorU32(color), text);
            }
            finally
            {
                Fugui.PopFont();
            }
        }

        /// <summary>
        /// Draws horizontally centered text inside a fixed-width area.
        /// </summary>
        public static void DrawCenteredText(
            FuDrawList drawList,
            Vector2 position,
            float width,
            string text,
            Color color,
            int fontSize = 14,
            FontType fontType = FontType.Regular)
        {
            // Text measurement uses the same pushed font as the final DrawList command.
            Vector2 textSize = MeasureText(text, fontSize, fontType);
            DrawText(
                drawList,
                new Vector2(position.x + Mathf.Max(0f, (width - textSize.x) * 0.5f), position.y),
                text,
                color,
                fontSize,
                fontType);
        }

        /// <summary>
        /// Measures text with an explicit configured Fugui font size.
        /// </summary>
        public static Vector2 MeasureText(
            string text,
            int fontSize = 14,
            FontType fontType = FontType.Regular)
        {
            // Matching measurement and rendering avoids alignment drift at scaled resolutions.
            Fugui.PushFont(fontSize, fontType);
            try
            {
                return Fugui.CalcTextSize(text ?? string.Empty);
            }
            finally
            {
                Fugui.PopFont();
            }
        }
        #endregion

        #region Interactions
        /// <summary>
        /// Draws an interactive button and reports a left-click release for its rectangle.
        /// </summary>
        public static bool DrawButton(
            FuDrawList drawList,
            Rect rect,
            string label,
            string shortcut,
            Color accent,
            bool enabled = true,
            bool selected = false)
        {
            // DrawList geometry needs explicit hit-testing because it does not submit an ImGui item.
            bool hovered = enabled && rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool pressed = hovered && Fugui.DefaultContainer.Mouse.IsDown(FuMouseButton.Left);
            bool clicked = hovered && Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);

            float scale = GetScale();
            float rounding = 9f * scale;
            Color background = enabled
                ? pressed
                    ? new Color(accent.r, accent.g, accent.b, 0.3f)
                    : hovered || selected
                        ? new Color(accent.r, accent.g, accent.b, 0.2f)
                        : new Color(0.11f, 0.13f, 0.145f, 0.96f)
                : new Color(0.08f, 0.09f, 0.1f, 0.72f);
            Color border = enabled
                ? new Color(accent.r, accent.g, accent.b, hovered || selected ? 0.95f : 0.38f)
                : new Color(0.25f, 0.27f, 0.29f, 0.45f);
            Color labelColor = enabled ? TextPrimary : new Color(0.42f, 0.44f, 0.46f, 1f);

            drawList.AddRectFilled(rect.min, rect.max, background, rounding);
            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (hovered || selected ? 2f : 1f) * scale));

            Vector2 labelSize = MeasureText(label, 16, FontType.Bold);
            float labelY = rect.y + Mathf.Max(0f, (rect.height - labelSize.y) * 0.5f);
            DrawText(
                drawList,
                new Vector2(rect.x + 18f * scale, labelY),
                label,
                labelColor,
                16,
                FontType.Bold);

            if (!string.IsNullOrWhiteSpace(shortcut))
            {
                Vector2 shortcutSize = MeasureText(shortcut, 12);
                DrawText(
                    drawList,
                    new Vector2(
                        rect.xMax - shortcutSize.x - 18f * scale,
                        rect.y + Mathf.Max(0f, (rect.height - shortcutSize.y) * 0.5f)),
                    shortcut,
                    enabled ? TextSecondary : new Color(0.36f, 0.38f, 0.4f, 1f),
                    12);
            }

            return clicked;
        }

        /// <summary>
        /// Draws one compact keyboard binding tile with optional pressed feedback.
        /// </summary>
        public static void DrawKey(
            FuDrawList drawList,
            Rect rect,
            string binding,
            bool pressed)
        {
            // Bright pressed feedback replaces the old collection of mutable uGUI key images.
            float scale = GetScale();
            float rounding = 7f * scale;
            Color fill = pressed
                ? new Color(Accent.r, Accent.g, Accent.b, 0.82f)
                : new Color(0.08f, 0.1f, 0.115f, 0.96f);
            Color border = pressed ? Accent : new Color(0.4f, 0.44f, 0.48f, 0.55f);
            Color text = pressed ? new Color(0.055f, 0.06f, 0.065f, 1f) : TextPrimary;

            drawList.AddRectFilled(rect.min, rect.max, fill, rounding);
            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, scale));
            Vector2 textSize = MeasureText(binding, 14, FontType.Bold);
            DrawText(
                drawList,
                rect.center - textSize * 0.5f,
                binding,
                text,
                14,
                FontType.Bold);
        }

        /// <summary>
        /// Draws a compact state tag used by active effects and contextual prompts.
        /// </summary>
        public static float DrawTag(
            FuDrawList drawList,
            Vector2 position,
            string text,
            Color accent)
        {
            // Tags expose turn-limited effects without expanding the main information panel.
            float scale = GetScale();
            Vector2 textSize = MeasureText(text, 12, FontType.Bold);
            Vector2 padding = new Vector2(10f, 6f) * scale;
            Rect rect = new Rect(position, textSize + padding * 2f);
            drawList.AddRectFilled(
                rect.min,
                rect.max,
                new Color(accent.r, accent.g, accent.b, 0.16f),
                7f * scale);
            drawList.AddRect(
                rect.min,
                rect.max,
                new Color(accent.r, accent.g, accent.b, 0.52f),
                7f * scale);
            DrawText(drawList, position + padding, text, accent, 12, FontType.Bold);
            return rect.width;
        }

        #endregion
    }
}

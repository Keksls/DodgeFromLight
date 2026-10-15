using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Provides the shared dark-fantasy visual language and interaction helpers for runtime DrawList screens.
    /// </summary>
    internal static class DrawListUi
    {
        public static readonly Color Accent =
            new Color(0.86f, 0.62f, 0.27f, 1f);
        public static readonly Color AccentBright =
            new Color(1f, 0.79f, 0.4f, 1f);
        public static readonly Color Danger =
            new Color(0.72f, 0.19f, 0.13f, 1f);
        public static readonly Color Success =
            new Color(0.58f, 0.68f, 0.37f, 1f);
        public static readonly Color Steel =
            new Color(0.45f, 0.56f, 0.62f, 1f);
        public static readonly Color TextPrimary =
            new Color(0.94f, 0.9f, 0.8f, 1f);
        public static readonly Color TextSecondary =
            new Color(0.62f, 0.58f, 0.52f, 1f);
        public static readonly Color Surface =
            new Color(0.03f, 0.026f, 0.025f, 0.96f);
        public static readonly Color SurfaceLight =
            new Color(0.075f, 0.06f, 0.05f, 0.98f);

        private static DarkFantasyUiSkin _skin;
        private static int _skinOwnerCount;

        #region Theme lifetime

        /// <summary>
        /// Acquires the shared runtime material skin for one active DrawList presenter.
        /// </summary>
        public static void AcquireSkin()
        {
            // Reference counting keeps scene hand-offs safe when old and new presenters overlap for one frame.
            _skinOwnerCount++;
            if (_skin == null)
                _skin = new DarkFantasyUiSkin();
        }

        /// <summary>
        /// Releases the shared runtime material skin owned by one DrawList presenter.
        /// </summary>
        public static void ReleaseSkin()
        {
            // The final owner releases every runtime Unity material created by the skin.
            _skinOwnerCount = Mathf.Max(0, _skinOwnerCount - 1);
            if (_skinOwnerCount > 0 || _skin == null)
                return;

            _skin.Dispose();
            _skin = null;
        }

        #endregion

        #region Geometry

        /// <summary>
        /// Gets the active Fugui display size in screen pixels.
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            // DrawList coordinates use the active Fugui container's top-left origin.
            return Fugui.DefaultContainer.Size;
        }

        /// <summary>
        /// Gets the current container-aware Fugui layout scale.
        /// </summary>
        public static float GetScale()
        {
            // The controller scales against its configured 1920x1080 reference resolution.
            return Fugui.CurrentContext == null
                ? 1f
                : Mathf.Max(0.5f, Fugui.CurrentContext.Scale);
        }

        /// <summary>
        /// Creates a screen-centered rectangle with the requested size.
        /// </summary>
        public static Rect CenterRect(Vector2 screenSize, Vector2 size)
        {
            // Central overlays remain stable across standard and ultrawide aspect ratios.
            return new Rect((screenSize - size) * 0.5f, size);
        }

        /// <summary>
        /// Draws the animated dark-fantasy atmosphere over a scene background.
        /// </summary>
        public static void DrawAtmosphere(
            FuDrawList drawList,
            Vector2 screenSize)
        {
            // The shader contributes restrained smoke and embers without hiding the authored 3D environment.
            bool shaderDrawn =
                _skin != null &&
                _skin.TryDrawBackdrop(drawList, screenSize, Accent);
            if (!shaderDrawn)
            {
                drawList.AddRectFilled(
                    Vector2.zero,
                    screenSize,
                    new Color(0.015f, 0.008f, 0.008f, 0.24f));
            }

            float railWidth = Mathf.Min(
                screenSize.x * 0.59f,
                820f * GetScale());
            uint strong = Fugui.GetColorU32(
                new Color(0.012f, 0.009f, 0.01f, 0.96f));
            uint clear = Fugui.GetColorU32(
                new Color(0.012f, 0.009f, 0.01f, 0f));
            drawList.AddRectFilledMultiColor(
                Vector2.zero,
                new Vector2(railWidth, screenSize.y),
                strong,
                clear,
                clear,
                strong);

            uint verticalClear = Fugui.GetColorU32(
                new Color(0f, 0f, 0f, 0f));
            uint verticalDark = Fugui.GetColorU32(
                new Color(0f, 0f, 0f, 0.62f));
            drawList.AddRectFilledMultiColor(
                Vector2.zero,
                screenSize,
                verticalDark,
                verticalDark,
                verticalClear,
                verticalClear);
        }

        /// <summary>
        /// Draws a translucent full-screen interaction scrim.
        /// </summary>
        public static void DrawScrim(
            FuDrawList drawList,
            Vector2 screenSize,
            float opacity)
        {
            // A warm black scrim separates terminal and pause states from the active board.
            Color top = new Color(
                0.012f,
                0.008f,
                0.009f,
                Mathf.Clamp01(opacity));
            Color bottom = new Color(
                0.025f,
                0.009f,
                0.008f,
                Mathf.Clamp01(opacity * 0.92f));
            drawList.AddRectFilledMultiColor(
                Vector2.zero,
                screenSize,
                Fugui.GetColorU32(top),
                Fugui.GetColorU32(top),
                Fugui.GetColorU32(bottom),
                Fugui.GetColorU32(bottom));
        }

        /// <summary>
        /// Draws a forged-iron panel with layered depth, gilded frame, and corner brackets.
        /// </summary>
        public static void DrawPanel(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            bool emphasized = false)
        {
            // Shader-backed matter sits beneath crisp DrawList framing and typography.
            float scale = GetScale();
            float rounding = 6f * scale;
            float shadowOffset = (emphasized ? 12f : 7f) * scale;
            Rect wideShadow = new Rect(
                rect.x - 4f * scale + shadowOffset,
                rect.y - 2f * scale + shadowOffset,
                rect.width + 8f * scale,
                rect.height + 8f * scale);
            drawList.AddRectFilled(
                wideShadow.min,
                wideShadow.max,
                new Color(0f, 0f, 0f, emphasized ? 0.62f : 0.4f),
                rounding + 4f * scale);

            bool shaderDrawn =
                _skin != null &&
                _skin.TryDrawPanel(
                    drawList,
                    rect,
                    accent,
                    rounding,
                    emphasized);
            if (!shaderDrawn)
            {
                drawList.AddRectFilled(
                    rect.min,
                    rect.max,
                    emphasized ? SurfaceLight : Surface,
                    rounding);
            }

            Color outerBorder = new Color(
                accent.r,
                accent.g,
                accent.b,
                emphasized ? 0.84f : 0.48f);
            Color innerBorder = new Color(
                0.89f,
                0.76f,
                0.53f,
                emphasized ? 0.3f : 0.16f);
            drawList.AddRect(
                rect.min,
                rect.max,
                outerBorder,
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (emphasized ? 1.8f : 1.1f) * scale));

            Rect inner = new Rect(
                rect.x + 5f * scale,
                rect.y + 5f * scale,
                rect.width - 10f * scale,
                rect.height - 10f * scale);
            drawList.AddRect(
                inner.min,
                inner.max,
                innerBorder,
                Mathf.Max(0f, rounding - 2f * scale),
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, 0.8f * scale));
            DrawCornerBrackets(
                drawList,
                rect,
                accent,
                emphasized ? 1f : 0.68f);
        }

        /// <summary>
        /// Draws a recessed iron plate for a metric, inventory row, or secondary value.
        /// </summary>
        public static void DrawPlate(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            bool highlighted = false)
        {
            // Plates remain quieter than parent panels while retaining the same material vocabulary.
            float scale = GetScale();
            float rounding = 4f * scale;
            bool shaderDrawn =
                _skin != null &&
                _skin.TryDrawPlate(
                    drawList,
                    rect,
                    accent,
                    rounding);
            if (!shaderDrawn)
            {
                drawList.AddRectFilled(
                    rect.min,
                    rect.max,
                    new Color(0.018f, 0.017f, 0.018f, 0.92f),
                    rounding);
            }

            drawList.AddRect(
                rect.min,
                rect.max,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    highlighted ? 0.58f : 0.25f),
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (highlighted ? 1.5f : 0.85f) * scale));
            drawList.AddLine(
                new Vector2(rect.x + 8f * scale, rect.y + 2f * scale),
                new Vector2(rect.xMax - 8f * scale, rect.y + 2f * scale),
                new Color(0.95f, 0.82f, 0.58f, 0.08f),
                Mathf.Max(1f, scale));
        }

        /// <summary>
        /// Draws a symmetrical ornamental divider.
        /// </summary>
        public static void DrawDivider(
            FuDrawList drawList,
            Vector2 center,
            float width,
            Color accent,
            float opacity = 0.55f)
        {
            // The center diamond and tapered gradients echo carved altar and sword motifs.
            float scale = GetScale();
            float halfWidth = Mathf.Max(12f * scale, width * 0.5f);
            Color transparent =
                new Color(accent.r, accent.g, accent.b, 0f);
            Color visible =
                new Color(accent.r, accent.g, accent.b, opacity);
            float lineHeight = Mathf.Max(1f, scale);

            drawList.AddRectFilledMultiColor(
                new Vector2(center.x - halfWidth, center.y),
                new Vector2(center.x - 7f * scale, center.y + lineHeight),
                Fugui.GetColorU32(transparent),
                Fugui.GetColorU32(visible),
                Fugui.GetColorU32(visible),
                Fugui.GetColorU32(transparent));
            drawList.AddRectFilledMultiColor(
                new Vector2(center.x + 7f * scale, center.y),
                new Vector2(center.x + halfWidth, center.y + lineHeight),
                Fugui.GetColorU32(visible),
                Fugui.GetColorU32(transparent),
                Fugui.GetColorU32(transparent),
                Fugui.GetColorU32(visible));
            DrawDiamond(
                drawList,
                center + new Vector2(0f, lineHeight * 0.5f),
                new Vector2(5f, 5f) * scale,
                visible,
                true);
            DrawDiamond(
                drawList,
                center + new Vector2(0f, lineHeight * 0.5f),
                new Vector2(9f, 9f) * scale,
                new Color(accent.r, accent.g, accent.b, opacity * 0.5f),
                false);
        }

        /// <summary>
        /// Draws a compact section label followed by a fading forged line.
        /// </summary>
        public static void DrawSectionLabel(
            FuDrawList drawList,
            Vector2 position,
            float availableWidth,
            string label,
            Color accent)
        {
            // Uppercase labels act as engraved captions rather than competing headings.
            float scale = GetScale();
            DrawText(
                drawList,
                position,
                label,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.84f),
                11,
                FontType.Bold);
            Vector2 labelSize = MeasureText(label, 11, FontType.Bold);
            float lineStart = position.x + labelSize.x + 13f * scale;
            float lineEnd = position.x + availableWidth;
            if (lineEnd <= lineStart)
                return;

            drawList.AddRectFilledMultiColor(
                new Vector2(lineStart, position.y + 7f * scale),
                new Vector2(lineEnd, position.y + 8f * scale),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.46f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.46f)));
        }

        /// <summary>
        /// Draws a small ritual emblem suitable for modal headers and loading states.
        /// </summary>
        public static void DrawRuneEmblem(
            FuDrawList drawList,
            Vector2 center,
            float radius,
            Color accent,
            float phase = 0f)
        {
            // Layered circles, cardinal blades, and a rotating inner diamond form a readable fantasy sigil.
            float scale = GetScale();
            float rotation = phase * Mathf.PI * 2f;
            Color quiet =
                new Color(accent.r, accent.g, accent.b, 0.28f);
            Color bright =
                new Color(accent.r, accent.g, accent.b, 0.78f);
            drawList.AddCircle(
                center,
                radius,
                Fugui.GetColorU32(bright),
                32,
                Mathf.Max(1f, 1.4f * scale));
            drawList.AddCircle(
                center,
                radius * 0.68f,
                Fugui.GetColorU32(quiet),
                24,
                Mathf.Max(1f, scale));

            for (int i = 0; i < 4; i++)
            {
                float angle = rotation + i * Mathf.PI * 0.5f;
                Vector2 direction =
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent =
                    new Vector2(-direction.y, direction.x);
                Vector2 tip = center + direction * radius * 1.18f;
                Vector2 baseCenter = center + direction * radius * 0.72f;
                drawList.AddTriangleFilled(
                    tip,
                    baseCenter + tangent * radius * 0.12f,
                    baseCenter - tangent * radius * 0.12f,
                    Fugui.GetColorU32(quiet));
            }

            Vector2 diamondSize =
                new Vector2(radius * 0.34f, radius * 0.34f);
            DrawDiamond(drawList, center, diamondSize, bright, false);
            drawList.AddCircleFilled(
                center,
                Mathf.Max(1.8f * scale, radius * 0.07f),
                accent);
        }

        #endregion

        #region Typography

        /// <summary>
        /// Draws text with an explicit configured Fugui font size.
        /// </summary>
        public static void DrawText(FuDrawList drawList, Vector2 position, string text, Color color, int fontSize = 14, FontType fontType = FontType.Regular)
        {
            // Font pushes affect the DrawList text command without creating a Fugui layout item.
            if (string.IsNullOrEmpty(text))
                return;

            bool fontPushed = TryPushConfiguredFont(fontSize, fontType);
            try
            {
                drawList.AddText(
                    position + Vector2.one * GetScale(),
                    Fugui.GetColorU32(
                        new Color(0f, 0f, 0f, color.a * 0.78f)),
                    text);
                drawList.AddText(
                    position,
                    Fugui.GetColorU32(color),
                    text);
            }
            finally
            {
                if (fontPushed)
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
                new Vector2(
                    position.x + Mathf.Max(
                        0f,
                        (width - textSize.x) * 0.5f),
                    position.y),
                text,
                color,
                fontSize,
                fontType);
        }

        /// <summary>
        /// Measures text with an explicit configured Fugui font size.
        /// </summary>
        public static Vector2 MeasureText(string text, int fontSize = 14, FontType fontType = FontType.Regular)
        {
            // Matching measurement and rendering avoids alignment drift at scaled resolutions.
            bool fontPushed = TryPushConfiguredFont(fontSize, fontType);
            try
            {
                return Fugui.CalcTextSize(text ?? string.Empty);
            }
            finally
            {
                if (fontPushed)
                    Fugui.PopFont();
            }
        }

        /// <summary>
        /// Selects an existing family, size, and style from the active atlas configuration before pushing it.
        /// </summary>
        private static bool TryPushConfiguredFont(int requestedSize, FontType requestedType)
        {
            // Use the same selection for drawing and measuring, including sparse user-authored font configs.
            FontConfig config = Fugui.Settings?.FontConfig;
            if (config == null || config.Fonts == null || Fugui.CurrentContext == null)
                return false;

            FontSizeConfig selected = null;
            FontType selectedType = FontType.Regular;
            int bestScore = int.MaxValue;
            foreach (FontSizeConfig candidate in config.Fonts)
            {
                if (candidate == null || candidate.Name != config.DefaultFontName)
                    continue;

                bool hasRequestedStyle = HasConfiguredStyle(candidate, requestedType);
                FontType candidateType = hasRequestedStyle ? requestedType : FontType.Regular;
                if (!HasConfiguredStyle(candidate, candidateType))
                    continue;

                int score = Mathf.Abs(candidate.Size - requestedSize) * 4 + (hasRequestedStyle ? 0 : 1);
                if (score >= bestScore)
                    continue;
                selected = candidate;
                selectedType = candidateType;
                bestScore = score;
            }

            // With no usable configured entry, retain Fugui's current atlas font without an unmatched pop.
            if (selected == null)
                return false;
            Fugui.PushFont(selected.Name, selected.Size, selectedType);
            return true;
        }

        /// <summary>
        /// Checks whether a configured font size supplies sources for the requested style.
        /// </summary>
        private static bool HasConfiguredStyle(FontSizeConfig config, FontType type)
        {
            // Never request a style that is absent from the atlas source configuration.
            SubFontConfig[] sources;
            switch (type)
            {
                case FontType.Bold:
                    sources = config.SubFonts_Bold;
                    break;
                case FontType.Italic:
                    sources = config.SubFonts_Italic;
                    break;
                default:
                    sources = config.SubFonts_Regular;
                    break;
            }
            return sources != null && sources.Length > 0;
        }

        #endregion

        #region Interactions

        /// <summary>
        /// Draws an interactive forged button and reports a left-click release for its rectangle.
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
            // DrawList geometry uses Fugui's public container input because it does not submit a retained widget.
            bool pointerHovered =
                enabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position);
            bool highlighted = pointerHovered || (enabled && selected);
            bool pressed =
                pointerHovered &&
                Fugui.DefaultContainer.Mouse.IsPressed(FuMouseButton.Left);
            bool clicked =
                pointerHovered &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left);

            float scale = GetScale();
            float rounding = 4f * scale;
            bool shaderDrawn =
                _skin != null &&
                _skin.TryDrawButton(
                    drawList,
                    rect,
                    accent,
                    rounding,
                    enabled,
                    highlighted,
                    pressed);
            if (!shaderDrawn)
            {
                Color background = !enabled
                    ? new Color(0.035f, 0.034f, 0.035f, 0.72f)
                    : pressed
                        ? new Color(
                            accent.r * 0.22f,
                            accent.g * 0.18f,
                            accent.b * 0.14f,
                            0.98f)
                        : highlighted
                            ? new Color(0.11f, 0.078f, 0.05f, 0.98f)
                            : new Color(0.052f, 0.044f, 0.04f, 0.96f);
                drawList.AddRectFilled(
                    rect.min,
                    rect.max,
                    background,
                    rounding);
            }

            Color border = enabled
                ? new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    highlighted ? 0.92f : 0.4f)
                : new Color(0.28f, 0.26f, 0.24f, 0.34f);
            Color labelColor = enabled
                ? highlighted
                    ? new Color(1f, 0.94f, 0.8f, 1f)
                    : TextPrimary
                : new Color(0.39f, 0.37f, 0.35f, 1f);
            float thickness =
                (highlighted ? 1.8f : 1f) * scale;

            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, thickness));

            float spineWidth = highlighted ? 5f : 3f;
            drawList.AddRectFilledMultiColor(
                new Vector2(rect.x, rect.y + 7f * scale),
                new Vector2(
                    rect.x + spineWidth * scale,
                    rect.yMax - 7f * scale),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.18f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.82f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.35f)),
                Fugui.GetColorU32(
                    new Color(accent.r, accent.g, accent.b, 0.08f)));

            Vector2 runeCenter =
                new Vector2(rect.x + 24f * scale, rect.center.y);
            DrawDiamond(
                drawList,
                runeCenter,
                new Vector2(5.5f, 5.5f) * scale,
                enabled
                    ? new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        highlighted ? 0.9f : 0.5f)
                    : new Color(0.32f, 0.3f, 0.28f, 0.34f),
                highlighted);

            Vector2 labelSize =
                MeasureText(label, 15, FontType.Bold);
            float labelY =
                rect.y +
                Mathf.Max(0f, (rect.height - labelSize.y) * 0.5f);
            DrawText(
                drawList,
                new Vector2(rect.x + 43f * scale, labelY),
                label,
                labelColor,
                15,
                FontType.Bold);

            if (!string.IsNullOrWhiteSpace(shortcut))
            {
                Vector2 shortcutSize = MeasureText(shortcut, 11);
                Vector2 padding = new Vector2(10f, 5f) * scale;
                Rect shortcutRect = new Rect(
                    rect.xMax - shortcutSize.x - padding.x * 2f - 13f * scale,
                    rect.center.y - (shortcutSize.y + padding.y * 2f) * 0.5f,
                    shortcutSize.x + padding.x * 2f,
                    shortcutSize.y + padding.y * 2f);
                DrawPlate(
                    drawList,
                    shortcutRect,
                    accent,
                    highlighted);
                DrawCenteredText(
                    drawList,
                    new Vector2(
                        shortcutRect.x,
                        shortcutRect.y + padding.y),
                    shortcutRect.width,
                    shortcut,
                    enabled
                        ? new Color(
                            TextSecondary.r,
                            TextSecondary.g,
                            TextSecondary.b,
                            highlighted ? 1f : 0.78f)
                        : new Color(0.34f, 0.33f, 0.32f, 1f),
                    11);
            }

            if (highlighted)
            {
                float pulse =
                    0.54f +
                    Mathf.Sin(Time.unscaledTime * 3.4f) * 0.16f;
                DrawCornerBrackets(
                    drawList,
                    rect,
                    new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        pulse),
                    0.76f);
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
            // Key tiles are miniature iron plates with brighter gilding while held.
            float scale = GetScale();
            float rounding = 4f * scale;
            bool shaderDrawn =
                _skin != null &&
                _skin.TryDrawButton(
                    drawList,
                    rect,
                    pressed ? AccentBright : Steel,
                    rounding,
                    true,
                    pressed,
                    pressed);
            if (!shaderDrawn)
            {
                drawList.AddRectFilled(
                    rect.min,
                    rect.max,
                    pressed
                        ? new Color(0.23f, 0.14f, 0.06f, 0.98f)
                        : new Color(0.035f, 0.034f, 0.035f, 0.96f),
                    rounding);
            }

            Color border = pressed
                ? AccentBright
                : new Color(
                    Steel.r,
                    Steel.g,
                    Steel.b,
                    0.46f);
            Color text = pressed
                ? new Color(1f, 0.92f, 0.7f, 1f)
                : TextPrimary;
            drawList.AddRect(
                rect.min,
                rect.max,
                border,
                rounding,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, (pressed ? 1.7f : 1f) * scale));

            float rivetRadius = Mathf.Max(1.2f, 1.4f * scale);
            Color rivet =
                new Color(border.r, border.g, border.b, 0.5f);
            drawList.AddCircleFilled(
                rect.position + new Vector2(6f, 6f) * scale,
                rivetRadius,
                rivet);
            drawList.AddCircleFilled(
                new Vector2(
                    rect.xMax - 6f * scale,
                    rect.yMax - 6f * scale),
                rivetRadius,
                rivet);

            Vector2 textSize =
                MeasureText(binding, 13, FontType.Bold);
            DrawText(
                drawList,
                rect.center - textSize * 0.5f,
                binding,
                text,
                13,
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
            // A recessed plate and flanking diamonds expose deterministic remaining turns.
            float scale = GetScale();
            Vector2 textSize = MeasureText(text, 11, FontType.Bold);
            Vector2 padding = new Vector2(13f, 6f) * scale;
            Rect rect = new Rect(
                position,
                textSize + padding * 2f);
            DrawPlate(drawList, rect, accent, true);
            DrawDiamond(
                drawList,
                new Vector2(rect.x + 7f * scale, rect.center.y),
                new Vector2(2.8f, 2.8f) * scale,
                accent,
                true);
            DrawText(
                drawList,
                position + padding,
                text,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.94f),
                11,
                FontType.Bold);
            return rect.width;
        }

        #endregion

        #region Decorative primitives

        /// <summary>
        /// Draws reinforced L-shaped brackets and rivets around a rectangle.
        /// </summary>
        private static void DrawCornerBrackets(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            float intensity)
        {
            // Short angular strokes suggest a forged frame without overwhelming small HUD panels.
            float scale = GetScale();
            float length =
                Mathf.Min(19f * scale, Mathf.Min(rect.width, rect.height) * 0.2f);
            float inset = 3f * scale;
            float thickness = Mathf.Max(1f, 1.2f * scale);
            Color color = new Color(
                accent.r,
                accent.g,
                accent.b,
                accent.a * intensity);

            Vector2 topLeft = rect.min + Vector2.one * inset;
            Vector2 topRight =
                new Vector2(rect.xMax - inset, rect.y + inset);
            Vector2 bottomLeft =
                new Vector2(rect.x + inset, rect.yMax - inset);
            Vector2 bottomRight =
                rect.max - Vector2.one * inset;

            drawList.AddLine(
                topLeft,
                topLeft + Vector2.right * length,
                color,
                thickness);
            drawList.AddLine(
                topLeft,
                topLeft + Vector2.up * length,
                color,
                thickness);
            drawList.AddLine(
                topRight,
                topRight + Vector2.left * length,
                color,
                thickness);
            drawList.AddLine(
                topRight,
                topRight + Vector2.up * length,
                color,
                thickness);
            drawList.AddLine(
                bottomLeft,
                bottomLeft + Vector2.right * length,
                color,
                thickness);
            drawList.AddLine(
                bottomLeft,
                bottomLeft + Vector2.down * length,
                color,
                thickness);
            drawList.AddLine(
                bottomRight,
                bottomRight + Vector2.left * length,
                color,
                thickness);
            drawList.AddLine(
                bottomRight,
                bottomRight + Vector2.down * length,
                color,
                thickness);

            float rivetRadius = Mathf.Max(1f, 1.35f * scale);
            drawList.AddCircleFilled(topLeft, rivetRadius, color);
            drawList.AddCircleFilled(topRight, rivetRadius, color);
            drawList.AddCircleFilled(bottomLeft, rivetRadius, color);
            drawList.AddCircleFilled(bottomRight, rivetRadius, color);
        }

        /// <summary>
        /// Draws one filled or outlined diamond centered on a point.
        /// </summary>
        private static void DrawDiamond(
            FuDrawList drawList,
            Vector2 center,
            Vector2 radius,
            Color color,
            bool filled)
        {
            // Diamonds provide a compact rune vocabulary that remains crisp at every UI scale.
            Vector2 top = center + Vector2.up * radius.y;
            Vector2 right = center + Vector2.right * radius.x;
            Vector2 bottom = center + Vector2.down * radius.y;
            Vector2 left = center + Vector2.left * radius.x;
            uint packedColor = Fugui.GetColorU32(color);
            if (filled)
            {
                drawList.AddQuadFilled(
                    top,
                    right,
                    bottom,
                    left,
                    packedColor);
                return;
            }

            drawList.AddQuad(
                top,
                right,
                bottom,
                left,
                packedColor,
                Mathf.Max(1f, GetScale()));
        }

        #endregion
    }
}

using System;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Owns the runtime materials used by the dark-fantasy Fugui DrawList surfaces.
    /// </summary>
    internal sealed class DarkFantasyUiSkin : IDisposable
    {
        #region Material state

        private const string SurfaceShaderResourcePath =
            "Shaders/DarkFantasySurface";

#if FU_CUSTOM_MATERIALS_ENABLED
        private static readonly int BaseColorProperty =
            Shader.PropertyToID("_BaseColor");
        private static readonly int DeepColorProperty =
            Shader.PropertyToID("_DeepColor");
        private static readonly int InteractionProperty =
            Shader.PropertyToID("_Interaction");
        private static readonly int OpacityProperty =
            Shader.PropertyToID("_Opacity");
        private static readonly int SurfaceModeProperty =
            Shader.PropertyToID("_SurfaceMode");

        private Material _backdropMaterial;
        private Material _panelMaterial;
        private Material _emphasizedPanelMaterial;
        private Material _plateMaterial;
        private Material _buttonMaterial;
        private Material _hoveredButtonMaterial;
        private Material _pressedButtonMaterial;
        private Material _disabledButtonMaterial;

        private FuDrawMaterial _backdropDrawMaterial;
        private FuDrawMaterial _panelDrawMaterial;
        private FuDrawMaterial _emphasizedPanelDrawMaterial;
        private FuDrawMaterial _plateDrawMaterial;
        private FuDrawMaterial _buttonDrawMaterial;
        private FuDrawMaterial _hoveredButtonDrawMaterial;
        private FuDrawMaterial _pressedButtonDrawMaterial;
        private FuDrawMaterial _disabledButtonDrawMaterial;
#endif

        private bool _loadAttempted;

        #endregion

        #region Drawing and lifetime

        /// <summary>
        /// Draws the animated full-screen atmosphere when custom materials are available.
        /// </summary>
        /// <param name="drawList">Foreground DrawList receiving the surface.</param>
        /// <param name="screenSize">Current Fugui container size.</param>
        /// <param name="accent">Ember tint passed through the Fugui vertex color.</param>
        /// <returns>True when the custom shader handled the draw.</returns>
        public bool TryDrawBackdrop(
            FuDrawList drawList,
            Vector2 screenSize,
            Color accent)
        {
            // The fallback remains available when a target platform disables custom materials.
#if FU_CUSTOM_MATERIALS_ENABLED
            if (!EnsureMaterials())
                return false;

            DrawMaterialSurface(
                drawList,
                new Rect(Vector2.zero, screenSize),
                accent,
                0f,
                _backdropDrawMaterial);
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Draws a shader-backed panel surface.
        /// </summary>
        /// <param name="drawList">Foreground DrawList receiving the surface.</param>
        /// <param name="rect">Panel bounds in screen coordinates.</param>
        /// <param name="accent">Metallic highlight tint.</param>
        /// <param name="rounding">Corner rounding in screen pixels.</param>
        /// <param name="emphasized">Whether the panel is a primary or modal surface.</param>
        /// <returns>True when the custom shader handled the draw.</returns>
        public bool TryDrawPanel(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            float rounding,
            bool emphasized)
        {
            // Primary panels receive stronger smoke and edge lighting than passive HUD plates.
#if FU_CUSTOM_MATERIALS_ENABLED
            if (!EnsureMaterials())
                return false;

            DrawMaterialSurface(
                drawList,
                rect,
                accent,
                rounding,
                emphasized
                    ? _emphasizedPanelDrawMaterial
                    : _panelDrawMaterial);
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Draws a recessed shader-backed information plate.
        /// </summary>
        /// <param name="drawList">Foreground DrawList receiving the surface.</param>
        /// <param name="rect">Plate bounds in screen coordinates.</param>
        /// <param name="accent">Plate edge tint.</param>
        /// <param name="rounding">Corner rounding in screen pixels.</param>
        /// <returns>True when the custom shader handled the draw.</returns>
        public bool TryDrawPlate(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            float rounding)
        {
            // Recessed plates separate secondary information without adding another opaque panel.
#if FU_CUSTOM_MATERIALS_ENABLED
            if (!EnsureMaterials())
                return false;

            DrawMaterialSurface(
                drawList,
                rect,
                accent,
                rounding,
                _plateDrawMaterial);
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Draws a shader-backed interactive button surface.
        /// </summary>
        /// <param name="drawList">Foreground DrawList receiving the surface.</param>
        /// <param name="rect">Button bounds in screen coordinates.</param>
        /// <param name="accent">Button accent tint.</param>
        /// <param name="rounding">Corner rounding in screen pixels.</param>
        /// <param name="enabled">Whether the button accepts input.</param>
        /// <param name="hovered">Whether the pointer or keyboard selection highlights it.</param>
        /// <param name="pressed">Whether the pointer currently presses it.</param>
        /// <returns>True when the custom shader handled the draw.</returns>
        public bool TryDrawButton(
            FuDrawList drawList,
            Rect rect,
            Color accent,
            float rounding,
            bool enabled,
            bool hovered,
            bool pressed)
        {
            // Fixed material variants keep every queued draw command immutable until Fugui renders it.
#if FU_CUSTOM_MATERIALS_ENABLED
            if (!EnsureMaterials())
                return false;

            FuDrawMaterial material = !enabled
                ? _disabledButtonDrawMaterial
                : pressed
                    ? _pressedButtonDrawMaterial
                    : hovered
                        ? _hoveredButtonDrawMaterial
                        : _buttonDrawMaterial;
            DrawMaterialSurface(
                drawList,
                rect,
                accent,
                rounding,
                material);
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Releases every caller-owned Unity material created by this skin.
        /// </summary>
        public void Dispose()
        {
            // FuDrawMaterial only stores immutable bindings and never owns the Unity materials.
#if FU_CUSTOM_MATERIALS_ENABLED
            DestroyRuntimeMaterial(_backdropMaterial);
            DestroyRuntimeMaterial(_panelMaterial);
            DestroyRuntimeMaterial(_emphasizedPanelMaterial);
            DestroyRuntimeMaterial(_plateMaterial);
            DestroyRuntimeMaterial(_buttonMaterial);
            DestroyRuntimeMaterial(_hoveredButtonMaterial);
            DestroyRuntimeMaterial(_pressedButtonMaterial);
            DestroyRuntimeMaterial(_disabledButtonMaterial);

            _backdropMaterial = null;
            _panelMaterial = null;
            _emphasizedPanelMaterial = null;
            _plateMaterial = null;
            _buttonMaterial = null;
            _hoveredButtonMaterial = null;
            _pressedButtonMaterial = null;
            _disabledButtonMaterial = null;

            _backdropDrawMaterial = null;
            _panelDrawMaterial = null;
            _emphasizedPanelDrawMaterial = null;
            _plateDrawMaterial = null;
            _buttonDrawMaterial = null;
            _hoveredButtonDrawMaterial = null;
            _pressedButtonDrawMaterial = null;
            _disabledButtonDrawMaterial = null;
#endif
            _loadAttempted = false;
        }

        #endregion

#if FU_CUSTOM_MATERIALS_ENABLED
        #region Material creation

        /// <summary>
        /// Lazily creates all immutable custom-material variants.
        /// </summary>
        /// <returns>True when the dark-fantasy shader and every material are ready.</returns>
        private bool EnsureMaterials()
        {
            // Lazy loading delays resource creation until an active Fugui context actually renders UI.
            if (_panelDrawMaterial != null)
                return true;
            if (_loadAttempted)
                return false;

            _loadAttempted = true;
            Shader shader = Resources.Load<Shader>(SurfaceShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError(
                    "The Dodge From Light dark-fantasy Fugui shader is missing.");
                return false;
            }

            _backdropMaterial = CreateMaterial(
                shader,
                "DFL UI - Backdrop",
                3f,
                0.24f,
                new Color(0.025f, 0.009f, 0.008f, 1f),
                new Color(0.004f, 0.003f, 0.004f, 1f),
                0f);
            _panelMaterial = CreateMaterial(
                shader,
                "DFL UI - Iron Panel",
                0f,
                0.96f,
                new Color(0.075f, 0.067f, 0.06f, 1f),
                new Color(0.012f, 0.011f, 0.012f, 1f),
                0f);
            _emphasizedPanelMaterial = CreateMaterial(
                shader,
                "DFL UI - Ritual Panel",
                0f,
                0.985f,
                new Color(0.105f, 0.077f, 0.058f, 1f),
                new Color(0.016f, 0.011f, 0.012f, 1f),
                0.42f);
            _plateMaterial = CreateMaterial(
                shader,
                "DFL UI - Recessed Plate",
                1f,
                0.92f,
                new Color(0.045f, 0.041f, 0.039f, 1f),
                new Color(0.009f, 0.009f, 0.011f, 1f),
                0f);
            _buttonMaterial = CreateMaterial(
                shader,
                "DFL UI - Button",
                2f,
                0.96f,
                new Color(0.085f, 0.069f, 0.055f, 1f),
                new Color(0.015f, 0.012f, 0.013f, 1f),
                0f);
            _hoveredButtonMaterial = CreateMaterial(
                shader,
                "DFL UI - Button Hover",
                2f,
                0.98f,
                new Color(0.12f, 0.087f, 0.059f, 1f),
                new Color(0.018f, 0.012f, 0.012f, 1f),
                0.62f);
            _pressedButtonMaterial = CreateMaterial(
                shader,
                "DFL UI - Button Pressed",
                2f,
                1f,
                new Color(0.15f, 0.094f, 0.058f, 1f),
                new Color(0.024f, 0.012f, 0.011f, 1f),
                1f);
            _disabledButtonMaterial = CreateMaterial(
                shader,
                "DFL UI - Button Disabled",
                2f,
                0.58f,
                new Color(0.045f, 0.043f, 0.042f, 1f),
                new Color(0.012f, 0.012f, 0.013f, 1f),
                -0.35f);

            _backdropDrawMaterial = new FuDrawMaterial(_backdropMaterial);
            _panelDrawMaterial = new FuDrawMaterial(_panelMaterial);
            _emphasizedPanelDrawMaterial =
                new FuDrawMaterial(_emphasizedPanelMaterial);
            _plateDrawMaterial = new FuDrawMaterial(_plateMaterial);
            _buttonDrawMaterial = new FuDrawMaterial(_buttonMaterial);
            _hoveredButtonDrawMaterial =
                new FuDrawMaterial(_hoveredButtonMaterial);
            _pressedButtonDrawMaterial =
                new FuDrawMaterial(_pressedButtonMaterial);
            _disabledButtonDrawMaterial =
                new FuDrawMaterial(_disabledButtonMaterial);
            return true;
        }

        /// <summary>
        /// Creates one hidden runtime material with immutable visual parameters.
        /// </summary>
        /// <param name="shader">Dark-fantasy surface shader.</param>
        /// <param name="name">Diagnostic material name.</param>
        /// <param name="surfaceMode">Shader surface family.</param>
        /// <param name="opacity">Surface opacity multiplier.</param>
        /// <param name="baseColor">Primary iron or smoke color.</param>
        /// <param name="deepColor">Recessed shadow color.</param>
        /// <param name="interaction">Fixed interaction intensity.</param>
        /// <returns>The newly created caller-owned Unity material.</returns>
        private static Material CreateMaterial(
            Shader shader,
            string name,
            float surfaceMode,
            float opacity,
            Color baseColor,
            Color deepColor,
            float interaction)
        {
            // Separate variants prevent later property changes from affecting queued DrawList commands.
            var material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
            };
            material.SetFloat(SurfaceModeProperty, surfaceMode);
            material.SetFloat(OpacityProperty, opacity);
            material.SetColor(BaseColorProperty, baseColor);
            material.SetColor(DeepColorProperty, deepColor);
            material.SetFloat(InteractionProperty, interaction);
            return material;
        }

        /// <summary>
        /// Emits one rounded white-texture quad inside a balanced custom material scope.
        /// </summary>
        /// <param name="drawList">Foreground DrawList receiving the quad.</param>
        /// <param name="rect">Quad bounds.</param>
        /// <param name="tint">Vertex tint interpreted as the surface accent.</param>
        /// <param name="rounding">Rounded corner radius.</param>
        /// <param name="material">Immutable Fugui material binding.</param>
        private static void DrawMaterialSurface(
            FuDrawList drawList,
            Rect rect,
            Color tint,
            float rounding,
            FuDrawMaterial material)
        {
            // The finally block guarantees Fugui's material and texture stacks remain balanced.
            drawList.PushMaterial(material, Texture2D.whiteTexture);
            try
            {
                drawList.AddImageRounded(
                    Texture2D.whiteTexture,
                    rect.min,
                    rect.max,
                    Vector2.zero,
                    Vector2.one,
                    tint,
                    rounding,
                    FuDrawFlags.RoundCornersAll);
            }
            finally
            {
                drawList.PopMaterial();
            }
        }

        /// <summary>
        /// Destroys one runtime material with the mode required by the current Unity state.
        /// </summary>
        /// <param name="material">Caller-owned material to release.</param>
        private static void DestroyRuntimeMaterial(Material material)
        {
            // Edit Mode requires immediate destruction while Play Mode uses Unity's deferred path.
            if (material == null)
                return;

            if (UnityEngine.Application.isPlaying)
                UnityEngine.Object.Destroy(material);
            else
                UnityEngine.Object.DestroyImmediate(material);
        }

        #endregion
#endif
    }
}

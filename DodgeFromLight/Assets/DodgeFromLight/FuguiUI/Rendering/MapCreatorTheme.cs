using System.Collections.Generic;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Owns the creator's illustrated tools, restrained forged surfaces, and atlas-checked display titles.
    /// </summary>
    internal static class MapCreatorTheme
    {
        private static readonly Dictionary<string, Texture2D> Tools = new Dictionary<string, Texture2D>();
        private static int _owners;
#if FU_CUSTOM_MATERIALS_ENABLED
        private static Material[] _materials;
        private static FuDrawMaterial[] _bindings;
#endif

        /// <summary>
        /// Acquires immutable material variants and the six independently illustrated tool textures.
        /// </summary>
        public static void Acquire()
        {
            if (_owners++ > 0) return;
            foreach (string tool in new[] { "Select", "Paint", "Rectangle", "Erase", "Move", "Pick", "Structure", "Decorations", "Enemies", "Gears", "WorkLight", "GameLight" })
                Tools[tool] = Resources.Load<Texture2D>("MapCreator/Tools/" + tool);
#if FU_CUSTOM_MATERIALS_ENABLED
            Shader shader = Resources.Load<Shader>("Shaders/MapCreatorSurface");
            if (shader == null) return;
            _materials = new Material[5];
            _bindings = new FuDrawMaterial[5];
            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i] = new Material(shader) { name = "Creator surface " + i, hideFlags = HideFlags.HideAndDontSave };
                _materials[i].SetFloat("_State", i);
                _bindings[i] = new FuDrawMaterial(_materials[i]);
            }
#endif
        }

        /// <summary>
        /// Releases owned material instances without destroying project texture assets.
        /// </summary>
        public static void Release()
        {
            _owners = Mathf.Max(0, _owners - 1);
            if (_owners != 0) return;
#if FU_CUSTOM_MATERIALS_ENABLED
            if (_materials != null)
                foreach (Material material in _materials)
                    if (material != null) Object.Destroy(material);
            _materials = null;
            _bindings = null;
#endif
            Tools.Clear();
        }

        /// <summary>
        /// Resolves real tool art without substituting arbitrary geometric pictograms for missing images.
        /// </summary>
        public static Texture2D GetTool(string id)
        {
            return Tools.TryGetValue(id, out Texture2D texture) ? texture : null;
        }

        /// <summary>
        /// Draws an opaque worn-metal surface with immutable panel, idle, active, hover, or disabled shading.
        /// </summary>
        public static void Surface(FuDrawList drawList, Rect rect, int state = 0)
        {
            float scale = DrawListUi.GetScale();
#if FU_CUSTOM_MATERIALS_ENABLED
            if (_bindings != null)
            {
                drawList.PushMaterial(_bindings[Mathf.Clamp(state, 0, 4)], Texture2D.whiteTexture);
                try
                {
                    drawList.AddImageRounded(Texture2D.whiteTexture, rect.min, rect.max, Vector2.zero, Vector2.one, Color.white, 4f * scale, FuDrawFlags.RoundCornersAll);
                }
                finally { drawList.PopMaterial(); }
            }
            else
#endif
            {
                uint top = Fugui.GetColorU32(new Color(0.11f, 0.095f, 0.08f));
                uint bottom = Fugui.GetColorU32(new Color(0.035f, 0.03f, 0.03f));
                drawList.AddRectFilledMultiColor(rect.min, rect.max, top, top, bottom, bottom);
            }
            Color trim = state == 2 ? DrawListUi.AccentBright : new Color(0.43f, 0.36f, 0.27f, state == 4 ? 0.22f : 0.60f);
            drawList.AddRect(rect.min, rect.max, trim, 4f * scale, FuDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));
            drawList.AddLine(rect.min + Vector2.one * 2f * scale, new Vector2(rect.xMax - 2f * scale, rect.y + 2f * scale), new Color(0.94f, 0.84f, 0.63f, 0.12f), scale);
        }

        /// <summary>
        /// Draws the existing fantasy display face only after its exact configured atlas entry is found.
        /// </summary>
        public static void Heading(FuDrawList drawList, Vector2 position, string text)
        {
            FontConfig config = Fugui.Settings?.FontConfig;
            bool available = false;
            if (Fugui.CurrentContext != null && config != null && config.Fonts != null)
                foreach (FontSizeConfig font in config.Fonts)
                    if (font != null && font.Name == "CreatorTitle" && font.Size == 18 && font.SubFonts_Regular != null && font.SubFonts_Regular.Length > 0)
                        available = true;
            if (!available)
            {
                DrawListUi.DrawText(drawList, position, text, DrawListUi.TextPrimary, 18);
                return;
            }
            // Body text keeps the user's regular family; the serif display face is reserved for section titles.
            Fugui.PushFont("CreatorTitle", 18, FontType.Regular);
            try { drawList.AddText(position, Fugui.GetColorU32(DrawListUi.TextPrimary), text); }
            finally { Fugui.PopFont(); }
        }
    }
}

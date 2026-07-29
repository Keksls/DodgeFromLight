using DodgeFromLight.FuguiUI.Rendering;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Screens
{
    /// <summary>
    /// Renders the main navigation screen on Fugui's foreground DrawList.
    /// </summary>
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        private const int MenuItemCount = 4;

        private MainMenuViewModel _viewModel;
        private FuContext _context;
        private int _selection;

        #region Lifecycle

        /// <summary>
        /// Binds the presenter and subscribes it to the Main Fugui context render event.
        /// </summary>
        public bool Initialize(MainMenuViewModel viewModel)
        {
            // The scene bootstrap owns the single valid runtime initialization path.
            if (viewModel == null || !Fugui.IsInitialized || Fugui.DefaultContext == null)
                return false;

            _viewModel = viewModel;
            _context = Fugui.DefaultContext;
            _context.OnRender -= Render;
            _context.OnRender += Render;
            return true;
        }

        /// <summary>
        /// Handles keyboard navigation independently from DrawList rendering.
        /// </summary>
        private void Update()
        {
            // Mouse hit-testing occurs during render while keyboard selection updates once per frame.
            if (_viewModel == null || _viewModel.IsTransitioning())
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.Z))
            {
                MoveSelection(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) ||
                     Input.GetKeyDown(KeyCode.S))
            {
                MoveSelection(1);
            }

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                ActivateSelection();
            }
        }

        /// <summary>
        /// Releases the Main context render subscription when the menu scene is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // The stored context remains safe to unsubscribe during a scene transition.
            if (_context != null)
                _context.OnRender -= Render;
            _context = null;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draws the complete menu navigation surface on the foreground DrawList.
        /// </summary>
        private void Render()
        {
            // The 3D menu environment remains visible behind a focused translucent navigation rail.
            if (_viewModel == null || Fugui.CurrentContext != _context)
                return;

            FuDrawList drawList = Fugui.GetForegroundDrawList();
            Vector2 screenSize = DrawListUi.GetScreenSize();
            float scale = DrawListUi.GetScale();
            DrawBackdrop(drawList, screenSize);
            DrawHeader(drawList, screenSize, scale);
            DrawNavigation(drawList, screenSize, scale);
            DrawFooter(drawList, screenSize, scale);

            if (_viewModel.IsTransitioning())
                DrawTransitionState(drawList, screenSize, scale);
        }

        /// <summary>
        /// Draws the left-to-right readability gradient over the 3D menu environment.
        /// </summary>
        private static void DrawBackdrop(FuDrawList drawList, Vector2 screenSize)
        {
            // A DrawList gradient preserves the animated environment without sacrificing contrast.
            float width = screenSize.x * 0.66f;
            uint strong = Fugui.GetColorU32(new Color(0.012f, 0.018f, 0.024f, 0.96f));
            uint clear = Fugui.GetColorU32(new Color(0.012f, 0.018f, 0.024f, 0f));
            drawList.AddRectFilledMultiColor(
                Vector2.zero,
                new Vector2(width, screenSize.y),
                strong,
                clear,
                clear,
                strong);
        }

        /// <summary>
        /// Draws the game title and positioning statement.
        /// </summary>
        private static void DrawHeader(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Text hierarchy is built with configured Fugui fonts rather than TextMeshPro objects.
            Vector2 origin = new Vector2(
                Mathf.Max(54f * scale, screenSize.x * 0.055f),
                Mathf.Max(54f * scale, screenSize.y * 0.075f));
            DrawListUi.DrawText(
                drawList,
                origin,
                "DODGE FROM LIGHT",
                DrawListUi.TextPrimary,
                20,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                origin + new Vector2(0f, 38f) * scale,
                "Puzzle tactique · optimise chaque mouvement",
                DrawListUi.TextSecondary,
                14);
            drawList.AddRectFilledMultiColor(
                origin + new Vector2(0f, 72f) * scale,
                origin + new Vector2(420f, 75f) * scale,
                Fugui.GetColorU32(DrawListUi.Accent),
                Fugui.GetColorU32(new Color(
                    DrawListUi.Accent.r,
                    DrawListUi.Accent.g,
                    DrawListUi.Accent.b,
                    0f)),
                0,
                Fugui.GetColorU32(DrawListUi.Accent));
        }

        /// <summary>
        /// Draws all main-menu actions and progression feedback.
        /// </summary>
        private void DrawNavigation(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Stable vertical placement makes the menu readable on ultrawide and standard displays.
            float x = Mathf.Max(54f * scale, screenSize.x * 0.055f);
            float width = Mathf.Min(460f * scale, screenSize.x * 0.42f);
            float height = 60f * scale;
            float gap = 14f * scale;
            float totalHeight = MenuItemCount * height + (MenuItemCount - 1) * gap;
            float y = Mathf.Max(190f * scale, (screenSize.y - totalHeight) * 0.52f);
            bool transitioning = _viewModel.IsTransitioning();

            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(x, y, width, height),
                    _viewModel.GetPlayLabel(),
                    "Entrée",
                    DrawListUi.Success,
                    !transitioning,
                    _selection == 0))
            {
                _viewModel.TryPlay();
            }

            y += height + gap;
            bool editorAvailable = _viewModel.CanOpenMapEditor();
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(x, y, width, height),
                    "Créateur de cartes",
                    editorAvailable ? string.Empty : "Tutoriel requis",
                    new Color(0.38f, 0.76f, 1f, 1f),
                    editorAvailable,
                    _selection == 1))
            {
                _viewModel.TryOpenMapEditor();
            }

            y += height + gap;
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(x, y, width, height),
                    "Campagne",
                    string.Empty,
                    DrawListUi.Accent,
                    !transitioning,
                    _selection == 2))
            {
                _viewModel.TryOpenCampaign();
            }

            y += height + gap;
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(x, y, width, height),
                    "Quitter",
                    string.Empty,
                    DrawListUi.Danger,
                    !transitioning,
                    _selection == 3))
            {
                _viewModel.TryExit();
            }

            if (!editorAvailable && !_viewModel.HasFinishedTutorial())
            {
                DrawListUi.DrawText(
                    drawList,
                    new Vector2(x, y + height + 24f * scale),
                    "Termine le tutoriel pour débloquer le créateur de cartes.",
                    DrawListUi.TextSecondary,
                    12);
            }
        }

        /// <summary>
        /// Draws version and input guidance at the bottom of the screen.
        /// </summary>
        private void DrawFooter(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Footer text remains deliberately quiet beneath the primary navigation.
            Vector2 position = new Vector2(
                Mathf.Max(54f * scale, screenSize.x * 0.055f),
                screenSize.y - 52f * scale);
            DrawListUi.DrawText(
                drawList,
                position,
                _viewModel.VersionLabel,
                DrawListUi.TextSecondary,
                12);
            DrawListUi.DrawText(
                drawList,
                position + new Vector2(120f, 0f) * scale,
                "↑ ↓ naviguer · Entrée valider",
                new Color(
                    DrawListUi.TextSecondary.r,
                    DrawListUi.TextSecondary.g,
                    DrawListUi.TextSecondary.b,
                    0.7f),
                12);
        }

        /// <summary>
        /// Draws a compact modal indicator while an authored scene transition plays.
        /// </summary>
        private static void DrawTransitionState(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // The overlay blocks ambiguous repeated navigation while keeping the environment visible.
            DrawListUi.DrawScrim(drawList, screenSize, 0.36f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(360f, 92f) * scale);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent, true);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 35f * scale),
                panel.width,
                "CHARGEMENT…",
                DrawListUi.TextPrimary,
                16,
                FontType.Bold);
        }

        #endregion

        #region Keyboard navigation

        /// <summary>
        /// Moves the keyboard selection to the next enabled menu entry.
        /// </summary>
        private void MoveSelection(int direction)
        {
            // Disabled editor navigation is skipped while tutorial progression is incomplete.
            for (int attempt = 0; attempt < MenuItemCount; attempt++)
            {
                _selection =
                    (_selection + direction + MenuItemCount) % MenuItemCount;
                if (_selection != 1 || _viewModel.CanOpenMapEditor())
                    return;
            }
        }

        /// <summary>
        /// Activates the currently selected menu intention.
        /// </summary>
        private void ActivateSelection()
        {
            // Keyboard and mouse paths call the same guarded view-model operations.
            switch (_selection)
            {
                case 0:
                    _viewModel.TryPlay();
                    break;
                case 1:
                    _viewModel.TryOpenMapEditor();
                    break;
                case 2:
                    _viewModel.TryOpenCampaign();
                    break;
                case 3:
                    _viewModel.TryExit();
                    break;
            }
        }

        #endregion
    }
}

using DodgeFromLight.FuguiUI.Rendering;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Screens
{
    /// <summary>
    /// Renders the dark-fantasy main navigation screen on Fugui's foreground DrawList.
    /// </summary>
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        private const int MenuItemCount = 4;

        private MainMenuViewModel _viewModel;
        private FuContext _context;
        private DarkFantasyUiAssets _assets;
        private int _selection;
        private bool _ownsSkin;

        #region Lifecycle

        /// <summary>
        /// Binds the presenter and subscribes it to the Main Fugui context render event.
        /// </summary>
        public bool Initialize(MainMenuViewModel viewModel)
        {
            // The scene bootstrap owns the single valid runtime initialization path.
            if (viewModel == null ||
                !Fugui.IsInitialized ||
                Fugui.DefaultContext == null)
            {
                return false;
            }

            _viewModel = viewModel;
            _context = Fugui.DefaultContext;
            _assets = FindFirstObjectByType<DarkFantasyUiAssets>();
            if (!_ownsSkin)
            {
                DrawListUi.AcquireSkin();
                _ownsSkin = true;
            }

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
        /// Releases render and dark-fantasy material resources when the menu scene is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // The stored context remains safe to unsubscribe during a scene transition.
            if (_context != null)
                _context.OnRender -= Render;
            _context = null;

            if (!_ownsSkin)
                return;

            DrawListUi.ReleaseSkin();
            _ownsSkin = false;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draws the complete menu navigation surface on the foreground DrawList.
        /// </summary>
        private void Render()
        {
            // The 3D menu environment remains visible beyond a focused forged navigation rail.
            if (_viewModel == null || Fugui.CurrentContext != _context)
                return;

            FuDrawList drawList = Fugui.GetForegroundDrawList();
            Vector2 screenSize = DrawListUi.GetScreenSize();
            float scale = DrawListUi.GetScale();
            DrawListUi.DrawAtmosphere(drawList, screenSize);

            Rect rail = GetNavigationRail(screenSize, scale);
            DrawListUi.DrawPanel(
                drawList,
                rail,
                DrawListUi.Accent,
                true);
            DrawHeader(drawList, rail, scale);
            DrawNavigation(drawList, rail, scale);
            DrawFooter(drawList, rail, scale);

            if (_viewModel.IsTransitioning())
                DrawTransitionState(drawList, screenSize, scale);
        }

        /// <summary>
        /// Computes the responsive forged navigation rail.
        /// </summary>
        /// <param name="screenSize">Current Fugui container size.</param>
        /// <param name="scale">Current Fugui layout scale.</param>
        /// <returns>Navigation rail bounds in screen coordinates.</returns>
        private static Rect GetNavigationRail(
            Vector2 screenSize,
            float scale)
        {
            // Width is capped so ultrawide layouts preserve a large view of the 3D scene.
            float margin = Mathf.Max(22f * scale, screenSize.y * 0.028f);
            float width = Mathf.Min(
                570f * scale,
                screenSize.x * 0.46f);
            return new Rect(
                margin,
                margin,
                width,
                screenSize.y - margin * 2f);
        }

        /// <summary>
        /// Draws the authored logo, positioning statement, and ornamental title rule.
        /// </summary>
        private void DrawHeader(
            FuDrawList drawList,
            Rect rail,
            float scale)
        {
            // The original title artwork remains the strongest piece of established visual identity.
            Vector2 origin =
                rail.position + new Vector2(38f, 42f) * scale;
            DrawListUi.DrawText(
                drawList,
                origin,
                "L'ÉPREUVE VOUS ATTEND",
                new Color(
                    DrawListUi.Accent.r,
                    DrawListUi.Accent.g,
                    DrawListUi.Accent.b,
                    0.82f),
                11,
                FontType.Bold);

            float logoWidth = rail.width - 76f * scale;
            float logoHeight = logoWidth * 243f / 2048f;
            Rect logoRect = new Rect(
                origin.x,
                origin.y + 27f * scale,
                logoWidth,
                logoHeight);
            if (_assets != null && _assets.TitleTexture != null)
            {
                Rect shadowRect = new Rect(
                    logoRect.x + 4f * scale,
                    logoRect.y + 5f * scale,
                    logoRect.width,
                    logoRect.height);
                drawList.AddImage(
                    _assets.TitleTexture,
                    shadowRect.min,
                    shadowRect.max,
                    new Vector4(0f, 0f, 0f, 0.82f));
                drawList.AddImage(
                    _assets.TitleTexture,
                    logoRect.min,
                    logoRect.max,
                    Vector4.one);
            }
            else
            {
                DrawListUi.DrawText(
                    drawList,
                    logoRect.position,
                    "DODGE FROM LIGHT",
                    DrawListUi.AccentBright,
                    20,
                    FontType.Bold);
            }

            float dividerY = logoRect.yMax + 23f * scale;
            DrawListUi.DrawDivider(
                drawList,
                new Vector2(rail.center.x, dividerY),
                logoWidth,
                DrawListUi.Accent,
                0.7f);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(
                    rail.x + 28f * scale,
                    dividerY + 19f * scale),
                rail.width - 56f * scale,
                "PUZZLE TACTIQUE  •  MAÎTRISE  •  PRÉCISION",
                DrawListUi.TextPrimary,
                12,
                FontType.Bold);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(
                    rail.x + 28f * scale,
                    dividerY + 43f * scale),
                rail.width - 56f * scale,
                "Chaque mouvement peut être le dernier.",
                DrawListUi.TextSecondary,
                12);
        }

        /// <summary>
        /// Draws all main-menu actions and progression feedback.
        /// </summary>
        private void DrawNavigation(
            FuDrawList drawList,
            Rect rail,
            float scale)
        {
            // The first action is deliberately dominant while secondary routes remain visually coherent.
            float x = rail.x + 38f * scale;
            float width = rail.width - 76f * scale;
            float height = 58f * scale;
            float gap = 12f * scale;
            float y = rail.y + Mathf.Max(
                245f * scale,
                rail.height * 0.31f);
            bool transitioning = _viewModel.IsTransitioning();

            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(x, y - 31f * scale),
                width,
                "ENTRER DANS L'ÉPREUVE",
                DrawListUi.Accent);

            Rect playRect = new Rect(
                x,
                y,
                width,
                64f * scale);
            UpdateMouseSelection(playRect, 0, true);
            if (DrawListUi.DrawButton(
                    drawList,
                    playRect,
                    _viewModel.GetPlayLabel(),
                    "Entrée",
                    DrawListUi.AccentBright,
                    !transitioning,
                    _selection == 0))
            {
                _viewModel.TryPlay();
            }

            y += 64f * scale + gap;
            bool editorAvailable = _viewModel.CanOpenMapEditor();
            Rect editorRect = new Rect(x, y, width, height);
            UpdateMouseSelection(editorRect, 1, editorAvailable);
            if (DrawListUi.DrawButton(
                    drawList,
                    editorRect,
                    "Créateur de cartes",
                    editorAvailable ? string.Empty : "Verrouillé",
                    DrawListUi.Steel,
                    editorAvailable && !transitioning,
                    _selection == 1))
            {
                _viewModel.TryOpenMapEditor();
            }

            y += height + gap;
            Rect campaignRect = new Rect(x, y, width, height);
            UpdateMouseSelection(campaignRect, 2, true);
            if (DrawListUi.DrawButton(
                    drawList,
                    campaignRect,
                    "Campagne",
                    string.Empty,
                    DrawListUi.Accent,
                    !transitioning,
                    _selection == 2))
            {
                _viewModel.TryOpenCampaign();
            }

            y += height + gap;
            Rect quitRect = new Rect(x, y, width, height);
            UpdateMouseSelection(quitRect, 3, true);
            if (DrawListUi.DrawButton(
                    drawList,
                    quitRect,
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
                Rect lockPlate = new Rect(
                    x,
                    y + height + 18f * scale,
                    width,
                    44f * scale);
                DrawListUi.DrawPlate(
                    drawList,
                    lockPlate,
                    DrawListUi.Steel);
                DrawListUi.DrawCenteredText(
                    drawList,
                    new Vector2(
                        lockPlate.x,
                        lockPlate.y + 14f * scale),
                    lockPlate.width,
                    "Termine le tutoriel pour ouvrir le créateur.",
                    DrawListUi.TextSecondary,
                    11);
            }
        }

        /// <summary>
        /// Draws version and input guidance at the bottom of the forged rail.
        /// </summary>
        private void DrawFooter(
            FuDrawList drawList,
            Rect rail,
            float scale)
        {
            // Footer information sits below a quiet divider and never competes with navigation.
            float dividerY = rail.yMax - 72f * scale;
            DrawListUi.DrawDivider(
                drawList,
                new Vector2(rail.center.x, dividerY),
                rail.width - 76f * scale,
                DrawListUi.Accent,
                0.34f);

            Vector2 position = new Vector2(
                rail.x + 38f * scale,
                dividerY + 24f * scale);
            DrawListUi.DrawText(
                drawList,
                position,
                _viewModel.VersionLabel,
                DrawListUi.TextSecondary,
                10,
                FontType.Bold);
            string controls = "↑  ↓  NAVIGUER    •    ENTRÉE  VALIDER";
            Vector2 controlsSize =
                DrawListUi.MeasureText(controls, 10, FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    rail.xMax - 38f * scale - controlsSize.x,
                    position.y),
                controls,
                new Color(
                    DrawListUi.TextSecondary.r,
                    DrawListUi.TextSecondary.g,
                    DrawListUi.TextSecondary.b,
                    0.72f),
                10,
                FontType.Bold);
        }

        /// <summary>
        /// Draws an ornate modal indicator while an authored scene transition plays.
        /// </summary>
        private static void DrawTransitionState(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // The ritual emblem communicates progress while repeated navigation is blocked.
            DrawListUi.DrawScrim(drawList, screenSize, 0.64f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(420f, 174f) * scale);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);

            float phase = Mathf.Repeat(
                Time.unscaledTime * 0.11f,
                1f);
            DrawListUi.DrawRuneEmblem(
                drawList,
                new Vector2(
                    panel.x + 67f * scale,
                    panel.center.y),
                27f * scale,
                DrawListUi.Accent,
                phase);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(122f, 55f) * scale,
                "CHARGEMENT",
                DrawListUi.TextPrimary,
                16,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(122f, 87f) * scale,
                "Les ombres se rassemblent…",
                DrawListUi.TextSecondary,
                12);
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Moves keyboard focus to a menu entry hovered by the pointer.
        /// </summary>
        /// <param name="rect">Candidate menu entry bounds.</param>
        /// <param name="index">Candidate selection index.</param>
        /// <param name="enabled">Whether the candidate can receive focus.</param>
        private void UpdateMouseSelection(
            Rect rect,
            int index,
            bool enabled)
        {
            // Pointer and keyboard focus share one highlight to avoid displaying two active choices.
            if (enabled &&
                rect.Contains(Fugui.DefaultContainer.Mouse.Position))
            {
                _selection = index;
            }
        }

        /// <summary>
        /// Moves the keyboard selection to the next enabled menu entry.
        /// </summary>
        private void MoveSelection(int direction)
        {
            // Disabled editor navigation is skipped while tutorial progression is incomplete.
            for (int attempt = 0; attempt < MenuItemCount; attempt++)
            {
                _selection =
                    (_selection + direction + MenuItemCount) %
                    MenuItemCount;
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

using System.Collections.Generic;
using DodgeFromLight.FuguiUI.Rendering;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Screens
{
    /// <summary>
    /// Renders the complete player-facing map creator on Fugui's foreground DrawList.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MapCreatorPresenter : MonoBehaviour
    {
        private const int MaximumMapNameLength = 48;
        private const int MaximumSearchLength = 40;

        private MapCreatorViewModel _viewModel;
        private FuContext _context;
        private MapCreatorPaletteGroup _activeGroup =
            MapCreatorPaletteGroup.Structure;
        private string _search = string.Empty;
        private string _nameDraft = string.Empty;
        private string _hoverDescription = string.Empty;
        private string _activeEnvironmentColorId = string.Empty;
        private string _activeEnvironmentColorLabel = string.Empty;
        private float _paletteScroll;
        private float _environmentScroll;
        private Color _environmentColorDraft = Color.white;
        private bool _searchFocused;
        private bool _editingName;
        private bool _showExitPrompt;
        private bool _showValidation;
        private bool _showEnvironment;
        private bool _showAdvancedEnvironment;
        private bool _ownsSkin;
        private bool _keyboardCaptured;

        #region Lifecycle

        /// <summary>
        /// Binds the presenter and subscribes it to the Main Fugui context render event.
        /// </summary>
        public bool Initialize(MapCreatorViewModel viewModel)
        {
            // The scene bootstrap owns the only supported runtime initialization path.
            if (viewModel == null ||
                !Fugui.IsInitialized ||
                Fugui.DefaultContext == null)
            {
                return false;
            }

            _viewModel = viewModel;
            _context = Fugui.DefaultContext;
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
        /// Collects keyboard input for DrawList text fields and modal workflows.
        /// </summary>
        private void Update()
        {
            // Board shortcuts remain controller-owned whenever the UI is not capturing text or a modal.
            if (_viewModel == null)
                return;

            UpdatePointerGuardForFrame();
            bool capture =
                _searchFocused ||
                _editingName ||
                _showExitPrompt ||
                _showValidation ||
                _showEnvironment;
            SetKeyboardCapture(capture);

            if (_editingName)
            {
                HandleNameInput();
                return;
            }

            if (_searchFocused)
            {
                HandleSearchInput();
                return;
            }

            if (_showExitPrompt ||
                _showValidation ||
                _showEnvironment)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseTopModal();
            }
        }

        /// <summary>
        /// Releases Fugui subscriptions, input capture, and shared material resources.
        /// </summary>
        private void OnDestroy()
        {
            // Explicit cleanup prevents stale editor input guards after a scene transition.
            if (_context != null)
                _context.OnRender -= Render;
            _context = null;

            if (_viewModel != null)
            {
                _viewModel.Actions.SetPointerOverUi(false);
                _viewModel.Actions.SetKeyboardCaptured(false);
            }

            if (!_ownsSkin)
                return;

            DrawListUi.ReleaseSkin();
            _ownsSkin = false;
        }

        #endregion

        #region Frame rendering

        /// <summary>
        /// Draws one complete immutable map-creator frame on the foreground DrawList.
        /// </summary>
        private void Render()
        {
            // The board remains unobstructed in the center while authoring surfaces frame it.
            if (_viewModel == null || Fugui.CurrentContext != _context)
                return;

            MapCreatorViewState state = _viewModel.GetState();
            if (state == null)
                return;

            FuDrawList drawList = Fugui.GetForegroundDrawList();
            Vector2 screenSize = DrawListUi.GetScreenSize();
            float scale = DrawListUi.GetScale();
            Rect top = GetTopBar(screenSize, scale);
            Rect left = GetPalettePanel(screenSize, scale);
            Rect right = GetInspectorPanel(screenSize, scale);
            Rect bottom = GetStatusBar(screenSize, scale);

            _hoverDescription = string.Empty;
            DrawEditorVignette(drawList, screenSize);
            MapCreatorDrawListUi.SetInteractionsEnabled(
                !HasActiveModal() && !state.IsBusy);
            DrawTopBar(drawList, top, state, scale);
            DrawPalette(drawList, left, state, scale);
            DrawInspector(drawList, right, state, scale);
            DrawStatusBar(drawList, bottom, state, scale);
            MapCreatorDrawListUi.SetInteractionsEnabled(true);
            DrawActiveModal(drawList, screenSize, state, scale);
            UpdatePointerGuard(top, left, right, bottom, screenSize);

            if (state.IsBusy)
                DrawBusyState(drawList, screenSize, state, scale);
        }

        /// <summary>
        /// Draws a restrained vignette that improves UI contrast without hiding the authored map.
        /// </summary>
        private static void DrawEditorVignette(
            FuDrawList drawList,
            Vector2 screenSize)
        {
            // Edge gradients visually anchor the workbench while preserving the central scene viewport.
            uint edge = Fugui.GetColorU32(
                new Color(0.01f, 0.008f, 0.009f, 0.68f));
            uint clear = Fugui.GetColorU32(
                new Color(0.01f, 0.008f, 0.009f, 0f));
            float sideWidth = Mathf.Min(520f, screenSize.x * 0.34f);
            drawList.AddRectFilledMultiColor(
                Vector2.zero,
                new Vector2(sideWidth, screenSize.y),
                edge,
                clear,
                clear,
                edge);
            drawList.AddRectFilledMultiColor(
                new Vector2(screenSize.x - sideWidth, 0f),
                screenSize,
                clear,
                edge,
                edge,
                clear);
        }

        /// <summary>
        /// Draws navigation, map identity, history, validation, save, and playtest commands.
        /// </summary>
        private void DrawTopBar(
            FuDrawList drawList,
            Rect rect,
            MapCreatorViewState state,
            float scale)
        {
            // High-frequency actions stay visible regardless of palette or inspector scroll state.
            DrawListUi.DrawPanel(drawList, rect, DrawListUi.Accent, true);
            float y = rect.y + 17f * scale;
            float height = 42f * scale;
            float gap = 9f * scale;
            float x = rect.x + 18f * scale;

            Rect back = new Rect(x, y, 54f * scale, height);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    back,
                    "←",
                    DrawListUi.Steel,
                    !state.IsBusy))
            {
                _showExitPrompt = state.IsDirty;
                if (!state.IsDirty)
                    _viewModel.Actions.DiscardAndClose();
            }

            x = back.xMax + 17f * scale;
            DrawListUi.DrawText(
                drawList,
                new Vector2(x, y - 2f * scale),
                "FORGE DE CARTES",
                DrawListUi.AccentBright,
                15,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(x, y + 23f * scale),
                state.Width + " × " + state.Height + " cases",
                DrawListUi.TextSecondary,
                10,
                FontType.Bold);

            x += 205f * scale;
            float mapNameWidth =
                Mathf.Clamp(rect.width * 0.22f, 250f * scale, 420f * scale);
            Rect mapNameRect =
                new Rect(x, y, mapNameWidth, height);
            DrawListUi.DrawPlate(
                drawList,
                mapNameRect,
                state.IsDirty ? DrawListUi.AccentBright : DrawListUi.Steel,
                state.IsDirty);
            DrawListUi.DrawText(
                drawList,
                mapNameRect.position + new Vector2(12f, 7f) * scale,
                state.MapName,
                DrawListUi.TextPrimary,
                12,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                mapNameRect.position + new Vector2(12f, 25f) * scale,
                state.IsDirty
                    ? "MODIFICATIONS NON ENREGISTRÉES"
                    : "CARTE ENREGISTRÉE",
                state.IsDirty
                    ? DrawListUi.AccentBright
                    : DrawListUi.TextSecondary,
                8,
                FontType.Bold);
            if (!HasActiveModal() &&
                !state.IsBusy &&
                mapNameRect.Contains(Fugui.DefaultContainer.Mouse.Position) &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left))
            {
                BeginNameEditing(state.MapName);
            }

            x = mapNameRect.xMax + gap;
            Rect environment =
                new Rect(x, y, 170f * scale, height);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    environment,
                    "Ambiance · " + state.EnvironmentLabel,
                    DrawListUi.Steel,
                    !state.IsBusy))
            {
                _showEnvironment = true;
            }

            float actionX = rect.xMax - 18f * scale;
            Rect play = PlaceToolbarButton(
                ref actionX,
                y,
                112f * scale,
                height,
                gap);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    play,
                    "TESTER  ▶",
                    state.ValidationErrors.Count == 0
                        ? DrawListUi.Success
                        : DrawListUi.Danger,
                    state.IsReady && !state.IsBusy))
            {
                _viewModel.Actions.Playtest();
            }

            Rect save = PlaceToolbarButton(
                ref actionX,
                y,
                102f * scale,
                height,
                gap);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    save,
                    "ENREGISTRER",
                    DrawListUi.AccentBright,
                    state.IsReady && !state.IsBusy))
            {
                _viewModel.Actions.Save();
            }

            Rect validate = PlaceToolbarButton(
                ref actionX,
                y,
                108f * scale,
                height,
                gap);
            string validationLabel = state.ValidationErrors.Count == 0
                ? "VALIDE  ✓"
                : state.ValidationErrors.Count + " ERREUR" +
                  (state.ValidationErrors.Count > 1 ? "S" : string.Empty);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    validate,
                    validationLabel,
                    state.ValidationErrors.Count == 0
                        ? DrawListUi.Success
                        : DrawListUi.Danger,
                    state.IsReady))
            {
                _showValidation = true;
            }

            Rect redo = PlaceToolbarButton(
                ref actionX,
                y,
                48f * scale,
                height,
                gap);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    redo,
                    "↷",
                    DrawListUi.Steel,
                    state.CanRedo && !state.IsBusy))
            {
                _viewModel.Actions.Redo();
            }

            Rect undo = PlaceToolbarButton(
                ref actionX,
                y,
                48f * scale,
                height,
                gap);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    undo,
                    "↶",
                    DrawListUi.Steel,
                    state.CanUndo && !state.IsBusy))
            {
                _viewModel.Actions.Undo();
            }
        }

        /// <summary>
        /// Draws palette tabs, search, filtered tool cards, and scrolling feedback.
        /// </summary>
        private void DrawPalette(
            FuDrawList drawList,
            Rect rect,
            MapCreatorViewState state,
            float scale)
        {
            // Structure, enemies, and decoration are separated by author intention rather than data type.
            DrawListUi.DrawPanel(drawList, rect, DrawListUi.Steel);
            Vector2 origin =
                rect.position + new Vector2(18f, 20f) * scale;
            DrawListUi.DrawText(
                drawList,
                origin,
                "PALETTE D’AUTEUR",
                DrawListUi.TextPrimary,
                14,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                origin + new Vector2(0f, 25f) * scale,
                "Choisis un outil, puis applique-le sur la grille.",
                DrawListUi.TextSecondary,
                9);

            float contentWidth = rect.width - 36f * scale;
            float tabY = origin.y + 54f * scale;
            float tabGap = 5f * scale;
            float tabWidth = (contentWidth - tabGap * 2f) / 3f;
            DrawPaletteTab(
                drawList,
                new Rect(origin.x, tabY, tabWidth, 38f * scale),
                "STRUCTURE",
                MapCreatorPaletteGroup.Structure);
            DrawPaletteTab(
                drawList,
                new Rect(
                    origin.x + tabWidth + tabGap,
                    tabY,
                    tabWidth,
                    38f * scale),
                "ENNEMIS",
                MapCreatorPaletteGroup.Enemies);
            DrawPaletteTab(
                drawList,
                new Rect(
                    origin.x + (tabWidth + tabGap) * 2f,
                    tabY,
                    tabWidth,
                    38f * scale),
                "DÉCORS",
                MapCreatorPaletteGroup.Decorations);

            Rect searchRect = new Rect(
                origin.x,
                tabY + 49f * scale,
                contentWidth,
                40f * scale);
            bool searchClicked =
                MapCreatorDrawListUi.DrawTextField(
                    drawList,
                    searchRect,
                    _search,
                    "Rechercher dans la palette…",
                    _searchFocused);
            if (searchClicked)
            {
                _searchFocused = true;
            }
            else if (_searchFocused &&
                     Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left) &&
                     !searchRect.Contains(
                         Fugui.DefaultContainer.Mouse.Position))
            {
                _searchFocused = false;
            }

            Rect viewport = new Rect(
                origin.x,
                searchRect.yMax + 13f * scale,
                contentWidth,
                rect.yMax - searchRect.yMax - 34f * scale);
            DrawPaletteItems(
                drawList,
                viewport,
                state,
                scale);
        }

        /// <summary>
        /// Draws the current selection inspector and context-sensitive authoring commands.
        /// </summary>
        private void DrawInspector(
            FuDrawList drawList,
            Rect rect,
            MapCreatorViewState state,
            float scale)
        {
            // Only commands valid for the selected content are exposed, reducing destructive mistakes.
            DrawListUi.DrawPanel(drawList, rect, DrawListUi.Accent);
            Vector2 origin =
                rect.position + new Vector2(18f, 20f) * scale;
            DrawListUi.DrawText(
                drawList,
                origin,
                "INSPECTEUR",
                DrawListUi.TextPrimary,
                14,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                origin + new Vector2(0f, 25f) * scale,
                state.Selection.HasSelection
                    ? "Case " + state.Selection.X + " : " + state.Selection.Y
                    : "Aucune case sélectionnée",
                state.Selection.HasSelection
                    ? DrawListUi.AccentBright
                    : DrawListUi.TextSecondary,
                10,
                FontType.Bold);

            Rect body = new Rect(
                origin.x,
                origin.y + 57f * scale,
                rect.width - 36f * scale,
                rect.height - 89f * scale);
            if (!state.Selection.HasSelection)
            {
                DrawEmptyInspector(drawList, body, state, scale);
                return;
            }

            DrawSelectionInspector(
                drawList,
                body,
                state,
                scale);
        }

        /// <summary>
        /// Draws live authoring guidance and active-tool status along the bottom edge.
        /// </summary>
        private void DrawStatusBar(
            FuDrawList drawList,
            Rect rect,
            MapCreatorViewState state,
            float scale)
        {
            // The status line answers what is active and what the next click will do.
            DrawListUi.DrawPanel(drawList, rect, DrawListUi.Steel);
            float y = rect.y + 15f * scale;
            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x + 18f * scale, y),
                "OUTIL · " + state.ActiveToolLabel.ToUpperInvariant(),
                DrawListUi.AccentBright,
                10,
                FontType.Bold);
            string guidance = !string.IsNullOrWhiteSpace(_hoverDescription)
                ? _hoverDescription
                : !string.IsNullOrWhiteSpace(state.StatusMessage)
                    ? state.StatusMessage
                    : "Clic gauche : appliquer/sélectionner  ·  Clic droit : pivoter  ·  Molette : zoom  ·  Espace : cadrer";
            DrawListUi.DrawText(
                drawList,
                new Vector2(rect.x + 220f * scale, y),
                guidance,
                DrawListUi.TextSecondary,
                9);

            string shortcuts =
                "S Sélection  ·  W Sol  ·  N Vide  ·  Ctrl+S Sauver  ·  Ctrl+Z/Y Historique";
            Vector2 shortcutSize =
                DrawListUi.MeasureText(shortcuts, 9, FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                new Vector2(
                    rect.xMax - 18f * scale - shortcutSize.x,
                    y),
                shortcuts,
                new Color(
                    DrawListUi.TextSecondary.r,
                    DrawListUi.TextSecondary.g,
                    DrawListUi.TextSecondary.b,
                    0.76f),
                9,
                FontType.Bold);
        }

        #endregion

        #region Palette

        /// <summary>
        /// Draws one palette tab and activates its content group when clicked.
        /// </summary>
        private void DrawPaletteTab(
            FuDrawList drawList,
            Rect rect,
            string label,
            MapCreatorPaletteGroup group)
        {
            // Changing author intent resets scroll but keeps the search for cross-category discovery.
            if (!MapCreatorDrawListUi.DrawTab(
                    drawList,
                    rect,
                    label,
                    _activeGroup == group))
            {
                return;
            }

            _activeGroup = group;
            _paletteScroll = 0f;
        }

        /// <summary>
        /// Draws filtered palette cards in a clipped, wheel-scrollable two-column grid.
        /// </summary>
        private void DrawPaletteItems(
            FuDrawList drawList,
            Rect viewport,
            MapCreatorViewState state,
            float scale)
        {
            // A two-column visual inventory balances discoverability with map viewport space.
            List<MapCreatorPaletteItem> items =
                GetFilteredItems(_activeGroup, _search);
            float gap = 9f * scale;
            float cardHeight = 72f * scale;
            float cardWidth = (viewport.width - gap) * 0.5f;
            int rows = Mathf.CeilToInt(items.Count * 0.5f);
            float contentHeight =
                rows * cardHeight + Mathf.Max(0, rows - 1) * gap;
            float maximumScroll =
                Mathf.Max(0f, contentHeight - viewport.height);

            if (viewport.Contains(Fugui.DefaultContainer.Mouse.Position))
            {
                _paletteScroll -=
                    Fugui.DefaultContainer.Mouse.Wheel.y *
                    54f *
                    scale;
            }
            _paletteScroll =
                Mathf.Clamp(_paletteScroll, 0f, maximumScroll);

            drawList.PushClipRect(viewport.min, viewport.max, true);
            for (int index = 0; index < items.Count; index++)
            {
                int row = index / 2;
                int column = index % 2;
                Rect card = new Rect(
                    viewport.x + column * (cardWidth + gap),
                    viewport.y + row * (cardHeight + gap) - _paletteScroll,
                    cardWidth,
                    cardHeight);
                if (card.yMax < viewport.y || card.y > viewport.yMax)
                    continue;

                MapCreatorPaletteItem item = items[index];
                bool hovered;
                bool selected =
                    IsPaletteItemSelected(item, state);
                if (MapCreatorDrawListUi.DrawPaletteCard(
                        drawList,
                        card,
                        item.Label,
                        item.Category,
                        item.Preview,
                        GetGroupAccent(item.Group),
                        selected,
                        out hovered))
                {
                    ActivatePaletteItem(item);
                }

                if (hovered)
                    _hoverDescription = item.Description;
            }
            drawList.PopClipRect();

            if (items.Count == 0)
            {
                DrawListUi.DrawCenteredText(
                    drawList,
                    new Vector2(viewport.x, viewport.y + 48f * scale),
                    viewport.width,
                    "Aucun résultat dans cette catégorie.",
                    DrawListUi.TextSecondary,
                    10);
            }

            DrawScrollIndicator(
                drawList,
                viewport,
                contentHeight,
                maximumScroll,
                scale);
        }

        /// <summary>
        /// Returns palette items matching one group and the current case-insensitive query.
        /// </summary>
        private List<MapCreatorPaletteItem> GetFilteredItems(
            MapCreatorPaletteGroup group,
            string query)
        {
            // Filtering includes labels, categories, and descriptions for fast asset discovery.
            var matches = new List<MapCreatorPaletteItem>();
            string normalized =
                string.IsNullOrWhiteSpace(query)
                    ? string.Empty
                    : query.Trim().ToLowerInvariant();
            foreach (MapCreatorPaletteItem item in _viewModel.GetItems())
            {
                if (item.Group != group)
                    continue;
                if (normalized.Length > 0 &&
                    !item.Label.ToLowerInvariant().Contains(normalized) &&
                    !item.Category.ToLowerInvariant().Contains(normalized) &&
                    !item.Description.ToLowerInvariant().Contains(normalized))
                {
                    continue;
                }

                matches.Add(item);
            }

            matches.Sort(
                (left, right) =>
                {
                    int order = left.SortOrder.CompareTo(right.SortOrder);
                    return order != 0
                        ? order
                        : string.CompareOrdinal(left.Label, right.Label);
                });
            return matches;
        }

        /// <summary>
        /// Reports whether a palette card represents the currently armed tool or decoration.
        /// </summary>
        private static bool IsPaletteItemSelected(
            MapCreatorPaletteItem item,
            MapCreatorViewState state)
        {
            // Decoration IDs replace the generic MapDeco tool ID once one asset is armed.
            return item != null &&
                   state != null &&
                   item.Id == state.ActiveToolId;
        }

        /// <summary>
        /// Routes one palette activation to the appropriate guarded authoring intention.
        /// </summary>
        private void ActivatePaletteItem(
            MapCreatorPaletteItem item)
        {
            // Domain-specific selection remains outside the presentation assembly.
            if (item.Group == MapCreatorPaletteGroup.Decorations)
                _viewModel.Actions.SelectDecoration(item.Id);
            else
                _viewModel.Actions.SelectTool(item.Id);
        }

        /// <summary>
        /// Gets the visual accent associated with one palette family.
        /// </summary>
        private static Color GetGroupAccent(
            MapCreatorPaletteGroup group)
        {
            // Family color remains restrained so authored preview art keeps priority.
            switch (group)
            {
                case MapCreatorPaletteGroup.Enemies:
                    return DrawListUi.Danger;
                case MapCreatorPaletteGroup.Decorations:
                    return DrawListUi.Success;
                case MapCreatorPaletteGroup.Gears:
                    return DrawListUi.AccentBright;
                case MapCreatorPaletteGroup.Environments:
                    return DrawListUi.Steel;
                default:
                    return DrawListUi.Accent;
            }
        }

        /// <summary>
        /// Draws a quiet proportional scroll indicator when palette content overflows.
        /// </summary>
        private void DrawScrollIndicator(
            FuDrawList drawList,
            Rect viewport,
            float contentHeight,
            float maximumScroll,
            float scale)
        {
            // The indicator communicates depth without stealing horizontal room from cards.
            if (maximumScroll <= 0f || contentHeight <= 0f)
                return;

            float railWidth = 3f * scale;
            Rect rail = new Rect(
                viewport.xMax - railWidth,
                viewport.y,
                railWidth,
                viewport.height);
            drawList.AddRectFilled(
                rail.min,
                rail.max,
                new Color(0f, 0f, 0f, 0.42f),
                railWidth);
            float handleHeight =
                Mathf.Max(
                    28f * scale,
                    viewport.height * viewport.height / contentHeight);
            float handleY =
                viewport.y +
                (_paletteScroll / maximumScroll) *
                (viewport.height - handleHeight);
            drawList.AddRectFilled(
                new Vector2(rail.x, handleY),
                new Vector2(rail.xMax, handleY + handleHeight),
                DrawListUi.Accent,
                railWidth);
        }

        #endregion

        #region Inspector

        /// <summary>
        /// Draws onboarding guidance when no board cell is selected.
        /// </summary>
        private static void DrawEmptyInspector(
            FuDrawList drawList,
            Rect body,
            MapCreatorViewState state,
            float scale)
        {
            // A useful empty state teaches the core loop instead of leaving unused space.
            Rect plate = new Rect(
                body.x,
                body.y,
                body.width,
                156f * scale);
            DrawListUi.DrawPlate(drawList, plate, DrawListUi.Steel);
            DrawListUi.DrawRuneEmblem(
                drawList,
                new Vector2(plate.center.x, plate.y + 46f * scale),
                22f * scale,
                DrawListUi.Steel);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(plate.x, plate.y + 82f * scale),
                plate.width,
                "SÉLECTIONNE UNE CASE",
                DrawListUi.TextPrimary,
                11,
                FontType.Bold);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(plate.x + 18f * scale, plate.y + 108f * scale),
                plate.width - 36f * scale,
                "Inspecte son contenu, oriente les obstacles et configure les mécaniques.",
                DrawListUi.TextSecondary,
                9);

            Rect tool = new Rect(
                body.x,
                plate.yMax + 14f * scale,
                body.width,
                42f * scale);
            MapCreatorDrawListUi.DrawValueRow(
                drawList,
                tool,
                "Outil armé",
                state.ActiveToolLabel,
                DrawListUi.Accent);
        }

        /// <summary>
        /// Draws metadata and only the editing controls valid for the current selection.
        /// </summary>
        private void DrawSelectionInspector(
            FuDrawList drawList,
            Rect body,
            MapCreatorViewState state,
            float scale)
        {
            // Contextual sections replace the old always-visible control wall.
            MapCreatorSelectionState selection = state.Selection;
            float y = body.y;
            float rowHeight = 36f * scale;
            float gap = 7f * scale;
            DrawInspectorRow(
                drawList,
                body,
                ref y,
                rowHeight,
                gap,
                "Type",
                selection.CellType,
                DrawListUi.Accent);
            DrawInspectorRow(
                drawList,
                body,
                ref y,
                rowHeight,
                gap,
                "Sol",
                selection.FloorId,
                DrawListUi.Steel);

            if (!string.IsNullOrWhiteSpace(selection.ElementId))
            {
                DrawInspectorRow(
                    drawList,
                    body,
                    ref y,
                    rowHeight,
                    gap,
                    "Décor",
                    selection.ElementId,
                    DrawListUi.Success);
            }
            if (!string.IsNullOrWhiteSpace(selection.EnemyId))
            {
                DrawInspectorRow(
                    drawList,
                    body,
                    ref y,
                    rowHeight,
                    gap,
                    "Ennemi",
                    selection.EnemyId,
                    DrawListUi.Danger);
            }
            if (selection.IsMovingPlatform)
            {
                DrawInspectorRow(
                    drawList,
                    body,
                    ref y,
                    rowHeight,
                    gap,
                    "Entité",
                    "Plateforme mobile",
                    DrawListUi.Accent);
            }
            if (!string.IsNullOrWhiteSpace(selection.GearId) &&
                selection.GearId != "None")
            {
                DrawInspectorRow(
                    drawList,
                    body,
                    ref y,
                    rowHeight,
                    gap,
                    "Gear",
                    selection.GearId,
                    DrawListUi.AccentBright);
            }

            y += 5f * scale;
            DrawOrientationControls(
                drawList,
                body,
                ref y,
                selection,
                scale);
            DrawElementControls(
                drawList,
                body,
                ref y,
                selection,
                scale);
            DrawMechanicControls(
                drawList,
                body,
                ref y,
                selection,
                scale);
            DrawPathControls(
                drawList,
                body,
                ref y,
                selection,
                scale);
            DrawGearControls(
                drawList,
                body,
                ref y,
                selection,
                scale);

            float deleteY =
                Mathf.Min(body.yMax - 42f * scale, y + 12f * scale);
            Rect delete = new Rect(
                body.x,
                deleteY,
                body.width,
                40f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    delete,
                    "SUPPRIMER LE CONTENU  ·  Suppr",
                    DrawListUi.Danger))
            {
                _viewModel.Actions.DeleteSelection();
            }
        }

        /// <summary>
        /// Draws one inspector row and advances the vertical cursor.
        /// </summary>
        private static void DrawInspectorRow(
            FuDrawList drawList,
            Rect body,
            ref float y,
            float height,
            float gap,
            string label,
            string value,
            Color accent)
        {
            // Centralized cursor movement keeps context-specific rows perfectly aligned.
            MapCreatorDrawListUi.DrawValueRow(
                drawList,
                new Rect(body.x, y, body.width, height),
                label,
                value,
                accent);
            y += height + gap;
        }

        /// <summary>
        /// Draws orthogonal orientation controls for the selected floor, element, or enemy.
        /// </summary>
        private void DrawOrientationControls(
            FuDrawList drawList,
            Rect body,
            ref float y,
            MapCreatorSelectionState selection,
            float scale)
        {
            // Cardinal buttons mirror grid directions and remain faster than repeated scene dragging.
            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(body.x, y),
                body.width,
                "ORIENTATION · " + selection.Orientation.ToUpperInvariant(),
                DrawListUi.Accent);
            y += 22f * scale;
            float gap = 6f * scale;
            float width = (body.width - gap * 3f) * 0.25f;
            string[] labels = { "↑", "→", "↓", "←" };
            for (int index = 0; index < labels.Length; index++)
            {
                Rect button = new Rect(
                    body.x + index * (width + gap),
                    y,
                    width,
                    34f * scale);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        button,
                        labels[index],
                        DrawListUi.Accent))
                {
                    _viewModel.Actions.SetOrientation(index + 1);
                }
            }
            y += 45f * scale;
        }

        /// <summary>
        /// Draws authored-decoration rotation and scale controls when supported.
        /// </summary>
        private void DrawElementControls(
            FuDrawList drawList,
            Rect body,
            ref float y,
            MapCreatorSelectionState selection,
            float scale)
        {
            // Explicit adjustments are predictable and complement direct scene manipulation.
            if (!selection.CanRotateElement &&
                !selection.CanScaleElement)
            {
                return;
            }

            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(body.x, y),
                body.width,
                "TRANSFORMATION DU DÉCOR",
                DrawListUi.Success);
            y += 22f * scale;
            float gap = 7f * scale;
            float half = (body.width - gap) * 0.5f;
            if (selection.CanRotateElement)
            {
                Rect left = new Rect(body.x, y, half, 34f * scale);
                Rect right =
                    new Rect(left.xMax + gap, y, half, 34f * scale);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        left,
                        "− 90°",
                        DrawListUi.Success))
                {
                    _viewModel.Actions.RotateElement(-90f);
                }
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        right,
                        "+ 90°",
                        DrawListUi.Success))
                {
                    _viewModel.Actions.RotateElement(90f);
                }
                y += 41f * scale;
            }
            if (selection.CanScaleElement)
            {
                Rect smaller = new Rect(body.x, y, half, 34f * scale);
                Rect larger =
                    new Rect(smaller.xMax + gap, y, half, 34f * scale);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        smaller,
                        "ÉCHELLE  −",
                        DrawListUi.Success))
                {
                    _viewModel.Actions.ScaleElement(-0.1f);
                }
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        larger,
                        "ÉCHELLE  +",
                        DrawListUi.Success))
                {
                    _viewModel.Actions.ScaleElement(0.1f);
                }
                y += 41f * scale;
            }
            y += 5f * scale;
        }

        /// <summary>
        /// Draws numeric or toggle controls for configurable puzzle cells.
        /// </summary>
        private void DrawMechanicControls(
            FuDrawList drawList,
            Rect body,
            ref float y,
            MapCreatorSelectionState selection,
            float scale)
        {
            // One common stepper handles spring distance and timed-bomb delay consistently.
            bool isSpring = selection.CellType == "Spring";
            bool isBomb = selection.CellType == "Bombe";
            bool isNeedle = selection.CellType == "Needle";
            if (!isSpring && !isBomb && !isNeedle)
                return;

            string title = isSpring
                ? "PORTÉE DU RESSORT"
                : isBomb
                    ? "DÉLAI DE LA BOMBE"
                    : "DÉCLENCHEMENT DES PICS";
            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(body.x, y),
                body.width,
                title,
                DrawListUi.AccentBright);
            y += 22f * scale;

            if (isNeedle)
            {
                Rect toggle =
                    new Rect(body.x, y, body.width, 36f * scale);
                string label = selection.ArgumentValue == 1
                    ? "IMMÉDIAT  ·  OUI"
                    : "IMMÉDIAT  ·  NON";
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        toggle,
                        label,
                        selection.ArgumentValue == 1
                            ? DrawListUi.Success
                            : DrawListUi.Steel,
                        true,
                        selection.ArgumentValue == 1))
                {
                    _viewModel.Actions.ToggleNeedle();
                }
                y += 47f * scale;
                return;
            }

            float buttonWidth = 48f * scale;
            float gap = 7f * scale;
            Rect minus = new Rect(
                body.x,
                y,
                buttonWidth,
                36f * scale);
            Rect value = new Rect(
                minus.xMax + gap,
                y,
                body.width - buttonWidth * 2f - gap * 2f,
                36f * scale);
            Rect plus = new Rect(
                value.xMax + gap,
                y,
                buttonWidth,
                36f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    minus,
                    "−",
                    DrawListUi.Accent))
            {
                _viewModel.Actions.AdjustArgument(-1);
            }
            MapCreatorDrawListUi.DrawValueRow(
                drawList,
                value,
                isSpring ? "Cases" : "Tours",
                selection.ArgumentValue.ToString(),
                DrawListUi.AccentBright);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    plus,
                    "+",
                    DrawListUi.Accent))
            {
                _viewModel.Actions.AdjustArgument(1);
            }
            y += 47f * scale;
        }

        /// <summary>
        /// Draws path authoring controls for patrol enemies and moving platforms.
        /// </summary>
        private void DrawPathControls(
            FuDrawList drawList,
            Rect body,
            ref float y,
            MapCreatorSelectionState selection,
            float scale)
        {
            // Path editing has explicit begin, validate, and cancel states instead of hidden P/Enter behavior.
            if (!selection.CanEditPath)
                return;

            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(body.x, y),
                body.width,
                "TRAJECTOIRE · " + selection.PathPointCount + " POINTS",
                DrawListUi.Danger);
            y += 22f * scale;
            if (!selection.IsEditingPath)
            {
                Rect edit =
                    new Rect(body.x, y, body.width, 36f * scale);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        edit,
                        "REDESSINER LA TRAJECTOIRE  ·  P",
                        DrawListUi.Danger))
                {
                    _viewModel.Actions.BeginPath();
                }
            }
            else
            {
                float gap = 7f * scale;
                float half = (body.width - gap) * 0.5f;
                Rect cancel =
                    new Rect(body.x, y, half, 36f * scale);
                Rect confirm =
                    new Rect(cancel.xMax + gap, y, half, 36f * scale);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        cancel,
                        "ANNULER",
                        DrawListUi.Steel))
                {
                    _viewModel.Actions.CancelPath();
                }
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        confirm,
                        "VALIDER",
                        DrawListUi.Success))
                {
                    _viewModel.Actions.ConfirmPath();
                }
            }
            y += 47f * scale;
        }

        /// <summary>
        /// Draws compact gear assignment controls for compatible walkable cells.
        /// </summary>
        private void DrawGearControls(
            FuDrawList drawList,
            Rect body,
            ref float y,
            MapCreatorSelectionState selection,
            float scale)
        {
            // Every retained gear remains assignable while incompatible cells hide the whole section.
            if (!selection.CanAssignGear)
                return;

            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(body.x, y),
                body.width,
                "GEAR DISPONIBLE",
                DrawListUi.AccentBright);
            y += 22f * scale;
            List<MapCreatorPaletteItem> gears =
                GetFilteredItems(
                    MapCreatorPaletteGroup.Gears,
                    string.Empty);
            float gap = 6f * scale;
            float width = (body.width - gap) * 0.5f;
            float height = 31f * scale;
            for (int index = 0; index < gears.Count; index++)
            {
                int row = index / 2;
                int column = index % 2;
                Rect button = new Rect(
                    body.x + column * (width + gap),
                    y + row * (height + gap),
                    width,
                    height);
                MapCreatorPaletteItem gear = gears[index];
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        button,
                        gear.Label,
                        DrawListUi.AccentBright,
                        true,
                        selection.GearId == gear.Id))
                {
                    _viewModel.Actions.SelectGear(gear.Id);
                }
            }
            y +=
                Mathf.CeilToInt(gears.Count * 0.5f) *
                (height + gap);
        }

        #endregion

        #region Modals

        /// <summary>
        /// Draws the currently active modal above all editor surfaces.
        /// </summary>
        private void DrawActiveModal(
            FuDrawList drawList,
            Vector2 screenSize,
            MapCreatorViewState state,
            float scale)
        {
            // Modal priority matches destructive risk, with exit confirmation always on top.
            if (_showExitPrompt)
            {
                DrawExitPrompt(drawList, screenSize, scale);
                return;
            }
            if (_editingName)
            {
                DrawNameEditor(drawList, screenSize, scale);
                return;
            }
            if (_showValidation)
            {
                DrawValidationModal(drawList, screenSize, state, scale);
                return;
            }
            if (_showEnvironment)
                DrawEnvironmentModal(drawList, screenSize, state, scale);
        }

        /// <summary>
        /// Reports whether one Fugui modal currently owns editor interaction.
        /// </summary>
        private bool HasActiveModal()
        {
            // Text, validation, environment, and destructive confirmation overlays are all exclusive.
            return _showExitPrompt ||
                   _editingName ||
                   _showValidation ||
                   _showEnvironment;
        }

        /// <summary>
        /// Draws save, discard, and cancel choices before leaving a dirty map.
        /// </summary>
        private void DrawExitPrompt(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Explicit three-way wording prevents accidental loss of authored work.
            DrawListUi.DrawScrim(drawList, screenSize, 0.72f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(590f, 270f) * scale);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Danger,
                true);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 29f) * scale,
                "QUITTER LA FORGE ?",
                DrawListUi.TextPrimary,
                17,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 69f) * scale,
                "Cette carte contient des modifications non enregistrées.",
                DrawListUi.TextSecondary,
                11);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 94f) * scale,
                "Tu peux les sauvegarder, les abandonner, ou revenir à l’édition.",
                DrawListUi.TextSecondary,
                10);

            float gap = 10f * scale;
            float buttonWidth = (panel.width - 60f * scale - gap * 2f) / 3f;
            float y = panel.yMax - 72f * scale;
            Rect cancel = new Rect(
                panel.x + 30f * scale,
                y,
                buttonWidth,
                42f * scale);
            Rect discard = new Rect(
                cancel.xMax + gap,
                y,
                buttonWidth,
                42f * scale);
            Rect save = new Rect(
                discard.xMax + gap,
                y,
                buttonWidth,
                42f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    cancel,
                    "REVENIR",
                    DrawListUi.Steel))
            {
                _showExitPrompt = false;
            }
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    discard,
                    "ABANDONNER",
                    DrawListUi.Danger))
            {
                _viewModel.Actions.DiscardAndClose();
            }
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    save,
                    "SAUVER & QUITTER",
                    DrawListUi.Success))
            {
                _viewModel.Actions.SaveAndClose();
            }
        }

        /// <summary>
        /// Draws a focused map-name editor with explicit validation and cancellation.
        /// </summary>
        private void DrawNameEditor(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Naming is modal so board shortcuts cannot mutate the map while the user types.
            DrawListUi.DrawScrim(drawList, screenSize, 0.68f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(560f, 220f) * scale);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(28f, 26f) * scale,
                "NOM DE LA CARTE",
                DrawListUi.TextPrimary,
                16,
                FontType.Bold);
            Rect field = new Rect(
                panel.x + 28f * scale,
                panel.y + 70f * scale,
                panel.width - 56f * scale,
                45f * scale);
            MapCreatorDrawListUi.DrawTextField(
                drawList,
                field,
                _nameDraft,
                "Saisis un nom…",
                true);
            float gap = 10f * scale;
            float width = (field.width - gap) * 0.5f;
            Rect cancel = new Rect(
                field.x,
                panel.yMax - 64f * scale,
                width,
                38f * scale);
            Rect confirm = new Rect(
                cancel.xMax + gap,
                cancel.y,
                width,
                38f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    cancel,
                    "ANNULER  ·  Échap",
                    DrawListUi.Steel))
            {
                CancelNameEditing();
            }
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    confirm,
                    "VALIDER  ·  Entrée",
                    DrawListUi.Success,
                    !string.IsNullOrWhiteSpace(_nameDraft)))
            {
                CommitNameEditing();
            }
        }

        /// <summary>
        /// Draws all grid validation results in a dedicated readable modal.
        /// </summary>
        private void DrawValidationModal(
            FuDrawList drawList,
            Vector2 screenSize,
            MapCreatorViewState state,
            float scale)
        {
            // Validation separates actionable blocking errors from the normal authoring workflow.
            DrawListUi.DrawScrim(drawList, screenSize, 0.7f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(680f, 510f) * scale);
            Color accent = state.ValidationErrors.Count == 0
                ? DrawListUi.Success
                : DrawListUi.Danger;
            DrawListUi.DrawPanel(drawList, panel, accent, true);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 27f) * scale,
                state.ValidationErrors.Count == 0
                    ? "CARTE PRÊTE À TESTER"
                    : "VALIDATION DE LA CARTE",
                DrawListUi.TextPrimary,
                17,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 61f) * scale,
                state.ValidationErrors.Count == 0
                    ? "Aucune erreur bloquante détectée."
                    : state.ValidationErrors.Count +
                      " problème(s) doivent être corrigés avant le playtest.",
                DrawListUi.TextSecondary,
                10);

            Rect list = new Rect(
                panel.x + 30f * scale,
                panel.y + 102f * scale,
                panel.width - 60f * scale,
                panel.height - 178f * scale);
            DrawListUi.DrawPlate(drawList, list, accent);
            float y = list.y + 17f * scale;
            if (state.ValidationErrors.Count == 0)
            {
                DrawListUi.DrawCenteredText(
                    drawList,
                    new Vector2(list.x, list.center.y - 8f * scale),
                    list.width,
                    "Le point de départ, la sortie et les trajectoires sont cohérents.",
                    DrawListUi.TextPrimary,
                    11,
                    FontType.Bold);
            }
            else
            {
                int visibleCount =
                    Mathf.Min(state.ValidationErrors.Count, 10);
                for (int index = 0; index < visibleCount; index++)
                {
                    DrawListUi.DrawText(
                        drawList,
                        new Vector2(list.x + 16f * scale, y),
                        "◆  " + state.ValidationErrors[index],
                        index == 0
                            ? new Color(1f, 0.68f, 0.53f, 1f)
                            : DrawListUi.TextPrimary,
                        10);
                    y += 29f * scale;
                }
            }

            Rect close = new Rect(
                panel.center.x - 100f * scale,
                panel.yMax - 58f * scale,
                200f * scale,
                38f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    close,
                    "FERMER",
                    accent))
            {
                _showValidation = false;
            }
        }

        /// <summary>
        /// Draws the complete built-in environment preset chooser.
        /// </summary>
        private void DrawEnvironmentModal(
            FuDrawList drawList,
            Vector2 screenSize,
            MapCreatorViewState state,
            float scale)
        {
            // Presets and complete advanced controls preserve every original environment capability.
            DrawListUi.DrawScrim(drawList, screenSize, 0.66f);
            if (!string.IsNullOrEmpty(_activeEnvironmentColorId))
            {
                DrawEnvironmentColorEditor(
                    drawList,
                    screenSize,
                    scale);
                return;
            }

            Vector2 panelSize = new Vector2(
                820f * scale,
                Mathf.Min(
                    720f * scale,
                    screenSize.y - 36f * scale));
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                panelSize);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Steel,
                true);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 27f) * scale,
                "AMBIANCE DE LA CARTE",
                DrawListUi.TextPrimary,
                17,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(30f, 61f) * scale,
                "Applique un preset. Le rendu de la scène est mis à jour immédiatement.",
                DrawListUi.TextSecondary,
                10);

            float tabGap = 8f * scale;
            float tabWidth =
                (panel.width - 60f * scale - tabGap) * 0.5f;
            Rect presetsTab = new Rect(
                panel.x + 30f * scale,
                panel.y + 91f * scale,
                tabWidth,
                39f * scale);
            Rect advancedTab = new Rect(
                presetsTab.xMax + tabGap,
                presetsTab.y,
                tabWidth,
                presetsTab.height);
            if (MapCreatorDrawListUi.DrawTab(
                    drawList,
                    presetsTab,
                    "PRESETS",
                    !_showAdvancedEnvironment))
            {
                _showAdvancedEnvironment = false;
                _environmentScroll = 0f;
            }
            if (MapCreatorDrawListUi.DrawTab(
                    drawList,
                    advancedTab,
                    "RÉGLAGES AVANCÉS",
                    _showAdvancedEnvironment))
            {
                _showAdvancedEnvironment = true;
                _environmentScroll = 0f;
            }

            Rect viewport = new Rect(
                panel.x + 30f * scale,
                presetsTab.yMax + 17f * scale,
                panel.width - 60f * scale,
                panel.yMax - presetsTab.yMax - 82f * scale);
            if (_showAdvancedEnvironment)
            {
                DrawAdvancedEnvironmentControls(
                    drawList,
                    viewport,
                    state,
                    scale);
            }
            else
            {
                DrawEnvironmentPresets(
                    drawList,
                    viewport,
                    state,
                    scale);
            }

            Rect close = new Rect(
                panel.center.x - 100f * scale,
                panel.yMax - 55f * scale,
                200f * scale,
                36f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    close,
                    "FERMER",
                    DrawListUi.Steel))
            {
                _showEnvironment = false;
            }
        }

        /// <summary>
        /// Draws all built-in environment presets in a two-column chooser.
        /// </summary>
        private void DrawEnvironmentPresets(
            FuDrawList drawList,
            Rect viewport,
            MapCreatorViewState state,
            float scale)
        {
            // Preset selection is a fast starting point before optional advanced tuning.
            List<MapCreatorPaletteItem> environments =
                GetFilteredItems(
                    MapCreatorPaletteGroup.Environments,
                    string.Empty);
            float gap = 10f * scale;
            float width =
                (viewport.width - gap) * 0.5f;
            float height = 58f * scale;
            for (int index = 0; index < environments.Count; index++)
            {
                int row = index / 2;
                int column = index % 2;
                MapCreatorPaletteItem environment = environments[index];
                Rect button = new Rect(
                    viewport.x + column * (width + gap),
                    viewport.y + row * (height + gap),
                    width,
                    height);
                if (MapCreatorDrawListUi.DrawCompactButton(
                        drawList,
                        button,
                        environment.Label,
                        DrawListUi.Steel,
                        true,
                        state.EnvironmentId == environment.Id))
                {
                    _viewModel.Actions.SelectEnvironment(environment.Id);
                    _showEnvironment = false;
                }
            }
        }

        /// <summary>
        /// Draws every numeric and color environment control in a clipped scrollable workbench.
        /// </summary>
        private void DrawAdvancedEnvironmentControls(
            FuDrawList drawList,
            Rect viewport,
            MapCreatorViewState state,
            float scale)
        {
            // Two columns preserve readable labels while making the full environment payload practical.
            float gap = 20f * scale;
            float columnWidth =
                (viewport.width - gap) * 0.5f;
            float numericHeight = 58f * scale;
            int numericRows =
                Mathf.CeilToInt(
                    state.EnvironmentValues.Count * 0.5f);
            float colorsStart =
                numericRows * numericHeight + 32f * scale;
            float colorHeight = 48f * scale;
            int colorRows =
                Mathf.CeilToInt(
                    state.EnvironmentColors.Count * 0.5f);
            float contentHeight =
                colorsStart +
                colorRows * (colorHeight + 8f * scale);
            float maximumScroll =
                Mathf.Max(
                    0f,
                    contentHeight - viewport.height);
            if (viewport.Contains(Fugui.DefaultContainer.Mouse.Position))
            {
                _environmentScroll -=
                    Fugui.DefaultContainer.Mouse.Wheel.y *
                    58f *
                    scale;
            }
            _environmentScroll =
                Mathf.Clamp(
                    _environmentScroll,
                    0f,
                    maximumScroll);

            drawList.PushClipRect(
                viewport.min,
                viewport.max,
                true);
            for (int index = 0;
                 index < state.EnvironmentValues.Count;
                 index++)
            {
                int row = index / 2;
                int column = index % 2;
                MapCreatorEnvironmentValue parameter =
                    state.EnvironmentValues[index];
                Rect slider = new Rect(
                    viewport.x + column * (columnWidth + gap),
                    viewport.y +
                    23f * scale +
                    row * numericHeight -
                    _environmentScroll,
                    columnWidth,
                    24f * scale);
                if (slider.yMax < viewport.y ||
                    slider.y - 20f * scale > viewport.yMax)
                {
                    continue;
                }

                float changedValue;
                if (MapCreatorDrawListUi.DrawSlider(
                        drawList,
                        slider,
                        parameter.Section + " · " + parameter.Label,
                        parameter.Value,
                        parameter.Minimum,
                        parameter.Maximum,
                        parameter.Format,
                        DrawListUi.Steel,
                        out changedValue))
                {
                    _viewModel.Actions.SetEnvironmentValue(
                        parameter.Id,
                        changedValue);
                }
            }

            float colorHeaderY =
                viewport.y +
                colorsStart -
                _environmentScroll;
            DrawListUi.DrawSectionLabel(
                drawList,
                new Vector2(viewport.x, colorHeaderY),
                viewport.width,
                "COULEURS",
                DrawListUi.Accent);
            float colorY = colorHeaderY + 24f * scale;
            float colorWidth =
                (viewport.width - 8f * scale) * 0.5f;
            for (int index = 0;
                 index < state.EnvironmentColors.Count;
                 index++)
            {
                int row = index / 2;
                int column = index % 2;
                MapCreatorEnvironmentColor color =
                    state.EnvironmentColors[index];
                Rect swatch = new Rect(
                    viewport.x +
                    column * (colorWidth + 8f * scale),
                    colorY +
                    row * (colorHeight + 8f * scale),
                    colorWidth,
                    colorHeight);
                if (swatch.yMax < viewport.y ||
                    swatch.y > viewport.yMax)
                {
                    continue;
                }

                if (MapCreatorDrawListUi.DrawColorSwatch(
                        drawList,
                        swatch,
                        color.Section + " · " + color.Label,
                        color.Value,
                        DrawListUi.Accent))
                {
                    _activeEnvironmentColorId = color.Id;
                    _activeEnvironmentColorLabel = color.Label;
                    _environmentColorDraft = color.Value;
                }
            }
            drawList.PopClipRect();
        }

        /// <summary>
        /// Draws RGB channel controls for one selected environment color.
        /// </summary>
        private void DrawEnvironmentColorEditor(
            FuDrawList drawList,
            Vector2 screenSize,
            float scale)
        {
            // Live channel updates make color tuning immediately visible on the authored scene.
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(560f, 420f) * scale);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(28f, 26f) * scale,
                "COULEUR · " +
                _activeEnvironmentColorLabel.ToUpperInvariant(),
                DrawListUi.TextPrimary,
                16,
                FontType.Bold);
            Rect preview = new Rect(
                panel.x + 28f * scale,
                panel.y + 70f * scale,
                panel.width - 56f * scale,
                64f * scale);
            drawList.AddRectFilled(
                preview.min,
                preview.max,
                _environmentColorDraft,
                5f * scale);
            drawList.AddRect(
                preview.min,
                preview.max,
                new Color(1f, 1f, 1f, 0.42f),
                5f * scale,
                FuDrawFlags.RoundCornersAll,
                Mathf.Max(1f, scale));

            float y = preview.yMax + 41f * scale;
            DrawColorChannel(
                drawList,
                new Rect(
                    preview.x,
                    y,
                    preview.width,
                    24f * scale),
                "Rouge",
                _environmentColorDraft.r,
                new Color(0.82f, 0.22f, 0.16f, 1f),
                0);
            y += 64f * scale;
            DrawColorChannel(
                drawList,
                new Rect(
                    preview.x,
                    y,
                    preview.width,
                    24f * scale),
                "Vert",
                _environmentColorDraft.g,
                new Color(0.28f, 0.72f, 0.35f, 1f),
                1);
            y += 64f * scale;
            DrawColorChannel(
                drawList,
                new Rect(
                    preview.x,
                    y,
                    preview.width,
                    24f * scale),
                "Bleu",
                _environmentColorDraft.b,
                new Color(0.25f, 0.48f, 0.9f, 1f),
                2);

            Rect close = new Rect(
                panel.center.x - 100f * scale,
                panel.yMax - 55f * scale,
                200f * scale,
                36f * scale);
            if (MapCreatorDrawListUi.DrawCompactButton(
                    drawList,
                    close,
                    "TERMINER",
                    DrawListUi.Success))
            {
                _activeEnvironmentColorId = string.Empty;
                _activeEnvironmentColorLabel = string.Empty;
            }
        }

        /// <summary>
        /// Draws and applies one RGB color channel slider.
        /// </summary>
        private void DrawColorChannel(
            FuDrawList drawList,
            Rect rect,
            string label,
            float value,
            Color accent,
            int channel)
        {
            // Channel updates preserve alpha and the other two current draft channels.
            float changedValue;
            if (!MapCreatorDrawListUi.DrawSlider(
                    drawList,
                    rect,
                    label,
                    value,
                    0f,
                    1f,
                    "0.00",
                    accent,
                    out changedValue))
            {
                return;
            }

            switch (channel)
            {
                case 0:
                    _environmentColorDraft.r = changedValue;
                    break;
                case 1:
                    _environmentColorDraft.g = changedValue;
                    break;
                case 2:
                    _environmentColorDraft.b = changedValue;
                    break;
            }
            _viewModel.Actions.SetEnvironmentColor(
                _activeEnvironmentColorId,
                _environmentColorDraft);
        }

        /// <summary>
        /// Draws a blocking save or scene-transition indicator.
        /// </summary>
        private static void DrawBusyState(
            FuDrawList drawList,
            Vector2 screenSize,
            MapCreatorViewState state,
            float scale)
        {
            // Busy feedback blocks repeated destructive actions while keeping progress explicit.
            DrawListUi.DrawScrim(drawList, screenSize, 0.62f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(420f, 156f) * scale);
            DrawListUi.DrawPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);
            DrawListUi.DrawRuneEmblem(
                drawList,
                new Vector2(panel.x + 65f * scale, panel.center.y),
                24f * scale,
                DrawListUi.Accent,
                Mathf.Repeat(Time.unscaledTime * 0.12f, 1f));
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(112f, 48f) * scale,
                "LA FORGE TRAVAILLE",
                DrawListUi.TextPrimary,
                14,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(112f, 80f) * scale,
                string.IsNullOrWhiteSpace(state.StatusMessage)
                    ? "Préparation de la carte…"
                    : state.StatusMessage,
                DrawListUi.TextSecondary,
                10);
        }

        #endregion

        #region Input and layout

        /// <summary>
        /// Begins modal map-name editing with the current authored value.
        /// </summary>
        private void BeginNameEditing(string currentName)
        {
            // A local draft prevents partial typing from mutating saved map metadata.
            _nameDraft = currentName ?? string.Empty;
            _editingName = true;
            _searchFocused = false;
        }

        /// <summary>
        /// Handles typed characters and keyboard commands for the map-name draft.
        /// </summary>
        private void HandleNameInput()
        {
            // Enter commits a non-empty name while Escape always restores the previous value.
            AppendTextInput(
                ref _nameDraft,
                MaximumMapNameLength);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelNameEditing();
                return;
            }

            if ((Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter)) &&
                !string.IsNullOrWhiteSpace(_nameDraft))
            {
                CommitNameEditing();
            }
        }

        /// <summary>
        /// Applies the normalized map-name draft and closes its modal.
        /// </summary>
        private void CommitNameEditing()
        {
            // Whitespace-only names remain invalid and cannot replace map metadata.
            string normalized = _nameDraft.Trim();
            if (normalized.Length == 0)
                return;

            _viewModel.Actions.RenameMap(normalized);
            _editingName = false;
            _nameDraft = string.Empty;
        }

        /// <summary>
        /// Cancels map-name editing without changing controller state.
        /// </summary>
        private void CancelNameEditing()
        {
            // The modal draft is discarded atomically.
            _editingName = false;
            _nameDraft = string.Empty;
        }

        /// <summary>
        /// Handles typed characters and dismissal commands for palette search.
        /// </summary>
        private void HandleSearchInput()
        {
            // Search updates immediately; Escape clears a non-empty query before releasing focus.
            AppendTextInput(
                ref _search,
                MaximumSearchLength);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_search.Length > 0)
                {
                    _search = string.Empty;
                    _paletteScroll = 0f;
                }
                else
                {
                    _searchFocused = false;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Return) ||
                     Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _searchFocused = false;
            }
        }

        /// <summary>
        /// Appends Unity text input to one bounded DrawList field and handles backspace.
        /// </summary>
        private static void AppendTextInput(
            ref string value,
            int maximumLength)
        {
            // Control characters are ignored except backspace, matching conventional desktop fields.
            string input = Input.inputString;
            foreach (char character in input)
            {
                if (character == '\b')
                {
                    if (value.Length > 0)
                        value = value.Substring(0, value.Length - 1);
                    continue;
                }
                if (character == '\n' ||
                    character == '\r' ||
                    char.IsControl(character) ||
                    value.Length >= maximumLength)
                {
                    continue;
                }

                value += character;
            }
        }

        /// <summary>
        /// Closes the highest-priority non-text modal.
        /// </summary>
        private void CloseTopModal()
        {
            // Escape is non-destructive and always dismisses only one overlay.
            if (_showExitPrompt)
                _showExitPrompt = false;
            else if (_showValidation)
                _showValidation = false;
            else if (!string.IsNullOrEmpty(_activeEnvironmentColorId))
            {
                _activeEnvironmentColorId = string.Empty;
                _activeEnvironmentColorLabel = string.Empty;
            }
            else if (_showEnvironment)
                _showEnvironment = false;
        }

        /// <summary>
        /// Synchronizes keyboard-capture changes with the Unity editor controller.
        /// </summary>
        private void SetKeyboardCapture(bool capture)
        {
            // Avoid redundant delegate calls while guaranteeing cleanup on modal transitions.
            if (_keyboardCaptured == capture)
                return;

            _keyboardCaptured = capture;
            _viewModel.Actions.SetKeyboardCaptured(capture);
        }

        /// <summary>
        /// Updates the controller input guard from all visible editor and modal rectangles.
        /// </summary>
        private void UpdatePointerGuard(
            Rect top,
            Rect left,
            Rect right,
            Rect bottom,
            Vector2 screenSize)
        {
            // Modal scrims consume the full pointer surface; normal editing leaves the center viewport free.
            Vector2 pointer =
                Fugui.DefaultContainer.Mouse.Position;
            bool modal =
                _showExitPrompt ||
                _editingName ||
                _showValidation ||
                _showEnvironment;
            bool overUi =
                modal && new Rect(Vector2.zero, screenSize).Contains(pointer) ||
                top.Contains(pointer) ||
                left.Contains(pointer) ||
                right.Contains(pointer) ||
                bottom.Contains(pointer);
            _viewModel.Actions.SetPointerOverUi(overUi);
        }

        /// <summary>
        /// Updates pointer capture before normal scene controllers process the current input frame.
        /// </summary>
        private void UpdatePointerGuardForFrame()
        {
            // Early execution prevents one click from activating a DrawList button and the board beneath it.
            if (_context == null)
                return;

            Vector2 screenSize =
                Fugui.DefaultContainer.Size;
            float scale =
                Mathf.Max(0.5f, _context.Scale);
            UpdatePointerGuard(
                GetTopBar(screenSize, scale),
                GetPalettePanel(screenSize, scale),
                GetInspectorPanel(screenSize, scale),
                GetStatusBar(screenSize, scale),
                screenSize);
        }

        /// <summary>
        /// Places one toolbar button from right to left and advances the placement cursor.
        /// </summary>
        private static Rect PlaceToolbarButton(
            ref float rightEdge,
            float y,
            float width,
            float height,
            float gap)
        {
            // Right-aligned command placement remains stable as map-name space flexes.
            rightEdge -= width;
            Rect rect = new Rect(rightEdge, y, width, height);
            rightEdge -= gap;
            return rect;
        }

        /// <summary>
        /// Computes the responsive top command bar.
        /// </summary>
        private static Rect GetTopBar(
            Vector2 screenSize,
            float scale)
        {
            // A small safe margin separates the workbench from display edges.
            float margin = Mathf.Max(10f * scale, screenSize.y * 0.012f);
            return new Rect(
                margin,
                margin,
                screenSize.x - margin * 2f,
                76f * scale);
        }

        /// <summary>
        /// Computes the responsive left palette panel.
        /// </summary>
        private static Rect GetPalettePanel(
            Vector2 screenSize,
            float scale)
        {
            // Width is capped to preserve the board viewport at common 16:9 resolutions.
            float margin = Mathf.Max(10f * scale, screenSize.y * 0.012f);
            float top = margin + 86f * scale;
            float bottom = margin + 48f * scale;
            float width =
                Mathf.Min(390f * scale, screenSize.x * 0.25f);
            return new Rect(
                margin,
                top,
                width,
                screenSize.y - top - bottom);
        }

        /// <summary>
        /// Computes the responsive right inspector panel.
        /// </summary>
        private static Rect GetInspectorPanel(
            Vector2 screenSize,
            float scale)
        {
            // The inspector receives slightly more width for readable values and contextual controls.
            float margin = Mathf.Max(10f * scale, screenSize.y * 0.012f);
            float top = margin + 86f * scale;
            float bottom = margin + 48f * scale;
            float width =
                Mathf.Min(390f * scale, screenSize.x * 0.25f);
            return new Rect(
                screenSize.x - margin - width,
                top,
                width,
                screenSize.y - top - bottom);
        }

        /// <summary>
        /// Computes the responsive bottom status bar.
        /// </summary>
        private static Rect GetStatusBar(
            Vector2 screenSize,
            float scale)
        {
            // The status surface spans only the viewport gap between both side panels.
            float margin = Mathf.Max(10f * scale, screenSize.y * 0.012f);
            float sideWidth =
                Mathf.Min(390f * scale, screenSize.x * 0.25f);
            float x = margin + sideWidth + 10f * scale;
            float right =
                screenSize.x - margin - sideWidth - 10f * scale;
            return new Rect(
                x,
                screenSize.y - margin - 38f * scale,
                right - x,
                38f * scale);
        }

        #endregion
    }
}

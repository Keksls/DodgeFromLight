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
        private string _tooltipTitle = string.Empty;
        private string _tooltipBody = string.Empty;
        private string _lastTooltipTitle = string.Empty;
        private Vector2 _tooltipAnchor;
        private float _tooltipSince;
        private string _activeEnvironmentColorId = string.Empty;
        private string _activeEnvironmentColorLabel = string.Empty;
        private readonly MapCreatorScrollRegion _paletteScroller = new MapCreatorScrollRegion();
        private readonly MapCreatorScrollRegion _categoryScroller = new MapCreatorScrollRegion();
        private readonly MapCreatorScrollRegion _inspectorScroller = new MapCreatorScrollRegion();
        private readonly MapCreatorScrollRegion _environmentScroller = new MapCreatorScrollRegion();
        private Color _environmentColorDraft = Color.white;
        private bool _searchFocused;
        private bool _editingName;
        private bool _showExitPrompt;
        private bool _showValidation;
        private bool _showEnvironment;
        private bool _showAdvancedEnvironment;
        private readonly HashSet<string> _expandedEnvironmentSections = new HashSet<string> { "Éclairage" };
        private bool _ownsSkin;
        private bool _keyboardCaptured;
        private string _category = string.Empty;
        private bool _categoryOpen;
        private bool _showPlacement = true;
        private bool _showMechanics = true;
        private bool _showRoute = true;
        private bool _showHelp;
        private string _inspectedCell = string.Empty;
        private float _inspectorContentHeight;
        private bool _showNewMap;
        private int _newWidth = 12;
        private int _newHeight = 12;
        private readonly Dictionary<string, MapCreatorPaletteItem> _catalogue = new Dictionary<string, MapCreatorPaletteItem>();

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
            _catalogue.Clear();
            foreach (var item in _viewModel.GetItems())
                if (!_catalogue.ContainsKey(item.Id))
                    _catalogue.Add(item.Id, item);
            _context = Fugui.DefaultContext;
            if (!_ownsSkin)
            {
                DrawListUi.AcquireSkin();
                MapCreatorTheme.Acquire();
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
            // Clicking the board releases text focus before the controller processes that same click.
            if (_searchFocused && Input.GetMouseButtonDown(0) &&
                !GetPalettePanel(Fugui.DefaultContainer.Size, DrawListUi.GetScale()).Contains(Fugui.DefaultContainer.Mouse.Position))
                _searchFocused = false;
            bool capture =
                _searchFocused ||
                _editingName ||
                _showExitPrompt ||
                _showValidation ||
                !string.IsNullOrEmpty(_activeEnvironmentColorId) || _showNewMap;
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
                _showEnvironment || !string.IsNullOrEmpty(_activeEnvironmentColorId) || _showNewMap)
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
            MapCreatorTheme.Release();
            _ownsSkin = false;
        }

        #endregion

        #region Workbench

        /// <summary>
        /// Renders the schematic board, authoring tools, contextual inspector, and exclusive modals.
        /// </summary>
        private void Render()
        {
            // One shared frame keeps hover previews and committed content in sync.
            if (_viewModel == null || Fugui.CurrentContext != _context)
                return;
            MapCreatorViewState state = _viewModel.GetState();
            if (state == null)
                return;
            FuDrawList drawList = Fugui.GetForegroundDrawList();
            Vector2 screen = DrawListUi.GetScreenSize();
            float scale = DrawListUi.GetScale();
            _hoverDescription = string.Empty;
            _tooltipTitle = string.Empty;
            _tooltipBody = string.Empty;
            MapCreatorDrawListUi.SetInteractionsEnabled(!HasActiveModal() && !state.IsBusy);
            DrawBoard(drawList, GetBoardViewport(screen, scale), state, scale);
            DrawTopBar(drawList, GetTopBar(screen, scale), state, scale);
            DrawPalette(drawList, GetPalettePanel(screen, scale), state, scale);
            DrawDetailsPanel(drawList, GetInspectorPanel(screen, scale), state, scale);
            DrawViewportToolbar(drawList, GetViewportToolbar(screen, scale), state, scale);
            DrawAxisGizmo(drawList, GetGizmoRect(screen, scale), state, scale);
            DrawStatusBar(drawList, GetStatusBar(screen, scale), state, scale);
            MapCreatorDrawListUi.SetInteractionsEnabled(true);
            DrawActiveModal(drawList, screen, state, scale);
            if (!HasActiveModal() && !state.IsBusy)
                DrawTooltip(drawList, screen, scale);
            UpdatePointerGuard(GetTopBar(screen, scale), GetPalettePanel(screen, scale),
                GetInspectorPanel(screen, scale), GetStatusBar(screen, scale), screen);
            if (state.IsBusy)
                DrawBusyState(drawList, screen, state, scale);
        }

        /// <summary>
        /// Draws restrained opaque panels with a clear visual hierarchy.
        /// </summary>
        private static void DrawWorkbenchPanel(FuDrawList drawList, Rect rect, Color accent, bool prominent = false)
        {
            // Subtle worn surfaces and warm metal replace software-style slate boxes without ornament behind text.
            MapCreatorTheme.Surface(drawList, rect);
            if (prominent)
                drawList.AddLine(rect.min + new Vector2(12f, 0f), new Vector2(rect.xMax - 12f, rect.y), accent, 2f);
        }

        /// <summary>
        /// Draws editor text with a readable minimum size using the verified shared font resolver.
        /// </summary>
        private static void Text(FuDrawList drawList, Vector2 position, string text, Color color, int fontSize = 14, FontType fontType = FontType.Regular)
        {
            // All typography still passes through the atlas-aware Fugui helper.
            DrawListUi.DrawText(drawList, position, text, color, Mathf.Max(12, fontSize), fontType);
        }

        /// <summary>
        /// Draws a simple action button shared by the workbench and its contextual controls.
        /// </summary>
        private static bool Button(FuDrawList drawList, Rect rect, string label, bool selected = false, bool enabled = true)
        {
            // The editor control helper suppresses background hit tests when a modal owns the pointer.
            return MapCreatorDrawListUi.DrawCompactButton(drawList, rect, label,
                DrawListUi.Accent, enabled, selected);
        }

        /// <summary>
        /// Couples a pictogram target to a readable contextual explanation without requiring icon memorization.
        /// </summary>
        private bool IconControl(FuDrawList drawList, Rect rect, string icon, string title, string help, bool selected = false, bool enabled = true, string caption = "")
        {
            // Hover explanations are drawn last so no panel can cover them.
            if (!HasActiveModal() && rect.Contains(Fugui.DefaultContainer.Mouse.Position))
            {
                _tooltipTitle = title;
                _tooltipBody = help;
                _hoverDescription = title + " : " + help;
            }
            return MapCreatorDrawListUi.DrawIconControl(drawList, rect, icon, caption, selected, enabled);
        }

        /// <summary>
        /// Displays the hovered control name and gesture inside a viewport-clamped floating hint.
        /// </summary>
        private void DrawTooltip(FuDrawList drawList, Vector2 screen, float scale)
        {
            // Measured text allows concise explanations without clipping or guessing label widths.
            if (string.IsNullOrEmpty(_tooltipTitle))
            {
                _lastTooltipTitle = string.Empty;
                return;
            }
            Vector2 pointer = Fugui.DefaultContainer.Mouse.Position;
            if (_lastTooltipTitle != _tooltipTitle || Vector2.Distance(pointer, _tooltipAnchor) > 5f * scale ||
                Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                _lastTooltipTitle = _tooltipTitle;
                _tooltipAnchor = pointer;
                _tooltipSince = Time.unscaledTime;
            }
            if (Time.unscaledTime - _tooltipSince < 0.35f)
                return;
            float width = Mathf.Max(DrawListUi.MeasureText(_tooltipTitle, 14, FontType.Bold).x,
                DrawListUi.MeasureText(_tooltipBody, 12, FontType.Regular).x) + 28f * scale;
            Rect hint = new Rect(Mathf.Clamp(pointer.x + 18f * scale, 4f * scale, Mathf.Max(4f * scale, screen.x - width - 4f * scale)),
                Mathf.Clamp(pointer.y + 24f * scale, 4f * scale, screen.y - 74f * scale), width, 64f * scale);
            DrawWorkbenchPanel(drawList, hint, DrawListUi.Steel);
            Text(drawList, hint.position + new Vector2(12f, 9f) * scale, _tooltipTitle, DrawListUi.TextPrimary, 14, FontType.Bold);
            Text(drawList, hint.position + new Vector2(12f, 34f) * scale, _tooltipBody, DrawListUi.TextSecondary, 12);
        }

        /// <summary>
        /// Keeps map identity, save state, validation, and playtest consistently visible.
        /// </summary>
        private void DrawTopBar(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            // Document commands remain separate from viewport gestures.
            DrawWorkbenchPanel(drawList, rect, DrawListUi.Accent);
            if (Button(drawList, new Rect(rect.x + 12f * scale, rect.y + 13f * scale, 74f * scale, 36f * scale), "Retour"))
            {
                _showExitPrompt = state.IsDirty;
                if (!state.IsDirty)
                    _viewModel.Actions.DiscardAndClose();
            }
            if (Button(drawList, new Rect(rect.x + 94f * scale, rect.y + 13f * scale, 78f * scale, 36f * scale), "Nouveau"))
                _showNewMap = true;
            float x = rect.x + 186f * scale;
            MapCreatorTheme.Heading(drawList, new Vector2(x, rect.y + 8f * scale), "Atelier de cartes");
            Rect title = new Rect(x, rect.y + 29f * scale, Mathf.Max(100f, rect.width - 772f * scale), 27f * scale);
            string name = MapCreatorDrawListUi.TruncateText(state.MapName, title.width, 14, FontType.Regular);
            Text(drawList, title.position, name, DrawListUi.TextSecondary, 14);
            if (!HasActiveModal() && !state.IsBusy && title.Contains(Fugui.DefaultContainer.Mouse.Position) &&
                Fugui.DefaultContainer.Mouse.IsUp(FuMouseButton.Left))
                BeginNameEditing(state.MapName);
            float right = rect.xMax - 12f * scale;
            var play = PlaceToolbarButton(ref right, rect.y + 13f * scale, 98f * scale, 36f * scale, 8f * scale);
            if (Button(drawList, play, "Jouer ma carte", true, state.IsReady && !state.IsBusy && !state.Selection.IsEditingPath))
            {
                if (state.ValidationErrors.Count > 0)
                    _showValidation = true;
                else
                    _viewModel.Actions.Playtest();
            }
            var save = PlaceToolbarButton(ref right, play.y, 100f * scale, play.height, 8f * scale);
            if (Button(drawList, save, "Enregistrer", false, state.IsReady && !state.IsBusy && !state.Selection.IsEditingPath))
                _viewModel.Actions.Save();
            var validation = PlaceToolbarButton(ref right, play.y, 96f * scale, play.height, 8f * scale);
            if (Button(drawList, validation, state.ValidationErrors.Count == 0 ? "Carte valide" : state.ValidationErrors.Count + " à corriger"))
                _showValidation = true;
            var redo = PlaceToolbarButton(ref right, play.y, 68f * scale, play.height, 8f * scale);
            if (IconControl(drawList, redo, "Redo", "Rétablir · Ctrl+Y", "Rétablit la dernière modification annulée.", false, state.CanRedo, "Rétablir"))
                _viewModel.Actions.Redo();
            var undo = PlaceToolbarButton(ref right, play.y, 68f * scale, play.height, 8f * scale);
            if (IconControl(drawList, undo, "Undo", "Annuler · Ctrl+Z", "Annule le dernier geste complet.", false, state.CanUndo, "Annuler"))
                _viewModel.Actions.Undo();
        }

        /// <summary>
        /// Presents illustrated content families and independently scrollable filters.
        /// </summary>
        private void DrawPalette(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            DrawWorkbenchPanel(drawList, rect, DrawListUi.Accent);
            float x = rect.x + 14f * scale;
            float width = rect.width - 28f * scale;
            float y = rect.y + 14f * scale;
            MapCreatorTheme.Heading(drawList, new Vector2(x, y), "Bibliothèque");
            y += 31f * scale;
            var groups = new[] { MapCreatorPaletteGroup.Structure, MapCreatorPaletteGroup.Decorations, MapCreatorPaletteGroup.Enemies, MapCreatorPaletteGroup.Gears };
            string[] labels = { "Construire", "Décorer", "Ennemis", "Équipement" };
            for (int i = 0; i < groups.Length; i++)
            {
                Rect tab = new Rect(x + i % 2 * width * 0.5f, y + i / 2 * 53f * scale, width * 0.5f - 4f * scale, 49f * scale);
                if (MapCreatorDrawListUi.DrawLibraryTab(drawList, tab, groups[i].ToString(), labels[i], _activeGroup == groups[i]))
                {
                    _activeGroup = groups[i];
                    _category = string.Empty;
                    _categoryOpen = false;
                    _paletteScroller.Offset = 0f;
                    _categoryScroller.Offset = 0f;
                    _searchFocused = false;
                }
            }
            y += 114f * scale;
            Rect search = new Rect(x, y, width, 35f * scale);
            if (MapCreatorDrawListUi.DrawTextField(drawList, search, _search, "Rechercher un bloc...", _searchFocused))
                _searchFocused = true;
            else if (Input.GetMouseButtonDown(0) && !search.Contains(Fugui.DefaultContainer.Mouse.Position))
                _searchFocused = false;
            y += 43f * scale;
            string filter = string.IsNullOrEmpty(_category) ? "Tout" : FriendlyCategory(_category);
            if (MapCreatorDrawListUi.DrawDisclosure(drawList, new Rect(x, y, width, 31f * scale), "Catégories", _categoryOpen, filter))
                _categoryOpen = !_categoryOpen;
            y += 38f * scale;
            var list = GetFilteredItems(_activeGroup, _search);
            if (_categoryOpen)
            {
                var categories = new List<string> { string.Empty };
                foreach (MapCreatorPaletteItem item in GetFilteredItems(_activeGroup, string.Empty))
                    if (!categories.Contains(item.Category)) categories.Add(item.Category);
                float rowHeight = 30f * scale;
                Rect filters = new Rect(x, y, width, Mathf.Min(132f * scale, categories.Count * rowHeight));
                float contentHeight = categories.Count * rowHeight;
                _categoryScroller.Update(filters, contentHeight, !HasActiveModal());
                Rect clip = new Rect(filters.x, filters.y, filters.width - 18f * scale, filters.height);
                drawList.PushClipRect(clip.min, clip.max, true);
                MapCreatorDrawListUi.SetInteractionClip(clip);
                for (int i = 0; i < categories.Count; i++)
                {
                    Rect row = new Rect(x, y + i * rowHeight - _categoryScroller.Offset, clip.width, rowHeight - 2f * scale);
                    if (row.yMax < clip.y || row.y > clip.yMax) continue;
                    if (MapCreatorDrawListUi.DrawTab(drawList, row, string.IsNullOrEmpty(categories[i]) ? "Tout afficher" : FriendlyCategory(categories[i]), _category == categories[i]))
                    {
                        _category = categories[i];
                        _paletteScroller.Offset = 0f;
                    }
                }
                MapCreatorDrawListUi.SetInteractionClip(null);
                drawList.PopClipRect();
                _categoryScroller.Draw(drawList, filters, contentHeight);
                y += filters.height + 9f * scale;
            }
            list.RemoveAll(item => (!string.IsNullOrEmpty(_category) && item.Category != _category) ||
                (item.Group == MapCreatorPaletteGroup.Gears && item.Id == "None") || item.Id == "Select");
            Rect viewport = new Rect(x, y, width, Mathf.Max(24f * scale, rect.yMax - y - 29f * scale));
            float itemHeight = 76f * scale;
            float height = list.Count * itemHeight;
            _paletteScroller.Update(viewport, height, !HasActiveModal());
            Rect itemsClip = new Rect(viewport.x, viewport.y, viewport.width - 18f * scale, viewport.height);
            drawList.PushClipRect(itemsClip.min, itemsClip.max, true);
            MapCreatorDrawListUi.SetInteractionClip(itemsClip);
            for (int i = 0; i < list.Count; i++)
            {
                Rect card = new Rect(x, y + i * itemHeight - _paletteScroller.Offset, itemsClip.width, 69f * scale);
                if (card.yMax < viewport.y || card.y > viewport.yMax) continue;
                MapCreatorPaletteItem item = list[i];
                if (MapCreatorDrawListUi.DrawPaletteCard(drawList, card, item.Label, FriendlyCategory(item.Category), item.Preview, DrawListUi.Accent,
                    state.ActiveToolId == item.Id, out bool hovered))
                {
                    ActivatePaletteItem(item);
                    _searchFocused = false;
                }
                if (hovered) _hoverDescription = item.Description;
            }
            if (list.Count == 0)
                Text(drawList, itemsClip.position + Vector2.one * 8f * scale, "Aucun bloc trouvé.", DrawListUi.TextSecondary);
            MapCreatorDrawListUi.SetInteractionClip(null);
            drawList.PopClipRect();
            _paletteScroller.Draw(drawList, viewport, height);
            Text(drawList, new Vector2(x, rect.yMax - 21f * scale), list.Count + " blocs · molette pour parcourir", DrawListUi.TextSecondary, 12);
        }

        /// <summary>
        /// Filters content by family, player-facing text, category, and stable identifier.
        /// </summary>
        private List<MapCreatorPaletteItem> GetFilteredItems(MapCreatorPaletteGroup group, string query)
        {
            // Stable database IDs remain searchable without being the primary UI label.
            var result = new List<MapCreatorPaletteItem>();
            foreach (var item in _viewModel.GetItems())
            {
                if (item.Group != group)
                    continue;
                if (!string.IsNullOrWhiteSpace(query) &&
                    (item.Label + " " + item.Category + " " + item.Description + " " + item.Id)
                        .IndexOf(query.Trim(), System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                result.Add(item);
            }
            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        /// <summary>
        /// Arms the same direct board-placement workflow for every content family.
        /// </summary>
        private void ActivatePaletteItem(MapCreatorPaletteItem item)
        {
            // No content family requires finding a hidden inspector assignment control.
            _showEnvironment = false;
            if (item.Group == MapCreatorPaletteGroup.Decorations)
                _viewModel.Actions.SelectDecoration(item.Id);
            else if (item.Group == MapCreatorPaletteGroup.Gears)
                _viewModel.Actions.SelectGear(item.Id);
            else
                _viewModel.Actions.SelectTool(item.Id);
        }

        /// <summary>
        /// Separates illustrated construction tools from material-preview spheres and environment editing.
        /// </summary>
        private void DrawViewportToolbar(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            DrawWorkbenchPanel(drawList, rect, DrawListUi.Steel);
            string[] modes = { "Select", "Paint", "Rectangle", "Erase", "Move", "Pick" };
            string[] labels = { "Sélection · S", "Pinceau", "Rectangle", "Gomme", "Déplacer", "Pipette" };
            string[] help = { "Clique une case pour voir ses propriétés.", "Clique et glisse pour poser le bloc choisi.",
                "Glisse entre deux coins pour remplir une zone.", "Retire d'abord l'objet, puis le sol.",
                "Choisis un objet, puis clique sa destination.", "Clique un objet pour reprendre son pinceau." };
            for (int i = 0; i < modes.Length; i++)
                if (IconControl(drawList, new Rect(rect.x + (8f + 60f * i) * scale, rect.y + 6f * scale, 55f * scale, 52f * scale),
                    modes[i], labels[i], help[i], state.Viewport.BrushMode == modes[i]))
                    _viewModel.Actions.SetBrushMode(modes[i]);
            int active = System.Array.IndexOf(modes, state.Viewport.BrushMode);
            if (active >= 0 && rect.width > 1030f * scale)
            {
                Text(drawList, new Vector2(rect.x + 383f * scale, rect.y + 12f * scale), labels[active], DrawListUi.TextPrimary, 16);
                Text(drawList, new Vector2(rect.x + 383f * scale, rect.y + 35f * scale), help[active], DrawListUi.TextSecondary, 12);
            }
            float right = rect.xMax - 8f * scale;
            if (IconControl(drawList, new Rect(right - 50f * scale, rect.y + 8f * scale, 50f * scale, 48f * scale),
                "GameLight", "Rendu du jeu", "Afficher le ciel, les lumières et les effets de la carte.", state.Viewport.ShowEnvironment))
                _viewModel.Actions.SetEnvironmentPreview(true);
            if (IconControl(drawList, new Rect(right - 106f * scale, rect.y + 8f * scale, 50f * scale, 48f * scale),
                "WorkLight", "Rendu de travail", "Lumière neutre et ombres pour lire les volumes.", !state.Viewport.ShowEnvironment))
                _viewModel.Actions.SetEnvironmentPreview(false);
        }

        /// <summary>
        /// Renders camera navigation inside the work area and keeps its entire footprint out of painting input.
        /// </summary>
        private void DrawAxisGizmo(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            Rect compass = new Rect(rect.x, rect.y, rect.width, rect.height - 40f * scale);
            string command = MapCreatorAxisGizmo.Draw(drawList, compass, state.Viewport.CameraRotation, state.Viewport.IsPlanView, state.Viewport.IsTransitioning, out string hint);
            if (!string.IsNullOrEmpty(command)) _viewModel.Actions.SetCameraView(command);
            if (!string.IsNullOrEmpty(hint))
            {
                _tooltipTitle = "Caméra";
                _tooltipBody = hint;
                _hoverDescription = hint;
            }
            if (Button(drawList, new Rect(rect.x, compass.yMax + 6f * scale, rect.width * 0.5f - 3f * scale, 30f * scale), "Cadrer"))
                _viewModel.Actions.FocusSelection();
            if (Button(drawList, new Rect(rect.center.x + 3f * scale, compass.yMax + 6f * scale, rect.width * 0.5f - 3f * scale, 30f * scale), "Grille", state.Viewport.ShowGrid))
                _viewModel.Actions.ToggleGrid();
        }

        /// <summary>
        /// Separates object properties from map atmosphere without changing the viewport rendering.
        /// </summary>
        private void DrawDetailsPanel(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            DrawWorkbenchPanel(drawList, rect, DrawListUi.Accent);
            float width = (rect.width - 28f * scale) * 0.5f;
            if (MapCreatorDrawListUi.DrawLibraryTab(drawList, new Rect(rect.x + 10f * scale, rect.y + 9f * scale, width, 46f * scale), "Select", "Objet", !_showEnvironment))
                _showEnvironment = false;
            if (MapCreatorDrawListUi.DrawLibraryTab(drawList, new Rect(rect.x + 18f * scale + width, rect.y + 9f * scale, width, 46f * scale), "GameLight", "Ambiance", _showEnvironment))
                _showEnvironment = true;
            Rect content = new Rect(rect.x, rect.y + 65f * scale, rect.width, rect.height - 65f * scale);
            if (_showEnvironment) DrawDockedEnvironment(drawList, content, state, scale);
            else DrawInspector(drawList, content, state, scale);
        }

        /// <summary>
        /// Overlays restrained authoring feedback on the actual map in both camera projections.
        /// </summary>
        private void DrawBoard(FuDrawList drawList, Rect viewport, MapCreatorViewState state, float scale)
        {
            // The Unity camera supplies every floor, enemy, and prop; the UI never substitutes schematic tiles.
            MapCreatorViewportState board = state.Viewport;
            if (board == null) return;
            drawList.PushClipRect(viewport.min, viewport.max, true);
            foreach (MapCreatorBoardCell cell in board.Cells)
            {
                Vector2[] c = cell.Corners;
                Vector2 minimum = Vector2.Min(Vector2.Min(c[0], c[1]), Vector2.Min(c[2], c[3]));
                Vector2 maximum = Vector2.Max(Vector2.Max(c[0], c[1]), Vector2.Max(c[2], c[3]));
                if (maximum.x < viewport.x || minimum.x > viewport.xMax || maximum.y < viewport.y || minimum.y > viewport.yMax)
                    continue;
                Color edge = cell.Selected ? DrawListUi.AccentBright :
                    cell.Preview ? (board.HoverValid ? new Color(0.70f, 0.80f, 0.52f) : new Color(0.94f, 0.35f, 0.25f)) :
                    new Color(0.89f, 0.85f, 0.72f, board.ShowEnvironment ? 0.20f : 0.15f);
                if (cell.Preview || cell.Selected)
                    drawList.AddQuadFilled(c[0], c[1], c[2], c[3], Fugui.GetColorU32(new Color(edge.r, edge.g, edge.b, 0.11f)));
                if (board.ShowGrid || cell.Preview || cell.Selected)
                    for (int i = 0; i < 4; i++)
                        drawList.AddLine(c[i], c[(i + 1) % 4], edge, (cell.Preview || cell.Selected ? 2f : 0.65f) * scale);
                Vector2 center = (c[0] + c[2]) * 0.5f;
                float size = Mathf.Min(Vector2.Distance(c[0], c[1]), Vector2.Distance(c[1], c[2]));
                MapCreatorPaletteItem asset = FindItem(cell.AssetId);
                if (size > 25f * scale && (cell.Selected || cell.HoverAnchor) && HasDirectionalSymbol(asset))
                    DrawDirection(drawList, c, center, size * 0.43f, cell.Orientation, DrawListUi.AccentBright, scale);
                if ((cell.IsStart || cell.IsExit) && size > 32f * scale)
                {
                    string label = cell.IsStart ? "Départ" : "Sortie";
                    Vector2 textSize = DrawListUi.MeasureText(label, 12);
                    Vector2 position = new Vector2(center.x - textSize.x * 0.5f, minimum.y + 3f * scale);
                    drawList.AddRectFilled(position - new Vector2(4f, 2f) * scale, position + textSize + new Vector2(4f, 2f) * scale, new Color(0.025f, 0.02f, 0.018f, 0.9f), 2f * scale);
                    Text(drawList, position, label, cell.IsStart ? new Color(0.79f, 0.87f, 0.62f) : DrawListUi.AccentBright, 12);
                }
                if (cell.HoverAnchor && !HasActiveModal())
                {
                    _tooltipTitle = "Case " + cell.X + " : " + cell.Y;
                    _tooltipBody = cell.IsStart ? "Départ du joueur" : cell.IsExit ? "Sortie de la carte" :
                        asset != null ? asset.Label : cell.Walkable ? "Sol praticable" : "Vide";
                    if (board.HoverValid && state.ActiveToolId != "Select" && board.BrushMode == "Paint")
                    {
                        MapCreatorPaletteItem armed = FindItem(state.ActiveToolId);
                        if (armed != null && armed.Preview != null && size > 24f * scale)
                            drawList.AddImage(armed.Preview, center - Vector2.one * size * 0.30f,
                                center + Vector2.one * size * 0.30f, new Vector4(1f, 1f, 1f, 0.55f));
                        if (HasDirectionalSymbol(armed))
                            DrawDirection(drawList, c, center, size * 0.43f, board.Orientation, edge, scale);
                    }
                }
            }
            for (int i = 0; i < board.Path.Count; i++)
            {
                Vector2 point = board.Path[i];
                if (i > 0)
                    drawList.AddLine(board.Path[i - 1], point, DrawListUi.AccentBright, 2f * scale);
                drawList.AddCircleFilled(point, 5f * scale, DrawListUi.AccentBright);
                Text(drawList, point + new Vector2(7f, -14f) * scale, (i + 1).ToString(), DrawListUi.TextPrimary, 14);
            }
            drawList.PopClipRect();
            string cameraLabel = board.IsTransitioning ? "Changement de point de vue…" : board.IsPlanView ? "Vue de dessus · Nord en haut" : "Vue en perspective";
            string lightingLabel = board.ShowEnvironment ? "Rendu du jeu · " + state.EnvironmentLabel : "Éclairage de travail";
            Vector2 heading = viewport.position + Vector2.one * 12f * scale;
            Vector2 headingSize = DrawListUi.MeasureText(cameraLabel + "   /   " + lightingLabel, 12);
            drawList.AddRectFilled(heading - Vector2.one * 6f * scale, heading + headingSize + Vector2.one * 6f * scale,
                new Color(0.035f, 0.028f, 0.024f, 0.88f), 3f * scale);
            Text(drawList, heading, cameraLabel + "   /   " + lightingLabel, DrawListUi.TextPrimary, 12);
        }

        /// <summary>
        /// Identifies content whose facing direction matters when positioning or inspecting it.
        /// </summary>
        private static bool HasDirectionalSymbol(MapCreatorPaletteItem item)
        {
            // Direction arrows are contextual, not permanent clutter over every decoration.
            return item != null && (item.Group == MapCreatorPaletteGroup.Enemies ||
                (item.Group == MapCreatorPaletteGroup.Decorations && item.Category != "Floor") ||
                item.Id == "Spring" || item.Id == "MovingPlatform");
        }

        /// <summary>
        /// Draws orientation using geometry rather than font-dependent arrow characters.
        /// </summary>
        private static void DrawDirection(FuDrawList drawList, Vector2[] corners, Vector2 center, float radius, int orientation, Color color, float scale)
        {
            // The plan remains north-up, so cardinal arrows are unambiguous.
            Vector2 edge = orientation == 2 ? (corners[1] + corners[2]) * 0.5f :
                orientation == 3 ? (corners[2] + corners[3]) * 0.5f :
                orientation == 4 ? (corners[3] + corners[0]) * 0.5f : (corners[0] + corners[1]) * 0.5f;
            Vector2 direction = (edge - center).normalized;
            Vector2 tip = center + direction * radius;
            Vector2 side = new Vector2(-direction.y, direction.x) * 4f * scale;
            drawList.AddTriangleFilled(tip, tip - direction * 9f * scale + side, tip - direction * 9f * scale - side, Fugui.GetColorU32(color));
        }

        /// <summary>
        /// Shows the actual selected object, contextual foldouts, and persistent move/delete commands.
        /// </summary>
        private void DrawInspector(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            float x = rect.x + 15f * scale;
            float width = rect.width - 30f * scale;
            MapCreatorSelectionState selected = state.Selection;
            bool inspecting = selected.HasSelection && (state.Viewport.BrushMode == "Select" || state.Viewport.BrushMode == "Move" || state.Viewport.BrushMode == "Erase" || selected.IsEditingPath);
            bool choosing = !inspecting && (state.Viewport.BrushMode == "Select" || state.Viewport.BrushMode == "Pick" || state.Viewport.BrushMode == "Move");
            MapCreatorPaletteItem armed = FindItem(state.ActiveToolId);
            string id = inspecting ? !string.IsNullOrEmpty(selected.ElementId) ? selected.ElementId :
                !string.IsNullOrEmpty(selected.EnemyId) ? selected.EnemyId : selected.GearId != "None" ? selected.GearId : selected.FloorId : state.ActiveToolId;
            MapCreatorPaletteItem content = FindItem(id);
            string key = inspecting ? selected.X + ":" + selected.Y + ":" + id : "brush:" + id;
            if (_inspectedCell != key)
            {
                _inspectedCell = key;
                _inspectorScroller.Offset = 0f;
            }
            MapCreatorTheme.Heading(drawList, new Vector2(x, rect.y + 14f * scale), inspecting ? "Objet sélectionné" : choosing ? "Choisis une case" : "Bloc à poser");
            Rect preview = new Rect(x, rect.y + 47f * scale, 58f * scale, 58f * scale);
            drawList.AddRectFilled(preview.min, preview.max, new Color(0.025f, 0.02f, 0.018f), 4f * scale);
            Texture2D illustration = choosing ? MapCreatorTheme.GetTool("Select") : content?.Preview;
            if (illustration != null) drawList.AddImage(illustration, preview.min, preview.max, Vector4.one);
            string title = choosing ? "Sur la carte" : content != null ? content.Label : inspecting ? selected.CellType : "Sélection";
            Text(drawList, new Vector2(preview.xMax + 9f * scale, preview.y + 8f * scale),
                MapCreatorDrawListUi.TruncateText(title, width - 68f * scale, 14, FontType.Regular), DrawListUi.AccentBright, 14);
            Text(drawList, new Vector2(preview.xMax + 9f * scale, preview.y + 34f * scale),
                inspecting ? "Case " + selected.X + " : " + selected.Y : choosing ? "Clique pour inspecter" : "Pinceau actif", DrawListUi.TextSecondary, 12);
            Rect viewport = new Rect(x, preview.yMax + 16f * scale, width, rect.yMax - preview.yMax - (inspecting ? 78f : 32f) * scale);
            _inspectorScroller.Update(viewport, _inspectorContentHeight, !HasActiveModal());
            Rect clip = new Rect(viewport.x, viewport.y, viewport.width - 18f * scale, viewport.height);
            drawList.PushClipRect(clip.min, clip.max, true);
            MapCreatorDrawListUi.SetInteractionClip(clip);
            float y = viewport.y - _inspectorScroller.Offset;
            float usable = clip.width;
            if (!inspecting)
            {
                Text(drawList, new Vector2(x, y), choosing ? "Clique un objet sur la carte." : "Clique ou glisse pour placer.", DrawListUi.TextSecondary, 12);
                y += 34f * scale;
                if (!choosing && HasDirectionalSymbol(armed))
                {
                    if (InspectorSection(drawList, x, ref y, usable, "Orientation du pinceau", ref _showPlacement, scale))
                    {
                        Text(drawList, new Vector2(x + 5f * scale, y), DirectionLabel(state.Viewport.Orientation), DrawListUi.TextSecondary, 14);
                        y += 25f * scale;
                        if (Button(drawList, new Rect(x, y, usable, 33f * scale), "Pivoter · R")) _viewModel.Actions.RotatePlacement();
                        y += 45f * scale;
                    }
                }
            }
            else
            {
                bool orientable = HasDirectionalSymbol(content) || (content != null && content.Group == MapCreatorPaletteGroup.Decorations);
                if ((orientable || selected.CanScaleElement) && InspectorSection(drawList, x, ref y, usable, "Placement", ref _showPlacement, scale))
                {
                    if (orientable)
                    {
                        Text(drawList, new Vector2(x, y), "Orientation : " + selected.Orientation, DrawListUi.TextSecondary, 12);
                        y += 25f * scale;
                        string[] labels = { "Nord", "Est", "Sud", "Ouest" };
                        for (int i = 0; i < 4; i++)
                            if (Button(drawList, new Rect(x + i % 2 * (usable + 6f * scale) * 0.5f, y + i / 2 * 36f * scale, (usable - 6f * scale) * 0.5f, 31f * scale), labels[i]))
                                _viewModel.Actions.SetOrientation(i + 1);
                        y += 82f * scale;
                    }
                    if (selected.CanScaleElement)
                    {
                        Text(drawList, new Vector2(x, y), "Taille : " + selected.ElementScale.ToString("0.0"), DrawListUi.TextSecondary, 12);
                        y += 24f * scale;
                        if (Button(drawList, new Rect(x, y, usable * 0.48f, 31f * scale), "Réduire")) _viewModel.Actions.ScaleElement(-0.1f);
                        if (Button(drawList, new Rect(x + usable * 0.52f, y, usable * 0.48f, 31f * scale), "Agrandir")) _viewModel.Actions.ScaleElement(0.1f);
                        y += 46f * scale;
                    }
                }
                bool spring = selected.CellType == "Ressort";
                bool bomb = selected.CellType == "Bombe temporisée";
                bool spikes = selected.CellType == "Pics";
                if ((spring || bomb || spikes) && InspectorSection(drawList, x, ref y, usable, "Mécanique", ref _showMechanics, scale))
                {
                    if (spikes)
                    {
                        if (Button(drawList, new Rect(x, y, usable, 33f * scale), selected.ArgumentValue == 1 ? "Pics immédiats" : "Pics alternés"))
                            _viewModel.Actions.ToggleNeedle();
                        y += 45f * scale;
                    }
                    else
                    {
                        Text(drawList, new Vector2(x, y), (spring ? "Distance : " : "Délai : ") + selected.ArgumentValue + (spring ? " cases" : " tours"), DrawListUi.TextPrimary, 14);
                        y += 25f * scale;
                        if (Button(drawList, new Rect(x, y, usable * 0.48f, 31f * scale), "Diminuer")) _viewModel.Actions.AdjustArgument(-1);
                        if (Button(drawList, new Rect(x + usable * 0.52f, y, usable * 0.48f, 31f * scale), "Augmenter")) _viewModel.Actions.AdjustArgument(1);
                        y += 45f * scale;
                    }
                }
                if (selected.CanEditPath && InspectorSection(drawList, x, ref y, usable, "Trajet", ref _showRoute, scale))
                {
                    Text(drawList, new Vector2(x, y), selected.PathPointCount + " points · cases alignées", DrawListUi.TextSecondary, 12);
                    y += 26f * scale;
                    if (selected.IsEditingPath)
                    {
                        if (Button(drawList, new Rect(x, y, usable * 0.48f, 33f * scale), "Valider")) _viewModel.Actions.ConfirmPath();
                        if (Button(drawList, new Rect(x + usable * 0.52f, y, usable * 0.48f, 33f * scale), "Annuler")) _viewModel.Actions.CancelPath();
                    }
                    else if (Button(drawList, new Rect(x, y, usable, 33f * scale), "Dessiner le trajet"))
                        _viewModel.Actions.BeginPath();
                    y += 46f * scale;
                }
                Text(drawList, new Vector2(x, y), selected.CellType, DrawListUi.TextSecondary, 12);
                y += 30f * scale;
            }
            if (InspectorSection(drawList, x, ref y, usable, "Aide et raccourcis", ref _showHelp, scale))
            {
                foreach (string line in new[] { "Sol : pinceau ou rectangle.", "Place un départ et une sortie.", "Ajoute décors et obstacles.", "Molette : zoom.", "Clic droit : déplacer ou tourner.", "Gizmo : choisir un point de vue.", "S : sélectionner · R : pivoter.", "Ctrl+Z : annuler un geste." })
                {
                    Text(drawList, new Vector2(x, y), line, DrawListUi.TextSecondary, 12);
                    y += 25f * scale;
                }
            }
            _inspectorContentHeight = y + _inspectorScroller.Offset - viewport.y + 8f * scale;
            MapCreatorDrawListUi.SetInteractionClip(null);
            drawList.PopClipRect();
            _inspectorScroller.Draw(drawList, viewport, _inspectorContentHeight);
            if (inspecting)
            {
                float footer = rect.yMax - 51f * scale;
                bool movable = !string.IsNullOrEmpty(selected.ElementId) || !string.IsNullOrEmpty(selected.EnemyId) || selected.GearId != "None";
                if (Button(drawList, new Rect(x, footer, width * 0.48f, 35f * scale), "Déplacer", false, movable)) _viewModel.Actions.SetBrushMode("Move");
                if (Button(drawList, new Rect(x + width * 0.52f, footer, width * 0.48f, 35f * scale), "Effacer")) _viewModel.Actions.DeleteSelection();
            }
        }

        /// <summary>
        /// Draws a consistent property disclosure and advances the scroll-content cursor.
        /// </summary>
        private static bool InspectorSection(FuDrawList drawList, float x, ref float y, float width, string title, ref bool expanded, float scale)
        {
            if (MapCreatorDrawListUi.DrawDisclosure(drawList, new Rect(x, y, width, 31f * scale), title, expanded)) expanded = !expanded;
            y += 41f * scale;
            return expanded;
        }

        /// <summary>
        /// Shows the active placement rule, feedback, and directly relevant navigation hints.
        /// </summary>
        private void DrawStatusBar(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            // Text feedback explains invalid placement instead of leaving clicks silently ineffective.
            DrawWorkbenchPanel(drawList, rect, DrawListUi.Steel);
            string hint = !string.IsNullOrEmpty(_hoverDescription) ? _hoverDescription : state.Viewport.Hint;
            Text(drawList, rect.position + new Vector2(14f, 9f) * scale,
                MapCreatorDrawListUi.TruncateText(hint, rect.width * 0.72f, 14, FontType.Regular),
                state.Viewport.HoverValid ? DrawListUi.TextPrimary : DrawListUi.TextSecondary, 14);
            string saved = state.IsDirty ? "Modifications à enregistrer" : "Enregistrée";
            Vector2 size = DrawListUi.MeasureText(saved, 12);
            Text(drawList, new Vector2(rect.xMax - size.x - 14f * scale, rect.y + 10f * scale), saved,
                state.IsDirty ? DrawListUi.AccentBright : DrawListUi.TextSecondary, 12);
        }

        /// <summary>
        /// Finds authored preview data from a stable identifier.
        /// </summary>
        private MapCreatorPaletteItem FindItem(string id)
        {
            // Palette data is reused by plan tiles, placement ghosts, and inspector headings.
            return id != null && _catalogue.TryGetValue(id, out var item) ? item : null;
        }

        /// <summary>
        /// Converts cardinal identifiers into explicit orientation labels.
        /// </summary>
        private static string DirectionLabel(int direction)
        {
            // Labels remain readable when a font lacks arrow glyphs.
            return direction == 2 ? "Est" : direction == 3 ? "Sud" : direction == 4 ? "Ouest" : "Nord";
        }

        /// <summary>
        /// Presents legacy database group identifiers as understandable browsing categories.
        /// </summary>
        private static string FriendlyCategory(string category)
        {
            // Database classification remains intact behind player-facing labels.
            switch (category.ToLowerInvariant())
            {
                case "floor": return "Sols";
                case "wall": return "Murs";
                case "rock": return "Rochers";
                case "prop": return "Mobilier";
                case "floordeco": return "Petits décors";
                case "pillar": return "Colonnes";
                case "gear": return "Équipements";
                default: return category;
            }
        }

        #endregion

        #region Modals

        /// <summary>
        /// Creates a sized local map while explicitly protecting unsaved work.
        /// </summary>
        private void DrawNewMapModal(FuDrawList drawList, Vector2 screenSize, MapCreatorViewState state, float scale)
        {
            // A new project changes no saved map until the player explicitly saves it.
            DrawListUi.DrawScrim(drawList, screenSize, 0.72f);
            Rect panel = DrawListUi.CenterRect(screenSize, new Vector2(560f, 342f) * scale);
            DrawWorkbenchPanel(drawList, panel, DrawListUi.Accent, true);
            float x = panel.x + 26f * scale;
            float width = panel.width - 52f * scale;
            Text(drawList, new Vector2(x, panel.y + 24f * scale), "NOUVELLE CARTE", DrawListUi.TextPrimary, 18, FontType.Bold);
            Text(drawList, new Vector2(x, panel.y + 61f * scale), "Choisis une taille. Tous les blocs restent disponibles.", DrawListUi.TextSecondary, 14);
            int[] sizes = { 8, 12, 20, 32 };
            for (int i = 0; i < sizes.Length; i++)
                if (Button(drawList, new Rect(x + i * width * 0.25f, panel.y + 97f * scale, width * 0.25f - 6f * scale, 34f * scale),
                    sizes[i] + " x " + sizes[i], _newWidth == sizes[i] && _newHeight == sizes[i]))
                    _newWidth = _newHeight = sizes[i];
            Text(drawList, new Vector2(x, panel.y + 149f * scale), "Largeur : " + _newWidth, DrawListUi.TextPrimary, 14);
            Text(drawList, new Vector2(x + width * 0.52f, panel.y + 149f * scale), "Profondeur : " + _newHeight, DrawListUi.TextPrimary, 14);
            for (int i = 0; i < 4; i++)
                if (Button(drawList, new Rect(x + i * width * 0.25f, panel.y + 178f * scale, width * 0.25f - 6f * scale, 31f * scale),
                    i % 2 == 0 ? "Diminuer" : "Augmenter"))
                {
                    if (i < 2)
                        _newWidth = Mathf.Clamp(_newWidth + (i == 0 ? -1 : 1), 4, 64);
                    else
                        _newHeight = Mathf.Clamp(_newHeight + (i == 2 ? -1 : 1), 4, 64);
                }
            Text(drawList, new Vector2(x, panel.y + 231f * scale), state.IsDirty
                ? "La carte actuelle contient des modifications non enregistrées."
                : "La carte actuelle reste disponible dans tes sauvegardes locales.", DrawListUi.TextSecondary, 12);
            float buttonWidth = (width - 12f * scale) / 3f;
            if (Button(drawList, new Rect(x, panel.yMax - 64f * scale, buttonWidth, 38f * scale), "Annuler"))
                _showNewMap = false;
            if (Button(drawList, new Rect(x + buttonWidth + 6f * scale, panel.yMax - 64f * scale, buttonWidth, 38f * scale), "Créer sans sauver"))
            {
                _viewModel.Actions.CreateMap(_newWidth, _newHeight, false);
                _showNewMap = false;
            }
            if (Button(drawList, new Rect(x + 2f * (buttonWidth + 6f * scale), panel.yMax - 64f * scale, buttonWidth, 38f * scale), "Sauver et créer", true))
            {
                _viewModel.Actions.CreateMap(_newWidth, _newHeight, state.IsDirty);
                _showNewMap = false;
            }
        }

        /// <summary>
        /// Draws the currently active modal above all editor surfaces.
        /// </summary>
        private void DrawActiveModal(FuDrawList drawList, Vector2 screenSize, MapCreatorViewState state, float scale)
        {
            if (_showNewMap)
            {
                DrawNewMapModal(drawList, screenSize, state, scale);
                return;
            }
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
            if (!string.IsNullOrEmpty(_activeEnvironmentColorId))
            {
                DrawListUi.DrawScrim(drawList, screenSize, 0.66f);
                DrawEnvironmentColorEditor(drawList, screenSize, scale);
            }
        }

        /// <summary>
        /// Reports whether one Fugui modal currently owns editor interaction.
        /// </summary>
        private bool HasActiveModal()
        {
            // Environment editing is docked; only its color picker and actual dialog overlays capture the board.
            return _showNewMap || _showExitPrompt ||
                   _editingName ||
                   _showValidation ||
                   !string.IsNullOrEmpty(_activeEnvironmentColorId);
        }

        /// <summary>
        /// Draws save, discard, and cancel choices before leaving a dirty map.
        /// </summary>
        private void DrawExitPrompt(FuDrawList drawList, Vector2 screenSize, float scale)
        {
            // Explicit three-way wording prevents accidental loss of authored work.
            DrawListUi.DrawScrim(drawList, screenSize, 0.72f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(590f, 270f) * scale);
            DrawWorkbenchPanel(
                drawList,
                panel,
                DrawListUi.Danger,
                true);
            Text(
                drawList,
                panel.position + new Vector2(30f, 29f) * scale,
                "FERMER LA CARTE ?",
                DrawListUi.TextPrimary,
                17,
                FontType.Bold);
            Text(
                drawList,
                panel.position + new Vector2(30f, 69f) * scale,
                "Cette carte contient des modifications non enregistrées.",
                DrawListUi.TextSecondary,
                11);
            Text(
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
        private void DrawNameEditor(FuDrawList drawList, Vector2 screenSize, float scale)
        {
            // Naming is modal so board shortcuts cannot mutate the map while the user types.
            DrawListUi.DrawScrim(drawList, screenSize, 0.68f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(560f, 220f) * scale);
            DrawWorkbenchPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);
            Text(
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
        private void DrawValidationModal(FuDrawList drawList, Vector2 screenSize, MapCreatorViewState state, float scale)
        {
            // Validation separates actionable blocking errors from the normal authoring workflow.
            DrawListUi.DrawScrim(drawList, screenSize, 0.7f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(680f, 510f) * scale);
            Color accent = state.ValidationErrors.Count == 0
                ? DrawListUi.Success
                : DrawListUi.Danger;
            DrawWorkbenchPanel(drawList, panel, accent, true);
            Text(
                drawList,
                panel.position + new Vector2(30f, 27f) * scale,
                state.ValidationErrors.Count == 0
                    ? "CARTE PRÊTE À TESTER"
                    : "VALIDATION DE LA CARTE",
                DrawListUi.TextPrimary,
                17,
                FontType.Bold);
            Text(
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
                    Text(
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
        /// Edits map lighting in a docked sidebar so presets and live tuning never hide the actual board.
        /// </summary>
        private void DrawDockedEnvironment(FuDrawList drawList, Rect rect, MapCreatorViewState state, float scale)
        {
            float x = rect.x + 15f * scale;
            float width = rect.width - 30f * scale;
            MapCreatorTheme.Heading(drawList, new Vector2(x, rect.y + 7f * scale), "Ambiance de la carte");
            Text(drawList, new Vector2(x, rect.y + 37f * scale), state.EnvironmentLabel, DrawListUi.AccentBright, 14);
            Text(drawList, new Vector2(x, rect.y + 65f * scale), state.Viewport.ShowEnvironment
                ? "Aperçu : rendu du jeu" : "Aperçu : lumière de travail", DrawListUi.TextSecondary, 12);
            Text(drawList, new Vector2(x, rect.y + 86f * scale), "Choisir une palette affiche son rendu.", DrawListUi.TextSecondary, 12);
            Rect viewport = new Rect(x, rect.y + 119f * scale, width, rect.height - 134f * scale);
            var environments = GetFilteredItems(MapCreatorPaletteGroup.Environments, string.Empty);
            List<string> sections = GetEnvironmentSections(state);
            float advancedHeight = 0f;
            if (_showAdvancedEnvironment)
            {
                advancedHeight = sections.Count * 41f * scale;
                foreach (MapCreatorEnvironmentValue value in state.EnvironmentValues)
                    if (_expandedEnvironmentSections.Contains(value.Section)) advancedHeight += 66f * scale;
                foreach (MapCreatorEnvironmentColor color in state.EnvironmentColors)
                    if (_expandedEnvironmentSections.Contains(color.Section)) advancedHeight += 52f * scale;
            }
            float height = Mathf.CeilToInt(environments.Count / 2f) * 96f * scale + 76f * scale + advancedHeight;
            _environmentScroller.Update(viewport, height, !HasActiveModal());
            Rect clip = new Rect(viewport.x, viewport.y, viewport.width - 18f * scale, viewport.height);
            drawList.PushClipRect(clip.min, clip.max, true);
            MapCreatorDrawListUi.SetInteractionClip(clip);
            float y = viewport.y - _environmentScroller.Offset;
            Text(drawList, new Vector2(x, y), "PALETTES D’AMBIANCE", DrawListUi.TextSecondary, 12);
            y += 26f * scale;
            float cardWidth = (clip.width - 8f * scale) * 0.5f;
            for (int i = 0; i < environments.Count; i++)
            {
                MapCreatorPaletteItem environment = environments[i];
                Rect choice = new Rect(x + i % 2 * (cardWidth + 8f * scale), y + i / 2 * 96f * scale, cardWidth, 88f * scale);
                if (choice.yMax < clip.y || choice.y > clip.yMax) continue;
                if (DrawEnvironmentCard(drawList, choice, environment, state.EnvironmentId == environment.Id, scale))
                {
                    // Applying a palette is an explicit preview action; opening this panel is not.
                    _viewModel.Actions.SelectEnvironment(environment.Id);
                    _viewModel.Actions.SetEnvironmentPreview(true);
                }
            }
            y += Mathf.CeilToInt(environments.Count / 2f) * 96f * scale;
            if (MapCreatorDrawListUi.DrawDisclosure(drawList, new Rect(x, y, clip.width, 32f * scale), "Réglages détaillés", _showAdvancedEnvironment))
                _showAdvancedEnvironment = !_showAdvancedEnvironment;
            y += 50f * scale;
            if (_showAdvancedEnvironment)
            {
                foreach (string section in sections)
                {
                    bool expanded = _expandedEnvironmentSections.Contains(section);
                    if (MapCreatorDrawListUi.DrawDisclosure(drawList, new Rect(x, y, clip.width, 31f * scale), section, expanded))
                    {
                        if (expanded) _expandedEnvironmentSections.Remove(section);
                        else _expandedEnvironmentSections.Add(section);
                        expanded = !expanded;
                    }
                    y += 41f * scale;
                    if (!expanded) continue;
                    foreach (MapCreatorEnvironmentValue value in state.EnvironmentValues)
                    {
                        if (value.Section != section) continue;
                        Rect slider = new Rect(x, y + 20f * scale, clip.width, 25f * scale);
                        if (slider.yMax >= clip.y && slider.y - 20f * scale <= clip.yMax &&
                            MapCreatorDrawListUi.DrawSlider(drawList, slider, value.Label, value.Value, value.Minimum, value.Maximum, value.Format, DrawListUi.Accent, out float changed))
                            _viewModel.Actions.SetEnvironmentValue(value.Id, changed);
                        y += 66f * scale;
                    }
                    foreach (MapCreatorEnvironmentColor color in state.EnvironmentColors)
                    {
                        if (color.Section != section) continue;
                        Rect swatch = new Rect(x, y, clip.width, 44f * scale);
                        if (swatch.yMax >= clip.y && swatch.y <= clip.yMax &&
                            MapCreatorDrawListUi.DrawColorSwatch(drawList, swatch, color.Label, color.Value, DrawListUi.Accent))
                        {
                            _activeEnvironmentColorId = color.Id;
                            _activeEnvironmentColorLabel = color.Label;
                            _environmentColorDraft = color.Value;
                        }
                        y += 52f * scale;
                    }
                }
            }
            MapCreatorDrawListUi.SetInteractionClip(null);
            drawList.PopClipRect();
            _environmentScroller.Draw(drawList, viewport, height);
        }

        /// <summary>
        /// Collects the authored parameter sections in their stable display order.
        /// </summary>
        private static List<string> GetEnvironmentSections(MapCreatorViewState state)
        {
            var sections = new List<string>();
            foreach (MapCreatorEnvironmentValue value in state.EnvironmentValues)
                if (!sections.Contains(value.Section)) sections.Add(value.Section);
            foreach (MapCreatorEnvironmentColor color in state.EnvironmentColors)
                if (!sections.Contains(color.Section)) sections.Add(color.Section);
            return sections;
        }

        /// <summary>
        /// Draws authored sky and ground colors as a selectable palette rather than a simulated map screenshot.
        /// </summary>
        private static bool DrawEnvironmentCard(FuDrawList drawList, Rect rect, MapCreatorPaletteItem item, bool selected, float scale)
        {
            bool hovered = MapCreatorDrawListUi.IsHovered(rect);
            MapCreatorTheme.Surface(drawList, rect);
            Rect palette = new Rect(rect.x + 7f * scale, rect.y + 7f * scale, rect.width - 14f * scale, 45f * scale);
            MapCreatorEnvironmentPreview colors = item.EnvironmentPreview;
            if (colors != null)
            {
                uint sky = Fugui.GetColorU32(colors.Sky);
                uint horizon = Fugui.GetColorU32(colors.Horizon);
                drawList.AddRectFilledMultiColor(palette.min, palette.max, sky, sky, horizon, horizon);
                drawList.AddRectFilled(new Vector2(palette.x, palette.yMax - 9f * scale), palette.max, colors.Ground);
                drawList.AddCircleFilled(new Vector2(palette.xMax - 13f * scale, palette.y + 12f * scale), 5f * scale, colors.Sun);
            }
            Text(drawList, new Vector2(rect.x + 9f * scale, rect.y + 61f * scale), item.Label, selected ? DrawListUi.AccentBright : DrawListUi.TextPrimary, 14);
            if (selected || hovered)
                drawList.AddRect(rect.min, rect.max, selected ? DrawListUi.AccentBright : DrawListUi.Steel, 4f * scale, FuDrawFlags.RoundCornersAll, (selected ? 2f : 1f) * scale);
            if (selected)
                drawList.AddCircleFilled(new Vector2(rect.xMax - 9f * scale, rect.y + 64f * scale), 3f * scale, DrawListUi.AccentBright);
            return MapCreatorDrawListUi.WasClicked(rect);
        }

        /// <summary>
        /// Draws RGB channel controls for one selected environment color.
        /// </summary>
        private void DrawEnvironmentColorEditor(FuDrawList drawList, Vector2 screenSize, float scale)
        {
            // Live channel updates make color tuning immediately visible on the authored scene.
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(560f, 420f) * scale);
            DrawWorkbenchPanel(
                drawList,
                panel,
                DrawListUi.Accent,
                true);
            Text(
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
        private void DrawColorChannel(FuDrawList drawList, Rect rect, string label, float value, Color accent, int channel)
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
        private static void DrawBusyState(FuDrawList drawList, Vector2 screenSize, MapCreatorViewState state, float scale)
        {
            // Busy feedback blocks repeated destructive actions while keeping progress explicit.
            DrawListUi.DrawScrim(drawList, screenSize, 0.62f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(420f, 156f) * scale);
            DrawWorkbenchPanel(
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
            Text(
                drawList,
                panel.position + new Vector2(112f, 48f) * scale,
                "CHARGEMENT",
                DrawListUi.TextPrimary,
                14,
                FontType.Bold);
            Text(
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
            string previous = _search;
            AppendTextInput(
                ref _search,
                MaximumSearchLength);
            if (previous != _search) _paletteScroller.Offset = 0f;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_search.Length > 0)
                {
                    _search = string.Empty;
                    _paletteScroller.Offset = 0f;
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
        private static void AppendTextInput(ref string value, int maximumLength)
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
            if (_showNewMap)
                _showNewMap = false;
            else if (_showExitPrompt)
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
        private void UpdatePointerGuard(Rect top, Rect left, Rect right, Rect bottom, Vector2 screenSize)
        {
            // Modal scrims consume the full pointer surface; normal editing leaves the center viewport free.
            Vector2 pointer =
                Fugui.DefaultContainer.Mouse.Position;
            bool modal =
                _showNewMap || _showExitPrompt ||
                _editingName ||
                _showValidation ||
                !string.IsNullOrEmpty(_activeEnvironmentColorId);
            bool overUi =
                modal && new Rect(Vector2.zero, screenSize).Contains(pointer) ||
                top.Contains(pointer) ||
                GetViewportToolbar(screenSize, DrawListUi.GetScale()).Contains(pointer) ||
                left.Contains(pointer) ||
                right.Contains(pointer) ||
                GetGizmoRect(screenSize, DrawListUi.GetScale()).Contains(pointer) ||
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
        private static Rect PlaceToolbarButton(ref float rightEdge, float y, float width, float height, float gap)
        {
            // Right-aligned command placement remains stable as map-name space flexes.
            rightEdge -= width;
            Rect rect = new Rect(rightEdge, y, width, height);
            rightEdge -= gap;
            return rect;
        }

        /// <summary>
        /// Reserves the document-command row across the complete window.
        /// </summary>
        private static Rect GetTopBar(Vector2 screenSize, float scale)
        {
            // A stable command row avoids crowding the camera controls.
            return new Rect(8f * scale, 8f * scale, screenSize.x - 16f * scale, 64f * scale);
        }

        /// <summary>
        /// Allocates readable catalogue space while keeping the board central.
        /// </summary>
        private static Rect GetPalettePanel(Vector2 screenSize, float scale)
        {
            // Width scales consistently with the project's reference resolution.
            return new Rect(8f * scale, 80f * scale, 304f * scale, screenSize.y - 130f * scale);
        }

        /// <summary>
        /// Allocates only the width needed by contextual object properties.
        /// </summary>
        private static Rect GetInspectorPanel(Vector2 screenSize, float scale)
        {
            // A fixed-width inspector keeps controls predictable as selection changes.
            return new Rect(screenSize.x - 328f * scale, 80f * scale, 320f * scale, screenSize.y - 130f * scale);
        }

        /// <summary>
        /// Places the always-visible feedback strip along the lower edge.
        /// </summary>
        private static Rect GetStatusBar(Vector2 screenSize, float scale)
        {
            // Status includes the reason an operation is accepted or rejected.
            return new Rect(8f * scale, screenSize.y - 42f * scale, screenSize.x - 16f * scale, 34f * scale);
        }

        /// <summary>
        /// Reserves two compact rows for gestures and camera modes above the board.
        /// </summary>
        private static Rect GetViewportToolbar(Vector2 screenSize, float scale)
        {
            // Gestures use explicit labels rather than undocumented mouse combinations.
            return new Rect(320f * scale, 80f * scale, screenSize.x - 656f * scale, 66f * scale);
        }

        /// <summary>
        /// Reserves a compact camera compass in the upper-right corner of the visible board.
        /// </summary>
        private static Rect GetGizmoRect(Vector2 screenSize, float scale)
        {
            Rect board = GetBoardViewport(screenSize, scale);
            return new Rect(board.xMax - 152f * scale, board.y + 8f * scale, 144f * scale, 208f * scale);
        }

        /// <summary>
        /// Computes the unobstructed board viewport beneath the gesture toolbar.
        /// </summary>
        public static Rect GetBoardViewport(Vector2 screenSize, float scale)
        {
            // Clipping prevents grid feedback from being drawn over panels.
            float top = GetViewportToolbar(screenSize, scale).yMax + 8f * scale;
            return new Rect(320f * scale, top, screenSize.x - 656f * scale, screenSize.y - top - 50f * scale);
        }

        #endregion
    }
}

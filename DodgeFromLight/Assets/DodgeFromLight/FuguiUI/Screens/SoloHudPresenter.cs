using DodgeFromLight.Application.Game;
using DodgeFromLight.FuguiUI.Rendering;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Screens
{
    /// <summary>
    /// Renders the complete solo runtime interface on Fugui's foreground DrawList.
    /// </summary>
    public sealed class SoloHudPresenter : MonoBehaviour
    {
        private SoloHudViewModel _viewModel;
        private FuContext _context;
        private int _pauseSelection;
        private int _resultSelection;
        private bool _wasPaused;
        private GameSessionStatus _previousStatus = GameSessionStatus.Running;

        #region Lifecycle

        /// <summary>
        /// Binds the presenter and subscribes it to the Main Fugui context render event.
        /// </summary>
        public bool Initialize(SoloHudViewModel viewModel)
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
        /// Handles keyboard navigation for modal DrawList states.
        /// </summary>
        private void Update()
        {
            // Gameplay shortcuts remain owned by GameManager; only modal selection lives here.
            if (_viewModel == null)
                return;

            bool paused = _viewModel.IsPaused();
            if (paused)
            {
                if (!_wasPaused)
                    _pauseSelection = 0;
                HandlePauseNavigation();
                _wasPaused = true;
                return;
            }

            _wasPaused = false;
            GameSessionReadModel state = _viewModel.GetState();
            if (state == null || state.Status == GameSessionStatus.Running)
            {
                _previousStatus = GameSessionStatus.Running;
                return;
            }

            if (_previousStatus != state.Status)
                _resultSelection = 0;
            _previousStatus = state.Status;
            HandleResultNavigation(state);
        }

        /// <summary>
        /// Releases the Main context render subscription when the gameplay scene is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // The stored context remains safe to unsubscribe even during Fugui shutdown.
            if (_context != null)
                _context.OnRender -= Render;
            _context = null;
        }

        /// <summary>
        /// Draws one complete immutable solo UI snapshot on the foreground DrawList.
        /// </summary>
        private void Render()
        {
            // No window or layout item participates in the runtime HUD.
            if (_viewModel == null || Fugui.CurrentContext != _context)
                return;

            FuDrawList drawList = Fugui.GetForegroundDrawList();
            Vector2 screenSize = DrawListUi.GetScreenSize();
            string statusMessage = _viewModel.GetStatusMessage();
            GameSessionReadModel state = _viewModel.GetState();
            if (state == null)
            {
                DrawLoading(drawList, screenSize, statusMessage);
                return;
            }

            if (_viewModel.IsPaused())
            {
                DrawPause(drawList, screenSize);
                return;
            }

            if (state.Status == GameSessionStatus.Running)
                DrawRunningHud(drawList, screenSize, state);
            else
                DrawResult(drawList, screenSize, state);

            if (!string.IsNullOrWhiteSpace(statusMessage))
                DrawStatusMessage(drawList, screenSize, statusMessage);
        }

        #endregion

        #region Running HUD

        /// <summary>
        /// Draws the non-modal information, equipment, actions, and tutorial guidance.
        /// </summary>
        private void DrawRunningHud(
            FuDrawList drawList,
            Vector2 screenSize,
            GameSessionReadModel state)
        {
            // The board remains visually dominant while each HUD cluster keeps a single purpose.
            float scale = DrawListUi.GetScale();
            float margin = 28f * scale;
            DrawRunInformation(drawList, state, margin, scale);
            DrawGearPanel(drawList, screenSize, state, margin, scale);
            DrawRunActions(drawList, screenSize, state, margin, scale);
            DrawContextPrompt(drawList, screenSize, state, margin, scale);
            if (_viewModel.IsTutorial())
                DrawTutorialGuide(drawList, screenSize, margin, scale);
        }

        /// <summary>
        /// Draws the map title, active time, turn count, and temporary effects.
        /// </summary>
        private void DrawRunInformation(
            FuDrawList drawList,
            GameSessionReadModel state,
            float margin,
            float scale)
        {
            // Turn count and active time are the two primary optimization metrics.
            int effectCount =
                (state.InvisibilityTurns > 0 ? 1 : 0) +
                (state.TimeStopTurns > 0 ? 1 : 0) +
                (state.SpeedBoostTurns > 0 ? 1 : 0);
            float panelHeight = (effectCount > 0 ? 150f : 116f) * scale;
            Rect panel = new Rect(margin, margin, 390f * scale, panelHeight);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent);

            string mapTitle = string.IsNullOrWhiteSpace(state.MapTitle)
                ? state.MapId
                : state.MapTitle;
            Vector2 content = panel.position + new Vector2(20f, 18f) * scale;
            DrawListUi.DrawText(
                drawList,
                content,
                mapTitle,
                DrawListUi.TextPrimary,
                18,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                content + new Vector2(0f, 34f) * scale,
                "TOUR " + state.TurnIndex,
                DrawListUi.TextSecondary,
                12,
                FontType.Bold);
            DrawListUi.DrawText(
                drawList,
                content + new Vector2(126f, 29f) * scale,
                _viewModel.GetElapsedLabel(),
                DrawListUi.TextPrimary,
                18,
                FontType.Bold);
            drawList.AddCircleFilled(
                content + new Vector2(6f, 72f) * scale,
                4f * scale,
                DrawListUi.Success);
            DrawListUi.DrawText(
                drawList,
                content + new Vector2(18f, 64f) * scale,
                "TENTATIVE EN COURS",
                DrawListUi.TextSecondary,
                12,
                FontType.Bold);

            if (effectCount > 0)
            {
                DrawActiveEffects(
                    drawList,
                    content + new Vector2(0f, 94f) * scale,
                    state,
                    scale);
            }
        }

        /// <summary>
        /// Draws active turn-limited effects as compact tags.
        /// </summary>
        private static void DrawActiveEffects(
            FuDrawList drawList,
            Vector2 position,
            GameSessionReadModel state,
            float scale)
        {
            // Each effect exposes its deterministic remaining duration.
            float x = position.x;
            if (state.InvisibilityTurns > 0)
            {
                x += DrawListUi.DrawTag(
                    drawList,
                    new Vector2(x, position.y),
                    "INVISIBLE · " + state.InvisibilityTurns,
                    new Color(0.64f, 0.7f, 1f, 1f)) + 8f * scale;
            }

            if (state.TimeStopTurns > 0)
            {
                x += DrawListUi.DrawTag(
                    drawList,
                    new Vector2(x, position.y),
                    "TEMPS · " + state.TimeStopTurns,
                    new Color(0.52f, 0.9f, 1f, 1f)) + 8f * scale;
            }

            if (state.SpeedBoostTurns > 0)
            {
                DrawListUi.DrawTag(
                    drawList,
                    new Vector2(x, position.y),
                    "VITESSE · " + state.SpeedBoostTurns,
                    DrawListUi.Success);
            }
        }

        /// <summary>
        /// Draws the two equipment slots in the lower-left HUD cluster.
        /// </summary>
        private void DrawGearPanel(
            FuDrawList drawList,
            Vector2 screenSize,
            GameSessionReadModel state,
            float margin,
            float scale)
        {
            // Inventory slots remain readable without obscuring the center of the puzzle.
            Rect panel = new Rect(
                margin,
                screenSize.y - margin - 132f * scale,
                390f * scale,
                132f * scale);
            DrawListUi.DrawPanel(drawList, panel, new Color(0.38f, 0.76f, 1f, 1f));
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(18f, 14f) * scale,
                "ÉQUIPEMENT",
                DrawListUi.TextSecondary,
                12,
                FontType.Bold);
            DrawGearSlot(
                drawList,
                panel.position + new Vector2(18f, 42f) * scale,
                _viewModel.GearSlotOneBinding,
                _viewModel.GetGearLabel(state.GearSlotOne),
                scale);
            DrawGearSlot(
                drawList,
                panel.position + new Vector2(18f, 84f) * scale,
                _viewModel.GearSlotTwoBinding,
                _viewModel.GetGearLabel(state.GearSlotTwo),
                scale);
        }

        /// <summary>
        /// Draws one equipment binding and its current content label.
        /// </summary>
        private static void DrawGearSlot(
            FuDrawList drawList,
            Vector2 position,
            string binding,
            string gearLabel,
            float scale)
        {
            // A compact key tile visually separates the action binding from the gear name.
            Rect keyRect = new Rect(position, new Vector2(54f, 30f) * scale);
            DrawListUi.DrawKey(drawList, keyRect, binding, false);
            DrawListUi.DrawText(
                drawList,
                position + new Vector2(70f, 6f) * scale,
                gearLabel,
                gearLabel == "Vide"
                    ? DrawListUi.TextSecondary
                    : DrawListUi.TextPrimary,
                14,
                gearLabel == "Vide" ? FontType.Regular : FontType.Bold);
        }

        /// <summary>
        /// Draws pause, undo, and restart actions in the upper-right corner.
        /// </summary>
        private void DrawRunActions(
            FuDrawList drawList,
            Vector2 screenSize,
            GameSessionReadModel state,
            float margin,
            float scale)
        {
            // Explicit click targets complement the established keyboard shortcuts.
            float y = margin;
            float pauseWidth = 112f * scale;
            float undoWidth = 158f * scale;
            float restartWidth = 184f * scale;
            float gap = 10f * scale;
            float right = screenSize.x - margin;

            Rect pauseRect = new Rect(
                right - pauseWidth,
                y,
                pauseWidth,
                46f * scale);
            right = pauseRect.x - gap;
            Rect restartRect = new Rect(
                right - restartWidth,
                y,
                restartWidth,
                46f * scale);
            right = restartRect.x - gap;
            Rect undoRect = new Rect(
                right - undoWidth,
                y,
                undoWidth,
                46f * scale);

            if (DrawListUi.DrawButton(
                    drawList,
                    undoRect,
                    "Annuler",
                    _viewModel.UndoBinding,
                    new Color(0.38f, 0.76f, 1f, 1f),
                    _viewModel.CanUndo(state)))
            {
                _viewModel.TryUndo();
            }

            if (DrawListUi.DrawButton(
                    drawList,
                    restartRect,
                    "Recommencer",
                    _viewModel.RestartBinding,
                    DrawListUi.Danger,
                    _viewModel.CanRestart(state)))
            {
                _viewModel.TryRestart();
            }

            if (DrawListUi.DrawButton(
                    drawList,
                    pauseRect,
                    "Pause",
                    "Échap",
                    DrawListUi.Accent))
            {
                _viewModel.TryPause();
            }
        }

        /// <summary>
        /// Draws the highest-priority contextual interaction hint.
        /// </summary>
        private void DrawContextPrompt(
            FuDrawList drawList,
            Vector2 screenSize,
            GameSessionReadModel state,
            float margin,
            float scale)
        {
            // Pick-up takes priority because either inventory slot can resolve it.
            string prompt = string.Empty;
            if (state.CanPickUpGear)
            {
                prompt =
                    "RAMASSER · " +
                    _viewModel.GearSlotOneBinding +
                    " OU " +
                    _viewModel.GearSlotTwoBinding;
            }
            else if (state.CanActivateCheckpoint)
            {
                prompt = "ACTIVER LE CHECKPOINT · " + _viewModel.CheckpointBinding;
            }
            else if (state.HasCheckpoint)
            {
                prompt = "CHECKPOINT ACTIF";
            }

            if (string.IsNullOrEmpty(prompt))
                return;

            Vector2 textSize = DrawListUi.MeasureText(prompt, 12, FontType.Bold);
            Rect panel = new Rect(
                (screenSize.x - textSize.x - 40f * scale) * 0.5f,
                _viewModel.IsTutorial()
                    ? screenSize.y - margin - 180f * scale
                    : screenSize.y - margin - 52f * scale,
                textSize.x + 40f * scale,
                44f * scale);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent, true);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 14f * scale),
                panel.width,
                prompt,
                DrawListUi.Accent,
                12,
                FontType.Bold);
        }

        /// <summary>
        /// Draws tutorial movement bindings and live pressed-key feedback.
        /// </summary>
        private void DrawTutorialGuide(
            FuDrawList drawList,
            Vector2 screenSize,
            float margin,
            float scale)
        {
            // A single compact guide replaces every gear-specific uGUI tutorial panel.
            Rect panel = new Rect(
                (screenSize.x - 560f * scale) * 0.5f,
                screenSize.y - margin - 126f * scale,
                560f * scale,
                126f * scale);
            DrawListUi.DrawPanel(drawList, panel, new Color(0.38f, 0.76f, 1f, 1f));
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(18f, 14f) * scale,
                "GUIDE TUTORIEL",
                DrawListUi.TextSecondary,
                12,
                FontType.Bold);

            SoloMovementInputState input = _viewModel.GetMovementInputState();
            float keyY = panel.y + 46f * scale;
            float keyWidth = 68f * scale;
            float keyHeight = 42f * scale;
            float gap = 10f * scale;
            float x = panel.x + 18f * scale;
            DrawListUi.DrawKey(
                drawList,
                new Rect(x, keyY, keyWidth, keyHeight),
                _viewModel.LeftBinding,
                input.Left);
            x += keyWidth + gap;
            DrawListUi.DrawKey(
                drawList,
                new Rect(x, keyY, keyWidth, keyHeight),
                _viewModel.ForwardBinding,
                input.Forward);
            x += keyWidth + gap;
            DrawListUi.DrawKey(
                drawList,
                new Rect(x, keyY, keyWidth, keyHeight),
                _viewModel.BackwardBinding,
                input.Backward);
            x += keyWidth + gap;
            DrawListUi.DrawKey(
                drawList,
                new Rect(x, keyY, keyWidth, keyHeight),
                _viewModel.RightBinding,
                input.Right);
            x += keyWidth + gap;
            DrawListUi.DrawKey(
                drawList,
                new Rect(x, keyY, 136f * scale, keyHeight),
                _viewModel.JumpBinding,
                input.Jump);

            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(18f, 98f) * scale,
                "Déplacement case par case · cherche la route la plus courte.",
                DrawListUi.TextSecondary,
                12);
        }

        #endregion

        #region Modal states

        /// <summary>
        /// Draws terminal attempt metrics and explicit post-level actions.
        /// </summary>
        private void DrawResult(
            FuDrawList drawList,
            Vector2 screenSize,
            GameSessionReadModel state)
        {
            // Results separate deterministic action count from active execution time.
            float scale = DrawListUi.GetScale();
            DrawListUi.DrawScrim(drawList, screenSize, 0.76f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(540f, 450f) * scale);
            Color accent = state.Status == GameSessionStatus.Won
                ? DrawListUi.Success
                : DrawListUi.Danger;
            DrawListUi.DrawPanel(drawList, panel, accent, true);

            string title = state.Status == GameSessionStatus.Won
                ? "NIVEAU TERMINÉ"
                : "TENTATIVE ÉCHOUÉE";
            string mapTitle = string.IsNullOrWhiteSpace(state.MapTitle)
                ? state.MapId
                : state.MapTitle;
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 34f * scale),
                panel.width,
                title,
                accent,
                20,
                FontType.Bold);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 72f * scale),
                panel.width,
                mapTitle,
                DrawListUi.TextSecondary,
                14);

            DrawMetric(
                drawList,
                new Rect(
                    panel.x + 34f * scale,
                    panel.y + 112f * scale,
                    222f * scale,
                    82f * scale),
                "ACTIONS",
                state.TurnIndex.ToString(),
                accent,
                scale);
            DrawMetric(
                drawList,
                new Rect(
                    panel.x + 284f * scale,
                    panel.y + 112f * scale,
                    222f * scale,
                    82f * scale),
                "TEMPS ACTIF",
                _viewModel.GetElapsedLabel(),
                accent,
                scale);

            float buttonX = panel.x + 34f * scale;
            float buttonWidth = panel.width - 68f * scale;
            float buttonHeight = 56f * scale;
            float buttonY = panel.y + 220f * scale;
            string restartLabel = state.Status == GameSessionStatus.Won
                ? "Rejouer"
                : "Recommencer";
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    restartLabel,
                    _viewModel.RestartBinding,
                    DrawListUi.Danger,
                    _viewModel.CanRestart(state),
                    _resultSelection == 0))
            {
                _viewModel.TryRestart();
            }

            int menuSelection = 1;
            buttonY += 68f * scale;
            if (state.Status == GameSessionStatus.Won)
            {
                if (DrawListUi.DrawButton(
                        drawList,
                        new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                        _viewModel.GetContinueLabel(),
                        _viewModel.ContinueBinding,
                        DrawListUi.Success,
                        _viewModel.CanContinue(state),
                        _resultSelection == 1))
                {
                    _viewModel.TryContinue();
                }
                menuSelection = 2;
                buttonY += 68f * scale;
            }

            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "Menu principal",
                    string.Empty,
                    DrawListUi.Accent,
                    true,
                    _resultSelection == menuSelection))
            {
                _viewModel.TryLeave();
            }
        }

        /// <summary>
        /// Draws one result metric card.
        /// </summary>
        private static void DrawMetric(
            FuDrawList drawList,
            Rect rect,
            string label,
            string value,
            Color accent,
            float scale)
        {
            // Equal-size cards make action count and active time visually comparable.
            drawList.AddRectFilled(
                rect.min,
                rect.max,
                new Color(0.06f, 0.075f, 0.085f, 0.94f),
                9f * scale);
            drawList.AddRect(
                rect.min,
                rect.max,
                new Color(accent.r, accent.g, accent.b, 0.28f),
                9f * scale);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(rect.x, rect.y + 14f * scale),
                rect.width,
                label,
                DrawListUi.TextSecondary,
                12,
                FontType.Bold);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(rect.x, rect.y + 40f * scale),
                rect.width,
                value,
                DrawListUi.TextPrimary,
                18,
                FontType.Bold);
        }

        /// <summary>
        /// Draws the pause overlay and its local-session navigation actions.
        /// </summary>
        private void DrawPause(FuDrawList drawList, Vector2 screenSize)
        {
            // Pause is a true modal surface and captures pointer intent over the board.
            float scale = DrawListUi.GetScale();
            DrawListUi.DrawScrim(drawList, screenSize, 0.82f);
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(520f, 390f) * scale);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent, true);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 42f * scale),
                panel.width,
                "PAUSE",
                DrawListUi.Accent,
                20,
                FontType.Bold);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 82f * scale),
                panel.width,
                "La tentative et le chronomètre sont suspendus.",
                DrawListUi.TextSecondary,
                14);

            float buttonX = panel.x + 38f * scale;
            float buttonY = panel.y + 132f * scale;
            float buttonWidth = panel.width - 76f * scale;
            float buttonHeight = 58f * scale;
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "Reprendre",
                    "Échap",
                    DrawListUi.Success,
                    true,
                    _pauseSelection == 0))
            {
                _viewModel.TryResume();
            }

            buttonY += 72f * scale;
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "Recommencer",
                    _viewModel.RestartBinding,
                    DrawListUi.Danger,
                    true,
                    _pauseSelection == 1))
            {
                _viewModel.TryRestart();
            }

            buttonY += 72f * scale;
            if (DrawListUi.DrawButton(
                    drawList,
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "Menu principal",
                    string.Empty,
                    DrawListUi.Accent,
                    true,
                    _pauseSelection == 2))
            {
                _viewModel.TryLeave();
            }
        }

        /// <summary>
        /// Draws a centered loading state before the application read model exists.
        /// </summary>
        private static void DrawLoading(
            FuDrawList drawList,
            Vector2 screenSize,
            string statusMessage)
        {
            // An animated radial indicator confirms that asynchronous map setup is progressing.
            float scale = DrawListUi.GetScale();
            Rect panel = DrawListUi.CenterRect(
                screenSize,
                new Vector2(420f, 132f) * scale);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent, true);
            Vector2 spinnerCenter =
                panel.position + new Vector2(52f, 66f) * scale;
            int activeDot = Mathf.FloorToInt(Time.unscaledTime * 8f) % 8;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 dotPosition = spinnerCenter +
                                      new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                                      18f * scale;
                float alpha = i == activeDot ? 1f : 0.2f + i * 0.035f;
                drawList.AddCircleFilled(
                    dotPosition,
                    3.5f * scale,
                    new Color(
                        DrawListUi.Accent.r,
                        DrawListUi.Accent.g,
                        DrawListUi.Accent.b,
                        alpha));
            }

            string message = string.IsNullOrWhiteSpace(statusMessage)
                ? "Chargement de la partie…"
                : statusMessage;
            DrawListUi.DrawText(
                drawList,
                panel.position + new Vector2(94f, 55f) * scale,
                message,
                DrawListUi.TextPrimary,
                16,
                FontType.Bold);
        }

        /// <summary>
        /// Draws a transient runtime status banner above the board.
        /// </summary>
        private static void DrawStatusMessage(
            FuDrawList drawList,
            Vector2 screenSize,
            string statusMessage)
        {
            // Loading and transition feedback stays visible even when a previous map snapshot exists.
            float scale = DrawListUi.GetScale();
            Vector2 textSize = DrawListUi.MeasureText(statusMessage, 14, FontType.Bold);
            Rect panel = new Rect(
                (screenSize.x - textSize.x - 52f * scale) * 0.5f,
                28f * scale,
                textSize.x + 52f * scale,
                48f * scale);
            DrawListUi.DrawPanel(drawList, panel, DrawListUi.Accent, true);
            DrawListUi.DrawCenteredText(
                drawList,
                new Vector2(panel.x, panel.y + 15f * scale),
                panel.width,
                statusMessage,
                DrawListUi.TextPrimary,
                14,
                FontType.Bold);
        }

        #endregion

        #region Keyboard navigation

        /// <summary>
        /// Moves and activates the current pause-menu keyboard selection.
        /// </summary>
        private void HandlePauseNavigation()
        {
            // Arrow keys and Z/S provide a minimal keyboard-first modal navigation path.
            _pauseSelection = MoveSelection(_pauseSelection, 3);
            if (!IsConfirmPressed())
                return;

            switch (_pauseSelection)
            {
                case 0:
                    _viewModel.TryResume();
                    break;
                case 1:
                    _viewModel.TryRestart();
                    break;
                case 2:
                    _viewModel.TryLeave();
                    break;
            }
        }

        /// <summary>
        /// Moves and activates the current result-screen keyboard selection.
        /// </summary>
        private void HandleResultNavigation(GameSessionReadModel state)
        {
            // Won attempts expose continue in addition to restart and return.
            int selectionCount = state.Status == GameSessionStatus.Won ? 3 : 2;
            _resultSelection = MoveSelection(_resultSelection, selectionCount);
            if (!IsConfirmPressed())
                return;

            if (_resultSelection == 0)
            {
                _viewModel.TryRestart();
                return;
            }

            if (state.Status == GameSessionStatus.Won && _resultSelection == 1)
            {
                _viewModel.TryContinue();
                return;
            }

            _viewModel.TryLeave();
        }

        /// <summary>
        /// Moves a cyclic selection index from standard vertical navigation input.
        /// </summary>
        private static int MoveSelection(int current, int count)
        {
            // Both arrow keys and common French movement bindings work in modal screens.
            if (Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.Z))
            {
                return (current - 1 + count) % count;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.S))
            {
                return (current + 1) % count;
            }

            return current;
        }

        /// <summary>
        /// Reports whether a standard keyboard confirmation key was pressed this frame.
        /// </summary>
        private static bool IsConfirmPressed()
        {
            // Return and Space mirror conventional menu confirmation behavior.
            return Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter) ||
                   Input.GetKeyDown(KeyCode.Space);
        }

        #endregion
    }
}

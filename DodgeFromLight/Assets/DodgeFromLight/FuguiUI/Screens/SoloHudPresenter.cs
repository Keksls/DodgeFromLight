using DodgeFromLight.Application.Game;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
using Fu.Framework;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Screens
{
    /// <summary>
    /// Renders the fixed solo gameplay HUD through Fugui.
    /// </summary>
    public sealed class SoloHudPresenter : MonoBehaviour
    {
        private static readonly FuWindowName WindowName =
            new FuWindowName(42000, "Solo HUD", false, 0);

        private const float RefreshInterval = 0.1f;

        private SoloHudViewModel _viewModel;
        private FuWindowDefinition _windowDefinition;
        private FuWindow _window;
        private float _nextRefreshTime;

        /// <summary>
        /// Binds the screen to its view model and creates its Fugui window.
        /// </summary>
        public bool Initialize(SoloHudViewModel viewModel)
        {
            // Runtime bootstrap owns the only valid initialization path.
            _viewModel = viewModel;
            return CreateWindow();
        }

        /// <summary>
        /// Requests low-frequency redraws for the timer and changing session state.
        /// </summary>
        private void Update()
        {
            // A 10 Hz HUD remains responsive without forcing Fugui to redraw every frame.
            if (_window == null || Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            _window.ForceDraw();
        }

        /// <summary>
        /// Closes and unregisters the scene-owned HUD window.
        /// </summary>
        private void OnDestroy()
        {
            // Fugui may already be disposing when Unity tears down the scene.
            if (!Fugui.IsInitialized)
                return;

            _window?.Close();
            if (_windowDefinition != null)
                Fugui.UnregisterWindowDefinition(_windowDefinition);
        }

        /// <summary>
        /// Registers the non-dockable HUD surface and creates its single instance.
        /// </summary>
        private bool CreateWindow()
        {
            // The controller is initialized before the presenter is attached.
            if (_viewModel == null || !Fugui.IsInitialized || _windowDefinition != null)
                return false;

            _windowDefinition = new FuWindowDefinition(
                WindowName,
                FuLayer.Hud,
                Draw,
                new Vector2Int(24, 24),
                new Vector2Int(360, 292),
                FuWindowFlags.NoExternalization |
                FuWindowFlags.NoDocking |
                FuWindowFlags.NoDockingOverMe |
                FuWindowFlags.NoClosable |
                FuWindowFlags.NoMouseInputFocus |
                FuWindowFlags.NoKeyboardInputFocus,
                FuExternalWindowFlags.Default,
                FuWindowStyleFlags.NoTitleBar |
                FuWindowStyleFlags.NoResize |
                FuWindowStyleFlags.NoMove |
                FuWindowStyleFlags.NoScrollbar |
                FuWindowStyleFlags.NoScrollWithMouse |
                FuWindowStyleFlags.NoCollapse |
                FuWindowStyleFlags.NoSavedSettings |
                FuWindowStyleFlags.NoDocking,
                FuWindowResizeSides.None);
            _window = Fugui.CreateWindow(WindowName);
            return _window != null;
        }

        /// <summary>
        /// Draws one immediate-mode snapshot of the solo HUD.
        /// </summary>
        private void Draw(FuWindow window)
        {
            // Each draw captures one immutable read model for internally consistent labels.
            var state = _viewModel.GetState();
            if (state == null)
            {
                Fugui.Layout.Text("Chargement de la partie…", FuTextStyle.Info);
                return;
            }

            var mapTitle = string.IsNullOrWhiteSpace(state.MapTitle)
                ? state.MapId
                : state.MapTitle;
            Fugui.Layout.Text(mapTitle, FuTextStyle.Highlight, FuTextWrapping.Clip);
            Fugui.Layout.Text(
                "Tour " + state.TurnIndex +
                "  •  " + _viewModel.GetElapsedLabel(),
                FuTextStyle.Info,
                FuTextWrapping.Clip);
            Fugui.Layout.Text(
                GetStatusLabel(state.Status),
                GetStatusStyle(state.Status),
                FuTextWrapping.Clip);
            Fugui.Layout.Separator();

            Fugui.Layout.Text(
                "[" + _viewModel.GearSlotOneBinding + "] " +
                _viewModel.GetGearLabel(state.GearSlotOne),
                FuTextWrapping.Clip);
            Fugui.Layout.Text(
                "[" + _viewModel.GearSlotTwoBinding + "] " +
                _viewModel.GetGearLabel(state.GearSlotTwo),
                FuTextWrapping.Clip);
            DrawActiveEffects(Fugui.Layout, state);
            DrawContextPrompt(Fugui.Layout, state);
            Fugui.Layout.Separator();

            if (!_viewModel.CanUndo(state))
                Fugui.Layout.DisableNextElement();
            if (Fugui.Layout.Button(
                    "Annuler  [" + _viewModel.UndoBinding + "]",
                    new FuElementSize(-1f, 36f),
                    FuButtonStyle.Info) &&
                _viewModel.TryUndo())
            {
                window.ForceDraw(3);
            }

            if (!_viewModel.CanRestart(state))
                Fugui.Layout.DisableNextElement();
            if (Fugui.Layout.Button(
                    "Recommencer  [" + _viewModel.RestartBinding + "]",
                    new FuElementSize(-1f, 36f),
                    FuButtonStyle.Warning) &&
                _viewModel.TryRestart())
            {
                window.ForceDraw(3);
            }
        }

        /// <summary>
        /// Draws temporary player alterations only while they are active.
        /// </summary>
        private static void DrawActiveEffects(
            FuLayout layout,
            GameSessionReadModel state)
        {
            // Turn counters make deterministic temporary effects readable at a glance.
            if (state.InvisibilityTurns > 0)
            {
                layout.Text(
                    "Invisible : " + state.InvisibilityTurns + " tour(s)",
                    FuTextStyle.Success,
                    FuTextWrapping.Clip);
            }

            if (state.TimeStopTurns > 0)
            {
                layout.Text(
                    "Temps figé : " + state.TimeStopTurns + " tour(s)",
                    FuTextStyle.Success,
                    FuTextWrapping.Clip);
            }

            if (state.SpeedBoostTurns > 0)
            {
                layout.Text(
                    "Vitesse : " + state.SpeedBoostTurns + " tour(s)",
                    FuTextStyle.Success,
                    FuTextWrapping.Clip);
            }
        }

        /// <summary>
        /// Draws the context-sensitive interaction hint for the current cell.
        /// </summary>
        private void DrawContextPrompt(
            FuLayout layout,
            GameSessionReadModel state)
        {
            // Pick-up takes visual priority because it uses either inventory slot.
            if (state.CanPickUpGear)
            {
                layout.Text(
                    "Ramasser : " +
                    _viewModel.GearSlotOneBinding +
                    " ou " +
                    _viewModel.GearSlotTwoBinding,
                    FuTextStyle.Warning,
                    FuTextWrapping.Clip);
            }
            else if (state.CanActivateCheckpoint)
            {
                layout.Text(
                    "Activer le checkpoint : " +
                    _viewModel.CheckpointBinding,
                    FuTextStyle.Warning,
                    FuTextWrapping.Clip);
            }
            else if (state.HasCheckpoint)
            {
                layout.Text(
                    "Checkpoint actif",
                    FuTextStyle.Deactivated,
                    FuTextWrapping.Clip);
            }
        }

        /// <summary>
        /// Returns the readable label associated with a simulation status.
        /// </summary>
        private static string GetStatusLabel(GameSessionStatus status)
        {
            // Terminal labels are shared by the HUD before dedicated result screens arrive.
            switch (status)
            {
                case GameSessionStatus.Won:
                    return "Réussi";
                case GameSessionStatus.Lost:
                    return "Défaite";
                default:
                    return "En cours";
            }
        }

        /// <summary>
        /// Returns the Fugui text style associated with a simulation status.
        /// </summary>
        private static FuTextStyle GetStatusStyle(GameSessionStatus status)
        {
            // Status color reinforces the textual result without becoming the only cue.
            switch (status)
            {
                case GameSessionStatus.Won:
                    return FuTextStyle.Success;
                case GameSessionStatus.Lost:
                    return FuTextStyle.Danger;
                default:
                    return FuTextStyle.Deactivated;
            }
        }
    }
}

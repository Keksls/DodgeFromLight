using System;
using DodgeFromLight.Application.Game;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Provides the solo HUD with read data and guarded player intentions.
    /// </summary>
    public sealed class SoloHudViewModel
    {
        private readonly Func<GameSessionReadModel> _stateProvider;
        private readonly Func<TimeSpan> _elapsedProvider;
        private readonly Func<string> _statusMessageProvider;
        private readonly Func<bool> _tutorialProvider;
        private readonly Func<SoloMovementInputState> _movementInputProvider;
        private readonly SoloHudActions _actions;

        public string ForwardBinding { get; }
        public string LeftBinding { get; }
        public string BackwardBinding { get; }
        public string RightBinding { get; }
        public string JumpBinding { get; }
        public string GearSlotOneBinding { get; }
        public string GearSlotTwoBinding { get; }
        public string CheckpointBinding { get; }
        public string UndoBinding { get; }
        public string RestartBinding { get; }
        public string ContinueBinding { get; }

        /// <summary>
        /// Creates a HUD contract without exposing Unity scene objects to the screen.
        /// </summary>
        public SoloHudViewModel(
            Func<GameSessionReadModel> stateProvider,
            Func<TimeSpan> elapsedProvider,
            Func<string> statusMessageProvider,
            Func<bool> tutorialProvider,
            Func<SoloMovementInputState> movementInputProvider,
            SoloHudActions actions,
            string forwardBinding,
            string leftBinding,
            string backwardBinding,
            string rightBinding,
            string jumpBinding,
            string gearSlotOneBinding,
            string gearSlotTwoBinding,
            string checkpointBinding,
            string undoBinding,
            string restartBinding,
            string continueBinding)
        {
            // Delegates keep Unity scene services outside the Fugui screen.
            _stateProvider =
                stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _elapsedProvider =
                elapsedProvider ?? throw new ArgumentNullException(nameof(elapsedProvider));
            _statusMessageProvider =
                statusMessageProvider ??
                throw new ArgumentNullException(nameof(statusMessageProvider));
            _tutorialProvider =
                tutorialProvider ?? throw new ArgumentNullException(nameof(tutorialProvider));
            _movementInputProvider =
                movementInputProvider ??
                throw new ArgumentNullException(nameof(movementInputProvider));
            _actions =
                actions ?? throw new ArgumentNullException(nameof(actions));
            ForwardBinding = NormalizeBinding(forwardBinding);
            LeftBinding = NormalizeBinding(leftBinding);
            BackwardBinding = NormalizeBinding(backwardBinding);
            RightBinding = NormalizeBinding(rightBinding);
            JumpBinding = NormalizeBinding(jumpBinding);
            GearSlotOneBinding = NormalizeBinding(gearSlotOneBinding);
            GearSlotTwoBinding = NormalizeBinding(gearSlotTwoBinding);
            CheckpointBinding = NormalizeBinding(checkpointBinding);
            UndoBinding = NormalizeBinding(undoBinding);
            RestartBinding = NormalizeBinding(restartBinding);
            ContinueBinding = NormalizeBinding(continueBinding);
        }

        /// <summary>
        /// Gets the latest immutable application read model.
        /// </summary>
        public GameSessionReadModel GetState()
        {
            // Immediate-mode drawing always consumes the latest authoritative snapshot.
            return _stateProvider();
        }

        /// <summary>
        /// Gets a stable minute, second, and hundredth display for the local timer.
        /// </summary>
        public string GetElapsedLabel()
        {
            // Total minutes remain readable for runs lasting longer than one hour.
            var elapsed = _elapsedProvider();
            var totalMinutes = (int)elapsed.TotalMinutes;
            var hundredths = elapsed.Milliseconds / 10;
            return totalMinutes + ":" +
                   elapsed.Seconds.ToString("00") + "." +
                   hundredths.ToString("00");
        }

        /// <summary>
        /// Gets the current transient runtime status message.
        /// </summary>
        public string GetStatusMessage()
        {
            // Loading state is supplied by the scene adapter without leaking its component type.
            return _statusMessageProvider() ?? string.Empty;
        }

        /// <summary>
        /// Reports whether tutorial assistance should be drawn for the loaded sequence.
        /// </summary>
        public bool IsTutorial()
        {
            // The active game rules remain authoritative for tutorial presentation.
            return _tutorialProvider();
        }

        /// <summary>
        /// Gets the latest movement-input snapshot for pressed-key feedback.
        /// </summary>
        public SoloMovementInputState GetMovementInputState()
        {
            // Input polling stays in the Unity bridge while DrawList rendering consumes a value.
            return _movementInputProvider();
        }

        /// <summary>
        /// Reports whether the local presentation is paused.
        /// </summary>
        public bool IsPaused()
        {
            // Pause state is queried through the guarded action adapter.
            return _actions.IsPaused();
        }

        /// <summary>
        /// Reports whether undo is currently available to the HUD.
        /// </summary>
        public bool CanUndo(GameSessionReadModel state)
        {
            // Application history and presentation availability must both allow the action.
            return state != null &&
                   state.CanUndo &&
                   _actions.CanUseRunActions();
        }

        /// <summary>
        /// Reports whether restart is currently available to the HUD.
        /// </summary>
        public bool CanRestart(GameSessionReadModel state)
        {
            // Result states remain restartable after their terminal animation finishes.
            return state != null && _actions.CanRestart();
        }

        /// <summary>
        /// Reports whether a won attempt can advance from the result screen.
        /// </summary>
        public bool CanContinue(GameSessionReadModel state)
        {
            // Only committed victories can traverse the active map sequence.
            return state != null &&
                   state.Status == GameSessionStatus.Won &&
                   _actions.CanContinue();
        }

        /// <summary>
        /// Requests restoration of the previous accepted command.
        /// </summary>
        public bool TryUndo()
        {
            // The application adapter repeats its guard to remain authoritative.
            return _actions.TryUndo();
        }

        /// <summary>
        /// Requests a restart of the current deterministic attempt.
        /// </summary>
        public bool TryRestart()
        {
            // Restart remains a session intention rather than a scene operation in the UI.
            return _actions.TryRestart();
        }

        /// <summary>
        /// Requests the next map or completion of the finite session.
        /// </summary>
        public bool TryContinue()
        {
            // Tutorial persistence and scene traversal remain outside Fugui.
            return _actions.TryContinue();
        }

        /// <summary>
        /// Requests suspension of the active run.
        /// </summary>
        public bool TryPause()
        {
            // The view model forwards an intention and does not mutate time directly.
            return _actions.TryPause();
        }

        /// <summary>
        /// Requests resumption of the paused run.
        /// </summary>
        public bool TryResume()
        {
            // The runtime adapter restores the synchronized pause state.
            return _actions.TryResume();
        }

        /// <summary>
        /// Requests departure from the current local session.
        /// </summary>
        public bool TryLeave()
        {
            // Scene cleanup and routing remain owned by the runtime adapter.
            return _actions.TryLeave();
        }

        /// <summary>
        /// Returns the result-screen label matching the remaining map sequence.
        /// </summary>
        public string GetContinueLabel()
        {
            // Finite sessions clearly distinguish another puzzle from their exit action.
            return _actions.HasNextMap()
                ? "Niveau suivant"
                : "Terminer";
        }

        /// <summary>
        /// Returns a temporary localized label for a stable gear identifier.
        /// </summary>
        public string GetGearLabel(string gearId)
        {
            // Stable IDs remain the source value until the localization catalogue is introduced.
            switch (gearId)
            {
                case "gear.bomb":
                    return "Bombe";
                case "gear.disco_ball":
                    return "Boule disco";
                case "gear.grapple":
                    return "Grapin";
                case "gear.invisibility_cloak":
                    return "Cape d'invisibilité";
                case "gear.mirror":
                    return "Miroir";
                case "gear.powder_of_darkness":
                    return "Poudre des ténèbres";
                case "gear.remote_control":
                    return "Télécommande";
                case "gear.speed_boots":
                    return "Bottes de vitesse";
                case "gear.time_controller":
                    return "Contrôleur temporel";
                default:
                    return "Vide";
            }
        }

        /// <summary>
        /// Normalizes one configured input label for safe HUD display.
        /// </summary>
        private static string NormalizeBinding(string binding)
        {
            // Unknown bindings remain explicit instead of producing empty brackets.
            return string.IsNullOrWhiteSpace(binding)
                ? "Non assigné"
                : binding;
        }
    }
}

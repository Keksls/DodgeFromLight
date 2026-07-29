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
        private readonly Func<bool> _canUseActionsProvider;
        private readonly Func<bool> _undoAction;
        private readonly Func<bool> _restartAction;

        public string GearSlotOneBinding { get; }
        public string GearSlotTwoBinding { get; }
        public string CheckpointBinding { get; }
        public string UndoBinding { get; }
        public string RestartBinding { get; }

        /// <summary>
        /// Creates a HUD contract without exposing Unity scene objects to the screen.
        /// </summary>
        public SoloHudViewModel(
            Func<GameSessionReadModel> stateProvider,
            Func<TimeSpan> elapsedProvider,
            Func<bool> canUseActionsProvider,
            Func<bool> undoAction,
            Func<bool> restartAction,
            string gearSlotOneBinding,
            string gearSlotTwoBinding,
            string checkpointBinding,
            string undoBinding,
            string restartBinding)
        {
            // Delegates keep the transitional legacy bridge outside the Fugui screen.
            _stateProvider =
                stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _elapsedProvider =
                elapsedProvider ?? throw new ArgumentNullException(nameof(elapsedProvider));
            _canUseActionsProvider =
                canUseActionsProvider ??
                throw new ArgumentNullException(nameof(canUseActionsProvider));
            _undoAction =
                undoAction ?? throw new ArgumentNullException(nameof(undoAction));
            _restartAction =
                restartAction ?? throw new ArgumentNullException(nameof(restartAction));
            GearSlotOneBinding = NormalizeBinding(gearSlotOneBinding);
            GearSlotTwoBinding = NormalizeBinding(gearSlotTwoBinding);
            CheckpointBinding = NormalizeBinding(checkpointBinding);
            UndoBinding = NormalizeBinding(undoBinding);
            RestartBinding = NormalizeBinding(restartBinding);
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
        /// Reports whether undo is currently available to the HUD.
        /// </summary>
        public bool CanUndo(GameSessionReadModel state)
        {
            // Application history and presentation availability must both allow the action.
            return state != null &&
                   state.CanUndo &&
                   _canUseActionsProvider();
        }

        /// <summary>
        /// Reports whether restart is currently available to the HUD.
        /// </summary>
        public bool CanRestart(GameSessionReadModel state)
        {
            // A loaded session can restart only outside pauses and active animations.
            return state != null && _canUseActionsProvider();
        }

        /// <summary>
        /// Requests restoration of the previous accepted command.
        /// </summary>
        public bool TryUndo()
        {
            // The application adapter repeats its guard to remain authoritative.
            return _undoAction();
        }

        /// <summary>
        /// Requests a restart of the current deterministic attempt.
        /// </summary>
        public bool TryRestart()
        {
            // Restart remains a session intention rather than a scene operation in the UI.
            return _restartAction();
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

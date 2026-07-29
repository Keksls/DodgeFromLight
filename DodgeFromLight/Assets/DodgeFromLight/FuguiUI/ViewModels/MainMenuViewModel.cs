using System;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Exposes main-menu state and guarded local navigation intentions.
    /// </summary>
    public sealed class MainMenuViewModel
    {
        private readonly Func<bool> _hasFinishedTutorialProvider;
        private readonly Func<bool> _isTransitioningProvider;
        private readonly Func<bool> _playAction;
        private readonly Func<bool> _mapEditorAction;
        private readonly Func<bool> _campaignAction;
        private readonly Func<bool> _exitAction;

        public string VersionLabel { get; }

        /// <summary>
        /// Creates a menu contract without exposing Unity scene objects to the renderer.
        /// </summary>
        public MainMenuViewModel(
            Func<bool> hasFinishedTutorialProvider,
            Func<bool> isTransitioningProvider,
            Func<bool> playAction,
            Func<bool> mapEditorAction,
            Func<bool> campaignAction,
            Func<bool> exitAction,
            string versionLabel)
        {
            // Every state read and action remains owned by the default Unity assembly.
            _hasFinishedTutorialProvider =
                hasFinishedTutorialProvider ??
                throw new ArgumentNullException(nameof(hasFinishedTutorialProvider));
            _isTransitioningProvider =
                isTransitioningProvider ??
                throw new ArgumentNullException(nameof(isTransitioningProvider));
            _playAction =
                playAction ?? throw new ArgumentNullException(nameof(playAction));
            _mapEditorAction =
                mapEditorAction ?? throw new ArgumentNullException(nameof(mapEditorAction));
            _campaignAction =
                campaignAction ?? throw new ArgumentNullException(nameof(campaignAction));
            _exitAction =
                exitAction ?? throw new ArgumentNullException(nameof(exitAction));
            VersionLabel = string.IsNullOrWhiteSpace(versionLabel)
                ? "version inconnue"
                : versionLabel;
        }

        /// <summary>
        /// Gets the primary play label matching current tutorial progression.
        /// </summary>
        public string GetPlayLabel()
        {
            // New players enter the tutorial while returning players start a normal run.
            return HasFinishedTutorial()
                ? "Jouer"
                : "Commencer le tutoriel";
        }

        /// <summary>
        /// Reports whether the tutorial sequence has been completed.
        /// </summary>
        public bool HasFinishedTutorial()
        {
            // The local tutorial controller owns the persisted progression.
            return _hasFinishedTutorialProvider();
        }

        /// <summary>
        /// Reports whether a scene transition is already in progress.
        /// </summary>
        public bool IsTransitioning()
        {
            // Presenters disable every menu action during the authored transition animation.
            return _isTransitioningProvider();
        }

        /// <summary>
        /// Reports whether the map editor is currently unlocked.
        /// </summary>
        public bool CanOpenMapEditor()
        {
            // The editor follows the existing progression gate.
            return HasFinishedTutorial() && !IsTransitioning();
        }

        /// <summary>
        /// Requests entry into the local puzzle flow.
        /// </summary>
        public bool TryPlay()
        {
            // Transition guards are repeated by the scene controller.
            return !IsTransitioning() && _playAction();
        }

        /// <summary>
        /// Requests entry into the local map editor.
        /// </summary>
        public bool TryOpenMapEditor()
        {
            // Locked tutorial progression cannot be bypassed from presentation code.
            return CanOpenMapEditor() && _mapEditorAction();
        }

        /// <summary>
        /// Requests entry into the campaign scene.
        /// </summary>
        public bool TryOpenCampaign()
        {
            // Campaign navigation remains available while no other transition is active.
            return !IsTransitioning() && _campaignAction();
        }

        /// <summary>
        /// Requests application shutdown.
        /// </summary>
        public bool TryExit()
        {
            // Shutdown cannot race an already-started scene transition.
            return !IsTransitioning() && _exitAction();
        }
    }
}

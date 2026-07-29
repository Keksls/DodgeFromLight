using System;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Exposes guarded solo-session intentions without leaking Unity scene services.
    /// </summary>
    public sealed class SoloHudActions
    {
        private readonly Func<bool> _canUseRunActionsProvider;
        private readonly Func<bool> _canRestartProvider;
        private readonly Func<bool> _canContinueProvider;
        private readonly Func<bool> _hasNextMapProvider;
        private readonly Func<bool> _isPausedProvider;
        private readonly Func<bool> _undoAction;
        private readonly Func<bool> _restartAction;
        private readonly Func<bool> _continueAction;
        private readonly Func<bool> _pauseAction;
        private readonly Func<bool> _resumeAction;
        private readonly Func<bool> _leaveAction;

        /// <summary>
        /// Creates the action contract used by gameplay and result HUD states.
        /// </summary>
        public SoloHudActions(
            Func<bool> canUseRunActionsProvider,
            Func<bool> canRestartProvider,
            Func<bool> canContinueProvider,
            Func<bool> hasNextMapProvider,
            Func<bool> isPausedProvider,
            Func<bool> undoAction,
            Func<bool> restartAction,
            Func<bool> continueAction,
            Func<bool> pauseAction,
            Func<bool> resumeAction,
            Func<bool> leaveAction)
        {
            // Every intention remains guarded by its authoritative Unity adapter.
            _canUseRunActionsProvider =
                canUseRunActionsProvider ??
                throw new ArgumentNullException(nameof(canUseRunActionsProvider));
            _canRestartProvider =
                canRestartProvider ??
                throw new ArgumentNullException(nameof(canRestartProvider));
            _canContinueProvider =
                canContinueProvider ??
                throw new ArgumentNullException(nameof(canContinueProvider));
            _hasNextMapProvider =
                hasNextMapProvider ??
                throw new ArgumentNullException(nameof(hasNextMapProvider));
            _isPausedProvider =
                isPausedProvider ??
                throw new ArgumentNullException(nameof(isPausedProvider));
            _undoAction =
                undoAction ?? throw new ArgumentNullException(nameof(undoAction));
            _restartAction =
                restartAction ?? throw new ArgumentNullException(nameof(restartAction));
            _continueAction =
                continueAction ?? throw new ArgumentNullException(nameof(continueAction));
            _pauseAction =
                pauseAction ?? throw new ArgumentNullException(nameof(pauseAction));
            _resumeAction =
                resumeAction ?? throw new ArgumentNullException(nameof(resumeAction));
            _leaveAction =
                leaveAction ?? throw new ArgumentNullException(nameof(leaveAction));
        }

        /// <summary>
        /// Reports whether mutable actions are available during an active run.
        /// </summary>
        public bool CanUseRunActions()
        {
            // Presentation animations and pauses can temporarily close this gate.
            return _canUseRunActionsProvider();
        }

        /// <summary>
        /// Reports whether the loaded attempt can currently be restarted.
        /// </summary>
        public bool CanRestart()
        {
            // Restart is valid for active, won, and lost attempts when presentation is idle.
            return _canRestartProvider();
        }

        /// <summary>
        /// Reports whether the completed attempt can advance its map sequence.
        /// </summary>
        public bool CanContinue()
        {
            // Victory animation and progression commit must finish before navigation.
            return _canContinueProvider();
        }

        /// <summary>
        /// Reports whether continuing loads another map instead of leaving the sequence.
        /// </summary>
        public bool HasNextMap()
        {
            // The label can distinguish another puzzle from the end of a finite session.
            return _hasNextMapProvider();
        }

        /// <summary>
        /// Reports whether the local presentation is currently paused.
        /// </summary>
        public bool IsPaused()
        {
            // Pause remains an adapter-owned runtime state rather than a DrawList concern.
            return _isPausedProvider();
        }

        /// <summary>
        /// Requests restoration of the previous accepted command.
        /// </summary>
        public bool TryUndo()
        {
            // The adapter repeats its guards before mutating the application session.
            return _undoAction();
        }

        /// <summary>
        /// Requests a restart of the loaded deterministic attempt.
        /// </summary>
        public bool TryRestart()
        {
            // Restart behavior, including checkpoint handling, belongs to the adapter.
            return _restartAction();
        }

        /// <summary>
        /// Requests traversal after a committed victory.
        /// </summary>
        public bool TryContinue()
        {
            // The adapter owns tutorial persistence and scene navigation.
            return _continueAction();
        }

        /// <summary>
        /// Requests suspension of the active local run.
        /// </summary>
        public bool TryPause()
        {
            // The Unity adapter owns time scale and stopwatch synchronization.
            return _pauseAction();
        }

        /// <summary>
        /// Requests resumption of the paused local run.
        /// </summary>
        public bool TryResume()
        {
            // The Unity adapter restores both simulation time and cursor behavior.
            return _resumeAction();
        }

        /// <summary>
        /// Requests departure from the current local play session.
        /// </summary>
        public bool TryLeave()
        {
            // Scene cleanup remains outside the Fugui presentation assembly.
            return _leaveAction();
        }
    }
}

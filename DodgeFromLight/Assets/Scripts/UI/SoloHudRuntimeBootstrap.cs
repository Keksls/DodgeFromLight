using DodgeFromLight.FuguiUI.Screens;
using DodgeFromLight.FuguiUI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DodgeFromLight.UnityPresentation.UI
{
    /// <summary>
    /// Attaches the Fugui solo HUD when the local gameplay scene becomes active.
    /// </summary>
    public static class SoloHudRuntimeBootstrap
    {
        private const string GameplaySceneName = "Main";
        /// <summary>
        /// Registers the scene callback before the first scene begins loading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Re-registration remains safe when entering Play Mode without a domain reload.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Creates the gameplay HUD bridge for the local Main scene.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Non-gameplay scenes keep their own UI migration schedule.
            if (scene.name != GameplaySceneName)
                return;

            var gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null || gameManager.InGameUI == null)
            {
                Debug.LogError(
                    "The Fugui solo HUD could not find its gameplay controllers.");
                return;
            }

            if (!FuguiRuntimeController.EnsureInitialized())
                return;

            if (gameManager.GetComponent<SoloHudPresenter>() != null)
                return;

            var actions = new SoloHudActions(
                () => gameManager.CanUseSoloHudActions,
                () => gameManager.CanControlLoadedRun &&
                      GameContext.WaitForEndOfAction <= 0,
                () => gameManager.CanContinueAfterVictory,
                () => gameManager.HasNextMapInSequence,
                () => gameManager.Paused,
                gameManager.TryUndoLastCommand,
                gameManager.InGameUI.TryRestart,
                gameManager.TryContinueAfterVictory,
                gameManager.InGameUI.TryPause,
                gameManager.InGameUI.TryResume,
                gameManager.InGameUI.TryExit);
            var viewModel = new SoloHudViewModel(
                gameManager.GetSoloSessionReadModel,
                () => gameManager.StopWatch.Elapsed,
                GetWorkerMessage,
                () => GameContext.CurrentRules != null &&
                      GameContext.CurrentRules.Tutorial,
                GetMovementInputState,
                actions,
                GetInputBinding(InputSettingsType.Forward),
                GetInputBinding(InputSettingsType.left),
                GetInputBinding(InputSettingsType.Backward),
                GetInputBinding(InputSettingsType.Right),
                GetInputBinding(InputSettingsType.Jump),
                GetInputBinding(InputSettingsType.Gear1),
                GetInputBinding(InputSettingsType.Gear2),
                GetInputBinding(InputSettingsType.TakeCheckPoint),
                GetInputBinding(InputSettingsType.Undo),
                GetInputBinding(InputSettingsType.RestartMap),
                GetInputBinding(InputSettingsType.NextMap));
            var presenter = gameManager.gameObject.AddComponent<SoloHudPresenter>();
            if (!presenter.Initialize(viewModel))
            {
                Object.Destroy(presenter);
                Debug.LogError("The Fugui solo DrawList presenter could not be attached.");
                return;
            }

        }

        /// <summary>
        /// Gets the readable label of one configured transitional input binding.
        /// </summary>
        private static string GetInputBinding(InputSettingsType type)
        {
            // Only the Unity scene bridge knows where input settings currently live.
            var settingsManager = GameContext.GameSettingsManager;
            if (settingsManager == null)
                return string.Empty;

            var binding = settingsManager.GetInput(type);
            return binding == null
                ? string.Empty
                : binding.ToString();
        }

        /// <summary>
        /// Gets the current visible loading message from the scene status source.
        /// </summary>
        private static string GetWorkerMessage()
        {
            // The presenter receives plain text rather than a reference to a scene component.
            var notifier = GameContext.UI_WorkerNotifier;
            return notifier != null && notifier.IsVisible
                ? notifier.Message
                : string.Empty;
        }

        /// <summary>
        /// Captures the movement inputs used by the tutorial key guide.
        /// </summary>
        private static SoloMovementInputState GetMovementInputState()
        {
            // Missing settings produce an unpressed snapshot during early scene startup.
            var settingsManager = GameContext.GameSettingsManager;
            if (settingsManager == null)
                return default;

            return new SoloMovementInputState(
                settingsManager.GetInput(InputSettingsType.Forward).IsPressed(),
                settingsManager.GetInput(InputSettingsType.left).IsPressed(),
                settingsManager.GetInput(InputSettingsType.Backward).IsPressed(),
                settingsManager.GetInput(InputSettingsType.Right).IsPressed(),
                settingsManager.GetInput(InputSettingsType.Jump).IsPressed());
        }
    }
}

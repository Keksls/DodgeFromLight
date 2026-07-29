using DodgeFromLight.FuguiUI.Screens;
using DodgeFromLight.FuguiUI.ViewModels;
using Fu;
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
        private const string ControllerResourceName =
            "DodgeFromLightFuguiController";

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
            if (gameManager == null)
            {
                Debug.LogError(
                    "The Fugui solo HUD could not find the gameplay manager.");
                return;
            }

            if (!EnsureFuguiController())
                return;

            if (gameManager.GetComponent<SoloHudPresenter>() != null)
                return;

            var viewModel = new SoloHudViewModel(
                gameManager.GetSoloSessionReadModel,
                () => gameManager.StopWatch.Elapsed,
                () => gameManager.CanUseSoloHudActions,
                gameManager.TryUndoLastCommand,
                gameManager.TryRestartCurrentRun,
                GetInputBinding(InputSettingsType.Gear1),
                GetInputBinding(InputSettingsType.Gear2),
                GetInputBinding(InputSettingsType.TakeCheckPoint),
                GetInputBinding(InputSettingsType.Undo),
                GetInputBinding(InputSettingsType.RestartMap));
            var presenter = gameManager.gameObject.AddComponent<SoloHudPresenter>();
            if (!presenter.Initialize(viewModel))
            {
                Object.Destroy(presenter);
                Debug.LogError("The Fugui solo HUD window could not be created.");
                return;
            }

            if (gameManager.InGameUI != null)
                gameManager.InGameUI.UseFuguiHud();
        }

        /// <summary>
        /// Ensures one initialized Fugui runtime owner is available.
        /// </summary>
        private static bool EnsureFuguiController()
        {
            // A persistent controller can already own Fugui after a scene transition.
            if (Fugui.IsInitialized)
                return true;

            if (Object.FindFirstObjectByType<FuController>() != null)
            {
                Debug.LogError(
                    "A Fugui controller exists but its runtime is not initialized.");
                return false;
            }

            var controllerPrefab =
                Resources.Load<GameObject>(ControllerResourceName);
            if (controllerPrefab == null)
            {
                Debug.LogError(
                    "The Dodge From Light Fugui controller resource is missing.");
                return false;
            }

            Object.Instantiate(controllerPrefab);
            if (Fugui.IsInitialized)
                return true;

            Debug.LogError("Fugui failed to initialize for the solo HUD.");
            return false;
        }

        /// <summary>
        /// Gets the readable label of one configured transitional input binding.
        /// </summary>
        private static string GetInputBinding(InputSettingsType type)
        {
            // Only the legacy bridge knows where input settings currently live.
            var settingsManager = GameContext.GameSettingsManager;
            if (settingsManager == null)
                return string.Empty;

            var binding = settingsManager.GetInput(type);
            return binding == null
                ? string.Empty
                : binding.ToString();
        }
    }
}

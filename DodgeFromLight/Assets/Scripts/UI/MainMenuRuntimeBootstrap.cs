using DodgeFromLight.FuguiUI.Screens;
using DodgeFromLight.FuguiUI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DodgeFromLight.UnityPresentation.UI
{
    /// <summary>
    /// Attaches the foreground DrawList menu when the MainMenu scene becomes active.
    /// </summary>
    public static class MainMenuRuntimeBootstrap
    {
        private const string MainMenuSceneName = "MainMenu";

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
        /// Creates the DrawList main-menu bridge for the local MainMenu scene.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Every other scene keeps its own dedicated runtime presenter.
            if (scene.name != MainMenuSceneName)
                return;

            var menuController =
                Object.FindFirstObjectByType<MainMenuController>();
            if (menuController == null)
            {
                Debug.LogError(
                    "The Fugui main menu could not find its scene controller.");
                return;
            }

            if (!FuguiRuntimeController.EnsureInitialized())
                return;
            if (menuController.GetComponent<MainMenuPresenter>() != null)
                return;

            var viewModel = new MainMenuViewModel(
                menuController.CanOpenMapEditor,
                () => menuController.IsTransitioning,
                menuController.TryPlay,
                menuController.TryOpenMapEditor,
                menuController.TryOpenCampaign,
                menuController.TryExit,
                "v " + UnityEngine.Application.version);
            var presenter =
                menuController.gameObject.AddComponent<MainMenuPresenter>();
            if (presenter.Initialize(viewModel))
                return;

            Object.Destroy(presenter);
            Debug.LogError(
                "The Fugui main-menu DrawList presenter could not be attached.");
        }
    }
}

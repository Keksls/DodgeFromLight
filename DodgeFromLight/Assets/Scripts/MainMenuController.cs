using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Owns the animated local scene transitions initiated by the DrawList main menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public GameObject fog;
    public GameObject foam;
    public List<Animator> Animators = new List<Animator>();
    public float AnimationDuration = 0.8f;

    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// Initializes the authored menu environment.
    /// </summary>
    private void Start()
    {
        // Menu rendering is owned by Fugui while this component retains the 3D environment.
        var environmentAsset = Resources.Load<TextAsset>("MainMenuEnvironment");
        if (environmentAsset == null)
        {
            Debug.LogError("The MainMenuEnvironment resource is missing.");
            return;
        }

        EnvironmentSettings environment =
            JsonConvert.DeserializeObject<EnvironmentSettings>(
                environmentAsset.text);
        GameContext.EnvironmentController.SetEnvironment(environment);
        GameContext.EnvironmentController.SetFogSettings(
            environment,
            fog,
            foam);
    }

    /// <summary>
    /// Handles the development fullscreen shortcut.
    /// </summary>
    private void Update()
    {
        // Preserve the established local fullscreen toggle.
        if (Input.GetKeyDown(KeyCode.F))
            Screen.fullScreen = !Screen.fullScreen;
    }

    /// <summary>
    /// Reports whether the persisted tutorial gate allows map-editor access.
    /// </summary>
    public bool CanOpenMapEditor()
    {
        // The tutorial controller remains the only source of progression truth.
        return GameContext.TutorialController != null &&
               GameContext.TutorialController.HasFinishedTutorial();
    }

    /// <summary>
    /// Tries to start the tutorial or normal local puzzle flow.
    /// </summary>
    public bool TryPlay()
    {
        // Direct and menu-driven play share the same deterministic session factory.
        if (IsTransitioning || GameContext.TutorialController == null)
            return false;

        GameContext.CurrentRules = LocalGameSessionFactory.CreateDefault(
            GameContext.TutorialController);
        return TryStartTransition("Main", "Attack", false);
    }

    /// <summary>
    /// Tries to open the local map editor.
    /// </summary>
    public bool TryOpenMapEditor()
    {
        // The existing tutorial progression gate cannot be bypassed by UI state.
        return CanOpenMapEditor() &&
               TryStartTransition("MapCreator", "Walk", true);
    }

    /// <summary>
    /// Tries to open the campaign scene.
    /// </summary>
    public bool TryOpenCampaign()
    {
        // Campaign retains its authored transition until its product pass begins.
        return TryStartTransition("Campaign", "Attack", true);
    }

    /// <summary>
    /// Tries to exit the application.
    /// </summary>
    public bool TryExit()
    {
        // Shutdown is rejected while a scene transition is already committed.
        if (IsTransitioning)
            return false;

#if UNITY_EDITOR
        // Application.Quit is ignored in Play Mode, so keep the editor menu interactive.
        return false;
#else
        IsTransitioning = true;
        Application.Quit();
        return true;
#endif
    }

    /// <summary>
    /// Starts one guarded animated scene transition.
    /// </summary>
    private bool TryStartTransition(
        string sceneName,
        string animationName,
        bool cleanGameManager)
    {
        // A single transition owns the scene until its target load begins.
        if (IsTransitioning)
            return false;

        IsTransitioning = true;
        StartCoroutine(
            PlayTransitionRoutine(
                sceneName,
                animationName,
                cleanGameManager));
        return true;
    }

    /// <summary>
    /// Plays the authored menu animation before loading a local scene.
    /// </summary>
    private IEnumerator PlayTransitionRoutine(
        string sceneName,
        string animationName,
        bool cleanGameManager)
    {
        // The DrawList presenter renders the loading state during this animation.
        foreach (Animator animator in Animators)
        {
            if (animator != null)
                animator.Play(animationName);
        }

        yield return new WaitForSeconds(AnimationDuration);
        if (cleanGameManager && GameContext.GameManager != null)
        {
            Destroy(GameContext.GameManager.gameObject);
            GameContext.GameManager = null;
        }

        GameContext.SceneTransitions.LoadScene(sceneName);
    }
}

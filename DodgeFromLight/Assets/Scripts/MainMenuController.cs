using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class MainMenuController : MonoBehaviour
{
    public EnvironmentController environmentController;
    public GameObject fog;
    public GameObject foam;
    public List<Animator> Animators;
    public List<GameObject> Buttons;
    public Button MapButton;
    public Button PlayButton;
    public Text VersionText;
    public float AnimationDuration = 0.8f;

    private void Awake()
    {
        // Configure local menu availability without waiting for an online session.
        MapButton.interactable = false;
        MapButton.gameObject.GetComponent<CursorSetter>().enabled = false;
        VersionText.text = "v " + Application.version;
    }

    /// <summary>
    /// Starts the local player flow.
    /// </summary>
    public void Play()
    {
        // Keep legacy button bindings routed through the local entry point.
        Player();
    }

    /// <summary>
    /// Initializes the menu environment and local feature availability.
    /// </summary>
    private void Start()
    {
        // The menu no longer checks connectivity or presents an account screen.
        EnvironmentSettings env = JsonConvert.DeserializeObject<EnvironmentSettings>(Resources.Load<TextAsset>("MainMenuEnvironment").text);
        GameContext.EnvironmentController.SetEnvironment(env);
        GameContext.EnvironmentController.SetFogSettings(env, fog, foam);

        if (GameContext.TutorialController.HasFinishTutorial())
        {
            PlayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
            MapButton.interactable = true;
            MapButton.gameObject.GetComponent<CursorSetter>().enabled = true;
        }
        else
        {
            PlayButton.GetComponentInChildren<TextMeshProUGUI>().text = "Tutorial";
            MapButton.interactable = false;
            MapButton.gameObject.GetComponent<CursorSetter>().enabled = false;
        }
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
    /// Plays the menu transition before loading a local scene.
    /// </summary>
    private IEnumerator Play_routine(string sceneName, string animationName, bool cleanGameManager = true)
    {
        // Hide menu controls while the character transition animation runs.
        foreach (var animator in Animators)
            animator.Play(animationName);

        foreach (var button in Buttons)
            button.SetActive(false);

        yield return new WaitForSeconds(AnimationDuration);
        if (cleanGameManager)
        {
            if (GameContext.GameManager != null)
            {
                Destroy(GameContext.GameManager.gameObject);
                Destroy(GameContext.GameManager);
                GameContext.GameManager = null;
            }
        }
        GameContext.SceneTransitions.LoadScene(sceneName);
    }

    /// <summary>
    /// Opens the local map editor.
    /// </summary>
    public void Map()
    {
        // The editor uses the last local workspace or creates a fresh map.
        StartCoroutine(Play_routine("MapCreator", "Walk"));
    }

    /// <summary>
    /// Opens the campaign scene when it becomes available.
    /// </summary>
    public void Campaign()
    {
        // Keep the existing scene route for future campaign work.
        StartCoroutine(Play_routine("Campaign", "Attack"));
    }

    /// <summary>
    /// Starts the tutorial or the most recently saved local map.
    /// </summary>
    public void Player()
    {
        // Share the same deterministic startup selection as direct Intro-to-Main launches.
        GameContext.CurrentRules = LocalGameSessionFactory.CreateDefault(
            GameContext.TutorialController);
        StartCoroutine(Play_routine("Main", "Attack", false));
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void Exit()
    {
        // Delegate shutdown to the Unity runtime.
        Application.Quit();
    }
}

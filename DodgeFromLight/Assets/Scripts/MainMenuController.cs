using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class MainMenuController : MonoBehaviour
{
    public UI_Loggin LogginPanel;
    public UI_WorkerNotifier UI_WorkerNotifier;
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
        MapButton.interactable = false;
        MapButton.gameObject.GetComponent<CursorSetter>().enabled = false;
        VersionText.text = "v " + Application.version;
        LogginPanel.gameObject.SetActive(false);
    }

    public void Play()
    {
        StartCoroutine(Play_routine("Main", "Attack"));
    }

    void Start()
    {
        EnvironmentSettings env = JsonConvert.DeserializeObject<EnvironmentSettings>(Resources.Load<TextAsset>("MainMenuEnvironment").text);
        DodgeFromLight.EnvironmentController.SetEnvironment(env);
        DodgeFromLight.EnvironmentController.SetFogSettings(env, fog, foam);
        UI_WorkerNotifier.Show("Checking online status...");
        DFLClient.CheckIfOnline((status) =>
        {
            UI_WorkerNotifier.Hide();
            if (status == OnlineState.Online)
            {
                if (DFLClient.LoginState != LoginState.LoggedIn)
                {
                    LogginPanel.gameObject.SetActive(true);
                    LogginPanel.ShowConnectionPanel();
                }
                else
                {
                    // already connected ?? wtf
                    LogginPanel.gameObject.SetActive(true);
                    LogginPanel.ShowConnectionPanel();
                }
            }
            else
            {
                // no internet
                UI_WorkerNotifier.Show("No internet connection...");
            }
        });

        LogginPanel.OnConnect = (success) =>
        {
            if (success)
            {
                Player();
                LogginPanel.gameObject.SetActive(false);
            }
        };


        if (DodgeFromLight.TutorialController.HasFinishTutorial())
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Screen.fullScreen = !Screen.fullScreen;
    }

    IEnumerator Play_routine(string sceneName, string animationName, bool cleanGameManager = true)
    {
        foreach (var animator in Animators)
            animator.Play(animationName);

        foreach (var button in Buttons)
            button.SetActive(false);

        yield return new WaitForSeconds(AnimationDuration);
        if (cleanGameManager)
        {
            if (DodgeFromLight.GameManager != null)
            {
                Destroy(DodgeFromLight.GameManager.gameObject);
                Destroy(DodgeFromLight.GameManager);
                DodgeFromLight.GameManager = null;
            }
        }
        DodgeFromLight.SceneTransitions.LoadScene(sceneName);
    }

    public void Map()
    {
        StartCoroutine(Play_routine("MapCreator", "Walk"));
    }

    public void Campaign()
    {
        StartCoroutine(Play_routine("Campaign", "Attack"));
    }

    public void Player()
    {
        string sceneName = "Lobby";
        if (!DodgeFromLight.TutorialController.HasFinishTutorial())
        {
            sceneName = "Main";
            DodgeFromLight.CurrentRules = new GameRules().SetTutorial(DodgeFromLight.TutorialController.GetRemaningMapsFolders());
        }
        StartCoroutine(Play_routine(sceneName, "Attack", false));
    }

    public void Exit()
    {
        Application.Quit();
    }
}
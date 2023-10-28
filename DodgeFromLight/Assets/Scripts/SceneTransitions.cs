using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneTransitions : MonoBehaviour
{
    public Animator animator;
    public float TransitionDuration = 0.5f;
    public float LoadingTextAnimationSpeed = 1f;
    public Text LoadingText;
    public TextMeshProUGUI VersionText;

    private void Awake()
    {
        DodgeFromLight.SceneTransitions = this;
        VersionText.text = "v " + Application.version;
    }

    public void LoadScene(string sceneName, Action Callback = null)
    {
        StartCoroutine(LoadSceneAsync(sceneName, Callback));
    }

    IEnumerator LoadSceneAsync(string SceneName, Action Callback)
    {
        //StartCoroutine(AnimateLoadingText());
        animator.SetTrigger("Loading");
        yield return new WaitForSeconds(TransitionDuration);
        AsyncOperation ao = SceneManager.LoadSceneAsync(SceneName);
        while (!ao.isDone)
            yield return new WaitForEndOfFrame();
        animator.SetTrigger("Loaded");
        Callback?.Invoke();
        //StopCoroutine(AnimateLoadingText());
    }

    IEnumerator AnimateLoadingText()
    {
        while (true)
        {
            LoadingText.text = "Loading";
            yield return new WaitForSeconds(LoadingTextAnimationSpeed);
            LoadingText.text = "Loading.";
            yield return new WaitForSeconds(LoadingTextAnimationSpeed);
            LoadingText.text = "Loading..";
            yield return new WaitForSeconds(LoadingTextAnimationSpeed);
            LoadingText.text = "Loading...";
            yield return new WaitForSeconds(LoadingTextAnimationSpeed);
        }
    }
}
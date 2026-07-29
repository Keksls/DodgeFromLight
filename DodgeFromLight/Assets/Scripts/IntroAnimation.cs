using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class IntroAnimation : MonoBehaviour
{
    private const string GameplaySceneName = "Main";

    public Camera Cam;
    public float animDuration = 11f;
    public GameObject STGO;
    public AudioSource AS;
    public float endAnimDuration = 1f;
    public float[] CamShakes;
    bool ending = false;

    /// <summary>
    /// Hides the cursor and schedules the authored camera shakes.
    /// </summary>
    private void Awake()
    {
        // Intro presentation remains independent from gameplay initialization.
        Cursor.visible = false;
        foreach (float shk in CamShakes)
            StartCoroutine(Shake(shk));
    }

    /// <summary>
    /// Allows the player to skip the intro with Backspace or Escape.
    /// </summary>
    private void Update()
    {
        // Only one transition may run even if several skip inputs are received.
        if ((Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)) && !ending)
        {
            ending = true;
            StopAllCoroutines();
            StartCoroutine(EndAnimation(CompleteIntro));
        }
    }

    /// <summary>
    /// Plays one camera shake at its authored timestamp.
    /// </summary>
    private IEnumerator Shake(float sec)
    {
        // Each configured timestamp owns one independent delayed shake.
        yield return new WaitForSeconds(sec);
        Cam.DOShakeRotation(0.5f, 4f, 50, 5, true);
    }

    /// <summary>
    /// Runs the complete intro before entering gameplay automatically.
    /// </summary>
    private IEnumerator Start()
    {
        // Hide the transition overlay until the authored animation has completed.
        STGO.SetActive(false);
        yield return new WaitForSeconds(animDuration);
        CompleteIntro();
    }

    /// <summary>
    /// Fades the intro audio before invoking the skip completion callback.
    /// </summary>
    private IEnumerator EndAnimation(Action callback)
    {
        // Fade continuously so skipping never cuts the intro audio abruptly.
        float enlapsed = 0f;
        while (enlapsed < endAnimDuration)
        {
            AS.volume = Mathf.Lerp(1f, 0f, enlapsed / endAnimDuration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        callback?.Invoke();
    }

    /// <summary>
    /// Reveals the transition overlay and loads the gameplay scene.
    /// </summary>
    private void CompleteIntro()
    {
        // Main initializes a valid local session before loading its first map.
        STGO.SetActive(true);
        Cursor.visible = true;
        GameContext.SceneTransitions.LoadScene(GameplaySceneName);
    }
}

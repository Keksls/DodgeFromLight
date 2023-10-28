using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class IntroAnimation : MonoBehaviour
{
    public Camera Cam;
    public float animDuration = 11f;
    public GameObject STGO;
    public AudioSource AS;
    public float endAnimDuration = 1f;
    public float[] CamShakes;
    bool ending = false;

    private void Awake()
    {
        Cursor.visible = false;
        foreach (float shk in CamShakes)
            StartCoroutine(Shake(shk));
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape)) && !ending)
        {
            ending = true;
            StopAllCoroutines();
            StartCoroutine(EndAnimation(() =>
            {
                STGO.SetActive(true);
                Cursor.visible = true;
                DodgeFromLight.SceneTransitions.LoadScene("MainMenu");
            }));
        }
    }

    IEnumerator Shake(float sec)
    {
        yield return new WaitForSeconds(sec);
        Cam.DOShakeRotation(0.5f, 4f, 50, 5, true);
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        STGO.SetActive(false);
        yield return new WaitForSeconds(animDuration);
        STGO.SetActive(true);
        Cursor.visible = true;
        DodgeFromLight.SceneTransitions.LoadScene("MainMenu");
    }

    IEnumerator EndAnimation(Action Callback)
    {
        float enlapsed = 0f;
        while (enlapsed < endAnimDuration)
        {
            AS.volume = Mathf.Lerp(1f, 0f, enlapsed / endAnimDuration);
            yield return null;
            enlapsed += Time.deltaTime;
        }
        Callback?.Invoke();
    }
}

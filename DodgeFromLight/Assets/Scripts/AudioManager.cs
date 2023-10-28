using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource UISource;

    private void Awake()
    {
        DodgeFromLight.AudioManager = this;
    }

    public void PlayUIFX(AudioClip clip, float volume)
    {
        UISource.PlayOneShot(clip, volume);
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

public class UISoundFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public AudioClip EnterClip;
    public float EnterVolume = 0.3f;
    public AudioClip ExitClip;
    public float ExitVolume = 0.3f;
    public AudioClip ClickClip;
    public float ClickVolume = 1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ClickClip != null)
            GameContext.AudioManager.PlayUIFX(ClickClip, ClickVolume);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EnterClip != null)
            GameContext.AudioManager.PlayUIFX(EnterClip, EnterVolume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ExitClip != null)
            GameContext.AudioManager.PlayUIFX(ExitClip, ExitVolume);
    }
}
using UnityEngine;

public class supersampling : MonoBehaviour
{
    [ExecuteInEditMode]

    RenderTexture supersamplingRT;
    public Camera cam;
    public int factor = 2;

    void Start()
    {
        supersamplingRT = new RenderTexture(Screen.width * factor, Screen.height * factor, 24, RenderTextureFormat.ARGB32);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        cam.targetTexture = supersamplingRT;
        cam.Render();
        cam.targetTexture = null;

        Graphics.Blit(supersamplingRT, destination);
    }
}
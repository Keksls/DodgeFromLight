using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreenShot : MonoBehaviour
{
    public string Name;
    public float delay = 0f;

    private void Start()
    {
        StartCoroutine(ScreenShoter.Screenshot(128, 128, (texture) =>
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Environment.CurrentDirectory + @"\Assets\Scripts\MapCreator\UIAssets\" + Name + ".png";
            System.IO.File.WriteAllBytes(path, bytes);
        }));
    }
}

public static class ScreenShoter
{
    /// <summary>
    /// Captures a square screenshot while temporarily disabling URP anti-aliasing.
    /// </summary>
    public static IEnumerator Screenshot(int width, int height, Action<Texture2D> Callback)
    {
        // Preserve the configured mode so the capture does not change player settings.
        AntialiasingMode aa = AntialiasingMode.None;
        if (GameContext.GameSettingsManager != null)
        {
            try
            {
                aa = GameContext.GameSettingsManager.CurrentSettings.Graphics.Antialiasing;
                GameContext.GameSettingsManager.SetAntialiasing(AntialiasingMode.None, false);
            }
            catch { }
        }
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        int resWidth = width;
        int resHeight = height;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
        RenderTexture transformedRenderTexture = null;
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            Screen.height,
            Screen.height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default,
            1);
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);
            transformedRenderTexture = RenderTexture.GetTemporary(
                screenShot.width,
                screenShot.height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default,
                1);
            Graphics.Blit(
                renderTexture,
                transformedRenderTexture,
                new Vector2(1.0f, -1.0f),
                new Vector2(0.0f, 1.0f));
            RenderTexture.active = transformedRenderTexture;
            screenShot.ReadPixels(
                new Rect(0, 0, screenShot.width, screenShot.height),
                0, 0);
        }
        catch (Exception e)
        {
            Debug.Log("Exception: " + e);
            Callback?.Invoke(null);
            yield break;
        }
        finally
        {
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            if (transformedRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(transformedRenderTexture);
            }
        }

        screenShot.Apply();

        if (GameContext.GameSettingsManager != null)
        {
            try
            {
                GameContext.GameSettingsManager.SetAntialiasing(aa, false);
            }
            catch { }
        }

        Callback?.Invoke(screenShot);
    }
}

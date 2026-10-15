using Fu;
using UnityEngine;

namespace DodgeFromLight.UnityPresentation.UI
{
    /// <summary>
    /// Validates the Fugui controller explicitly authored in the active scene.
    /// </summary>
    public static class FuguiRuntimeController
    {
        /// <summary>
        /// Checks that the scene-authored controller initialized its Main Fugui context.
        /// </summary>
        public static bool EnsureInitialized()
        {
            // Scene-authored controllers own their camera and font configuration through the Inspector.
            if (Fugui.IsInitialized)
                return true;

            if (Object.FindFirstObjectByType<FuController>() != null)
            {
                Debug.LogError(
                    "A Fugui controller exists but its runtime is not initialized.");
                return false;
            }

            Debug.LogError(
                "No active Fugui controller is present. Add the DodgeFromLightFuguiController prefab to this scene.");
            return false;
        }
    }
}

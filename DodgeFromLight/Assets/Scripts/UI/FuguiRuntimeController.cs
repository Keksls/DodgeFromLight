using Fu;
using UnityEngine;

namespace DodgeFromLight.UnityPresentation.UI
{
    /// <summary>
    /// Ensures runtime scenes share one persistent initialized Fugui controller.
    /// </summary>
    public static class FuguiRuntimeController
    {
        private const string ControllerResourceName =
            "DodgeFromLightFuguiController";

        /// <summary>
        /// Ensures the persistent Main Fugui context is initialized.
        /// </summary>
        public static bool EnsureInitialized()
        {
            // Scene transitions reuse the controller configured to persist between scenes.
            if (Fugui.IsInitialized)
                return true;

            if (Object.FindFirstObjectByType<FuController>() != null)
            {
                Debug.LogError(
                    "A Fugui controller exists but its runtime is not initialized.");
                return false;
            }

            GameObject controllerPrefab =
                Resources.Load<GameObject>(ControllerResourceName);
            if (controllerPrefab == null)
            {
                Debug.LogError(
                    "The Dodge From Light Fugui controller resource is missing.");
                return false;
            }

            Object.Instantiate(controllerPrefab);
            if (Fugui.IsInitialized)
                return true;

            Debug.LogError("Fugui failed to initialize its Main runtime context.");
            return false;
        }
    }
}

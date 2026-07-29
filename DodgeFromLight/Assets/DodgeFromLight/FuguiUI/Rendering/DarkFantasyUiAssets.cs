using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Exposes authored project artwork to runtime-only Fugui DrawList presenters.
    /// </summary>
    public sealed class DarkFantasyUiAssets : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _titleTexture;

        public Texture2D TitleTexture => _titleTexture;
    }
}

using System;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Associates one stable map-creator palette identifier with an authored preview texture.
    /// </summary>
    [Serializable]
    public sealed class MapCreatorIconBinding
    {
        public string Id;
        public Texture2D Texture;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DodgeFromLight.FuguiUI.Rendering
{
    /// <summary>
    /// Stores authored map-creator icon references addressable by stable palette identifiers.
    /// </summary>
    public sealed class MapCreatorUiAssets : ScriptableObject
    {
        [SerializeField]
        private List<MapCreatorIconBinding> _icons =
            new List<MapCreatorIconBinding>();

        private Dictionary<string, Texture2D> _textures;

        /// <summary>
        /// Gets the authored preview texture for one palette identifier.
        /// </summary>
        public Texture2D GetTexture(string id)
        {
            // Lazy indexing keeps the serialized asset simple while lookups remain constant-time.
            EnsureIndex();
            Texture2D texture;
            return !string.IsNullOrEmpty(id) &&
                   _textures.TryGetValue(id, out texture)
                ? texture
                : null;
        }

        /// <summary>
        /// Builds the runtime texture index once from the authored binding list.
        /// </summary>
        private void EnsureIndex()
        {
            // Duplicate or incomplete authoring entries are ignored deterministically.
            if (_textures != null)
                return;

            _textures = new Dictionary<string, Texture2D>();
            foreach (MapCreatorIconBinding icon in _icons)
            {
                if (icon == null ||
                    string.IsNullOrWhiteSpace(icon.Id) ||
                    icon.Texture == null ||
                    _textures.ContainsKey(icon.Id))
                {
                    continue;
                }

                _textures.Add(icon.Id, icon.Texture);
            }
        }
    }
}

using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Describes one selectable tool or authored asset without exposing editor scene objects.
    /// </summary>
    public sealed class MapCreatorPaletteItem
    {
        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
        public string Category { get; }
        public MapCreatorPaletteGroup Group { get; }
        public Texture2D Preview { get; }
        public int SortOrder { get; }
        public MapCreatorEnvironmentPreview EnvironmentPreview { get; }

        /// <summary>
        /// Creates one immutable palette entry used by the DrawList editor.
        /// </summary>
        public MapCreatorPaletteItem(string id, string label, string description, string category, MapCreatorPaletteGroup group, Texture2D preview, int sortOrder, MapCreatorEnvironmentPreview environmentPreview = null)
        {
            // Stable identifiers keep presentation labels independent from gameplay enums.
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
            Category = category ?? string.Empty;
            Group = group;
            Preview = preview;
            SortOrder = sortOrder;
            EnvironmentPreview = environmentPreview;
        }
    }
}

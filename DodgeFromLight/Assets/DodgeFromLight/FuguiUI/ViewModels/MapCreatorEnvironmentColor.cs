using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Describes one editable environment color.
    /// </summary>
    public sealed class MapCreatorEnvironmentColor
    {
        public string Id { get; }
        public string Label { get; }
        public string Section { get; }
        public Color Value { get; }

        /// <summary>
        /// Creates one immutable environment color control.
        /// </summary>
        public MapCreatorEnvironmentColor(
            string id,
            string label,
            string section,
            Color value)
        {
            // Stable identifiers keep shader-facing property names outside the presenter.
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            Section = section ?? string.Empty;
            Value = value;
        }
    }
}

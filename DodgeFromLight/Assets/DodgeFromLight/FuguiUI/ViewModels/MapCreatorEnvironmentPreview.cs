using UnityEngine;

namespace DodgeFromLight.FuguiUI.ViewModels
{
    /// <summary>
    /// Exposes authored preset colors as a palette, not as a fabricated screenshot of the map.
    /// </summary>
    public sealed class MapCreatorEnvironmentPreview
    {
        public Color Sky { get; }
        public Color Horizon { get; }
        public Color Ground { get; }
        public Color Sun { get; }

        /// <summary>
        /// Captures the four authored colors used to compare atmosphere presets at a glance.
        /// </summary>
        public MapCreatorEnvironmentPreview(Color sky, Color horizon, Color ground, Color sun)
        {
            Sky = sky;
            Horizon = horizon;
            Ground = ground;
            Sun = sun;
        }
    }
}

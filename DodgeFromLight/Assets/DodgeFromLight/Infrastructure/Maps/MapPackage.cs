using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Infrastructure.Maps
{
    public sealed class MapPackage
    {
        public MapDefinition Map { get; }
        public string EnvironmentJson { get; }

        /// <summary>
        /// Groups the logical map revision with presentation environment data.
        /// </summary>
        public MapPackage(MapDefinition map, string environmentJson)
        {
            // Keep visual data outside the simulation while preserving it during migration.
            Map = map ?? throw new ArgumentNullException(nameof(map));
            EnvironmentJson = environmentJson ?? "{}";
        }
    }
}

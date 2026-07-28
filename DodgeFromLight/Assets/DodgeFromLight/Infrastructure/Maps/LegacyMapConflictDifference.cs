using System;

namespace DodgeFromLight.Infrastructure.Maps
{
    [Flags]
    public enum LegacyMapConflictDifference
    {
        None = 0,
        Gameplay = 1,
        Environment = 2,
        Preview = 4
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal sealed class PlatformTurnResult
    {
        internal PlayerState Player { get; }
        internal IReadOnlyList<MovingPlatformState> Platforms { get; }

        /// <summary>
        /// Creates the immutable result of resolving every moving platform.
        /// </summary>
        internal PlatformTurnResult(
            PlayerState player,
            IEnumerable<MovingPlatformState> platforms)
        {
            // Copy platform states so prior undo snapshots remain untouched.
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Platforms = new ReadOnlyCollection<MovingPlatformState>(
                new List<MovingPlatformState>(
                    platforms ?? throw new ArgumentNullException(nameof(platforms))));
        }
    }
}

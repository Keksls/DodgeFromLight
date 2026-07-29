using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Simulation
{
    internal sealed class PlayerCommandResolution
    {
        internal bool IsAccepted { get; }
        internal string RejectionReason { get; }
        internal PlayerState Player { get; }
        internal BoardState Board { get; }
        internal IReadOnlyList<EnemyState> Enemies { get; }
        internal IReadOnlyList<MovingPlatformState> Platforms { get; }
        internal DeterministicRandomState Random { get; }
        internal bool IsJumping { get; }

        /// <summary>
        /// Creates the immutable result of resolving one player command intent.
        /// </summary>
        internal PlayerCommandResolution(
            bool isAccepted,
            string rejectionReason,
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            IEnumerable<MovingPlatformState> platforms,
            DeterministicRandomState random,
            bool isJumping)
        {
            // Copy every actor collection before the shared world turn begins.
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Enemies = new ReadOnlyCollection<EnemyState>(
                new List<EnemyState>(
                    enemies ?? throw new ArgumentNullException(nameof(enemies))));
            Platforms = new ReadOnlyCollection<MovingPlatformState>(
                new List<MovingPlatformState>(
                    platforms ?? throw new ArgumentNullException(nameof(platforms))));
            IsAccepted = isAccepted;
            RejectionReason = rejectionReason ?? string.Empty;
            IsJumping = isJumping;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal sealed class GearCommandResult
    {
        internal bool IsAccepted { get; }
        internal string RejectionReason { get; }
        internal PlayerState Player { get; }
        internal BoardState Board { get; }
        internal IReadOnlyList<EnemyState> Enemies { get; }
        internal IReadOnlyList<MovingPlatformState> Platforms { get; }

        /// <summary>
        /// Creates one immutable inventory-command resolution.
        /// </summary>
        internal GearCommandResult(
            bool isAccepted,
            string rejectionReason,
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            IEnumerable<MovingPlatformState> platforms)
        {
            // Copy runtime collections so a rejected or accepted result owns its snapshot.
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Enemies = new ReadOnlyCollection<EnemyState>(
                new List<EnemyState>(
                    enemies ?? throw new ArgumentNullException(nameof(enemies))));
            Platforms = new ReadOnlyCollection<MovingPlatformState>(
                new List<MovingPlatformState>(
                    platforms ?? throw new ArgumentNullException(nameof(platforms))));
            IsAccepted = isAccepted;
            RejectionReason = rejectionReason ?? string.Empty;
        }
    }
}

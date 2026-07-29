using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal sealed class EnemyTurnResult
    {
        internal PlayerState Player { get; }
        internal BoardState Board { get; }
        internal IReadOnlyList<EnemyState> Enemies { get; }
        internal DeterministicRandomState Random { get; }
        internal int DetectingEnemyIndex { get; }

        /// <summary>
        /// Creates the immutable result of resolving every enemy for one turn.
        /// </summary>
        internal EnemyTurnResult(
            PlayerState player,
            BoardState board,
            IEnumerable<EnemyState> enemies,
            DeterministicRandomState random,
            int detectingEnemyIndex)
        {
            // Copy the enemy list before returning it to the simulation engine.
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            if (detectingEnemyIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(detectingEnemyIndex));
            Enemies = new ReadOnlyCollection<EnemyState>(
                new List<EnemyState>(
                    enemies ?? throw new ArgumentNullException(nameof(enemies))));
            DetectingEnemyIndex = detectingEnemyIndex;
        }
    }
}

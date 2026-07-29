using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Systems
{
    internal sealed class EnemyInteractionResult
    {
        internal PlayerState Player { get; }
        internal BoardState Board { get; }
        internal EnemyState Enemy { get; }
        internal DeterministicRandomState Random { get; }
        internal bool DetectsPlayer { get; }
        internal bool StopsEnemy { get; }

        /// <summary>
        /// Creates one internal enemy-interaction result.
        /// </summary>
        internal EnemyInteractionResult(
            PlayerState player,
            BoardState board,
            EnemyState enemy,
            DeterministicRandomState random,
            bool detectsPlayer,
            bool stopsEnemy)
        {
            // Store only the values modified by one interaction point.
            Player = player;
            Board = board;
            Enemy = enemy;
            Random = random;
            DetectsPlayer = detectsPlayer;
            StopsEnemy = stopsEnemy;
        }
    }
}

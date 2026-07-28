using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class EnemyState
    {
        public int DefinitionIndex { get; }
        public GridPosition Position { get; }
        public Direction Direction { get; }
        public int PatrolIndex { get; }
        public bool IsPatrolReversed { get; }

        /// <summary>
        /// Creates the immutable runtime state of one enemy placement.
        /// </summary>
        public EnemyState(
            int definitionIndex,
            GridPosition position,
            Direction direction,
            int patrolIndex,
            bool isPatrolReversed)
        {
            // Keep only deterministic values required to continue the enemy simulation.
            if (definitionIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(definitionIndex));
            if (direction == Direction.None || !Enum.IsDefined(typeof(Direction), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (patrolIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(patrolIndex));

            DefinitionIndex = definitionIndex;
            Position = position;
            Direction = direction;
            PatrolIndex = patrolIndex;
            IsPatrolReversed = isPatrolReversed;
        }
    }
}

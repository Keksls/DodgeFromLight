namespace DodgeFromLight.Core.Model
{
    public sealed class NeedleState
    {
        public GridPosition Position { get; }
        public bool IsActive { get; }

        /// <summary>
        /// Creates the immutable runtime state of one needle cell.
        /// </summary>
        public NeedleState(GridPosition position, bool isActive)
        {
            // Store the resolved phase so later time-stopping effects can pause it.
            Position = position;
            IsActive = isActive;
        }

        /// <summary>
        /// Returns the needle state for the next active board turn.
        /// </summary>
        public NeedleState Toggle()
        {
            // Needle cells alternate between dangerous and safe phases.
            return new NeedleState(Position, !IsActive);
        }
    }
}

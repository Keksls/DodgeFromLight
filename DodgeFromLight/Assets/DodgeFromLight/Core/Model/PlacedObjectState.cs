using System;

namespace DodgeFromLight.Core.Model
{
    public sealed class PlacedObjectState
    {
        public string ObjectId { get; }
        public GridPosition Position { get; }
        public int RemainingTurns { get; }

        /// <summary>
        /// Creates the immutable state of a temporary object placed on the board.
        /// </summary>
        public PlacedObjectState(
            string objectId,
            GridPosition position,
            int remainingTurns)
        {
            // Temporary objects always need an identity and a positive lifetime.
            if (string.IsNullOrWhiteSpace(objectId))
                throw new ArgumentException("A placed-object ID is required.", nameof(objectId));
            if (remainingTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(remainingTurns));

            ObjectId = objectId;
            Position = position;
            RemainingTurns = remainingTurns;
        }

        /// <summary>
        /// Produces the next lifetime state or null when the object expires.
        /// </summary>
        public PlacedObjectState Tick()
        {
            // Expired objects are removed from the next board snapshot.
            return RemainingTurns <= 1
                ? null
                : new PlacedObjectState(ObjectId, Position, RemainingTurns - 1);
        }
    }
}

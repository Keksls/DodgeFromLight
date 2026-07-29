using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Events
{
    public sealed class SimulationEvent
    {
        public SimulationEventType Type { get; }
        public int TurnIndex { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public Direction Direction { get; }
        public int EntityIndex { get; }
        public string ContentId { get; }
        public int Value { get; }

        /// <summary>
        /// Describes one resolved gameplay fact for presentation and diagnostics.
        /// </summary>
        public SimulationEvent(
            SimulationEventType type,
            int turnIndex,
            GridPosition from,
            GridPosition to,
            Direction direction,
            int entityIndex = -1,
            string contentId = "",
            int value = 0)
        {
            // Store resolved values only; events never contain presentation objects.
            if (entityIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            Type = type;
            TurnIndex = turnIndex;
            From = from;
            To = to;
            Direction = direction;
            EntityIndex = entityIndex;
            ContentId = contentId ?? string.Empty;
            Value = value;
        }
    }
}

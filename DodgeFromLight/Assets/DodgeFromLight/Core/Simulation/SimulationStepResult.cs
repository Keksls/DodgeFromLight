using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Events;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Simulation
{
    public sealed class SimulationStepResult
    {
        public bool IsAccepted { get; }
        public SimulationState PreviousState { get; }
        public SimulationState State { get; }
        public IReadOnlyList<SimulationEvent> Events { get; }
        public string RejectionReason { get; }
        public string StateHash { get; }

        /// <summary>
        /// Creates the immutable result of one attempted command.
        /// </summary>
        private SimulationStepResult(
            bool isAccepted,
            SimulationState previousState,
            SimulationState state,
            IEnumerable<SimulationEvent> events,
            string rejectionReason,
            string stateHash)
        {
            // Copy events so consumers cannot modify the authoritative resolution log.
            IsAccepted = isAccepted;
            PreviousState = previousState ?? throw new ArgumentNullException(nameof(previousState));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = new ReadOnlyCollection<SimulationEvent>(
                new List<SimulationEvent>(events ?? Array.Empty<SimulationEvent>()));
            RejectionReason = rejectionReason ?? string.Empty;
            StateHash = stateHash ?? string.Empty;
        }

        /// <summary>
        /// Creates a successful command result.
        /// </summary>
        public static SimulationStepResult CreateAccepted(
            SimulationState previousState,
            SimulationState state,
            IEnumerable<SimulationEvent> events,
            string stateHash)
        {
            // Successful commands expose both states for presentation and diagnostics.
            return new SimulationStepResult(
                true,
                previousState,
                state,
                events,
                string.Empty,
                stateHash);
        }

        /// <summary>
        /// Creates a rejected command result without advancing the simulation.
        /// </summary>
        public static SimulationStepResult CreateRejected(
            SimulationState state,
            string rejectionReason,
            string stateHash)
        {
            // Rejected commands preserve the exact same state and contain no events.
            return new SimulationStepResult(
                false,
                state,
                state,
                Array.Empty<SimulationEvent>(),
                rejectionReason,
                stateHash);
        }
    }
}

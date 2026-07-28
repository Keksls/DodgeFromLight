using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DodgeFromLight.Core.Commands;
using DodgeFromLight.Core.Model;
using DodgeFromLight.Core.Simulation;

namespace DodgeFromLight.Core.Replays
{
    public sealed class SimulationHistory
    {
        private readonly SimulationEngine _engine;
        private readonly List<PlayerCommand> _commands;
        private readonly IReadOnlyList<PlayerCommand> _commandsView;
        private readonly List<SimulationState> _states;

        public SimulationState CurrentState => _states[_states.Count - 1];
        public bool CanUndo => _commands.Count > 0;
        public IReadOnlyList<PlayerCommand> Commands => _commandsView;

        /// <summary>
        /// Creates a command history from a map's initial state.
        /// </summary>
        public SimulationHistory(MapDefinition map, SimulationEngine engine)
        {
            // Retain immutable states so undo never reconstructs partial mutable objects.
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _commands = new List<PlayerCommand>();
            _commandsView = new ReadOnlyCollection<PlayerCommand>(_commands);
            _states = new List<SimulationState>
            {
                SimulationState.CreateInitial(map)
            };
        }

        /// <summary>
        /// Executes a command and records it only when the engine accepts it.
        /// </summary>
        public SimulationStepResult Execute(PlayerCommand command)
        {
            // Store the immutable state returned by the authoritative engine.
            var result = _engine.Step(CurrentState, command);
            if (result.IsAccepted)
            {
                _commands.Add(command);
                _states.Add(result.State);
            }

            return result;
        }

        /// <summary>
        /// Restores the exact immutable state preceding the last accepted command.
        /// </summary>
        public bool TryUndo(out SimulationState state)
        {
            // Remove command and state together to keep both histories aligned.
            if (!CanUndo)
            {
                state = CurrentState;
                return false;
            }

            _commands.RemoveAt(_commands.Count - 1);
            _states.RemoveAt(_states.Count - 1);
            state = CurrentState;
            return true;
        }

        /// <summary>
        /// Exports the accepted command stream as a verifiable replay.
        /// </summary>
        public ReplayDefinition CreateReplay()
        {
            // Bind the replay to the exact map, ruleset, and final state hash.
            return new ReplayDefinition(
                CurrentState.Map.MapId,
                CurrentState.Map.RevisionId,
                CurrentState.Map.RulesetVersion,
                MapDefinitionHasher.Compute(CurrentState.Map),
                _commands,
                SimulationStateHasher.Compute(CurrentState));
        }
    }
}

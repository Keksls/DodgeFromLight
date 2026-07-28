using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Commands
{
    public sealed class PlayerCommand
    {
        public PlayerCommandType Type { get; }
        public Direction Direction { get; }

        /// <summary>
        /// Creates a validated player command.
        /// </summary>
        public PlayerCommand(PlayerCommandType type, Direction direction)
        {
            // Enforce command-specific invariants at creation time.
            if (!Enum.IsDefined(typeof(PlayerCommandType), type))
                throw new ArgumentOutOfRangeException(nameof(type));
            if (type == PlayerCommandType.Move &&
                (direction == Direction.None || !Enum.IsDefined(typeof(Direction), direction)))
            {
                throw new ArgumentException("A move command requires a cardinal direction.", nameof(direction));
            }
            if (type == PlayerCommandType.Wait && direction != Direction.None)
                throw new ArgumentException("A wait command cannot contain a direction.", nameof(direction));

            Type = type;
            Direction = direction;
        }

        /// <summary>
        /// Creates a cardinal movement command.
        /// </summary>
        public static PlayerCommand Move(Direction direction)
        {
            // Route creation through the validating constructor.
            return new PlayerCommand(PlayerCommandType.Move, direction);
        }

        /// <summary>
        /// Creates a command that advances the world without moving the player.
        /// </summary>
        public static PlayerCommand Wait()
        {
            // Wait commands intentionally carry no direction.
            return new PlayerCommand(PlayerCommandType.Wait, Direction.None);
        }

        /// <summary>
        /// Formats the command for diagnostics and replay errors.
        /// </summary>
        public override string ToString()
        {
            // Include the direction only when it changes the command meaning.
            return Type == PlayerCommandType.Move
                ? Type + ":" + Direction
                : Type.ToString();
        }
    }
}

using System;
using DodgeFromLight.Core.Model;

namespace DodgeFromLight.Core.Commands
{
    public sealed class PlayerCommand
    {
        public PlayerCommandType Type { get; }
        public Direction Direction { get; }
        public int SlotIndex { get; }

        /// <summary>
        /// Creates a validated player command.
        /// </summary>
        public PlayerCommand(PlayerCommandType type, Direction direction)
            : this(type, direction, -1)
        {
            // Keep the original constructor for movement and wait commands.
        }

        /// <summary>
        /// Creates a validated player command with an optional inventory slot.
        /// </summary>
        public PlayerCommand(
            PlayerCommandType type,
            Direction direction,
            int slotIndex)
        {
            // Enforce command-specific invariants at creation time.
            if (!Enum.IsDefined(typeof(PlayerCommandType), type))
                throw new ArgumentOutOfRangeException(nameof(type));
            if (type == PlayerCommandType.Move &&
                (direction == Direction.None || !Enum.IsDefined(typeof(Direction), direction)))
            {
                throw new ArgumentException("A move command requires a cardinal direction.", nameof(direction));
            }
            if (type != PlayerCommandType.Move && direction != Direction.None)
                throw new ArgumentException(
                    "Only a move command can contain a direction.",
                    nameof(direction));
            if ((type == PlayerCommandType.PickupGear ||
                 type == PlayerCommandType.UseGear) &&
                (slotIndex < 0 || slotIndex > 1))
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
            if (type != PlayerCommandType.PickupGear &&
                type != PlayerCommandType.UseGear &&
                slotIndex != -1)
            {
                throw new ArgumentException(
                    "This command type cannot contain an inventory slot.",
                    nameof(slotIndex));
            }

            Type = type;
            Direction = direction;
            SlotIndex = slotIndex;
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
        /// Creates a command that picks the current-cell gear into one inventory slot.
        /// </summary>
        public static PlayerCommand PickupGear(int slotIndex)
        {
            // The validating constructor enforces the two-slot inventory boundary.
            return new PlayerCommand(
                PlayerCommandType.PickupGear,
                Direction.None,
                slotIndex);
        }

        /// <summary>
        /// Creates a command that uses the gear in one inventory slot.
        /// </summary>
        public static PlayerCommand UseGear(int slotIndex)
        {
            // Gear targeting uses the player's current orientation.
            return new PlayerCommand(
                PlayerCommandType.UseGear,
                Direction.None,
                slotIndex);
        }

        /// <summary>
        /// Creates a command that activates the checkpoint under the player.
        /// </summary>
        public static PlayerCommand ActivateCheckpoint()
        {
            // Checkpoint activation has no directional payload.
            return new PlayerCommand(
                PlayerCommandType.ActivateCheckpoint,
                Direction.None);
        }

        /// <summary>
        /// Creates a command that plants the anchor or swaps with its saved position.
        /// </summary>
        public static PlayerCommand PlantOrSwapAnchor()
        {
            // The current anchor state determines whether the action plants or swaps.
            return new PlayerCommand(
                PlayerCommandType.PlantOrSwapAnchor,
                Direction.None);
        }

        /// <summary>
        /// Creates a command that pushes the planted anchor forward.
        /// </summary>
        public static PlayerCommand PushAnchor()
        {
            // The push follows the player's current orientation.
            return new PlayerCommand(PlayerCommandType.PushAnchor, Direction.None);
        }

        /// <summary>
        /// Creates a command that jumps in place for one protected resolution.
        /// </summary>
        public static PlayerCommand Jump()
        {
            // Jump is modeled as one explicit accepted command.
            return new PlayerCommand(PlayerCommandType.Jump, Direction.None);
        }

        /// <summary>
        /// Formats the command for diagnostics and replay errors.
        /// </summary>
        public override string ToString()
        {
            // Include the direction only when it changes the command meaning.
            if (Type == PlayerCommandType.Move)
                return Type + ":" + Direction;
            if (Type == PlayerCommandType.PickupGear ||
                Type == PlayerCommandType.UseGear)
            {
                return Type + ":" + SlotIndex;
            }

            return Type.ToString();
        }
    }
}

using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    public enum CommandKind { MoveCannon = 0 }

    public struct InputCommand
    {
        public readonly int Tick;
        public readonly long Sequence;
        public readonly CommandKind Kind;
        public readonly int Value;

        public InputCommand(int tick, long sequence, CommandKind kind, int value)
        {
            Tick = tick;
            Sequence = sequence;
            Kind = kind;
            Value = value;
        }
    }

    /// <summary>Commands are ordered by (tick, sequence). Duplicate keys never replay.</summary>
    public sealed class InputCommandQueue
    {
        private readonly SortedDictionary<int, List<InputCommand>> pending =
            new SortedDictionary<int, List<InputCommand>>();
        private readonly HashSet<string> commandKeys = new HashSet<string>();

        public bool Submit(InputCommand command, int nextTick, int lastTick)
        {
            if (command.Tick < nextTick || command.Tick > lastTick ||
                command.Sequence < 0 || !Enum.IsDefined(typeof(CommandKind), command.Kind)) return false;
            string key = command.Tick + ":" + command.Sequence;
            if (!commandKeys.Add(key)) return false;
            if (!pending.TryGetValue(command.Tick, out var list))
            {
                list = new List<InputCommand>();
                pending.Add(command.Tick, list);
            }
            list.Add(command);
            return true;
        }

        public List<InputCommand> TakeForTick(int tick)
        {
            if (!pending.TryGetValue(tick, out var list)) return new List<InputCommand>(0);
            pending.Remove(tick);
            list.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            return list;
        }

        public void Clear() { pending.Clear(); commandKeys.Clear(); }
    }
}

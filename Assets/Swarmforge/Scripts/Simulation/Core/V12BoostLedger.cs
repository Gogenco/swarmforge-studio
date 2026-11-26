using System;

namespace Swarmforge.Simulation
{
    /// <summary>
    /// One active, one pending reward and one reserved terminal entitlement.
    /// All transitions happen on the authoritative 30Hz simulation tick.
    /// </summary>
    public sealed class V12BoostLedger
    {
        public int ActiveMultiplier { get; private set; } = 1;
        public int ActiveEndsAtTick { get; private set; } = -1;
        public int PendingMultiplier { get; private set; } = 1;
        public int PendingDurationTicks { get; private set; }
        public int PendingEarliestTick { get; private set; }
        public int TerminalPromises { get; private set; }
        public int RewardsEarned { get; private set; }

        public bool HasActive { get { return ActiveMultiplier > 1; } }
        public bool HasPending { get { return PendingMultiplier > 1; } }
        public int OccupiedSlots { get { return (HasActive ? 1 : 0) + (HasPending ? 1 : 0) + TerminalPromises; } }
        public bool CanReserve { get { return OccupiedSlots < 2; } }

        public bool ReserveTerminal()
        {
            if (!CanReserve) return false;
            TerminalPromises++;
            return true;
        }

        public void ReleaseTerminal()
        {
            if (TerminalPromises <= 0) throw new InvalidOperationException("Missing terminal promise.");
            TerminalPromises--;
        }

        public void Earn(int multiplier, int durationTicks, int awardedOnTick, bool consumesPromise)
        {
            if (multiplier < 2 || durationTicks < 1 || awardedOnTick < 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            if (consumesPromise)
            {
                if (TerminalPromises <= 0) throw new InvalidOperationException("Terminal box reward is not reserved.");
                TerminalPromises--;
            }
            if (OccupiedSlots >= 2)
                throw new InvalidOperationException("Reward capacity exceeded; box must have been locked before dispatch.");
            if (HasPending)
                throw new InvalidOperationException("Pending slot already occupied.");
            PendingMultiplier = multiplier;
            PendingDurationTicks = durationTicks;
            PendingEarliestTick = checked(awardedOnTick + 1);
            RewardsEarned++;
        }

        public bool BeginTick(int tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            bool changed = false;
            if (HasActive && tick >= ActiveEndsAtTick)
            {
                ActiveMultiplier = 1;
                ActiveEndsAtTick = -1;
                changed = true;
            }
            if (!HasActive && HasPending && tick >= PendingEarliestTick)
            {
                ActiveMultiplier = PendingMultiplier;
                ActiveEndsAtTick = checked(tick + PendingDurationTicks);
                PendingMultiplier = 1;
                PendingDurationTicks = 0;
                PendingEarliestTick = 0;
                changed = true;
            }
            return changed;
        }

        public int RemainingTicks(int tick)
        {
            return HasActive ? Math.Max(0, ActiveEndsAtTick - tick) : 0;
        }
    }
}

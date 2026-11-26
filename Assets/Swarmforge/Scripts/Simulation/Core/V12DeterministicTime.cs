using System;

namespace Swarmforge.Simulation
{
    /// <summary>
    /// P2-only value type for exact within-tick event time. Unit: 30Hz tick.
    /// Not connected to the legacy simulation or Battle scene.
    /// No float, frame delta, rounding to the next tick, or platform hash.
    /// </summary>
    public readonly struct V12TickTime : IComparable<V12TickTime>, IEquatable<V12TickTime>
    {
        public readonly long Tick;
        public readonly long Numerator;
        public readonly long Denominator;

        public V12TickTime(long tick, long numerator = 0, long denominator = 1)
        {
            if (tick < 0 || numerator < 0 || denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(numerator), "Time must be nonnegative with positive denominator.");
            Tick = checked(tick + numerator / denominator);
            long remainder = numerator % denominator;
            if (remainder == 0)
            {
                Numerator = 0;
                Denominator = 1;
            }
            else
            {
                long divisor = GreatestCommonDivisor(remainder, denominator);
                Numerator = remainder / divisor;
                Denominator = denominator / divisor;
            }
        }

        public V12TickTime AfterCooldown(int cooldownTicks)
        {
            EnsureInitialized();
            if (cooldownTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(cooldownTicks), "A repeat strike requires positive cooldown.");
            return new V12TickTime(checked(Tick + cooldownTicks), Numerator, Denominator);
        }

        public int CompareTo(V12TickTime other)
        {
            EnsureInitialized();
            other.EnsureInitialized();
            int tickOrder = Tick.CompareTo(other.Tick);
            if (tickOrder != 0) return tickOrder;
            // Exact cross multiplication, never a truncated fraction. If the
            // representation exceeds Int64, fail loudly and request smaller
            // validated fixed-point input ranges; do not silently change TOI.
            long left = checked(Numerator * other.Denominator);
            long right = checked(other.Numerator * Denominator);
            return left.CompareTo(right);
        }

        public bool Equals(V12TickTime other)
        {
            return Tick == other.Tick && Numerator == other.Numerator && Denominator == other.Denominator;
        }

        public override bool Equals(object obj)
        {
            return obj is V12TickTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            // Only for local in-memory collections; never for authoritative
            // match replay or versioned StateHash/EventHash.
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Tick.GetHashCode();
                hash = hash * 31 + Numerator.GetHashCode();
                hash = hash * 31 + Denominator.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            EnsureInitialized();
            return Numerator == 0 ? Tick.ToString() : Tick + "+" + Numerator + "/" + Denominator;
        }

        private void EnsureInitialized()
        {
            if (Denominator <= 0)
                throw new InvalidOperationException("Uninitialized exact time has no valid rational denominator.");
        }

        private static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                long next = a % b;
                a = b;
                b = next;
            }
            return a;
        }
    }

    /// <summary>Deterministic canonical phase for the same exact (tick, TOI).</summary>
    public enum V12ContactPhase
    {
        EnemyCombat = 0,
        BoxImpact = 1,
        FortressImpact = 2,
        BaseBreach = 3,
        TerminalOutcome = 4
    }

    /// <summary>
    /// Event log sort key, not a contact generator. The simulator must freeze
    /// the combat HP batch BEFORE applying BoxImpact, as Director C2/C4 require.
    /// </summary>
    public readonly struct V12ContactEventKey : IComparable<V12ContactEventKey>
    {
        public readonly V12TickTime Time;
        public readonly V12ContactPhase Phase;
        public readonly long TargetId;
        public readonly long ActorId;

        public V12ContactEventKey(V12TickTime time, V12ContactPhase phase, long targetId, long actorId)
        {
            if (targetId < 0 || actorId < 0 || phase < V12ContactPhase.EnemyCombat || phase > V12ContactPhase.TerminalOutcome)
                throw new ArgumentOutOfRangeException(nameof(targetId), "Event IDs and phase must be valid.");
            Time = time;
            Phase = phase;
            TargetId = targetId;
            ActorId = actorId;
        }

        public int CompareTo(V12ContactEventKey other)
        {
            int order = Time.CompareTo(other.Time);
            if (order != 0) return order;
            order = Phase.CompareTo(other.Phase);
            if (order != 0) return order;
            order = TargetId.CompareTo(other.TargetId);
            return order != 0 ? order : ActorId.CompareTo(other.ActorId);
        }
    }
}

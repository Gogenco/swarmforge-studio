using System;
using NUnit.Framework;
using Swarmforge.Simulation;

namespace Swarmforge.Tests
{
    public sealed class V12DeterministicTimeTests
    {
        [Test]
        public void ExactFractionalCooldown_HitsAt115AndOneQuarter_Not116()
        {
            V12TickTime strike = new V12TickTime(100, 1, 4);
            V12TickTime next = strike.AfterCooldown(15);
            Assert.That(next.Tick, Is.EqualTo(115L));
            Assert.That(next.Numerator, Is.EqualTo(1L));
            Assert.That(next.Denominator, Is.EqualTo(4L));
            Assert.That(next.CompareTo(new V12TickTime(115)), Is.GreaterThan(0));
            Assert.That(next.CompareTo(new V12TickTime(116)), Is.LessThan(0));
            Assert.That(next.ToString(), Is.EqualTo("115+1/4"));
        }

        [Test]
        public void FractionsNormalizeAndSortByExactRationalValue()
        {
            V12TickTime a = new V12TickTime(100, 2, 8);
            V12TickTime b = new V12TickTime(100, 1, 4);
            V12TickTime c = new V12TickTime(99, 5, 4);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(b.Equals(c), Is.True);
            Assert.That(a.CompareTo(new V12TickTime(100, 1, 3)), Is.LessThan(0));
            Assert.That(new V12TickTime(100, 3, 4).CompareTo(a), Is.GreaterThan(0));
        }

        [Test]
        public void CombatBeforeBoxBeforeTerminal_IndependentOfArrivalOrder()
        {
            var atSameTime = new V12TickTime(20, 1, 7);
            var keys = new[]
            {
                new V12ContactEventKey(atSameTime, V12ContactPhase.BoxImpact, 2, 20),
                new V12ContactEventKey(atSameTime, V12ContactPhase.EnemyCombat, 8, 30),
                new V12ContactEventKey(atSameTime, V12ContactPhase.TerminalOutcome, 0, 0),
                new V12ContactEventKey(atSameTime, V12ContactPhase.EnemyCombat, 8, 10)
            };
            Array.Sort(keys);
            Assert.That(keys[0].Phase, Is.EqualTo(V12ContactPhase.EnemyCombat));
            Assert.That(keys[0].ActorId, Is.EqualTo(10L));
            Assert.That(keys[1].ActorId, Is.EqualTo(30L));
            Assert.That(keys[2].Phase, Is.EqualTo(V12ContactPhase.BoxImpact));
            Assert.That(keys[3].Phase, Is.EqualTo(V12ContactPhase.TerminalOutcome));
        }

        [Test]
        public void InvalidCooldownAndOverflowRejectUnsafeRounding()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new V12TickTime(1).AfterCooldown(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new V12TickTime(1, 1, 0));
            Assert.Throws<OverflowException>(() => new V12TickTime(long.MaxValue).AfterCooldown(1));
            var a = new V12TickTime(10, 3999999999L, 4000000000L);
            var b = new V12TickTime(10, 3999999998L, 4000000001L);
            Assert.Throws<OverflowException>(() => a.CompareTo(b));
            Assert.Throws<InvalidOperationException>(() => default(V12TickTime).CompareTo(new V12TickTime(0)));
        }
    }
}

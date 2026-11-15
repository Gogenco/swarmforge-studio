using System;
using NUnit.Framework;
using Swarmforge.Presentation;
using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Tests
{
    public sealed class V12FormationLayoutTests
    {
        [Test]
        public void ConsecutiveIds_StayVisuallySeparateWithinP2Group()
        {
            // The same-tick ×2 pairs and same-lane enemy slots have consecutive
            // IDs; both groups were visually overlapping before this change.
            for (long id = 1; id < 48; id++)
            {
                Vector2 a = V12FormationLayout.ForId(id);
                Vector2 b = V12FormationLayout.ForId(id + 1);
                Assert.That(Vector2.Distance(a, b), Is.GreaterThanOrEqualTo(.88f),
                    "Visual separation too small for red spheres at ID " + id);
            }
        }

        [Test]
        public void FourSlotFormation_IsBoundedAndPairwiseSeparated()
        {
            for (long a = 1; a <= 4; a++)
            {
                Vector2 offset = V12FormationLayout.ForId(a);
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(.52f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(.45f));
                for (long b = a + 1; b <= 4; b++)
                    Assert.That(Vector2.Distance(offset, V12FormationLayout.ForId(b)),
                        Is.GreaterThanOrEqualTo(.88f));
            }
        }

        [Test]
        public void Formation_IsStableIndependentOfQueryOrderAndFrame()
        {
            Vector2 before = V12FormationLayout.ForId(102);
            for (long id = 200; id >= 1; id--) V12FormationLayout.ForId(id);
            Vector2 after = V12FormationLayout.ForId(102);
            Assert.That(after, Is.EqualTo(before));
            Assert.Throws<ArgumentOutOfRangeException>(() => V12FormationLayout.ForId(0));
        }

        [Test]
        public void PresentationOnlyOffsets_CannotChangeAuthoritativeStateHash()
        {
            var sim = new V12TrialSimulation(new V12MatchConfig(V12P2Presets.Create(2)));
            for (int tick = 0; tick <= 180; tick++) sim.AdvanceOneTick();
            V12TrialSnapshot before = sim.Snapshot();
            foreach (V12TrialUnitView bot in before.Bots) V12FormationLayout.ForId(bot.Id);
            foreach (V12TrialUnitView enemy in before.Enemies) V12FormationLayout.ForId(enemy.Id);
            V12TrialSnapshot after = sim.Snapshot();
            Assert.That(after.StateHash, Is.EqualTo(before.StateHash));
            Assert.That(after.EventHash, Is.EqualTo(before.EventHash));
            Assert.That(after.CommandHash, Is.EqualTo(before.CommandHash));
            Assert.That(after.EnemiesSpawned, Is.EqualTo(4));
        }
    }
}

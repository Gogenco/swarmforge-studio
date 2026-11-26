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
        public void SoloMiniBot_NeverDeviatesFromExactCannonMuzzleLane()
        {
            foreach (int position in new[] { 0, 340, 800, 2400, 5100, 51000 })
                Assert.That(V12FormationLayout.ForBot(position, 51000),
                    Is.EqualTo(Vector2.zero), "Every green shot must stay on its true aimed X and Z.");
        }

        [Test]
        public void BoostTwins_SameXButSeparatedByActualSequentialSpawnTime()
        {
            var sim = new V12TrialSimulation(new V12MatchConfig(V12P2Presets.Create(2)));
            Assert.That(sim.SubmitAim(0, 1, -950), Is.True);  // Open the box.
            Assert.That(sim.SubmitAim(180, 2, 0), Is.True);    // Fire boosted pair on center.
            for (int tick = 0; tick <= 188; tick++) sim.AdvanceOneTick();
            V12TrialUnitView first = default(V12TrialUnitView), second = default(V12TrialUnitView);
            int count = 0;
            foreach (V12TrialUnitView bot in sim.Snapshot().Bots)
                if (bot.X == 0)
                {
                    if (count == 0) first = bot;
                    else if (count == 1) second = bot;
                    count++;
                }
            Assert.That(count, Is.EqualTo(2), "Actual boosted robots must both be created.");
            Assert.That(first.X, Is.EqualTo(second.X));
            Assert.That(Mathf.Abs(first.Position - second.Position),
                Is.GreaterThanOrEqualTo(7 * 340),
                "Real tick-spaced bots must have a visible fore-aft gap, not overlapping capsules.");
            Assert.That(V12FormationLayout.ForBot(first.Position, 51000), Is.EqualTo(Vector2.zero));
            Assert.That(V12FormationLayout.ForBot(second.Position, 51000), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void GreenBots_NeverAcquireLateralOffsetAfterPartnerDeathOrReorder()
        {
            for (int position = 0; position <= 51000; position += 170)
                Assert.That(V12FormationLayout.ForBot(position, 51000), Is.EqualTo(Vector2.zero));
            // No visual cohort cache: killing or removing any bot cannot
            // change the position of remaining green bots across frames.
            Assert.That(V12FormationLayout.ForBot(3100, 51000),
                Is.EqualTo(V12FormationLayout.ForBot(3100, 51000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => V12FormationLayout.ForBot(0, 0));
        }

        [Test]
        public void ConsecutiveIds_StayVisuallySeparateWithinP2Group()
        {
            // Red enemy presentation keeps its four stable slots.
            for (long id = 1; id < 48; id++)
            {
                Vector2 a = V12FormationLayout.ForId(id);
                Vector2 b = V12FormationLayout.ForId(id + 1);
                Assert.That(Vector2.Distance(a, b), Is.GreaterThanOrEqualTo(1.39f));
            }
        }

        [Test]
        public void FourSlotFormation_IsBoundedAndPairwiseSeparated()
        {
            for (long a = 1; a <= 4; a++)
            {
                Vector2 offset = V12FormationLayout.ForId(a);
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(.70f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(.70f));
                for (long b = a + 1; b <= 4; b++)
                    Assert.That(Vector2.Distance(offset, V12FormationLayout.ForId(b)),
                        Is.GreaterThanOrEqualTo(1.39f));
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
            foreach (V12TrialUnitView bot in before.Bots)
                V12FormationLayout.ForBot(bot.Position, 51000);
            foreach (V12TrialUnitView enemy in before.Enemies) V12FormationLayout.ForId(enemy.Id);
            V12TrialSnapshot after = sim.Snapshot();
            Assert.That(after.StateHash, Is.EqualTo(before.StateHash));
            Assert.That(after.EventHash, Is.EqualTo(before.EventHash));
            Assert.That(after.CommandHash, Is.EqualTo(before.CommandHash));
            Assert.That(after.EnemiesSpawned, Is.EqualTo(4));
        }
    }
}

using System;
using NUnit.Framework;
using Swarmforge.Presentation;
using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Tests
{
    public sealed class V12FormationLayoutTests
    {
        private static V12TrialUnitView Bot(long id, int x, int position)
        {
            return new V12TrialUnitView(id, x, position, 2, 2, false);
        }

        [Test]
        public void SoloMiniBot_NeverDeviatesFromExactCannonMuzzleLane()
        {
            var cohorts = new V12BotVisualCohorts();
            for (long id = 1; id <= 12; id++)
            {
                V12TrialUnitView bot = Bot(id, (int)(id * 30), 0);
                float slot = cohorts.GetOrAssign(bot, new[] { bot });
                Assert.That(slot, Is.EqualTo(0f));
                foreach (int position in new[] { 0, 340, 800, 2400, 5100, 51000 })
                    Assert.That(V12FormationLayout.ForBot(position, 51000, slot),
                        Is.EqualTo(Vector2.zero), "Single MiniBot must remain centered at every distance.");
            }
        }

        [Test]
        public void BoostTwins_ShareExactMuzzleThenSmoothlyFanOutAfterTravel()
        {
            var cohorts = new V12BotVisualCohorts();
            V12TrialUnitView left = Bot(41, -800, 0), right = Bot(42, -800, 0);
            var both = new[] { right, left }; // Reverse list order must not change ID slots.
            float leftSlot = cohorts.GetOrAssign(left, both);
            float rightSlot = cohorts.GetOrAssign(right, both);
            Assert.That(leftSlot, Is.EqualTo(-1f));
            Assert.That(rightSlot, Is.EqualTo(1f));
            Assert.That(V12FormationLayout.ForBot(0, 51000, leftSlot), Is.EqualTo(Vector2.zero));
            Assert.That(V12FormationLayout.ForBot(0, 51000, rightSlot), Is.EqualTo(Vector2.zero));
            Assert.That(V12FormationLayout.ForBot(600, 51000, leftSlot), Is.EqualTo(Vector2.zero),
                "The bots must clear the barrel before lateral separation starts.");

            float previous = 0f;
            for (int position = 0; position <= 5100; position += 100)
            {
                Vector2 l = V12FormationLayout.ForBot(position, 51000, leftSlot);
                Vector2 r = V12FormationLayout.ForBot(position, 51000, rightSlot);
                float separation = r.x - l.x;
                Assert.That(l.y, Is.EqualTo(0f));
                Assert.That(r.y, Is.EqualTo(0f), "No forward/backward spawn teleporting.");
                Assert.That(l.x, Is.EqualTo(-r.x).Within(.00001f));
                Assert.That(separation, Is.GreaterThanOrEqualTo(previous - .00001f),
                    "Fan-out must be monotonic, not oscillate or snap between slots.");
                Assert.That(separation, Is.LessThanOrEqualTo(1.04f + .00001f));
                previous = separation;
            }
            Assert.That(previous, Is.EqualTo(1.04f).Within(.00001f),
                "Paired bots must become distinctly visible after leaving the muzzle.");
        }

        [Test]
        public void TwinSlot_RemainsStableWhenPartnerDisappearsOrFrameOrderChanges()
        {
            var cohorts = new V12BotVisualCohorts();
            V12TrialUnitView a = Bot(71, 800, 0), b = Bot(72, 800, 0);
            Assert.That(cohorts.GetOrAssign(b, new[] { b, a }), Is.EqualTo(1f));
            Assert.That(cohorts.GetOrAssign(a, new[] { a, b }), Is.EqualTo(-1f));
            cohorts.Forget(a.Id);
            Assert.That(cohorts.GetOrAssign(b, new[] { b }), Is.EqualTo(1f),
                "A surviving paired MiniBot must not snap to the center.");
            Vector2 first = V12FormationLayout.ForBot(3100, 51000, cohorts.GetOrAssign(b, new[] { b }));
            Vector2 second = V12FormationLayout.ForBot(3100, 51000, cohorts.GetOrAssign(b, new[] { b }));
            Assert.That(first, Is.EqualTo(second), "Animation must depend only on distance, not frame time.");
            Assert.Throws<ArgumentOutOfRangeException>(() => V12FormationLayout.ForBot(0, 0, 1f));
        }

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
            var cohorts = new V12BotVisualCohorts();
            foreach (V12TrialUnitView bot in before.Bots)
                V12FormationLayout.ForBot(bot.Position, 51000, cohorts.GetOrAssign(bot, before.Bots));
            foreach (V12TrialUnitView enemy in before.Enemies) V12FormationLayout.ForId(enemy.Id);
            V12TrialSnapshot after = sim.Snapshot();
            Assert.That(after.StateHash, Is.EqualTo(before.StateHash));
            Assert.That(after.EventHash, Is.EqualTo(before.EventHash));
            Assert.That(after.CommandHash, Is.EqualTo(before.CommandHash));
            Assert.That(after.EnemiesSpawned, Is.EqualTo(4));
        }
    }
}

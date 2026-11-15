using System;
using System.Reflection;
using NUnit.Framework;
using Swarmforge.Simulation;

namespace Swarmforge.Tests
{
    public sealed class V12P2PlayableTests
    {
        private const ulong FnvOffset = 14695981039346656037UL;

        private static ulong InvokePrivateHash(Type argumentType, object value)
        {
            MethodInfo hash = typeof(V12TrialSimulation).GetMethod("Hash",
                BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(ulong).MakeByRefType(), argumentType }, null);
            Assert.That(hash, Is.Not.Null, "Both signed and unsigned 64-bit hashes must be supported.");
            object[] arguments = { FnvOffset, value };
            hash.Invoke(null, arguments);
            return (ulong)arguments[0];
        }

        [Test]
        public void Hash_UnsignedHighBit_UsesEveryByteInCanonicalLittleEndianOrder()
        {
            const ulong input = 0xFEDCBA9876543210UL;
            ulong expected = FnvOffset;
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    expected ^= (byte)(input >> (8 * i));
                    expected *= 1099511628211UL;
                }
            }
            Assert.That(InvokePrivateHash(typeof(ulong), input), Is.EqualTo(expected));
        }

        [Test]
        public void Hash_SignedNegativeSentinel_PreservesOriginalEightBytes()
        {
            // Explicitly guard the old signed path while the new ulong path
            // resolves Owner's CS1503 at ConfigHash/EventHash/CommandHash.
            ulong signed = InvokePrivateHash(typeof(long), -1L);
            ulong unsigned = InvokePrivateHash(typeof(ulong), ulong.MaxValue);
            Assert.That(signed, Is.EqualTo(unsigned));
        }

        [Test]
        public void SnapshotHash_IncludesUnsignedConfigEventAndCommandFingerprints()
        {
            V12TrialSimulation baseline = NewTrial();
            V12TrialSimulation aimed = NewTrial();
            Assert.That(aimed.SubmitAim(0, 1, -800), Is.True);
            baseline.AdvanceOneTick();
            aimed.AdvanceOneTick();
            Assert.That(aimed.Snapshot().ConfigHash, Is.EqualTo(baseline.Snapshot().ConfigHash));
            Assert.That(aimed.Snapshot().EventHash, Is.Not.EqualTo(baseline.Snapshot().EventHash));
            Assert.That(aimed.Snapshot().CommandHash, Is.Not.EqualTo(baseline.Snapshot().CommandHash));
            Assert.That(aimed.Snapshot().StateHash, Is.Not.EqualTo(baseline.Snapshot().StateHash));
        }

        private static V12TrialSimulation NewTrial(int hp = 2)
        {
            return new V12TrialSimulation(new V12MatchConfig(V12P2Presets.Create(hp)));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void P2_SeparateHpProfiles_AreExactlyOneParameterApart(int hp)
        {
            V12BalanceDefinition a = V12P2Presets.Create(2);
            V12BalanceDefinition b = V12P2Presets.Create(3);
            Assert.That(a.MiniBotMaxHP, Is.EqualTo(2));
            Assert.That(b.MiniBotMaxHP, Is.EqualTo(3));
            Assert.That(a.TickRate, Is.EqualTo(30));
            Assert.That(a.ShotPeriodTicks, Is.EqualTo(30));
            Assert.That(b.ShotPeriodTicks, Is.EqualTo(30));
            Assert.That(a.FirstShotTick, Is.EqualTo(0));
            Assert.That(a.MatchTicks, Is.EqualTo(1800));
            Assert.That(a.Boxes.Length, Is.EqualTo(1));
            Assert.That(a.Boxes[0].MaxHP, Is.EqualTo(5));
            Assert.That(a.Boxes[0].Multiplier, Is.EqualTo(2));
            Assert.That(a.Boxes[0].DurationTicks, Is.EqualTo(360));
            Assert.That(a.Waves.Length, Is.EqualTo(1));
            Assert.That(a.Waves[0].Spawns.Length, Is.EqualTo(2));
            Assert.That(new V12MatchConfig(a).ConfigHash, Is.Not.EqualTo(new V12MatchConfig(b).ConfigHash));
            Assert.That(NewTrial(hp).MatchConfig.CreateEditableCopy().MiniBotMaxHP, Is.EqualTo(hp));
        }

        [Test]
        public void Box_ExactlyFiveContacts_Break_AndGrantNextTickX2For360Ticks()
        {
            // Keep the test alive through the entire boost window, independently
            // of the separately verified four-enemy BaseHP=3 defeat scenario.
            V12BalanceDefinition balance = V12P2Presets.Create(2);
            balance.BaseHP = 10;
            var sim = new V12TrialSimulation(new V12MatchConfig(balance));
            Assert.That(sim.SubmitAim(0, 1, -800), Is.True);
            int rewardTick = -1;
            for (int t = 0; t < 240 && rewardTick < 0; t++)
            {
                V12TrialSnapshot s = sim.AdvanceOneTick();
                foreach (V12TrialEvent e in s.RecentEvents)
                    if (e.Kind == V12TrialEventKind.RewardEarned) rewardTick = (int)e.Time.Tick;
            }
            Assert.That(rewardTick, Is.GreaterThanOrEqualTo(0), "Five bots must actually reach the box.");
            V12TrialSnapshot afterImpact = sim.Snapshot();
            Assert.That(afterImpact.BoxHP, Is.EqualTo(0));
            Assert.That(afterImpact.BoxHits, Is.EqualTo(5));
            Assert.That(afterImpact.BoostMultiplier, Is.EqualTo(1), "Reward cannot affect the impact tick.");
            Assert.That(afterImpact.QueuedMultiplier, Is.EqualTo(2));
            Assert.That(afterImpact.TerminalPromises, Is.EqualTo(0));
            sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().BoostMultiplier, Is.EqualTo(2));
            Assert.That(sim.Snapshot().BoostRemainingTicks, Is.EqualTo(360));
            for (int t = rewardTick + 2; t <= rewardTick + 360; t++) sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().BoostMultiplier, Is.EqualTo(2), "End tick must be exclusive.");
            sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().BoostMultiplier, Is.EqualTo(1));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void FireRate_X1_ExactlyOneBotEveryThirtyTicksFromTickZero(int hp)
        {
            V12TrialSimulation sim = NewTrial(hp);
            for (int tick = 0; tick <= 90; tick++)
            {
                V12TrialSnapshot snapshot = sim.AdvanceOneTick();
                int expected = tick / 30 + 1;
                Assert.That(snapshot.RawShots, Is.EqualTo(expected), "Raw shot on tick " + tick);
                Assert.That(snapshot.BotsProduced, Is.EqualTo(expected), "Bot count on tick " + tick);
                int rawOnTick = 0, botsOnTick = 0;
                foreach (V12TrialEvent e in snapshot.RecentEvents)
                {
                    if (e.Kind == V12TrialEventKind.RawShot)
                    {
                        rawOnTick++;
                        Assert.That(e.Time.Tick, Is.EqualTo(tick));
                        Assert.That(e.Value, Is.EqualTo(1));
                    }
                    if (e.Kind == V12TrialEventKind.BotSpawned) botsOnTick++;
                }
                int expectedOnTick = tick % 30 == 0 ? 1 : 0;
                Assert.That(rawOnTick, Is.EqualTo(expectedOnTick));
                Assert.That(botsOnTick, Is.EqualTo(expectedOnTick));
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        public void FireRate_X2_TwoBotsAtEachShot_ThenReturnsToOne(int hp)
        {
            V12BalanceDefinition balance = V12P2Presets.Create(hp);
            balance.BaseHP = 10;
            var sim = new V12TrialSimulation(new V12MatchConfig(balance));
            Assert.That(sim.SubmitAim(0, 1, -800), Is.True);
            int rewardTick = -1, activationTick = -1, expiryTick = -1;
            int boostedShots = 0, boostedBots = 0;
            for (int tick = 0; tick <= 510; tick++)
            {
                V12TrialSnapshot snapshot = sim.AdvanceOneTick();
                int rawOnTick = 0, botsOnTick = 0;
                foreach (V12TrialEvent e in snapshot.RecentEvents)
                {
                    if (e.Kind == V12TrialEventKind.RewardEarned) rewardTick = tick;
                    if (e.Kind == V12TrialEventKind.BoostStarted) activationTick = tick;
                    if (e.Kind == V12TrialEventKind.BoostExpired) expiryTick = tick;
                    if (e.Kind == V12TrialEventKind.RawShot)
                    {
                        rawOnTick++;
                        Assert.That(tick % 30, Is.EqualTo(0), "Boost must not change the shot clock.");
                        Assert.That(e.Value, Is.EqualTo(snapshot.BoostMultiplier));
                        if (e.Value == 2) boostedShots++;
                    }
                    if (e.Kind == V12TrialEventKind.BotSpawned) botsOnTick++;
                }
                Assert.That(rawOnTick, Is.LessThanOrEqualTo(1));
                if (rawOnTick == 1)
                {
                    Assert.That(botsOnTick, Is.EqualTo(snapshot.BoostMultiplier),
                        "A raw shot should produce one or two bots on tick " + tick);
                    if (snapshot.BoostMultiplier == 2) boostedBots += botsOnTick;
                }
                Assert.That(snapshot.Outcome, Is.EqualTo(MatchOutcome.Playing));
            }
            Assert.That(rewardTick, Is.EqualTo(146), "Five box hits are spaced 30 ticks apart.");
            Assert.That(activationTick, Is.EqualTo(147), "Reward is not active on its impact tick.");
            Assert.That(expiryTick, Is.EqualTo(507), "Twelve seconds are 360 simulation ticks.");
            Assert.That(boostedShots, Is.EqualTo(12));
            Assert.That(boostedBots, Is.EqualTo(24));
            Assert.That(sim.Snapshot().BoostMultiplier, Is.EqualTo(1));
            Assert.That(sim.Snapshot().BoxHits, Is.EqualTo(5));
            V12TrialEvent finalShot = default(V12TrialEvent);
            foreach (V12TrialEvent e in sim.Snapshot().RecentEvents)
                if (e.Kind == V12TrialEventKind.RawShot) finalShot = e;
            Assert.That(finalShot.Time.Tick, Is.EqualTo(510));
            Assert.That(finalShot.Value, Is.EqualTo(1), "Post-boost shot must produce only one bot.");
        }

        [TestCase(2)]
        [TestCase(3)]
        public void Restart_NewSimulationResetsOnePerSecondScheduleAndReplayState(int hp)
        {
            var config = new V12MatchConfig(V12P2Presets.Create(hp));
            var previous = new V12TrialSimulation(config);
            V12TrialSnapshot originalFirst = previous.AdvanceOneTick();
            for (int tick = 1; tick <= 100; tick++) previous.AdvanceOneTick();
            var restarted = new V12TrialSimulation(config);
            Assert.That(restarted.NextTick, Is.EqualTo(0));
            V12TrialSnapshot firstAgain = restarted.AdvanceOneTick();
            Assert.That(firstAgain.Tick, Is.EqualTo(0));
            Assert.That(firstAgain.RawShots, Is.EqualTo(1));
            Assert.That(firstAgain.BotsProduced, Is.EqualTo(1));
            Assert.That(firstAgain.StateHash, Is.EqualTo(originalFirst.StateHash));
            Assert.That(firstAgain.EventHash, Is.EqualTo(originalFirst.EventHash));
            for (int tick = 1; tick <= 29; tick++) restarted.AdvanceOneTick();
            Assert.That(restarted.Snapshot().RawShots, Is.EqualTo(1));
            restarted.AdvanceOneTick();
            Assert.That(restarted.Snapshot().RawShots, Is.EqualTo(2));
        }

        [Test]
        public void Capacity_OneActiveOneQueued_AndReservedTerminalCannotOvercommit()
        {
            var ledger = new V12BoostLedger();
            Assert.That(ledger.ReserveTerminal(), Is.True);
            ledger.Earn(2, 360, 0, true);
            Assert.That(ledger.HasPending, Is.True);
            Assert.That(ledger.BeginTick(1), Is.True);
            Assert.That(ledger.ActiveMultiplier, Is.EqualTo(2));
            Assert.That(ledger.ActiveEndsAtTick, Is.EqualTo(361));
            Assert.That(ledger.ReserveTerminal(), Is.True);
            ledger.Earn(3, 90, 5, true);
            Assert.That(ledger.OccupiedSlots, Is.EqualTo(2));
            Assert.That(ledger.CanReserve, Is.False);
            Assert.That(ledger.ReserveTerminal(), Is.False);
            Assert.Throws<InvalidOperationException>(() => ledger.Earn(5, 20, 6, false));
            Assert.That(ledger.BeginTick(360), Is.False);
            Assert.That(ledger.BeginTick(361), Is.True);
            Assert.That(ledger.ActiveMultiplier, Is.EqualTo(3));
            Assert.That(ledger.ActiveEndsAtTick, Is.EqualTo(451));
        }

        [Test]
        public void GroupWave_ExactlyFourEnemiesShareTick180()
        {
            V12TrialSimulation sim = NewTrial();
            for (int i = 0; i <= 180; i++) sim.AdvanceOneTick();
            V12TrialSnapshot s = sim.Snapshot();
            Assert.That(s.WaveGroupsSpawned, Is.EqualTo(1));
            Assert.That(s.EnemiesSpawned, Is.EqualTo(4));
            int onSameTick = 0;
            foreach (V12TrialEvent e in s.RecentEvents)
                if (e.Kind == V12TrialEventKind.EnemySpawned && e.Time.Tick == 180) onSameTick++;
            Assert.That(onSameTick, Is.EqualTo(4));
        }

        [Test]
        public void Combat_CollisionCommitsBotAndEnemyPreHpDamage()
        {
            V12BalanceDefinition config = V12P2Presets.Create(2);
            config.Boxes[0] = new V12BoxBalance("X2", 800, 9000, 450, 5, 2, 360);
            config.Waves[0] = new V12WaveBalance(1, 0, new[] { new V12WaveSpawn("SCOUT", -1, 1) });
            config.EnemyPathLength = 1800;
            config.EnemyTypes[0] = new V12EnemyBalance("SCOUT", 1, 1, 230, 15);
            var sim = new V12TrialSimulation(new V12MatchConfig(config));
            Assert.That(sim.SubmitAim(0, 1, -800), Is.True);
            for (int t = 0; t < 11; t++) sim.AdvanceOneTick();
            bool botHit = false, enemyHit = false, destroyed = false;
            foreach (V12TrialEvent e in sim.History)
            {
                if (e.Kind == V12TrialEventKind.BotStruck) botHit = true;
                if (e.Kind == V12TrialEventKind.EnemyStruck) enemyHit = true;
                if (e.Kind == V12TrialEventKind.EnemyDestroyed) destroyed = true;
            }
            Assert.That(botHit, Is.True);
            Assert.That(enemyHit, Is.True, "Killing the enemy in the same batch must not suppress its retaliation.");
            Assert.That(destroyed, Is.True);
        }

        [Test]
        public void Terminal_FortressZeroMeansVictory()
        {
            V12BalanceDefinition cfg = V12P2Presets.Create(2);
            cfg.FortressHP = 1;
            var sim = new V12TrialSimulation(new V12MatchConfig(cfg));
            for (int i = 0; i < 200 && !sim.IsTerminal; i++) sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().Outcome, Is.EqualTo(MatchOutcome.Win));
        }

        [Test]
        public void Terminal_AllFourEnemiesBreachMeansDefeat()
        {
            var sim = NewTrial();
            for (int i = 0; i < 520 && !sim.IsTerminal; i++) sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().Outcome, Is.EqualTo(MatchOutcome.LoseBase));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void Terminal_NoBoxAndTenBaseHp_ExpiresAsTimeoutAfterExactly1800Ticks(int hp)
        {
            V12BalanceDefinition balance = V12P2Presets.Create(hp);
            balance.BaseHP = 10;
            var sim = new V12TrialSimulation(new V12MatchConfig(balance));
            for (int tick = 0; tick < 1799; tick++)
                Assert.That(sim.AdvanceOneTick().Outcome, Is.EqualTo(MatchOutcome.Playing));
            V12TrialSnapshot terminal = sim.AdvanceOneTick();
            Assert.That(terminal.Tick, Is.EqualTo(1799));
            Assert.That(terminal.Outcome, Is.EqualTo(MatchOutcome.LoseTimeout));
            Assert.That(terminal.FortressHP, Is.GreaterThan(0));
            Assert.That(sim.NextTick, Is.EqualTo(1800));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void Balance_CanonicalFortress65ExceedsEvenIdealMaximumDamage(int hp)
        {
            // A strict *optimistic* bound: assume all bots hit the fortress,
            // no enemy casualties, and all 12 boosted shots occur in time.
            // Five bots must first be sacrificed to open the 5HP reward box.
            V12BalanceDefinition balance = V12P2Presets.Create(hp);
            int movementTicks = (balance.AttackPathLength + balance.MiniBotSpeedPerTick - 1) /
                balance.MiniBotSpeedPerTick;
            int latestShotTick = balance.MatchTicks - 1 - movementTicks;
            int scheduledShots = 1 + (latestShotTick - balance.FirstShotTick) / balance.ShotPeriodTicks;
            int maxBoostedShots = (balance.Boxes[0].DurationTicks + balance.ShotPeriodTicks - 1) /
                balance.ShotPeriodTicks;
            int upperBoundOnDamage = scheduledShots + maxBoostedShots - balance.Boxes[0].MaxHP;
            Assert.That(scheduledShots, Is.EqualTo(55));
            Assert.That(maxBoostedShots, Is.EqualTo(12));
            Assert.That(upperBoundOnDamage, Is.EqualTo(62));
            Assert.That(balance.FortressHP, Is.EqualTo(65), "Do not change Owner's fortress HP silently.");
            Assert.That(balance.FortressHP, Is.GreaterThan(upperBoundOnDamage),
                "Owner approval is required for a lower fortress HP or another time/balance change.");
        }

        [Test]
        public void Replay_IdenticalCommandsProduceIdenticalEventAndStateHashes()
        {
            V12TrialSimulation left = NewTrial(3), right = NewTrial(3);
            Assert.That(left.SubmitAim(0, 1, -800), Is.True);
            Assert.That(right.SubmitAim(0, 1, -800), Is.True);
            Assert.That(left.SubmitAim(240, 2, 800), Is.True);
            Assert.That(right.SubmitAim(240, 2, 800), Is.True);
            for (int i = 0; i < 420 && !left.IsTerminal && !right.IsTerminal; i++)
            {
                V12TrialSnapshot a = left.AdvanceOneTick();
                V12TrialSnapshot b = right.AdvanceOneTick();
                Assert.That(a.StateHash, Is.EqualTo(b.StateHash), "Divergence on tick " + i);
                Assert.That(a.EventHash, Is.EqualTo(b.EventHash));
            }
        }

        [Test]
        public void P2_DoesNotOverwriteP1_ThreeBoxBalanceFixture()
        {
            V12BalanceDefinition original = V12BalanceDefinition.CreateTrialPreset(2);
            Assert.That(original.Boxes.Length, Is.EqualTo(3));
            Assert.That(V12P2Presets.Create(2).Boxes.Length, Is.EqualTo(1));
        }
    }
}

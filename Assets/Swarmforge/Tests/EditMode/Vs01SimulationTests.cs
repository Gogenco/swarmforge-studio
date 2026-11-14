using System;
using System.Reflection;
using NUnit.Framework;
using Swarmforge.Presentation;
using Swarmforge.Simulation;

namespace Swarmforge.Tests
{
    public sealed class Vs01SimulationTests
    {
        private static LevelDefinition TestConfig()
        {
            return new LevelDefinition
            {
                LevelId = "EDIT_MODE_TEST", EnemyWaves = Array.Empty<EnemyWave>(),
                FortressHP = 99999, GatePosition = 0, AttackPathLength = 1000000,
                EnemyPathLength = 51000, ShotPeriodTicks = 100
            };
        }

        [Test]
        public void ShotPhase_HasExactly180Shots_AndLastShotAt1790()
        {
            LevelDefinition config = TestConfig();
            config.GatePosition = 0;
            config.ShotPeriodTicks = 10;
            config.AttackPathLength = 1000000;
            var sim = new MatchSimulation(config);
            for (int tick = 0; tick <= 1800; tick++) sim.AdvanceOneTick();
            MatchSnapshot end = sim.GetSnapshot();
            Assert.That(end.RawShots, Is.EqualTo(180));
            Assert.That(end.LastShotTick, Is.EqualTo(1790));
            Assert.That(end.Outcome, Is.EqualTo(MatchOutcome.LoseTimeout));
            Assert.That(end.TerminalTick, Is.EqualTo(1800));
            Assert.That(end.TerminalEvents, Is.EqualTo(1));
            Assert.That(end.ProducedTotal, Is.EqualTo(360));
            Assert.That(end.ActiveMiniBots, Is.EqualTo(360));
        }

        [TestCase(-700, 2, 20)]
        [TestCase(700, 3, 30)]
        public void TenShots_GateMultipliesExactlyOnce(int x, int multiplier, int produced)
        {
            var sim = new MatchSimulation(TestConfig());
            Assert.That(sim.Submit(new InputCommand(0, 1, CommandKind.MoveCannon, x)), Is.True);
            for (int tick = 0; tick <= 950; tick++) sim.AdvanceOneTick();
            MatchSnapshot result = sim.GetSnapshot();
            Assert.That(result.RawShots, Is.EqualTo(10));
            Assert.That(result.ProducedTotal, Is.EqualTo(produced));
            Assert.That(result.GateExtraProduced, Is.EqualTo(produced - 10));
            foreach (var bot in result.MiniBots) Assert.That(bot.GateUsed, Is.True);
            Assert.That(result.ActiveMiniBots, Is.EqualTo(produced));
            Assert.That(multiplier, Is.EqualTo(produced / 10));
            for (int i = 0; i < 30; i++) sim.AdvanceOneTick();
            Assert.That(sim.GetSnapshot().ProducedTotal, Is.EqualTo(produced));
        }

        [Test]
        public void DuplicateInputKey_RejectedAndCannonMovesOnlyAtTick()
        {
            var sim = new MatchSimulation(TestConfig());
            Assert.That(sim.Submit(new InputCommand(1, 10, CommandKind.MoveCannon, 500)), Is.True);
            Assert.That(sim.Submit(new InputCommand(1, 10, CommandKind.MoveCannon, -500)), Is.False);
            sim.AdvanceOneTick();
            Assert.That(sim.GetSnapshot().CannonX, Is.EqualTo(-650));
            sim.AdvanceOneTick();
            Assert.That(sim.GetSnapshot().CannonX, Is.EqualTo(500));
            Assert.That(sim.Submit(new InputCommand(1, 11, CommandKind.MoveCannon, -500)), Is.False);
        }

        [Test]
        public void FortressHit_IsIdempotent_AndVictoryLatched()
        {
            LevelDefinition config = TestConfig();
            config.AttackPathLength = 340;
            config.FortressHP = 2;
            var sim = new MatchSimulation(config);
            sim.AdvanceOneTick(); // spawn, no movement
            sim.AdvanceOneTick(); // both gated bots hit fortress exactly once
            MatchSnapshot end = sim.GetSnapshot();
            Assert.That(end.FortressHP, Is.Zero);
            Assert.That(end.FortressHits, Is.EqualTo(2));
            Assert.That(end.ProducedTotal, Is.EqualTo(2));
            Assert.That(end.ActiveMiniBots, Is.Zero);
            Assert.That(end.FortressStage, Is.EqualTo(3));
            Assert.That(end.Outcome, Is.EqualTo(MatchOutcome.Win));
            Assert.That(end.TerminalEvents, Is.EqualTo(1));
            ulong hash = end.EventHash;
            for (int i = 0; i < 10; i++) sim.AdvanceOneTick();
            Assert.That(sim.GetSnapshot().EventHash, Is.EqualTo(hash));
            Assert.That(sim.GetSnapshot().TerminalEvents, Is.EqualTo(1));
        }

        [Test]
        public void FirstEnemyContact_ConsumesOneEnemyAndOneMiniBot()
        {
            LevelDefinition config = TestConfig();
            config.GateLeftMaxX = -1;
            config.LeftGateMultiplier = 1;
            config.AttackPathLength = 680;
            config.EnemyPathLength = 680;
            config.EnemyWaves = new[] { new EnemyWave(0, 1, 0) };
            config.ShotPeriodTicks = 2000;
            var sim = new MatchSimulation(config);
            sim.AdvanceOneTick();
            sim.AdvanceOneTick();
            MatchSnapshot result = sim.GetSnapshot();
            Assert.That(result.EnemiesDestroyed, Is.EqualTo(1));
            Assert.That(result.EnemyLosses, Is.EqualTo(1));
            Assert.That(result.ProducedTotal, Is.EqualTo(1));
            Assert.That(result.ActiveMiniBots, Is.Zero);
            Assert.That(result.ActiveEnemies, Is.Zero);
            Assert.That(result.Breaches, Is.Zero);
        }

        [Test]
        public void ThreeEnemyBreaches_ImmediatelyCauseLoss()
        {
            LevelDefinition config = TestConfig();
            config.FirstShotTick = 1790;
            config.EnemyPathLength = 230;
            config.EnemyWaves = new[] { new EnemyWave(0, 2, 1) };
            var sim = new MatchSimulation(config);
            sim.AdvanceOneTick();
            sim.AdvanceOneTick();
            MatchSnapshot end = sim.GetSnapshot();
            Assert.That(end.BaseHP, Is.Zero);
            Assert.That(end.Breaches, Is.EqualTo(3));
            Assert.That(end.Outcome, Is.EqualTo(MatchOutcome.LoseBase));
            Assert.That(end.TerminalEvents, Is.EqualTo(1));
        }

        [Test]
        public void BaseAndFortressBothZero_PlayerLoses()
        {
            Assert.That(OutcomeResolver.Resolve(0, 0, 500, 1800), Is.EqualTo(MatchOutcome.LoseBase));
            Assert.That(OutcomeResolver.Resolve(1, 0, 1800, 1800), Is.EqualTo(MatchOutcome.Win));
            Assert.That(OutcomeResolver.Resolve(1, 1, 1800, 1800), Is.EqualTo(MatchOutcome.LoseTimeout));
        }

        [Test]
        public void FortressDestroyedExactlyOnFinalTick_IsVictory()
        {
            LevelDefinition config = TestConfig();
            config.ShotPeriodTicks = 2000;
            config.GateLeftMaxX = -1;
            config.LeftGateMultiplier = 1;
            config.AttackPathLength = 340 * 1800;
            config.FortressHP = 1;
            var sim = new MatchSimulation(config);
            for (int t = 0; t <= 1800; t++) sim.AdvanceOneTick();
            Assert.That(sim.GetSnapshot().FortressHits, Is.EqualTo(1));
            Assert.That(sim.GetSnapshot().TerminalTick, Is.EqualTo(1800));
            Assert.That(sim.GetSnapshot().Outcome, Is.EqualTo(MatchOutcome.Win));
        }

        [Test]
        public void Restart_FreshMatchClearsAllCountersAndIds()
        {
            var first = new MatchSimulation(TestConfig(), 1);
            for (int t = 0; t <= 500; t++) first.AdvanceOneTick();
            var restarted = new MatchSimulation(TestConfig(), 2);
            Assert.That(restarted.MatchId, Is.EqualTo(2));
            Assert.That(restarted.NextTick, Is.Zero);
            Assert.That(restarted.GetSnapshot().RawShots, Is.Zero);
            Assert.That(restarted.GetSnapshot().ActiveMiniBots, Is.Zero);
            Assert.That(restarted.GetSnapshot().FortressHits, Is.Zero);
            Assert.That(restarted.GetSnapshot().Outcome, Is.EqualTo(MatchOutcome.Playing));
            restarted.AdvanceOneTick();
            Assert.That(restarted.GetSnapshot().MiniBots[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void PauseAndCatchUp_NoLostOrExtraTicks()
        {
            var sim = new MatchSimulation(TestConfig());
            var clock = new SimulationClock(30) { MaxTicksPerFrame = 3, Paused = true };
            Assert.That(clock.Advance(5, () => sim.AdvanceOneTick()), Is.Zero);
            Assert.That(sim.NextTick, Is.Zero);
            clock.Paused = false;
            Assert.That(clock.Advance(.2, () => sim.AdvanceOneTick()), Is.EqualTo(3));
            Assert.That(clock.Advance(0, () => sim.AdvanceOneTick()), Is.EqualTo(3));
            Assert.That(sim.NextTick, Is.EqualTo(6));
        }

        [Test]
        public void SameCommandLog_ThirtyAndSixtyRenderFPS_HashesMatch()
        {
            MatchSnapshot thirty = Replay(30);
            MatchSnapshot sixty = Replay(60);
            Assert.That(thirty.Tick, Is.EqualTo(1800));
            Assert.That(sixty.Tick, Is.EqualTo(1800));
            Assert.That(thirty.RawShots, Is.EqualTo(180));
            Assert.That(thirty.StateHash, Is.EqualTo(sixty.StateHash));
            Assert.That(thirty.EventHash, Is.EqualTo(sixty.EventHash));
            Assert.That(thirty.Outcome, Is.EqualTo(sixty.Outcome));
            Assert.That(thirty.FortressHP, Is.EqualTo(sixty.FortressHP));
        }

        private static MatchSnapshot Replay(int renderRate)
        {
            LevelDefinition config = TestConfig();
            config.ShotPeriodTicks = 10;
            config.GatePosition = 9000;
            config.AttackPathLength = 1000000;
            var sim = new MatchSimulation(config);
            Assert.That(sim.Submit(new InputCommand(1, 1, CommandKind.MoveCannon, 700)), Is.True);
            Assert.That(sim.Submit(new InputCommand(600, 2, CommandKind.MoveCannon, -700)), Is.True);
            Assert.That(sim.Submit(new InputCommand(1200, 3, CommandKind.MoveCannon, 700)), Is.True);
            sim.AdvanceOneTick(); // t=0; clock advances ticks 1..1800 in exactly 60s.
            var clock = new SimulationClock(config.TickRate);
            for (int f = 0; f < 60 * renderRate; f++)
                clock.Advance(1.0 / renderRate, () => sim.AdvanceOneTick());
            return sim.GetSnapshot();
        }

        [TestCase(-1000, -1, 2)]
        [TestCase(-120, -1, 2)]
        [TestCase(-119, 0, 1)]
        [TestCase(0, 0, 1)]
        [TestCase(119, 0, 1)]
        [TestCase(120, 1, 3)]
        [TestCase(1000, 1, 3)]
        public void GateThresholds_AgreeWithBotLaneAndMultiplier(int x, int lane, int multiplier)
        {
            LevelDefinition config = TestConfig();
            Assert.That(config.LaneForX(x), Is.EqualTo(lane));
            Assert.That(config.GateForX(x), Is.EqualTo(multiplier));
            var sim = new MatchSimulation(config);
            Assert.That(sim.Submit(new InputCommand(0, 1, CommandKind.MoveCannon, x)), Is.True);
            sim.AdvanceOneTick();
            MatchSnapshot s = sim.GetSnapshot();
            Assert.That(s.MiniBots[0].CannonX, Is.EqualTo(x));
            Assert.That(s.ActiveMiniBots, Is.EqualTo(multiplier));
            for (int i = 0; i < s.MiniBots.Count; i++)
                Assert.That(s.MiniBots[i].Lane, Is.EqualTo(lane));
        }

        [Test]
        public void ReplayHash_RecordsPerUnitSpawnXEvenWhenCannonReturnsToSamePosition()
        {
            var a = new MatchSimulation(TestConfig());
            var b = new MatchSimulation(TestConfig());
            Assert.That(a.Submit(new InputCommand(0, 1, CommandKind.MoveCannon, -600)), Is.True);
            Assert.That(b.Submit(new InputCommand(0, 1, CommandKind.MoveCannon, -700)), Is.True);
            foreach (MatchSimulation sim in new[] { a, b })
            {
                Assert.That(sim.Submit(new InputCommand(1, 2, CommandKind.MoveCannon, -650)), Is.True);
                sim.AdvanceOneTick();
                sim.AdvanceOneTick();
            }
            Assert.That(a.GetSnapshot().CannonX, Is.EqualTo(b.GetSnapshot().CannonX));
            Assert.That(a.GetSnapshot().MiniBots[0].CannonX, Is.EqualTo(-600));
            Assert.That(b.GetSnapshot().MiniBots[0].CannonX, Is.EqualTo(-700));
            Assert.That(a.GetSnapshot().StateHash, Is.Not.EqualTo(b.GetSnapshot().StateHash));
            Assert.That(a.GetSnapshot().CommandHash, Is.Not.EqualTo(b.GetSnapshot().CommandHash));
        }

        [Test]
        public void HashOverloads_PreserveAllUnsignedBits_AndExistingSignedBitPatterns()
        {
            Type[] signedSignature = { typeof(ulong).MakeByRefType(), typeof(long) };
            Type[] unsignedSignature = { typeof(ulong).MakeByRefType(), typeof(ulong) };
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo signedHash = typeof(MatchSnapshot).GetMethod("Hash", flags,
                null, signedSignature, null);
            MethodInfo unsignedHash = typeof(MatchSnapshot).GetMethod("Hash", flags,
                null, unsignedSignature, null);
            Assert.That(signedHash, Is.Not.Null, "Signed hash overload must remain compatible.");
            Assert.That(unsignedHash, Is.Not.Null, "Unsigned hash overload is required for replay evidence.");

            const ulong offset = 14695981039346656037UL;
            object[] minusOne = { offset, -1L };
            object[] unsignedMax = { offset, ulong.MaxValue };
            signedHash.Invoke(null, minusOne);
            unsignedHash.Invoke(null, unsignedMax);
            Assert.That(minusOne[0], Is.EqualTo(unsignedMax[0]),
                "A signed -1 must hash as the same eight bytes as ulong.MaxValue.");

            object[] signedMin = { offset, long.MinValue };
            object[] unsignedTopBit = { offset, 0x8000000000000000UL };
            signedHash.Invoke(null, signedMin);
            unsignedHash.Invoke(null, unsignedTopBit);
            Assert.That(signedMin[0], Is.EqualTo(unsignedTopBit[0]),
                "The high bit must not be lost or sign-extended incorrectly.");

            object[] zero = { offset, 0UL };
            unsignedHash.Invoke(null, zero);
            Assert.That(zero[0], Is.Not.EqualTo(unsignedTopBit[0]));
        }

        [Test]
        public void LastEventBatch_RemainsImmutableAfterLaterTicks_AndHistoryIsMatchScoped()
        {
            var sim = new MatchSimulation(TestConfig(), 67);
            sim.AdvanceOneTick();
            GameEventBatch first = sim.LastEventBatch;
            var retained = sim.LastEvents;
            Assert.That(first.MatchId, Is.EqualTo(67));
            Assert.That(first.Tick, Is.Zero);
            Assert.That(retained.Count, Is.GreaterThan(0));
            GameEvent firstEvent = retained[0];
            sim.AdvanceOneTick();
            Assert.That(first.Tick, Is.Zero);
            Assert.That(retained.Count, Is.EqualTo(2)); // RawShot + GateMultiplied.
            Assert.That(retained[0].Kind, Is.EqualTo(firstEvent.Kind));
            Assert.That(sim.LastEventBatch.Tick, Is.EqualTo(1));
            Assert.That(sim.GetEventHistory().Count, Is.EqualTo(2));
            Assert.That(sim.GetEventHistory()[0].CumulativeHash, Is.EqualTo(first.CumulativeHash));
        }

        [Test]
        public void SweptContact_EliminatesFastCrossingInsteadOfAllowingTunneling()
        {
            LevelDefinition config = TestConfig();
            config.AttackPathLength = 4000;
            config.EnemyPathLength = 4000;
            config.MiniBotSpeedPerTick = 2400;
            config.EnemySpeedPerTick = 2300;
            config.GateLeftMaxX = -1;
            config.LeftGateMultiplier = 1;
            config.ContactRadius = 25;
            config.ShotPeriodTicks = 2000;
            config.EnemyWaves = new[] { new EnemyWave(0, 1, 0) };
            var sim = new MatchSimulation(config);
            sim.AdvanceOneTick();
            sim.AdvanceOneTick();
            MatchSnapshot s = sim.GetSnapshot();
            Assert.That(s.EnemiesDestroyed, Is.EqualTo(1));
            Assert.That(s.EnemyLosses, Is.EqualTo(1));
            Assert.That(s.FortressHits, Is.Zero);
            Assert.That(s.Breaches, Is.Zero);
            Assert.That(s.ActiveMiniBots, Is.Zero);
            Assert.That(s.ActiveEnemies, Is.Zero);
        }

        [Test]
        public void VisualCap_120To121PreservesExistingSelectedIds()
        {
            var units = new System.Collections.Generic.List<UnitView>();
            for (int i = 1; i <= 120; i++) units.Add(new UnitView(i, i, -1, true));
            var first = StableVisualSelection.Choose(units, null, 120);
            Assert.That(first.Count, Is.EqualTo(120));
            units.Add(new UnitView(121, 121, 1, true));
            var second = StableVisualSelection.Choose(units, first, 120);
            Assert.That(second, Is.EquivalentTo(first));
            units.RemoveAt(0);
            var third = StableVisualSelection.Choose(units, second, 120);
            Assert.That(third.Count, Is.EqualTo(120));
            // List<long>.Contains(long) performs an exact typed membership check.
            // NUnit's Does.Not.Contain(...) resolves to its string overload here.
            Assert.That(third.Contains(121L), Is.True);
            Assert.That(third.Contains(1L), Is.False);
        }
    }
}

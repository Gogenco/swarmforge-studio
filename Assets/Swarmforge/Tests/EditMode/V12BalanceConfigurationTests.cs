using System;
using NUnit.Framework;
using Swarmforge.Simulation;

namespace Swarmforge.Tests
{
    public sealed class V12BalanceConfigurationTests
    {
        [Test]
        public void Hp2AndHp3Presets_OnlyDifferInHPAndIdentity()
        {
            V12BalanceDefinition two = V12BalanceDefinition.CreateTrialPreset(2);
            V12BalanceDefinition three = V12BalanceDefinition.CreateTrialPreset(3);
            Assert.That(two.MiniBotMaxHP, Is.EqualTo(2));
            Assert.That(three.MiniBotMaxHP, Is.EqualTo(3));
            V12BalanceDefinition comparison = three.Copy();
            comparison.MiniBotMaxHP = two.MiniBotMaxHP;
            comparison.ProfileId = two.ProfileId;
            Assert.That(V12ConfigHasher.Compute(comparison), Is.EqualTo(V12ConfigHasher.Compute(two)),
                "Both matched playtest presets must have identical non-HP settings.");
            Assert.That(new V12MatchConfig(two).ConfigHash,
                Is.Not.EqualTo(new V12MatchConfig(three).ConfigHash));
        }

        [Test]
        public void InspectorTunables_AreDefinedByProfileAndFrozenPerMatch()
        {
            V12BalanceDefinition source = V12BalanceDefinition.CreateTrialPreset(2);
            V12MatchConfig match = new V12MatchConfig(source);
            ulong fingerprint = match.ConfigHash;
            source.MiniBotMaxHP = 3;
            source.Boxes[0].MaxHP = 99;
            source.Waves[0].Spawns[0].Count = 42;
            V12BalanceDefinition leakedCopy = match.CreateEditableCopy();
            leakedCopy.MiniBotMaxHP = 9;
            leakedCopy.EnemyTypes[0].MaxHP = 100;
            leakedCopy.Waves[0].Spawns[0].Count = 11;
            Assert.That(match.MiniBotMaxHP, Is.EqualTo(2));
            Assert.That(match.ConfigHash, Is.EqualTo(fingerprint));
            V12BalanceDefinition restored = match.CreateEditableCopy();
            Assert.That(restored.Boxes[0].MaxHP, Is.EqualTo(5));
            Assert.That(restored.EnemyTypes[0].MaxHP, Is.EqualTo(1));
            Assert.That(restored.Waves[0].Spawns[0].Count, Is.EqualTo(2));
            Assert.That(V12ConfigHasher.Compute(restored), Is.EqualTo(fingerprint));
        }

        [Test]
        public void ApprovedInitialBoxDurabilityAndTimeOnlyBoosts_AreEditableData()
        {
            V12BalanceDefinition s = V12BalanceDefinition.CreateTrialPreset(2);
            Assert.That(s.TickRate, Is.EqualTo(30));
            Assert.That(s.Boxes.Length, Is.EqualTo(3));
            Assert.That(s.Boxes[0].MaxHP, Is.EqualTo(5));
            Assert.That(s.Boxes[1].MaxHP, Is.EqualTo(10));
            Assert.That(s.Boxes[2].MaxHP, Is.EqualTo(25));
            Assert.That(s.Boxes[0].DurationTicks, Is.EqualTo(12 * s.TickRate));
            Assert.That(s.Boxes[1].DurationTicks, Is.EqualTo(10 * s.TickRate));
            Assert.That(s.Boxes[2].DurationTicks, Is.EqualTo(6 * s.TickRate));
            ulong before = V12ConfigHasher.Compute(s);
            s.Boxes[0].MaxHP = 6;
            Assert.That(V12ConfigHasher.Compute(s), Is.Not.EqualTo(before));
        }

        [Test]
        public void Fingerprint_IsStableAcrossIndependentAllocationsAndCopies()
        {
            var a = new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(3));
            var b = new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(3).Copy());
            Assert.That(a.ConfigHash, Is.EqualTo(b.ConfigHash));
            Assert.That(a.ReplayIdentity().IsCompatibleWith(b.ReplayIdentity()), Is.True);
        }

        [Test]
        public void Fingerprint_CoversAllNestedWaveValuesAndDefinitionOrder()
        {
            V12BalanceDefinition baseConfig = V12BalanceDefinition.CreateTrialPreset(2);
            ulong before = V12ConfigHasher.Compute(baseConfig);
            V12BalanceDefinition changed = baseConfig.Copy();
            changed.Waves[0].Spawns[1].Count++;
            Assert.That(V12ConfigHasher.Compute(changed), Is.Not.EqualTo(before));
            changed = baseConfig.Copy();
            V12WaveSpawn first = changed.Waves[0].Spawns[0];
            changed.Waves[0].Spawns[0] = changed.Waves[0].Spawns[1];
            changed.Waves[0].Spawns[1] = first;
            Assert.That(V12ConfigHasher.Compute(changed), Is.Not.EqualTo(before));
        }

        [Test]
        public void ReplayIdentity_RejectsDifferentBalanceOrLegacyUnversionedEvidence()
        {
            var two = new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(2));
            var three = new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(3));
            V12ReplayIdentity a = two.ReplayIdentity();
            Assert.That(a.IsCompatibleWith(three.ReplayIdentity()), Is.False);
            Assert.That(a.IsCompatibleWith(V12ReplayIdentity.LegacyUnversioned()), Is.False);
            Assert.Throws<InvalidOperationException>(() => a.RequireCompatible(three.ReplayIdentity()));
            Assert.Throws<InvalidOperationException>(() => a.RequireCompatible(V12ReplayIdentity.LegacyUnversioned()));
            Assert.DoesNotThrow(() => a.RequireCompatible(two.ReplayIdentity()));
        }

        [Test]
        public void InvalidSchemaAndNonThirtyHz_AreRejected()
        {
            V12BalanceDefinition s = V12BalanceDefinition.CreateTrialPreset(2);
            s.SchemaVersion = 3;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
            s = V12BalanceDefinition.CreateTrialPreset(2);
            s.TickRate = 60;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
            s = V12BalanceDefinition.CreateTrialPreset(2);
            s.MiniBotContactCooldownTicks = 0;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
        }

        [Test]
        public void InvalidBoxOrWaveArchetypeOrChronology_IsRejected()
        {
            V12BalanceDefinition s = V12BalanceDefinition.CreateTrialPreset(2);
            s.Boxes[1].Key = s.Boxes[0].Key;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
            s = V12BalanceDefinition.CreateTrialPreset(2);
            s.Waves[0].Spawns[0].EnemyKey = "UNKNOWN";
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
            s = V12BalanceDefinition.CreateTrialPreset(2);
            s.Waves[0].WarningLeadTicks = s.Waves[0].Tick + 1;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
            s = V12BalanceDefinition.CreateTrialPreset(2);
            s.Boxes[0].DurationTicks = 0;
            Assert.Throws<ArgumentException>(() => new V12MatchConfig(s));
        }

        [Test]
        public void NewOptInConfiguration_DoesNotAlterLegacyMatchOrReplayHashes()
        {
            var legacy = new LevelDefinition
            {
                EnemyWaves = Array.Empty<EnemyWave>(),
                GatePosition = 0, AttackPathLength = 1000000,
                FortressHP = 99999
            };
            var untouched = new MatchSimulation(legacy);
            var alongsidePrototype = new MatchSimulation(legacy);
            new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(2));
            new V12MatchConfig(V12BalanceDefinition.CreateTrialPreset(3));
            for (int tick = 0; tick < 80; tick++)
            {
                untouched.AdvanceOneTick();
                alongsidePrototype.AdvanceOneTick();
            }
            MatchSnapshot a = untouched.GetSnapshot();
            MatchSnapshot b = alongsidePrototype.GetSnapshot();
            Assert.That(b.StateHash, Is.EqualTo(a.StateHash));
            Assert.That(b.EventHash, Is.EqualTo(a.EventHash));
            Assert.That(b.CommandHash, Is.EqualTo(a.CommandHash));
            Assert.That(b.RawShots, Is.EqualTo(a.RawShots));
        }
    }
}

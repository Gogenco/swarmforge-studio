using System;

namespace Swarmforge.Simulation
{
    /// <summary>
    /// P1-only opt-in preparation seam. Future v1.2 gameplay may accept this
    /// immutable per-match copy; legacy MatchSimulation still accepts only
    /// LevelDefinition and is never switched implicitly.
    /// </summary>
    public sealed class V12MatchConfig
    {
        private readonly V12BalanceDefinition settings;

        public int SchemaVersion { get { return settings.SchemaVersion; } }
        public string RulesetId { get { return settings.RulesetId; } }
        public string ProfileId { get { return settings.ProfileId; } }
        public int TickRate { get { return settings.TickRate; } }
        public int MiniBotMaxHP { get { return settings.MiniBotMaxHP; } }
        public ulong ConfigHash { get; private set; }

        public V12MatchConfig(V12BalanceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            settings = definition.Copy();
            settings.Validate();
            ConfigHash = V12ConfigHasher.Compute(settings);
        }

        /// <summary>
        /// Caller can edit the returned copy; neither the match's config nor its
        /// fingerprint changes. Never pass the live backing data to presentation.
        /// </summary>
        public V12BalanceDefinition CreateEditableCopy()
        {
            return settings.Copy();
        }

        public V12ReplayIdentity ReplayIdentity()
        {
            return V12ReplayIdentity.ForPrototype(this);
        }
    }

    /// <summary>
    /// Replay identity is explicit: DEV-002.2 legacy replays do NOT silently
    /// become compatible with the new v1.2 config/hash schema.
    /// This describes configuration compatibility, not a completed gameplay replay.
    /// </summary>
    public readonly struct V12ReplayIdentity
    {
        public readonly string RulesetId;
        public readonly int SchemaVersion;
        public readonly ulong ConfigHash;

        private V12ReplayIdentity(string rulesetId, int schemaVersion, ulong configHash)
        {
            RulesetId = rulesetId; SchemaVersion = schemaVersion; ConfigHash = configHash;
        }

        public static V12ReplayIdentity ForPrototype(V12MatchConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new V12ReplayIdentity(config.RulesetId, config.SchemaVersion, config.ConfigHash);
        }

        // Marker only. Old replays predate the versioned config fingerprint and
        // must be validated via their original DEV-002.2 test harness.
        public static V12ReplayIdentity LegacyUnversioned()
        {
            return new V12ReplayIdentity("DEV0022_LEGACY_UNVERSIONED", 1, 0UL);
        }

        public bool IsCompatibleWith(V12ReplayIdentity other)
        {
            return RulesetId == V12BalanceDefinition.TrialRulesetId &&
                other.RulesetId == V12BalanceDefinition.TrialRulesetId &&
                SchemaVersion == V12BalanceDefinition.CurrentSchemaVersion &&
                other.SchemaVersion == V12BalanceDefinition.CurrentSchemaVersion &&
                ConfigHash == other.ConfigHash;
        }

        public void RequireCompatible(V12ReplayIdentity other)
        {
            if (!IsCompatibleWith(other))
                throw new InvalidOperationException("Replay configuration or ruleset mismatch.");
        }
    }

    /// <summary>
    /// Portable FNV-1a over deliberately specified little-endian byte sequences.
    /// Includes ALL authored gameplay settings, definition order and schema tag.
    /// No platform-dependent GetHashCode(), floats, JSON ordering or Unity APIs.
    /// </summary>
    public static class V12ConfigHasher
    {
        public static ulong Compute(V12BalanceDefinition source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            V12BalanceDefinition s = source.Copy();
            s.Validate();

            ulong hash = 14695981039346656037UL;
            AddString(ref hash, "SWARMFORGE_CONFIG_V12_1");
            AddInt(ref hash, s.SchemaVersion);
            AddString(ref hash, s.RulesetId);
            AddString(ref hash, s.LevelId);
            AddString(ref hash, s.ProfileId);
            AddInt(ref hash, s.TickRate);
            AddInt(ref hash, s.MatchTicks);
            AddInt(ref hash, s.FirstShotTick);
            AddInt(ref hash, s.ShotPeriodTicks);
            AddInt(ref hash, s.BaseHP);
            AddInt(ref hash, s.FortressHP);
            AddInt(ref hash, s.MiniBotMaxHP);
            AddInt(ref hash, s.MiniBotDamage);
            AddInt(ref hash, s.MiniBotSpeedPerTick);
            AddInt(ref hash, s.MiniBotContactCooldownTicks);
            AddInt(ref hash, s.CannonMinX);
            AddInt(ref hash, s.CannonMaxX);
            AddInt(ref hash, s.AttackPathLength);
            AddInt(ref hash, s.EnemyPathLength);
            AddInt(ref hash, s.ContactRadius);

            AddInt(ref hash, s.EnemyTypes.Length);
            for (int i = 0; i < s.EnemyTypes.Length; i++)
            {
                V12EnemyBalance enemy = s.EnemyTypes[i];
                AddString(ref hash, enemy.Key);
                AddInt(ref hash, enemy.MaxHP);
                AddInt(ref hash, enemy.Damage);
                AddInt(ref hash, enemy.SpeedPerTick);
                AddInt(ref hash, enemy.ContactCooldownTicks);
            }
            AddInt(ref hash, s.Boxes.Length);
            for (int i = 0; i < s.Boxes.Length; i++)
            {
                V12BoxBalance box = s.Boxes[i];
                AddString(ref hash, box.Key);
                AddInt(ref hash, box.X);
                AddInt(ref hash, box.Position);
                AddInt(ref hash, box.Radius);
                AddInt(ref hash, box.MaxHP);
                AddInt(ref hash, box.Multiplier);
                AddInt(ref hash, box.DurationTicks);
            }
            AddInt(ref hash, s.Waves.Length);
            for (int i = 0; i < s.Waves.Length; i++)
            {
                V12WaveBalance wave = s.Waves[i];
                AddInt(ref hash, wave.Tick);
                AddInt(ref hash, wave.WarningLeadTicks);
                AddInt(ref hash, wave.Spawns.Length);
                for (int j = 0; j < wave.Spawns.Length; j++)
                {
                    V12WaveSpawn spawn = wave.Spawns[j];
                    AddString(ref hash, spawn.EnemyKey);
                    AddInt(ref hash, spawn.Lane);
                    AddInt(ref hash, spawn.Count);
                }
            }
            return hash;
        }

        private static void AddString(ref ulong hash, string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            AddInt(ref hash, value.Length);
            // Explicit UTF-16 code-unit encoding is stable across Mono/IL2CPP.
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                AddByte(ref hash, (byte)c);
                AddByte(ref hash, (byte)(c >> 8));
            }
        }

        private static void AddInt(ref ulong hash, int value)
        {
            unchecked
            {
                uint bits = (uint)value;
                for (int i = 0; i < 4; i++)
                    AddByte(ref hash, (byte)(bits >> (8 * i)));
            }
        }

        private static void AddByte(ref ulong hash, byte value)
        {
            unchecked
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
        }
    }
}

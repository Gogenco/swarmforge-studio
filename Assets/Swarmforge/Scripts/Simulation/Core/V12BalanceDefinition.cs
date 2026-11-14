using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    /// <summary>
    /// Opt-in v1.2 trial data. This class deliberately does not replace the DEV-002.2
    /// LevelDefinition, the approved Battle scene, or the legacy gate simulation.
    /// Fields are public so a dedicated Unity ScriptableObject can edit them.
    /// Values are initial hypotheses, NOT a signed combat/balance contract.
    /// </summary>
    [Serializable]
    public sealed class V12BalanceDefinition
    {
        public const int CurrentSchemaVersion = 2;
        public const string TrialRulesetId = "SWARMFORGE_V12_TRIAL";

        public int SchemaVersion = CurrentSchemaVersion;
        public string RulesetId = TrialRulesetId;
        public string LevelId = "V12_TRIAL_ARENA";
        public string ProfileId = "HP2_TRIAL";
        public int TickRate = 30;
        public int MatchTicks = 1800;
        public int FirstShotTick = 0;
        public int ShotPeriodTicks = 10;
        public int BaseHP = 3;
        public int FortressHP = 100;
        public int MiniBotMaxHP = 2;
        public int MiniBotDamage = 1;
        public int MiniBotSpeedPerTick = 340;
        public int MiniBotContactCooldownTicks = 15;
        public int CannonMinX = -1000;
        public int CannonMaxX = 1000;
        public int AttackPathLength = 51000;
        public int EnemyPathLength = 51000;
        public int ContactRadius = 450;
        public V12EnemyBalance[] EnemyTypes = Array.Empty<V12EnemyBalance>();
        public V12BoxBalance[] Boxes = Array.Empty<V12BoxBalance>();
        public V12WaveBalance[] Waves = Array.Empty<V12WaveBalance>();

        public V12BalanceDefinition Copy()
        {
            var copy = (V12BalanceDefinition)MemberwiseClone();
            copy.EnemyTypes = EnemyTypes == null ? null : (V12EnemyBalance[])EnemyTypes.Clone();
            copy.Boxes = Boxes == null ? null : (V12BoxBalance[])Boxes.Clone();
            if (Waves != null)
            {
                copy.Waves = new V12WaveBalance[Waves.Length];
                for (int i = 0; i < Waves.Length; i++)
                    copy.Waves[i] = Waves[i].Copy();
            }
            else copy.Waves = null;
            return copy;
        }

        public void Validate()
        {
            if (SchemaVersion != CurrentSchemaVersion || RulesetId != TrialRulesetId ||
                string.IsNullOrWhiteSpace(LevelId) || string.IsNullOrWhiteSpace(ProfileId))
                throw new ArgumentException("Unsupported v1.2 ruleset/schema/profile.");
            if (TickRate != 30 || MatchTicks < 1 || FirstShotTick < 0 ||
                FirstShotTick >= MatchTicks || ShotPeriodTicks < 1 ||
                BaseHP < 1 || FortressHP < 1 || MiniBotMaxHP < 1 ||
                MiniBotDamage < 1 || MiniBotSpeedPerTick < 1 ||
                MiniBotContactCooldownTicks < 1 || CannonMinX >= CannonMaxX ||
                AttackPathLength < 1 || EnemyPathLength < 1 || ContactRadius < 0)
                throw new ArgumentException("Invalid v1.2 match or MiniBot tuning.");
            if (EnemyTypes == null || EnemyTypes.Length == 0 ||
                Boxes == null || Boxes.Length == 0 || Waves == null || Waves.Length == 0)
                throw new ArgumentException("Trial requires enemy, box and wave definitions.");

            var enemies = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < EnemyTypes.Length; i++)
            {
                V12EnemyBalance enemy = EnemyTypes[i];
                if (string.IsNullOrWhiteSpace(enemy.Key) || !enemies.Add(enemy.Key) ||
                    enemy.MaxHP < 1 || enemy.Damage < 1 || enemy.SpeedPerTick < 1 ||
                    enemy.ContactCooldownTicks < 1)
                    throw new ArgumentException("Invalid/duplicate enemy archetype at " + i);
            }

            var boxes = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Boxes.Length; i++)
            {
                V12BoxBalance box = Boxes[i];
                if (string.IsNullOrWhiteSpace(box.Key) || !boxes.Add(box.Key) ||
                    box.MaxHP < 1 || box.Multiplier < 2 || box.DurationTicks < 1 ||
                    box.Radius < 1 || box.Position < 0 || box.Position >= AttackPathLength ||
                    box.X < CannonMinX || box.X > CannonMaxX)
                    throw new ArgumentException("Invalid/duplicate box at " + i);
            }

            int previousTick = -1;
            for (int i = 0; i < Waves.Length; i++)
            {
                V12WaveBalance wave = Waves[i];
                if (wave.Tick <= previousTick || wave.Tick >= MatchTicks ||
                    wave.WarningLeadTicks < 0 || wave.WarningLeadTicks > wave.Tick ||
                    wave.Spawns == null || wave.Spawns.Length == 0)
                    throw new ArgumentException("Invalid/noncanonical wave at " + i);
                previousTick = wave.Tick;
                for (int j = 0; j < wave.Spawns.Length; j++)
                {
                    V12WaveSpawn spawn = wave.Spawns[j];
                    if (!enemies.Contains(spawn.EnemyKey) || spawn.Count < 1 ||
                        spawn.Lane < -1 || spawn.Lane > 1)
                        throw new ArgumentException("Invalid wave member at " + i + ":" + j);
                }
            }
        }

        public static V12BalanceDefinition CreateTrialPreset(int miniBotHP)
        {
            if (miniBotHP != 2 && miniBotHP != 3)
                throw new ArgumentOutOfRangeException(nameof(miniBotHP), "Only comparable HP2/HP3 presets are prepared.");
            return new V12BalanceDefinition
            {
                ProfileId = miniBotHP == 2 ? "HP2_TRIAL" : "HP3_TRIAL",
                MiniBotMaxHP = miniBotHP,
                EnemyTypes = new[]
                {
                    new V12EnemyBalance("SCOUT", 1, 1, 230, 15),
                    new V12EnemyBalance("ARMORED_TRIAL", 3, 1, 180, 15)
                },
                Boxes = new[]
                {
                    new V12BoxBalance("X2", -800, 9000, 450, 5, 2, 360),
                    new V12BoxBalance("X3", 0, 9000, 450, 10, 3, 300),
                    new V12BoxBalance("X5", 800, 9000, 450, 25, 5, 180)
                },
                Waves = new[]
                {
                    // Preliminary paired-test fixture, NOT final 60-second level design.
                    new V12WaveBalance(360, 90, new[]
                    {
                        new V12WaveSpawn("SCOUT", -1, 2),
                        new V12WaveSpawn("SCOUT", 1, 2)
                    })
                }
            };
        }
    }

    [Serializable]
    public struct V12EnemyBalance
    {
        public string Key;
        public int MaxHP;
        public int Damage;
        public int SpeedPerTick;
        public int ContactCooldownTicks;

        public V12EnemyBalance(string key, int maxHP, int damage, int speedPerTick, int cooldown)
        {
            Key = key; MaxHP = maxHP; Damage = damage;
            SpeedPerTick = speedPerTick; ContactCooldownTicks = cooldown;
        }
    }

    [Serializable]
    public struct V12BoxBalance
    {
        public string Key;
        public int X;
        public int Position;
        public int Radius;
        public int MaxHP;
        public int Multiplier;
        public int DurationTicks;

        public V12BoxBalance(string key, int x, int position, int radius,
            int maxHP, int multiplier, int durationTicks)
        {
            Key = key; X = x; Position = position; Radius = radius;
            MaxHP = maxHP; Multiplier = multiplier; DurationTicks = durationTicks;
        }
    }

    [Serializable]
    public struct V12WaveSpawn
    {
        public string EnemyKey;
        public int Lane;
        public int Count;

        public V12WaveSpawn(string enemyKey, int lane, int count)
        {
            EnemyKey = enemyKey; Lane = lane; Count = count;
        }
    }

    [Serializable]
    public struct V12WaveBalance
    {
        public int Tick;
        public int WarningLeadTicks;
        public V12WaveSpawn[] Spawns;

        public V12WaveBalance(int tick, int warningLeadTicks, V12WaveSpawn[] spawns)
        {
            Tick = tick; WarningLeadTicks = warningLeadTicks;
            Spawns = spawns == null ? null : (V12WaveSpawn[])spawns.Clone();
        }

        public V12WaveBalance Copy()
        {
            return new V12WaveBalance(Tick, WarningLeadTicks, Spawns);
        }
    }
}

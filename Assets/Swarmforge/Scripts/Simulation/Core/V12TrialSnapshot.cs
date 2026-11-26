using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    public enum V12TrialEventKind
    {
        AimChanged, RawShot, BotSpawned, EnemySpawned, BoxDamaged, BoxBroken,
        RewardEarned, BoostStarted, BoostExpired, ShotSuppressed,
        BotStruck, EnemyStruck, BotDestroyed, EnemyDestroyed,
        FortressDamaged, BaseBreached, MatchEnded
    }

    public readonly struct V12TrialEvent
    {
        public readonly V12TickTime Time;
        public readonly V12TrialEventKind Kind;
        public readonly long ActorId;
        public readonly long TargetId;
        public readonly int Value;

        public V12TrialEvent(V12TickTime time, V12TrialEventKind kind, long actorId, long targetId, int value)
        {
            Time = time; Kind = kind; ActorId = actorId; TargetId = targetId; Value = value;
        }
    }

    public readonly struct V12TrialUnitView
    {
        public readonly long Id;
        public readonly int X;
        public readonly int Position;
        public readonly int HP;
        public readonly int MaxHP;
        public readonly bool BoxDirected;

        public V12TrialUnitView(long id, int x, int position, int hp, int maxHP, bool boxDirected)
        {
            Id = id; X = x; Position = position; HP = hp;
            MaxHP = maxHP; BoxDirected = boxDirected;
        }
    }

    /// <summary>Owned copies for Unity rendering and replay evidence.</summary>
    public sealed class V12TrialSnapshot
    {
        public readonly int Tick;
        public readonly int NextTick;
        public readonly int CannonX;
        public readonly int BoxHP;
        public readonly int BoxMaxHP;
        public readonly int BaseHP;
        public readonly int FortressHP;
        public readonly int RawShots;
        public readonly int BotsProduced;
        public readonly int EnemiesSpawned;
        public readonly int EnemiesKilled;
        public readonly int WaveGroupsSpawned;
        public readonly int BoxHits;
        public readonly int BoostMultiplier;
        public readonly int BoostRemainingTicks;
        public readonly int QueuedMultiplier;
        public readonly int TerminalPromises;
        public readonly MatchOutcome Outcome;
        public readonly ulong ConfigHash;
        public readonly ulong EventHash;
        public readonly ulong CommandHash;
        public readonly ulong StateHash;

        private readonly IReadOnlyList<V12TrialUnitView> bots;
        private readonly IReadOnlyList<V12TrialUnitView> enemies;
        private readonly IReadOnlyList<V12TrialEvent> recentEvents;

        public IReadOnlyList<V12TrialUnitView> Bots { get { return bots; } }
        public IReadOnlyList<V12TrialUnitView> Enemies { get { return enemies; } }
        public IReadOnlyList<V12TrialEvent> RecentEvents { get { return recentEvents; } }

        internal V12TrialSnapshot(int tick, int nextTick, int cannonX, int boxHp, int boxMaxHp,
            int baseHp, int fortressHp, int rawShots, int botsProduced, int enemiesSpawned,
            int enemiesKilled, int waveGroupsSpawned, int boxHits, V12BoostLedger ledger,
            MatchOutcome outcome, ulong configHash, ulong eventHash, ulong commandHash,
            ulong stateHash, List<V12TrialUnitView> liveBots, List<V12TrialUnitView> liveEnemies,
            List<V12TrialEvent> events)
        {
            Tick = tick; NextTick = nextTick; CannonX = cannonX;
            BoxHP = boxHp; BoxMaxHP = boxMaxHp;
            BaseHP = baseHp; FortressHP = fortressHp;
            RawShots = rawShots; BotsProduced = botsProduced;
            EnemiesSpawned = enemiesSpawned; EnemiesKilled = enemiesKilled;
            WaveGroupsSpawned = waveGroupsSpawned; BoxHits = boxHits;
            BoostMultiplier = ledger.ActiveMultiplier;
            BoostRemainingTicks = ledger.RemainingTicks(tick);
            QueuedMultiplier = ledger.PendingMultiplier;
            TerminalPromises = ledger.TerminalPromises;
            Outcome = outcome; ConfigHash = configHash;
            EventHash = eventHash; CommandHash = commandHash; StateHash = stateHash;
            bots = Array.AsReadOnly(liveBots.ToArray());
            enemies = Array.AsReadOnly(liveEnemies.ToArray());
            recentEvents = Array.AsReadOnly(events.ToArray());
        }
    }
}

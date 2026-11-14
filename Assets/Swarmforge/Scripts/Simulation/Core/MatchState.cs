using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    public enum MatchOutcome { Playing, Win, LoseBase, LoseTimeout }
    public enum GameEventKind { RawShot, GateMultiplied, EnemySpawned, EnemyDefeated, MiniBotLost,
        FortressDamaged, FortressStageChanged, BaseBreached, MatchEnded, CannonMoved }

    public readonly struct GameEvent
    {
        public readonly int Tick;
        public readonly GameEventKind Kind;
        public readonly long SourceId;
        public readonly long TargetId;
        public readonly int Value;

        public GameEvent(int tick, GameEventKind kind, long sourceId, long targetId, int value)
        {
            Tick = tick;
            Kind = kind;
            SourceId = sourceId;
            TargetId = targetId;
            Value = value;
        }
    }

    /// <summary>Immutable, match-scoped event evidence for a single simulation tick.</summary>
    public sealed class GameEventBatch
    {
        public readonly long MatchId;
        public readonly int Tick;
        public readonly ulong CumulativeHash;
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<GameEvent> items;
        public IReadOnlyList<GameEvent> Items { get { return items; } }

        internal GameEventBatch(long matchId, int tick, ulong hash, List<GameEvent> source)
        {
            MatchId = matchId;
            Tick = tick;
            CumulativeHash = hash;
            items = System.Array.AsReadOnly(source.ToArray());
        }
    }

    internal sealed class MiniBot
    {
        public long Id;
        public int BornTick;
        public int Position;
        public int PreviousPosition;
        public int CannonX;
        public int Lane;
        public bool GateUsed;
        public bool Alive = true;
    }

    internal sealed class EnemyDrone
    {
        public long Id;
        public int BornTick;
        public int Position;
        public int PreviousPosition;
        public int Lane;
        public bool Alive = true;
    }

    internal sealed class MatchState
    {
        internal readonly List<MiniBot> MiniBots = new List<MiniBot>();
        internal readonly List<EnemyDrone> Enemies = new List<EnemyDrone>();
        internal long NextStableId = 1;
        internal int CannonX = -650;
        internal int RawShots;
        internal int ProducedTotal;
        internal int GateExtraProduced;
        internal int FortressHits;
        internal int EnemyLosses;
        internal int EnemiesDestroyed;
        internal int Breaches;
        internal int BaseHP;
        internal int FortressHP;
        internal int FortressStage;
        internal int LastShotTick = -1;
        internal int TerminalEvents;
        internal int TerminalTick = -1;
        internal MatchOutcome Outcome = MatchOutcome.Playing;
        internal ulong EventHash = 14695981039346656037UL;
        internal ulong CommandHash = 14695981039346656037UL;

        internal MatchState(LevelDefinition config)
        {
            BaseHP = config.BaseHP;
            FortressHP = config.FortressHP;
        }

        internal long IssueId() { return NextStableId++; }
    }

    public static class OutcomeResolver
    {
        public static MatchOutcome Resolve(int baseHp, int fortressHp, int tick, int lastTick)
        {
            if (baseHp <= 0) return MatchOutcome.LoseBase;
            if (fortressHp <= 0) return MatchOutcome.Win;
            return tick >= lastTick ? MatchOutcome.LoseTimeout : MatchOutcome.Playing;
        }
    }
}

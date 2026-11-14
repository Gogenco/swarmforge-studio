using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    public readonly struct UnitView
    {
        public readonly long Id;
        public readonly int Position;
        public readonly int Lane;
        public readonly int CannonX;
        public readonly int BornTick;
        public readonly int PreviousPosition;
        public readonly bool GateUsed;

        public UnitView(long id, int position, int lane, bool gateUsed,
            int cannonX = 0, int bornTick = 0, int previousPosition = 0)
        {
            Id = id; Position = position; Lane = lane; GateUsed = gateUsed;
            CannonX = cannonX; BornTick = bornTick; PreviousPosition = previousPosition;
        }
    }

    /// <summary>Owned copies, never references to live simulation entities.</summary>
    public sealed class MatchSnapshot
    {
        public readonly int Tick;
        public readonly int BaseHP;
        public readonly int FortressHP;
        public readonly int FortressStage;
        public readonly int CannonX;
        public readonly int RawShots;
        public readonly int ProducedTotal;
        public readonly int GateExtraProduced;
        public readonly int ActiveMiniBots;
        public readonly int FortressHits;
        public readonly int EnemyLosses;
        public readonly int EnemiesDestroyed;
        public readonly int Breaches;
        public readonly int ActiveEnemies;
        public readonly int LastShotTick;
        public readonly int TerminalEvents;
        public readonly int TerminalTick;
        public readonly MatchOutcome Outcome;
        public readonly ulong EventHash;
        public readonly ulong CommandHash;
        public readonly ulong StateHash;
        private readonly UnitView[] minis;
        private readonly UnitView[] drones;
        private readonly IReadOnlyList<UnitView> miniReadOnly;
        private readonly IReadOnlyList<UnitView> enemyReadOnly;

        public IReadOnlyList<UnitView> MiniBots { get { return miniReadOnly; } }
        public IReadOnlyList<UnitView> Enemies { get { return enemyReadOnly; } }

        internal MatchSnapshot(int tick, MatchState s)
        {
            Tick = tick; BaseHP = s.BaseHP; FortressHP = s.FortressHP;
            FortressStage = s.FortressStage; CannonX = s.CannonX;
            RawShots = s.RawShots; ProducedTotal = s.ProducedTotal;
            GateExtraProduced = s.GateExtraProduced; FortressHits = s.FortressHits;
            EnemyLosses = s.EnemyLosses; EnemiesDestroyed = s.EnemiesDestroyed;
            Breaches = s.Breaches; LastShotTick = s.LastShotTick;
            TerminalEvents = s.TerminalEvents; TerminalTick = s.TerminalTick;
            Outcome = s.Outcome; EventHash = s.EventHash; CommandHash = s.CommandHash;
            minis = new UnitView[s.MiniBots.Count];
            for (int i = 0; i < minis.Length; i++)
            {
                MiniBot m = s.MiniBots[i];
                minis[i] = new UnitView(m.Id, m.Position, m.Lane, m.GateUsed,
                    m.CannonX, m.BornTick, m.PreviousPosition);
            }
            drones = new UnitView[s.Enemies.Count];
            for (int i = 0; i < drones.Length; i++)
            {
                EnemyDrone d = s.Enemies[i];
                drones[i] = new UnitView(d.Id, d.Position, d.Lane, true,
                    0, d.BornTick, d.PreviousPosition);
            }
            ActiveMiniBots = minis.Length; ActiveEnemies = drones.Length;
            miniReadOnly = Array.AsReadOnly(minis);
            enemyReadOnly = Array.AsReadOnly(drones);
            ulong hash = 14695981039346656037UL;
            Hash(ref hash, tick); Hash(ref hash, s.BaseHP); Hash(ref hash, s.FortressHP);
            Hash(ref hash, s.CannonX); Hash(ref hash, s.RawShots); Hash(ref hash, s.ProducedTotal);
            Hash(ref hash, s.FortressHits); Hash(ref hash, s.EnemyLosses);
            Hash(ref hash, s.EnemiesDestroyed); Hash(ref hash, s.Breaches);
            Hash(ref hash, s.GateExtraProduced); Hash(ref hash, s.FortressStage);
            Hash(ref hash, s.NextStableId); Hash(ref hash, s.CommandHash);
            Hash(ref hash, s.EventHash);
            Hash(ref hash, (int)s.Outcome); Hash(ref hash, s.TerminalTick);
            for (int i = 0; i < minis.Length; i++)
            {
                Hash(ref hash, minis[i].Id); Hash(ref hash, minis[i].Position);
                Hash(ref hash, minis[i].Lane); Hash(ref hash, minis[i].GateUsed ? 1 : 0);
                Hash(ref hash, minis[i].CannonX); Hash(ref hash, minis[i].BornTick);
                Hash(ref hash, minis[i].PreviousPosition);
            }
            for (int i = 0; i < drones.Length; i++)
            {
                Hash(ref hash, drones[i].Id); Hash(ref hash, drones[i].Position);
                Hash(ref hash, drones[i].Lane);
                Hash(ref hash, drones[i].BornTick); Hash(ref hash, drones[i].PreviousPosition);
            }
            StateHash = hash;
        }

        internal static void Hash(ref ulong hash, long number)
        {
            // Preserve the exact 64-bit two's-complement pattern of signed values.
            // This is the same unchecked conversion used in DEV-001/DEV-002.
            Hash(ref hash, unchecked((ulong)number));
        }

        internal static void Hash(ref ulong hash, ulong number)
        {
            // EventHash/CommandHash are already unsigned. Hash all 64 bits
            // directly: no narrowing conversion, truncation, or sign change.
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(number >> (8 * i));
                    hash *= 1099511628211UL;
                }
            }
        }
    }
}

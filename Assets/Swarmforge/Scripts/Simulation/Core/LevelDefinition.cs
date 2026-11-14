using System;

namespace Swarmforge.Simulation
{
    /// <summary>Pure data. Integer positions use FixedUnitsPerWorldUnit subdivisions.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public string LevelId = "VS01_TEST_ARENA";
        public int ConfigVersion = 1;
        public int TickRate = 30;
        public int MatchTicks = 1800;
        public int FirstShotTick = 0;
        public int ShotPeriodTicks = 10;
        public int BaseHP = 3;
        public int FortressHP = 90;
        public int FixedUnitsPerWorldUnit = 3000;
        public int MiniBotSpeedPerTick = 340;
        public int EnemySpeedPerTick = 230;
        public int GatePosition = 9000;
        public int AttackPathLength = 51000;
        public int EnemyPathLength = 51000;
        public int GateLeftMaxX = -120;
        public int GateRightMinX = 120;
        public int LeftGateMultiplier = 2;
        public int RightGateMultiplier = 3;
        public int ContactRadius = 450;
        public int CannonMinX = -1000;
        public int CannonMaxX = 1000;
        public int MaxVisibleMiniBots = 120;
        public EnemyWave[] EnemyWaves = { new EnemyWave(360, 1, 1) };

        public int GateForX(int x)
        {
            if (x <= GateLeftMaxX) return LeftGateMultiplier;
            if (x >= GateRightMinX) return RightGateMultiplier;
            return 1;
        }

        // The neutral gap must never be presented or simulated as a multiplied lane.
        // -1 = x2, 0 = bypass, +1 = x3.
        public int LaneForX(int x)
        {
            if (x <= GateLeftMaxX) return -1;
            if (x >= GateRightMinX) return 1;
            return 0;
        }

        public LevelDefinition Copy()
        {
            var clone = (LevelDefinition)MemberwiseClone();
            clone.EnemyWaves = (EnemyWave[])EnemyWaves.Clone();
            return clone;
        }

        public void Validate()
        {
            if (TickRate != 30 || MatchTicks <= 0 || ShotPeriodTicks <= 0 ||
                FirstShotTick < 0 || FirstShotTick >= MatchTicks ||
                BaseHP <= 0 || FortressHP <= 0 ||
                FixedUnitsPerWorldUnit <= 0 || MiniBotSpeedPerTick <= 0 || EnemySpeedPerTick <= 0 ||
                GatePosition < 0 || GatePosition >= AttackPathLength || AttackPathLength <= 0 ||
                EnemyPathLength <= 0 || ContactRadius < 0 ||
                GateLeftMaxX >= GateRightMinX ||
                GateLeftMaxX < CannonMinX || GateRightMinX > CannonMaxX ||
                LeftGateMultiplier < 1 || RightGateMultiplier < 1 ||
                CannonMinX >= CannonMaxX || MaxVisibleMiniBots < 1 || EnemyWaves == null)
                throw new ArgumentException("Invalid VS-01 LevelDefinition.");
            for (int i = 0; i < EnemyWaves.Length; i++)
            {
                EnemyWave wave = EnemyWaves[i];
                if (wave.Tick < 0 || wave.Tick > MatchTicks || wave.Left < 0 || wave.Right < 0)
                    throw new ArgumentException("Invalid enemy wave at index " + i);
            }
        }
    }

    [Serializable]
    public struct EnemyWave
    {
        public int Tick;
        public int Left;
        public int Right;

        public EnemyWave(int tick, int left, int right)
        {
            Tick = tick;
            Left = left;
            Right = right;
        }
    }
}

namespace Swarmforge.Simulation
{
    /// <summary>
    /// Dedicated, intentionally narrow P2 fixture. The P1 three-box authored
    /// presets remain available and unchanged for subsequent migration review.
    /// HP2 and HP3 differ in one parameter only; all other tuning is editable.
    /// </summary>
    public static class V12P2Presets
    {
        public static V12BalanceDefinition Create(int miniBotHp)
        {
            V12BalanceDefinition value = V12BalanceDefinition.CreateTrialPreset(miniBotHp);
            value.ProfileId = miniBotHp == 2 ? "P2_HP2" : "P2_HP3";
            value.LevelId = "P2_PLAYABLE_60S";
            // Owner P2 fire-rate approval: one raw shot per 30 simulation
            // ticks. The active ×2 boost copies that shot (2 bots/sec).
            value.ShotPeriodTicks = 30;
            // Owner-approved P2 trial balance candidate; P1/VS-01 remain as-is.
            value.FortressHP = 40;
            value.BaseHP = 3;
            value.EnemyTypes = new[]
            {
                new V12EnemyBalance("SCOUT", 2, 1, 230, 15)
            };
            value.Boxes = new[]
            {
                new V12BoxBalance("X2", -950, 9000, 450, 5, 2, 360)
            };
            value.Waves = new[]
            {
                new V12WaveBalance(180, 60, new[]
                {
                    new V12WaveSpawn("SCOUT", -1, 2),
                    new V12WaveSpawn("SCOUT", 1, 2)
                })
            };
            value.Validate();
            return value;
        }
    }
}

using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Infrastructure
{
    /// <summary>
    /// Inspector-editable v1.2 trial profile. It is intentionally NOT referenced
    /// by the approved DEV-002.2 Battle scene or LevelConfigAsset.
    /// Calling CreateMatchConfig validates and freezes a copy for a future trial.
    /// </summary>
    [CreateAssetMenu(menuName = "SWARMFORGE/v1.2 Trial Balance Profile", fileName = "V12_Trial_Balance")]
    public sealed class V12BalanceConfigAsset : ScriptableObject
    {
        [SerializeField] private V12BalanceDefinition data = V12BalanceDefinition.CreateTrialPreset(2);

        public V12MatchConfig CreateMatchConfig()
        {
            return new V12MatchConfig(data);
        }

        public V12BalanceDefinition CreateEditableCopy()
        {
            return data.Copy();
        }
    }
}

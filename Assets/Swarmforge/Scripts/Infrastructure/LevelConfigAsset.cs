using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Infrastructure
{
    [CreateAssetMenu(menuName = "SWARMFORGE/VS-01 Level Definition", fileName = "VS01_Arena")]
    public sealed class LevelConfigAsset : ScriptableObject
    {
        [SerializeField] private LevelDefinition data = new LevelDefinition();
        public LevelDefinition CreateDefinition() { return data.Copy(); }
    }
}

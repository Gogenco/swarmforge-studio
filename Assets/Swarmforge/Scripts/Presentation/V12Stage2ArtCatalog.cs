using UnityEngine;

namespace Swarmforge.Presentation
{
    /// <summary>
    /// Seven pure-visual Stage 2 prefabs. Generated and wired in the Unity Editor
    /// from Art Director's authored OBJ meshes; referenced by builds via Resources.
    /// Does not own collision, AI, simulation time or game balance.
    /// </summary>
    public sealed class V12Stage2ArtCatalog : ScriptableObject
    {
        [SerializeField] private GameObject friendlyMiniBot;
        [SerializeField] private GameObject redEnemyRobot;
        [SerializeField] private GameObject autoCannon;
        [SerializeField] private GameObject x2Chest;
        [SerializeField] private GameObject playerBase;
        [SerializeField] private GameObject enemyFortress;
        [SerializeField] private GameObject arena;

        public GameObject FriendlyMiniBot => friendlyMiniBot;
        public GameObject RedEnemyRobot => redEnemyRobot;
        public GameObject AutoCannon => autoCannon;
        public GameObject X2Chest => x2Chest;
        public GameObject PlayerBase => playerBase;
        public GameObject EnemyFortress => enemyFortress;
        public GameObject Arena => arena;
        public bool IsComplete => friendlyMiniBot != null && redEnemyRobot != null &&
            autoCannon != null && x2Chest != null && playerBase != null &&
            enemyFortress != null && arena != null;

        public static V12Stage2ArtCatalog Load()
        {
            return Resources.Load<V12Stage2ArtCatalog>("SwarmforgeStage2ArtCatalog");
        }

        // Called exclusively from the explicitly invoked Stage 2 Editor binder.
        public void Assign(GameObject miniBot, GameObject enemy, GameObject cannon,
            GameObject chest, GameObject friendlyBase, GameObject fortress, GameObject floor)
        {
            friendlyMiniBot = miniBot;
            redEnemyRobot = enemy;
            autoCannon = cannon;
            x2Chest = chest;
            playerBase = friendlyBase;
            enemyFortress = fortress;
            arena = floor;
        }
    }
}

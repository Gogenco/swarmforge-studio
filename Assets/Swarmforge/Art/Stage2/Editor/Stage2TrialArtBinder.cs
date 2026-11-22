#if UNITY_EDITOR
using System;
using System.IO;
using Swarmforge.Art.Editor;
using Swarmforge.Presentation;
using UnityEditor;
using UnityEngine;

namespace Swarmforge.Art.Editor
{
    /// <summary>
    /// Deliberate one-click Stage 2 integration: generate the original seven
    /// native art prefabs, then bind them into a build-safe Resources catalog.
    /// No gameplay, scene, package or ProjectSettings modifications.
    /// </summary>
    public static class Stage2TrialArtBinder
    {
        private const string SourcePrefabs = "Assets/Swarmforge/Art/Stage2/Generated/Prefabs/";
        private const string CatalogPath = "Assets/Swarmforge/Resources/SwarmforgeStage2ArtCatalog.asset";

        [MenuItem("SWARMFORGE/Stage 2/Generate and Integrate P8 Art")]
        public static void GenerateAndIntegrate()
        {
            Stage2ArtBuilder.Generate(); // Real authored Wavefront OBJ → native Unity prefabs.
            GameObject mini = Prefab("S2_FriendlyMiniBot");
            GameObject red = Prefab("S2_RedEnemyRobot");
            GameObject cannon = Prefab("S2_AutoCannon");
            GameObject chest = Prefab("S2_X2Chest");
            GameObject playerBase = Prefab("S2_PlayerBase");
            GameObject enemyFortress = Prefab("S2_EnemyFortress");
            GameObject arena = Prefab("S2_Arena");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Swarmforge/Resources"));
            AssetDatabase.Refresh();
            V12Stage2ArtCatalog catalog =
                AssetDatabase.LoadAssetAtPath<V12Stage2ArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<V12Stage2ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Assign(mini, red, cannon, chest, playerBase, enemyFortress, arena);
            if (!catalog.IsComplete)
                throw new InvalidOperationException("Stage 2 art catalog is missing one or more prefabs.");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SWARMFORGE Stage 2: all seven 3D prefabs bound to P8 view. " +
                "Open V12_Trial.unity and press Play. No simulation code changed.");
        }

        private static GameObject Prefab(string key)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SourcePrefabs + key + ".prefab");
            if (prefab == null)
                throw new InvalidOperationException("Missing generated Stage 2 prefab " + key +
                    ". Check Console and re-run Generate and Integrate P8 Art.");
            return prefab;
        }
    }
}
#endif

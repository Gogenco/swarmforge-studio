using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Swarmforge.Editor
{
    /// <summary>Idempotent VS-01 setup; generates URP assets through Unity's own API.</summary>
    [InitializeOnLoad]
    public static class SwarmforgeProjectSetup
    {
        private const string RendererPath = "Assets/Swarmforge/Configs/Tuning/SWARMFORGE_Renderer.asset";
        private const string PipelinePath = "Assets/Swarmforge/Configs/Tuning/SWARMFORGE_URP.asset";

        static SwarmforgeProjectSetup()
        {
            EditorApplication.delayCall += AutoConfigureMissingPipeline;
        }

        private static void AutoConfigureMissingPipeline()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoConfigureMissingPipeline;
                return;
            }
            if (GraphicsSettings.defaultRenderPipeline == null) EnsureUrp();
        }

        [MenuItem("SWARMFORGE/Configure VS-01 project (URP + portrait + battle scene)")]
        public static void Configure()
        {
            EnsureUrp();
            PlayerSettings.companyName = "AI Game Development Studio";
            PlayerSettings.productName = "SWARMFORGE";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Swarmforge/Scenes/Battle.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("SWARMFORGE VS-01: URP, portrait and Battle scene configured. " +
                "Verify the URP asset in Graphics and Quality settings before testing.");
        }

        private static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset configured)
            {
                QualitySettings.renderPipeline = configured;
                return;
            }
            if (GraphicsSettings.defaultRenderPipeline == null &&
                QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrp)
            {
                GraphicsSettings.defaultRenderPipeline = qualityUrp;
                AssetDatabase.SaveAssets();
                return;
            }
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Debug.LogWarning("SWARMFORGE: A non-URP pipeline is already assigned. " +
                    "Preserved existing setting; ask Tech Lead before replacing it.");
                return;
            }

            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log("SWARMFORGE: generated/assigned URP renderer + pipeline assets.");
        }
    }
}

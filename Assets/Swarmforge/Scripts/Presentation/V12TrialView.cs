using System.Collections.Generic;
using Swarmforge.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swarmforge.Presentation
{
    /// <summary>
    /// Purely presentational geometry for the opt-in P2 scene. There are no
    /// physics callbacks, collision-driven damage, or dependencies on VS-01.
    /// Simulation coordinates 0..AttackPathLength map into the portrait arena.
    /// </summary>
    internal sealed class V12TrialView
    {
        private readonly V12BalanceDefinition config;
        private readonly V12Stage2ArtCatalog art;
        private readonly Vector3 intactFortressScale;
        private readonly bool hasNativeArt;
        private readonly Transform root;
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly Dictionary<long, GameObject> activeBots = new Dictionary<long, GameObject>();
        private readonly Dictionary<long, GameObject> activeEnemies = new Dictionary<long, GameObject>();
        private readonly Stack<GameObject> botPool = new Stack<GameObject>();
        private readonly Stack<GameObject> enemyPool = new Stack<GameObject>();
        private readonly HashSet<long> seen = new HashSet<long>();
        private readonly List<long> cleanup = new List<long>();
        private readonly Material botMaterial;
        private readonly Material enemyMaterial;
        private readonly Material destroyedMaterial;
        private readonly GameObject cannon;
        private readonly GameObject box;
        private readonly GameObject fortress;
        private readonly Camera camera;

        public V12TrialView(V12BalanceDefinition definition)
        {
            config = definition;
            // A single Editor binder generates an asset catalog; gameplay is
            // still fully playable as the original P8 greybox before binding.
            art = V12Stage2ArtCatalog.Load();
            hasNativeArt = art != null && art.IsComplete;
            root = new GameObject("V12_Trial_Presentation_Only").transform;
            var floorMaterial = MakeMaterial(new Color(.09f, .13f, .24f));
            var laneMaterial = MakeMaterial(new Color(.17f, .27f, .39f));
            var borderMaterial = MakeMaterial(new Color(.12f, .57f, .75f));
            var boxMaterial = MakeMaterial(new Color(1f, .72f, .15f));
            botMaterial = MakeMaterial(new Color(.27f, .99f, .66f));
            enemyMaterial = MakeMaterial(new Color(.96f, .25f, .36f));
            destroyedMaterial = MakeMaterial(new Color(.38f, .45f, .48f));

            if (hasNativeArt)
            {
                // Final City: all authored buildings, textured boulevard, trees,
                // lawns and furniture are part of the one native city prefab.
                // It is purely visual. Never duplicate the greybox road here.
                PlaceArt("Trial_Art_Arena", art.Arena, new Vector3(0f, 0f, 0f), Vector3.one);
                PlaceArt("Player_Base", art.PlayerBase,
                    new Vector3(0f, .015f, -1.5f), Vector3.one * .95f);
                fortress = PlaceArt("P2_Enemy_Fortress", art.EnemyFortress,
                    new Vector3(0f, .015f, 18.35f), Vector3.one * .90f);
                // Keep authored +Z .9535 muzzle at the *real* sim birth Z=0.
                // No gameplay shot positions, aim or timing are altered.
                cannon = PlaceArt("P2_Auto_Cannon", art.AutoCannon,
                    new Vector3(0f, .02f, -Stage2MuzzleBackOffset), Vector3.one * CannonModelScale);
                V12BoxBalance b = definition.Boxes[0];
                box = PlaceArt("X2_Destructible_Box", art.X2Chest,
                    new Vector3(WorldX(b.X), .015f, WorldZ(b.Position)), Vector3.one);
            }
            else
            {
                Block("Trial_Floor", floorMaterial, new Vector3(0, -.3f, 9f), new Vector3(8.3f, .35f, 22));
                Block("Left_Lane", laneMaterial, new Vector3(-2.0f, -.08f, 9f), new Vector3(3.7f, .1f, 19.4f));
                Block("Right_Lane", laneMaterial, new Vector3(2.0f, -.08f, 9f), new Vector3(3.7f, .1f, 19.4f));
                Block("Center_Divider", borderMaterial, new Vector3(0, -.01f, 9f), new Vector3(.08f, .1f, 19));
                Block("Player_Base", borderMaterial, new Vector3(0, .48f, -1.5f), new Vector3(5, 1f, .75f));
                fortress = Block("P2_Enemy_Fortress", destroyedMaterial,
                    new Vector3(0, 1.15f, 18.35f), new Vector3(4.8f, 2.3f, .9f));
                cannon = Block("P2_Auto_Cannon", botMaterial,
                    new Vector3(0, .63f, -.35f), new Vector3(.78f, 1.25f, .9f));
                V12BoxBalance b = definition.Boxes[0];
                box = Block("X2_Destructible_Box", boxMaterial,
                    new Vector3(WorldX(b.X), .74f, WorldZ(b.Position)),
                    new Vector3(1.3f, 1.3f, .9f));
            }
            intactFortressScale = fortress.transform.localScale;

            var cameraObject = new GameObject("V12_Trial_Camera");
            cameraObject.transform.SetParent(root);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            // P12 is closer and lower than the rejected P11 FOV55 camera.
            // Zero yaw preserves the P8 projected cannon-rail aim mapping.
            camera.orthographic = false;
            camera.fieldOfView = PerspectiveVerticalFov;
            camera.transform.position = new Vector3(0f, 11f, -9f);
            camera.transform.LookAt(new Vector3(0f, 1f, 4f), Vector3.up);
            camera.nearClipPlane = .15f;
            camera.farClipPlane = 110f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.66f, .81f, .94f); // daylight sky

            // A single, restrained soft-shadow sun. The merged native city
            // casts only from its Surface layer; bot crowds do not cast into
            // the shadow map. Respect URP/quality shadow availability.
            var lightObject = new GameObject("V12_Trial_Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(53f, -42f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .975f, .94f);
            light.intensity = 1.34f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .30f;
            light.shadowBias = .07f;
            light.shadowNormalBias = .35f;
            light.shadowResolution = LightShadowResolution.Low;

            // Cool, shadow-free daylight fill keeps the URP Lit glass, armor
            // and foliage readable without extra point lights or Stage3 VFX.
            var fillObject = new GameObject("V12_Trial_Cool_Fill");
            fillObject.transform.SetParent(root);
            fillObject.transform.rotation = Quaternion.Euler(32f, 138f, 0f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.73f, .83f, 1f);
            fill.intensity = .27f;
            fill.shadows = LightShadows.None;
        }

        private const float CannonModelScale = .84f;
        // P9 model's authored front muzzle ends near +0.9535 local Z.
        // The root stays negative by this much; shot birth remains world Z=0.
        private const float Stage2MuzzleBackOffset = .9535f * CannonModelScale;
        // P12 cinematic portrait proposal (Owner Unity validation required).
        // Exterior skyscraper crowns may clip to keep the P8 road, real
        // single left chest and both fortresses visibly playable.
        private const float PerspectiveVerticalFov = 50f;
        // P12 authored MiniBot has local Z depth 1.315; the true 8-tick
        // sequential x2 gap is at least ~.84 world units. Narrow ONLY the
        // render depth to prevent overlapping large heroes. True X/Z stays.
        private static readonly Vector3 NativeGreenScale = new Vector3(.78f, .80f, .57f);
        private static readonly Vector3 NativeRedScale = new Vector3(.72f, .72f, .72f);

        public Vector3 BoxLabelWorld()
        {
            V12BoxBalance b = config.Boxes[0];
            return new Vector3(WorldX(b.X), hasNativeArt ? 1.85f : 1.7f, WorldZ(b.Position));
        }

        /// <summary>Map a finger X to the projected cannon rail at its own depth.
        /// Do NOT cast a ray through an arbitrary touch Y: mobile players
        /// drag across the bottom 28% and the cannon aim must remain stable.
        /// </summary>
        public int AimFromScreenX(float screenX)
        {
            return V12TrialAimProjection.ScreenToAim(camera, screenX,
                config.CannonMinX, config.CannonMaxX,
                WorldX(config.CannonMinX), WorldX(config.CannonMaxX));
        }

        public void Render(V12TrialSnapshot s)
        {
            cannon.transform.position = new Vector3(WorldX(s.CannonX),
                hasNativeArt ? .02f : .63f,
                hasNativeArt ? -Stage2MuzzleBackOffset : -.35f);
            box.SetActive(s.BoxHP > 0);
            // Both models and legacy cube use the same sim-driven fortress state.
            fortress.transform.localScale = s.FortressHP <= 0 ?
                new Vector3(intactFortressScale.x, intactFortressScale.y * .174f, intactFortressScale.z) :
                intactFortressScale;
            ShowUnits(s.Bots, activeBots, botPool, PrimitiveType.Capsule, botMaterial, "P2_MiniBot", 100, true);
            ShowUnits(s.Enemies, activeEnemies, enemyPool, PrimitiveType.Sphere, enemyMaterial, "P2_Enemy", 100, false);
        }

        private void ShowUnits(IReadOnlyList<V12TrialUnitView> units,
            Dictionary<long, GameObject> active, Stack<GameObject> pool,
            PrimitiveType primitive, Material material, string label, int visualLimit, bool greenBot)
        {
            seen.Clear();
            // Stable first-N visuals cap. All simulation entities remain logical.
            for (int i = 0; i < units.Count && i < visualLimit; i++)
            {
                V12TrialUnitView u = units[i];
                seen.Add(u.Id);
                if (!active.TryGetValue(u.Id, out GameObject instance))
                {
                    GameObject nativePrefab = hasNativeArt ? (greenBot ? art.FriendlyMiniBot : art.RedEnemyRobot) : null;
                    instance = Rent(pool, primitive, material, label, nativePrefab);
                    // The four red enemies may cast small contact shadows;
                    // the many green bots deliberately avoid costly shadow
                    // casters. Geometry never affects hitboxes.
                    if (nativePrefab != null) SetNativeShadowPolicy(instance, false, greenBot);
                    active.Add(u.Id, instance);
                }
                // Green bots use ONLY authoritative X/Z. Sequential x2
                // spacing is already produced by the 30Hz simulation;
                // never fan out sideways or fake depth. Red enemies keep
                // their previously approved ID-stable visual separation.
                Vector2 visualOffset = greenBot ?
                    V12FormationLayout.ForBot(u.Position, config.AttackPathLength) :
                    V12FormationLayout.ForId(u.Id);
                instance.transform.position = new Vector3(WorldX(u.X) + visualOffset.x,
                    hasNativeArt ? .012f : (greenBot ? .39f : .49f),
                    WorldZ(u.Position) + visualOffset.y);
                instance.transform.localScale = hasNativeArt ?
                    (greenBot ? NativeGreenScale : NativeRedScale) :
                    greenBot ? new Vector3(.30f, .32f, .30f) :
                    new Vector3(.54f + .10f * (u.MaxHP - 1), .54f, .54f);
            }
            cleanup.Clear();
            foreach (var pair in active) if (!seen.Contains(pair.Key)) cleanup.Add(pair.Key);
            for (int i = 0; i < cleanup.Count; i++)
            {
                GameObject old = active[cleanup[i]];
                old.SetActive(false);
                pool.Push(old);
                active.Remove(cleanup[i]);
            }
        }

        private GameObject Rent(Stack<GameObject> pool, PrimitiveType primitive,
            Material material, string label, GameObject prefab)
        {
            GameObject go;
            if (pool.Count != 0) go = pool.Pop();
            else if (prefab != null) go = Object.Instantiate(prefab, root);
            else go = GameObject.CreatePrimitive(primitive);
            go.name = label;
            go.transform.SetParent(root);
            // Native prefabs own their mesh renderers/materials on children.
            // Only change the root renderer for the primitive fallback.
            if (prefab == null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            go.SetActive(true);
            return go;
        }

        private GameObject PlaceArt(string name, GameObject prefab, Vector3 position, Vector3 scale)
        {
            GameObject go = Object.Instantiate(prefab, root);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            SetNativeShadowPolicy(go, name == "Trial_Art_Arena", false);
            return go;
        }

        // P12 Editor generation names the visual groups LOD0_Surface etc.
        // Restrict the city shadow caster to the merged Surface, not PBR
        // asphalt/grass/glass/sidewalk render layers. No new physics.
        private static void SetNativeShadowPolicy(GameObject visual, bool city, bool green)
        {
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                string layer = renderer.gameObject.name;
                bool glow = layer == "Glow" || layer.EndsWith("_Glow");
                bool cityCaster = city &&
                    (layer == "Surface" || layer == "LOD0_Surface");
                renderer.shadowCastingMode =
                    !glow && !green && (!city || cityCaster) ?
                    ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = !glow;
            }
        }

        private GameObject Block(string name, Material material, Vector3 position, Vector3 size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Collider>().enabled = false;
            return go;
        }

        private Material MakeMaterial(Color color)
        {
            bool urpActive = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(urpActive ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null) shader = Shader.Find(urpActive ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("No compatible trial shader found.");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            ownedMaterials.Add(material);
            return material;
        }

        private float WorldX(int x)
        {
            return Mathf.Lerp(-3.2f, 3.2f, (x - config.CannonMinX) /
                (float)(config.CannonMaxX - config.CannonMinX));
        }

        private float WorldZ(int z) { return 18f * z / config.AttackPathLength; }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
            for (int i = 0; i < ownedMaterials.Count; i++) Object.Destroy(ownedMaterials[i]);
        }
    }

    /// <summary>
    /// Pure presentation-level X mapping shared by the real pointer path and
    /// Unity PlayMode regressions. Both rail endpoints are projected at the
    /// SAME world Y/Z plane, so perspective scaling cannot redirect a shot.
    /// Sim commands, spawn X and collision remain authoritative elsewhere.
    /// </summary>
    public static class V12TrialAimProjection
    {
        public static int ScreenToAim(Camera camera, float screenX,
            int cannonMinX, int cannonMaxX, float leftWorldX, float rightWorldX)
        {
            if (camera == null) throw new System.ArgumentNullException(nameof(camera));
            float left = camera.WorldToScreenPoint(new Vector3(leftWorldX, .5f, 0f)).x;
            float right = camera.WorldToScreenPoint(new Vector3(rightWorldX, .5f, 0f)).x;
            float t = Mathf.Clamp01((screenX - left) / Mathf.Max(1f, right - left));
            return Mathf.RoundToInt(Mathf.Lerp(cannonMinX, cannonMaxX, t));
        }
    }
}

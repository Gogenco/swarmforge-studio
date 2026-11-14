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
            root = new GameObject("V12_Trial_Presentation_Only").transform;
            var floorMaterial = MakeMaterial(new Color(.09f, .13f, .24f));
            var laneMaterial = MakeMaterial(new Color(.17f, .27f, .39f));
            var borderMaterial = MakeMaterial(new Color(.12f, .57f, .75f));
            var boxMaterial = MakeMaterial(new Color(1f, .72f, .15f));
            botMaterial = MakeMaterial(new Color(.27f, .99f, .66f));
            enemyMaterial = MakeMaterial(new Color(.96f, .25f, .36f));
            destroyedMaterial = MakeMaterial(new Color(.38f, .45f, .48f));

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

            var cameraObject = new GameObject("V12_Trial_Camera");
            cameraObject.transform.SetParent(root);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 11.75f;
            camera.transform.position = new Vector3(0, 27f, 8f);
            camera.transform.rotation = Quaternion.Euler(85f, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .065f, .12f);
            var lightObject = new GameObject("V12_Trial_Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(43f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
        }

        public Vector3 BoxLabelWorld()
        {
            V12BoxBalance b = config.Boxes[0];
            return new Vector3(WorldX(b.X), 1.7f, WorldZ(b.Position));
        }

        /// <summary>Screen-space projection of the *visible* cannon rail.</summary>
        public int AimFromScreenX(float screenX)
        {
            float left = camera.WorldToScreenPoint(new Vector3(WorldX(config.CannonMinX), .5f, -.35f)).x;
            float right = camera.WorldToScreenPoint(new Vector3(WorldX(config.CannonMaxX), .5f, -.35f)).x;
            float t = Mathf.Clamp01((screenX - left) / Mathf.Max(1f, right - left));
            return Mathf.RoundToInt(Mathf.Lerp(config.CannonMinX, config.CannonMaxX, t));
        }

        public void Render(V12TrialSnapshot s)
        {
            cannon.transform.position = new Vector3(WorldX(s.CannonX), .63f, -.35f);
            box.SetActive(s.BoxHP > 0);
            if (s.FortressHP <= 0) fortress.transform.localScale = new Vector3(4.8f, .4f, .9f);
            else fortress.transform.localScale = new Vector3(4.8f, 2.3f, .9f);
            ShowUnits(s.Bots, activeBots, botPool, PrimitiveType.Capsule, botMaterial, "P2_MiniBot", 100);
            ShowUnits(s.Enemies, activeEnemies, enemyPool, PrimitiveType.Sphere, enemyMaterial, "P2_Enemy", 100);
        }

        private void ShowUnits(IReadOnlyList<V12TrialUnitView> units,
            Dictionary<long, GameObject> active, Stack<GameObject> pool,
            PrimitiveType primitive, Material material, string label, int visualLimit)
        {
            seen.Clear();
            // Stable first-N visuals cap. All simulation entities remain logical.
            for (int i = 0; i < units.Count && i < visualLimit; i++)
            {
                V12TrialUnitView u = units[i];
                seen.Add(u.Id);
                if (!active.TryGetValue(u.Id, out GameObject instance))
                {
                    instance = Rent(pool, primitive, material, label);
                    active.Add(u.Id, instance);
                }
                float offset = ((u.Id % 3) - 1) * .10f;
                instance.transform.position = new Vector3(WorldX(u.X) + offset,
                    primitive == PrimitiveType.Capsule ? .39f : .49f, WorldZ(u.Position));
                instance.transform.localScale = primitive == PrimitiveType.Capsule ?
                    new Vector3(.30f, .32f, .30f) :
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

        private GameObject Rent(Stack<GameObject> pool, PrimitiveType primitive, Material material, string label)
        {
            GameObject go = pool.Count == 0 ? GameObject.CreatePrimitive(primitive) : pool.Pop();
            go.name = label;
            go.transform.SetParent(root);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            go.SetActive(true);
            return go;
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
}

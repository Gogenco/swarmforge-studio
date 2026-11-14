using System.Collections.Generic;
using Swarmforge.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swarmforge.Presentation
{
    /// <summary>Disposable greybox visuals; neither physics nor Animator can mutate sim.</summary>
    internal sealed class PrimitiveBattleView
    {
        private readonly Transform root;
        private readonly Dictionary<long, GameObject> botActive = new Dictionary<long, GameObject>();
        private readonly Dictionary<long, GameObject> enemyActive = new Dictionary<long, GameObject>();
        private readonly Stack<GameObject> botPool = new Stack<GameObject>();
        private readonly Stack<GameObject> enemyPool = new Stack<GameObject>();
        private readonly HashSet<long> visibleBots = new HashSet<long>();
        private readonly HashSet<long> visibleEnemies = new HashSet<long>();
        private readonly List<long> cleanup = new List<long>();
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly Material[] fortressMaterials;
        private readonly Material botMaterial;
        private readonly Material enemyMaterial;
        private readonly GameObject fortress;
        private readonly GameObject cannon;
        private readonly LevelDefinition config;

        public PrimitiveBattleView(LevelDefinition data)
        {
            config = data;
            root = new GameObject("VS01_Presentation_Only").transform;
            Material floor = MakeMaterial(new Color(.11f, .16f, .24f));
            Material lane = MakeMaterial(new Color(.19f, .29f, .41f));
            Material gateTwo = MakeMaterial(new Color(.12f, .67f, .91f));
            Material gateNeutral = MakeMaterial(new Color(.38f, .42f, .47f));
            Material gateThree = MakeMaterial(new Color(.94f, .73f, .15f));
            botMaterial = MakeMaterial(new Color(.22f, .95f, .70f));
            enemyMaterial = MakeMaterial(new Color(1f, .26f, .32f));
            fortressMaterials = new[]
            {
                MakeMaterial(new Color(.4f,.45f,.55f)), MakeMaterial(new Color(.7f,.45f,.25f)),
                MakeMaterial(new Color(.87f,.22f,.21f)), MakeMaterial(new Color(.16f,.14f,.16f))
            };
            CreateBlock("Floor", floor, new Vector3(0, -.35f, 8), new Vector3(8.8f, .4f, 23));
            CreateBlock("Left_Lane", lane, new Vector3(-1.85f, -.12f, 8), new Vector3(3.55f, .12f, 19));
            CreateBlock("Right_Lane", lane, new Vector3(1.85f, -.12f, 8), new Vector3(3.55f, .12f, 19));
            float zGate = data.GatePosition / (float)data.FixedUnitsPerWorldUnit;
            // World geometry matches exact integer simulation thresholds.
            CreateGateBlock("Gate_x2", gateTwo, data.CannonMinX, data.GateLeftMaxX, zGate);
            CreateGateBlock("Gate_x1_Bypass", gateNeutral,
                data.GateLeftMaxX, data.GateRightMinX, zGate);
            CreateGateBlock("Gate_x3", gateThree, data.GateRightMinX, data.CannonMaxX, zGate);
            float zFortress = data.AttackPathLength / (float)data.FixedUnitsPerWorldUnit + .8f;
            fortress = CreateBlock("Fortress_Core", fortressMaterials[0], new Vector3(0,1.25f,zFortress), new Vector3(5,2.5f,1.3f));
            CreateBlock("Player_Base", gateTwo, new Vector3(0,.5f,-2.1f), new Vector3(5,1,1));
            cannon = CreateBlock("Auto_Cannon", botMaterial,
                new Vector3(WorldX(-650),.65f,-.55f), new Vector3(.9f,1.3f,1.1f));
            var cameraObject = new GameObject("Portrait_Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            camera.transform.position = new Vector3(0, 27, 5);
            camera.transform.rotation = Quaternion.Euler(85, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f,.09f,.17f);
            var lightObject = new GameObject("Greybox_Directional_Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(44, -23, 0);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
        }

        public void Render(MatchSnapshot snapshot)
        {
            cannon.transform.position = new Vector3(WorldX(snapshot.CannonX), .65f, -.55f);
            fortress.GetComponent<Renderer>().sharedMaterial = fortressMaterials[snapshot.FortressStage];
            fortress.transform.localScale = snapshot.FortressStage == 3 ?
                new Vector3(5, .55f, 1.3f) : new Vector3(5, 2.5f - snapshot.FortressStage * .4f, 1.3f);

            visibleBots.Clear();
            // Visual cap affects ONLY presentation, never logical simulation or damage.
            List<long> desired = StableVisualSelection.Choose(
                snapshot.MiniBots, botActive.Keys, config.MaxVisibleMiniBots);
            var lookup = new Dictionary<long, UnitView>(snapshot.MiniBots.Count);
            for (int i = 0; i < snapshot.MiniBots.Count; i++)
                lookup.Add(snapshot.MiniBots[i].Id, snapshot.MiniBots[i]);
            for (int i = 0; i < desired.Count; i++)
            {
                UnitView item = lookup[desired[i]];
                visibleBots.Add(item.Id);
                if (!botActive.TryGetValue(item.Id, out var model))
                {
                    model = Rent(botPool, PrimitiveType.Capsule, botMaterial, "MiniBot_Visual");
                    botActive.Add(item.Id, model);
                }
                float progressToTarget = Mathf.Clamp01((item.Position - config.GatePosition) /
                    (float)Mathf.Max(1, config.AttackPathLength - config.GatePosition));
                float x = Mathf.Lerp(WorldX(item.CannonX), item.Lane * 1.85f, progressToTarget);
                float offset = (item.Id % 3 - 1) * .07f;
                model.transform.position = new Vector3(x, .40f,
                    item.Position / (float)config.FixedUnitsPerWorldUnit + offset);
            }
            RecycleMissing(botActive, botPool, visibleBots);

            visibleEnemies.Clear();
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                UnitView item = snapshot.Enemies[i];
                visibleEnemies.Add(item.Id);
                if (!enemyActive.TryGetValue(item.Id, out var model))
                {
                    model = Rent(enemyPool, PrimitiveType.Sphere, enemyMaterial, "Enemy_Visual");
                    enemyActive.Add(item.Id, model);
                }
                model.transform.position = new Vector3(item.Lane * 1.85f, .52f,
                    (config.EnemyPathLength - item.Position) / (float)config.FixedUnitsPerWorldUnit);
            }
            RecycleMissing(enemyActive, enemyPool, visibleEnemies);
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
            for (int i = 0; i < ownedMaterials.Count; i++) Object.Destroy(ownedMaterials[i]);
        }

        public Vector3 GateLabelWorld(int lane)
        {
            int min = lane < 0 ? config.CannonMinX : lane == 0 ? config.GateLeftMaxX : config.GateRightMinX;
            int max = lane < 0 ? config.GateLeftMaxX : lane == 0 ? config.GateRightMinX : config.CannonMaxX;
            return new Vector3((WorldX(min) + WorldX(max)) * .5f, .55f,
                config.GatePosition / (float)config.FixedUnitsPerWorldUnit);
        }

        private float WorldX(int value)
        {
            return Mathf.Lerp(-3.2f, 3.2f, (value - config.CannonMinX) /
                (float)(config.CannonMaxX - config.CannonMinX));
        }

        private void CreateGateBlock(string label, Material material, int left, int right, float z)
        {
            float start = WorldX(left);
            float end = WorldX(right);
            CreateBlock(label, material, new Vector3((start + end) * .5f, .10f, z),
                new Vector3(Mathf.Max(.03f, end - start), .18f, .75f));
        }

        private void RecycleMissing(Dictionary<long, GameObject> active, Stack<GameObject> pool, HashSet<long> visible)
        {
            cleanup.Clear();
            foreach (var pair in active)
                if (!visible.Contains(pair.Key)) cleanup.Add(pair.Key);
            for (int i = 0; i < cleanup.Count; i++)
            {
                long id = cleanup[i];
                GameObject instance = active[id];
                instance.SetActive(false);
                pool.Push(instance);
                active.Remove(id);
            }
        }

        private GameObject Rent(Stack<GameObject> pool, PrimitiveType shape, Material material, string label)
        {
            GameObject go = pool.Count > 0 ? pool.Pop() : GameObject.CreatePrimitive(shape);
            go.name = label;
            go.transform.SetParent(root);
            go.transform.localScale = shape == PrimitiveType.Capsule ? new Vector3(.32f,.30f,.32f) :
                new Vector3(.65f,.65f,.65f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            go.SetActive(true);
            return go;
        }

        private GameObject CreateBlock(string label, Material material, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(root);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Collider>().enabled = false;
            return go;
        }

        private Material MakeMaterial(Color color)
        {
            // Never choose URP-only shaders in Built-in RP: that yields magenta.
            bool urpActive = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(urpActive ? "Universal Render Pipeline/Unlit" : "Unlit/Color");
            if (shader == null) shader = Shader.Find(urpActive ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException(
                "No compatible shader found. Run SWARMFORGE/Configure VS-01 project.");
            Material m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            ownedMaterials.Add(m);
            return m;
        }
    }
}

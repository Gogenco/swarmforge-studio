#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Swarmforge.Art.Editor
{
    /// <summary>
    /// Converts the checked-in ART Stage 2 OBJ polygon sources into native
    /// Unity Mesh .asset, URP color Material .mat, and prefab .prefab files.
    /// EDITOR ONLY. It never edits battle scenes, sim data, tests or gameplay.
    /// Run manually: SWARMFORGE > Stage 2 > Generate Native Art Assets.
    /// </summary>
    public static class Stage2ArtBuilder
    {
        private const string Root = "Assets/Swarmforge/Art/Stage2";
        private const string Input = Root + "/Models/";
        private const string Output = Root + "/Generated/";
        private static readonly string[] Models =
        {
            "S2_FriendlyMiniBot", "S2_RedEnemyRobot", "S2_AutoCannon",
            "S2_X2Chest", "S2_PlayerBase", "S2_EnemyFortress", "S2_Arena"
        };

        private static readonly Dictionary<string, string> Colors =
            new Dictionary<string, string>
        {
            { "Mint", "#2EE994" }, { "MintLight", "#8AFFC7" },
            { "MintDark", "#095745" }, { "MintEdge", "#0C9E7D" },
            { "Red", "#EB2436" }, { "RedLight", "#FF6E5E" },
            { "RedDark", "#640E1F" }, { "RedEdge", "#B51224" },
            { "Visor", "#1FDEFF" }, { "VisorRed", "#FFBA52" },
            { "Metal", "#7D96A8" }, { "MetalLight", "#CCDCE0" },
            { "MetalDark", "#293B52" }, { "Gold", "#FFB029" },
            { "GoldLight", "#FFE66E" }, { "GoldDark", "#7A4A0E" },
            { "Cyan", "#12A6D1" }, { "CyanLight", "#7AE0FF" },
            { "BlueDark", "#0E294F" }, { "BlueMid", "#1C476B" },
            { "Floor", "#111B2B" }, { "Path", "#21354A" },
            { "PathLight", "#33526A" }, { "Edge", "#0F708C" },
            { "Hazard", "#E65E2E" }, { "Stone", "#384752" },
            { "Glass", "#1C5C80" }
        };

        private sealed class MeshChunk
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = Vertices.Count;
                Vertices.Add(a);
                Vertices.Add(b);
                Vertices.Add(c);
                Triangles.Add(start);
                Triangles.Add(start + 1);
                Triangles.Add(start + 2);
            }
        }

        [MenuItem("SWARMFORGE/Stage 2/Generate Native Art Assets")]
        public static void Generate()
        {
            CreateFolder(Output + "Meshes");
            CreateFolder(Output + "Materials");
            CreateFolder(Output + "Prefabs");
            AssetDatabase.Refresh();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("No usable Unity shader for ART Stage 2.");

            var materials = new Dictionary<string, Material>();
            foreach (var pair in Colors)
            {
                string assetPath = Output + "Materials/" + pair.Key + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (material == null)
                {
                    material = new Material(shader) { name = pair.Key };
                    AssetDatabase.CreateAsset(material, assetPath);
                }
                else material.shader = shader;

                Color color;
                if (!ColorUtility.TryParseHtmlString(pair.Value, out color))
                    throw new InvalidOperationException("Invalid palette color " + pair.Key);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
                EditorUtility.SetDirty(material);
                materials.Add(pair.Key, material);
            }

            foreach (string name in Models)
            {
                Dictionary<string, MeshChunk> chunks = ReadObj(Input + name + ".obj");
                GameObject prefabRoot = new GameObject(name);
                try
                {
                    foreach (var pair in chunks)
                    {
                        if (!materials.ContainsKey(pair.Key))
                            throw new InvalidOperationException(name + ": undefined material " + pair.Key);
                        Mesh mesh = LoadOrCreateMesh(name, pair.Key, pair.Value);
                        var child = new GameObject(pair.Key);
                        child.transform.SetParent(prefabRoot.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = mesh;
                        child.AddComponent<MeshRenderer>().sharedMaterial = materials[pair.Key];
                    }
                    // No colliders/Rigidbodies/simulation scripts by design.
                    string prefabPath = Output + "Prefabs/" + name + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SWARMFORGE Stage 2: native 7 model meshes, materials and prefabs generated. " +
                      "These are art resources ONLY; ask Game Developer to integrate into V12TrialView.");
        }

        private static Mesh LoadOrCreateMesh(string model, string material, MeshChunk chunk)
        {
            string path = Output + "Meshes/" + model + "_" + material + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool newAsset = mesh == null;
            if (newAsset) mesh = new Mesh();
            mesh.name = model + "_" + material;
            mesh.Clear();
            if (chunk.Vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(chunk.Vertices);
            mesh.SetTriangles(chunk.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (newAsset) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Dictionary<string, MeshChunk> ReadObj(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("ART Stage 2 model missing", absolutePath);

            var vertices = new List<Vector3>();
            var result = new Dictionary<string, MeshChunk>();
            string material = "MetalDark";
            foreach (string original in File.ReadAllLines(absolutePath))
            {
                string line = original.Trim();
                if (line.StartsWith("usemtl ", StringComparison.Ordinal))
                {
                    material = line.Substring(7).Trim();
                    continue;
                }
                if (line.Length == 0 || line[0] == '#') continue;
                string[] tokens = line.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                if (tokens[0] == "v" && tokens.Length >= 4)
                {
                    vertices.Add(new Vector3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture)));
                }
                else if (tokens[0] == "f" && tokens.Length >= 4)
                {
                    MeshChunk chunk;
                    if (!result.TryGetValue(material, out chunk))
                    {
                        chunk = new MeshChunk();
                        result.Add(material, chunk);
                    }
                    int first = VertexIndex(tokens[1], vertices.Count);
                    for (int i = 3; i < tokens.Length; i++)
                    {
                        int second = VertexIndex(tokens[i - 1], vertices.Count);
                        int third = VertexIndex(tokens[i], vertices.Count);
                        chunk.AddTriangle(vertices[first], vertices[second], vertices[third]);
                    }
                }
            }
            if (vertices.Count == 0 || result.Count == 0)
                throw new InvalidOperationException("No polygon data in " + relativePath);
            return result;
        }

        private static int VertexIndex(string token, int count)
        {
            int slash = token.IndexOf('/');
            if (slash >= 0) token = token.Substring(0, slash);
            int i = int.Parse(token, CultureInfo.InvariantCulture);
            int index = i > 0 ? i - 1 : count + i;
            if (index < 0 || index >= count)
                throw new InvalidOperationException("OBJ face index out of range: " + i);
            return index;
        }

        private static void CreateFolder(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(fullPath);
        }
    }
}
#endif

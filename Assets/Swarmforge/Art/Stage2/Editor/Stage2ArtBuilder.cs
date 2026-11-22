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
    /// Stage 2 P9 authored OBJ -> native Unity Mesh, Lit atlas Material and Prefab.
    /// Two shared renderers/materials per model (body and luminous trim), not
    /// one renderer/draw call per source OBJ color. No gameplay components.
    /// Invoked explicitly by the Owner's existing Generate and Integrate P8 Art menu.
    /// </summary>
    public static class Stage2ArtBuilder
    {
        private const string Root = "Assets/Swarmforge/Art/Stage2";
        private const string Input = Root + "/Models/";
        private const string Output = Root + "/Generated/";
        private const int AtlasSize = 8;

        private static readonly string[] Models =
        {
            "S2_FriendlyMiniBot", "S2_RedEnemyRobot", "S2_AutoCannon",
            "S2_X2Chest", "S2_PlayerBase", "S2_EnemyFortress", "S2_Arena"
        };

        // Alphabetical index assignment below is deterministic across editor reloads.
        private static readonly Dictionary<string, string> Colors = new Dictionary<string, string>
        {
            { "Mint", "#23D991" }, { "MintLight", "#B0FFE0" },
            { "MintDark", "#0D5749" }, { "MintEdge", "#0A9E7D" },
            { "Red", "#E5314C" }, { "RedLight", "#FF896A" },
            { "RedDark", "#5F1526" }, { "RedEdge", "#B91835" },
            { "Visor", "#66ECFF" }, { "VisorRed", "#FFCA78" },
            { "Metal", "#748EA5" }, { "MetalLight", "#CBDCE5" },
            { "MetalDark", "#253345" }, { "Gold", "#F6B43C" },
            { "GoldLight", "#FFE9A2" }, { "GoldDark", "#865016" },
            { "Cyan", "#24BCD9" }, { "CyanLight", "#8EEAFF" },
            { "BlueDark", "#102641" }, { "BlueMid", "#25506A" },
            { "Floor", "#101D30" }, { "Path", "#1B3249" },
            { "PathLight", "#2A465C" }, { "Edge", "#147A92" },
            { "Hazard", "#E87C39" }, { "Stone", "#33475D" },
            { "Glass", "#285675" }, { "Carbon", "#172331" },
            { "Steel", "#536779" }, { "GlowMint", "#79FFD0" },
            { "GlowRed", "#FF7069" }, { "GlowGold", "#FFE793" },
            { "GlowBlue", "#75D9FF" }, { "Shadow", "#0A1424" },
            { "White", "#DDEBF4" }, { "DarkRust", "#532738" }
        };

        private sealed class MeshChunk
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> UVs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
            {
                int start = Vertices.Count;
                Vertices.Add(a);
                Vertices.Add(b);
                Vertices.Add(c);
                UVs.Add(uv);
                UVs.Add(uv);
                UVs.Add(uv);
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

            var uvForColor = new Dictionary<string, Vector2>();
            var keys = new List<string>(Colors.Keys);
            keys.Sort(StringComparer.Ordinal);
            if (keys.Count > AtlasSize * AtlasSize)
                throw new InvalidOperationException("ART Stage 2 palette exceeds texture atlas.");
            Texture2D palette = GetPalette(keys, uvForColor);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("ART Stage 2 requires URP/Lit or Standard shader.");

            Material surface = GetMaterial("P9_AtlasSurface", shader, palette, false);
            Material glow = GetMaterial("P9_AtlasGlow", shader, palette, true);
            var materials = new Dictionary<string, Material>
            {
                { "Surface", surface }, { "Glow", glow }
            };

            foreach (string name in Models)
            {
                Dictionary<string, MeshChunk> chunks = ReadObj(Input + name + ".obj", uvForColor);
                var prefabRoot = new GameObject(name);
                try
                {
                    foreach (var pair in chunks)
                    {
                        Mesh mesh = LoadOrCreateMesh(name, pair.Key, pair.Value);
                        var child = new GameObject(pair.Key);
                        child.transform.SetParent(prefabRoot.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = mesh;
                        child.AddComponent<MeshRenderer>().sharedMaterial = materials[pair.Key];
                    }
                    PrefabUtility.SaveAsPrefabAsset(
                        prefabRoot, Output + "Prefabs/" + name + ".prefab");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SWARMFORGE Stage 2 P9 Art: seven detailed prefabs generated, " +
                      "shared lit texture palette and two material groups. " +
                      "Use Game View in Unity for actual visual acceptance.");
        }

        private static Texture2D GetPalette(
            List<string> keys, Dictionary<string, Vector2> uvForColor)
        {
            string path = Output + "Materials/P9_Palette.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool created = texture == null;
            if (created)
                texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
            if (texture.width != AtlasSize || texture.height != AtlasSize)
                throw new InvalidOperationException("Unexpected P9 palette atlas size.");
            texture.name = "P9_Palette";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.anisoLevel = 0;
            for (int y = 0; y < AtlasSize; y++)
                for (int x = 0; x < AtlasSize; x++)
                    texture.SetPixel(x, y, Color.white);

            for (int i = 0; i < keys.Count; i++)
            {
                Color c;
                if (!ColorUtility.TryParseHtmlString(Colors[keys[i]], out c))
                    throw new InvalidOperationException("Invalid palette: " + keys[i]);
                int px = i % AtlasSize, py = i / AtlasSize;
                texture.SetPixel(px, py, c);
                uvForColor.Add(keys[i],
                    new Vector2((px + .5f) / AtlasSize, (py + .5f) / AtlasSize));
            }
            texture.Apply(false, false);
            if (created) AssetDatabase.CreateAsset(texture, path);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Material GetMaterial(
            string name, Shader shader, Texture2D atlas, bool emissive)
        {
            string path = Output + "Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", atlas);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", atlas);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", emissive ? .03f : .28f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", emissive ? .65f : .42f);
            if (emissive)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", Color.white * .72f);
                    mat.EnableKeyword("_EMISSION");
                }
                if (mat.HasProperty("_EmissionMap"))
                    mat.SetTexture("_EmissionMap", atlas);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", Color.black);
                if (mat.HasProperty("_EmissionMap"))
                    mat.SetTexture("_EmissionMap", null);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh LoadOrCreateMesh(string model, string layer, MeshChunk chunk)
        {
            string path = Output + "Meshes/" + model + "_" + layer + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            mesh.name = model + "_" + layer;
            mesh.Clear();
            if (chunk.Vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            else
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(chunk.Vertices);
            mesh.SetUVs(0, chunk.UVs);
            mesh.SetTriangles(chunk.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Dictionary<string, MeshChunk> ReadObj(
            string relativePath, Dictionary<string, Vector2> uvForColor)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(root,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                throw new FileNotFoundException("P9 art OBJ source missing", path);

            var sourceVertices = new List<Vector3>();
            var result = new Dictionary<string, MeshChunk>();
            string colorName = "Carbon";
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("usemtl ", StringComparison.Ordinal))
                {
                    colorName = line.Substring(7).Trim();
                    if (!uvForColor.ContainsKey(colorName))
                        throw new InvalidOperationException("Unknown Stage 2 color: " + colorName);
                    continue;
                }
                if (line.Length == 0 || line[0] == '#') continue;
                string[] tokens = line.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                if (tokens[0] == "v" && tokens.Length >= 4)
                {
                    sourceVertices.Add(new Vector3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture)));
                }
                else if (tokens[0] == "f" && tokens.Length >= 4)
                {
                    bool glows = colorName.StartsWith("Glow", StringComparison.Ordinal) ||
                        colorName == "Visor" || colorName == "VisorRed";
                    string key = glows ? "Glow" : "Surface";
                    MeshChunk chunk;
                    if (!result.TryGetValue(key, out chunk))
                    {
                        chunk = new MeshChunk();
                        result.Add(key, chunk);
                    }
                    int first = VertexIndex(tokens[1], sourceVertices.Count);
                    for (int i = 3; i < tokens.Length; i++)
                    {
                        int second = VertexIndex(tokens[i - 1], sourceVertices.Count);
                        int third = VertexIndex(tokens[i], sourceVertices.Count);
                        chunk.AddTriangle(
                            sourceVertices[first],
                            sourceVertices[second],
                            sourceVertices[third],
                            uvForColor[colorName]);
                    }
                }
            }
            if (sourceVertices.Count == 0 || result.Count == 0)
                throw new InvalidOperationException("No polygons in Stage 2 OBJ: " + relativePath);
            return result;
        }

        private static int VertexIndex(string value, int count)
        {
            int slash = value.IndexOf('/');
            if (slash >= 0) value = value.Substring(0, slash);
            int id = int.Parse(value, CultureInfo.InvariantCulture);
            int index = id > 0 ? id - 1 : count + id;
            if (index < 0 || index >= count)
                throw new InvalidOperationException("OBJ face index invalid: " + id);
            return index;
        }

        private static void CreateFolder(string path)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(Path.Combine(root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
#endif

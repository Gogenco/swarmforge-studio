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
    /// Stage 2 P12 real 3D source OBJ + PBR PNG maps -> native Unity Mesh, Lit Material and Prefab.
    /// Two shared renderers/materials per model (body and luminous trim), not
    /// one renderer/draw call per source OBJ color. No gameplay components.
    /// Invoked explicitly by the Owner's existing Generate and Integrate P8 Art menu.
    /// </summary>
    public static class Stage2ArtBuilder
    {
        private const string Root = "Assets/Swarmforge/Art/Stage2";
        private const string Input = Root + "/Models/";
        private const string Output = Root + "/Generated/";
        private const int AtlasSize = 16; // room for city/landscape material swatches

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
            { "White", "#DDEBF4" }, { "DarkRust", "#532738" },
            { "RoadAsphalt", "#303943" },
            { "RoadPatches", "#3B4249" },
            { "RoadMarking", "#DBDEE0" },
            { "RoadYellow", "#EFC96A" },
            { "CurbStone", "#9EA9B0" },
            { "Pavement", "#AAB4B5" },
            { "PavementDark", "#7C8C99" },
            { "Grass", "#598B44" },
            { "GrassDeep", "#3E6B35" },
            { "GrassFresh", "#7AA64F" },
            { "Bark", "#624735" },
            { "LeafGreen", "#2E813F" },
            { "LeafLight", "#67A949" },
            { "LeafLime", "#A4C65A" },
            { "LeafOrange", "#C66B35" },
            { "LeafRed", "#AC4350" },
            { "LeafPink", "#ED8EAA" },
            { "LeafBlossom", "#F4BDD1" },
            { "FlowerWhite", "#FFF0D2" },
            { "FlowerYellow", "#FFD366" },
            { "FlowerPurple", "#AB8BD0" },
            { "FlowerPink", "#F1A1BD" },
            { "CityConcrete", "#A6B4BE" },
            { "CityConcreteDark", "#708493" },
            { "CityFrame", "#445565" },
            { "CityRoof", "#53616D" },
            { "CityGlass", "#3C6F85" },
            { "CityGlassDark", "#285063" },
            { "CityGlassLight", "#6B9DB1" },
            { "WindowWarm", "#E4BD7B" },
            { "WindowCool", "#90BDD2" },
            { "CafeWood", "#957358" },
            { "CoffeeAwning", "#D6C4A4" },
            { "ShadowGreen", "#355D3C" },
            { "StreetLamp", "#FFDB97" },
            { "TealBanner", "#247D86" },
            { "RedBanner", "#8E3749" }
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

            // For real tiled city surfaces, use separate UVs for each vertex.
            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c,
                Vector2 uvA, Vector2 uvB, Vector2 uvC)
            {
                int start = Vertices.Count;
                Vertices.Add(a);
                Vertices.Add(b);
                Vertices.Add(c);
                UVs.Add(uvA);
                UVs.Add(uvB);
                UVs.Add(uvC);
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
            // P12: physically based texture maps are real repository PNG assets.
            // Normal maps, metallic/smoothness masks and albedos import in Unity.
            Material asphalt = GetP12CityMaterial("Asphalt", shader);
            Material grass = GetP12CityMaterial("Grass", shader);
            Material pavement = GetP12CityMaterial("Pavement", shader);
            Material glass = GetP12CityMaterial("CityGlass", shader);
            var materials = new Dictionary<string, Material>
            {
                { "Surface", surface }, { "Glow", glow },
                { "Asphalt", asphalt }, { "Grass", grass },
                { "Pavement", pavement }, { "CityGlass", glass }
            };

            foreach (string name in Models)
            {
                Dictionary<string, MeshChunk> chunks = ReadObj(Input + name + ".obj", uvForColor);
                var prefabRoot = new GameObject(name);
                try
                {
                    // Runtime-facing prefab slot remains identical. Bots now carry
                    // three real geometry LOD meshes (P12, LOD1 and LOD2).
                    Renderer[] level0 = BuildVisualLayers(prefabRoot, name, "LOD0", chunks, materials);
                    if (name == "S2_FriendlyMiniBot" || name == "S2_RedEnemyRobot")
                    {
                        var one = ReadObj(Input + name + "_LOD1.obj", uvForColor);
                        var two = ReadObj(Input + name + "_LOD2.obj", uvForColor);
                        Renderer[] level1 = BuildVisualLayers(prefabRoot, name, "LOD1", one, materials);
                        Renderer[] level2 = BuildVisualLayers(prefabRoot, name, "LOD2", two, materials);
                        var group = prefabRoot.AddComponent<LODGroup>();
                        group.SetLODs(new[]
                        {
                            new LOD(.035f, level0),
                            new LOD(.012f, level1),
                            new LOD(.004f, level2)
                        });
                        group.RecalculateBounds();
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
            Debug.Log("SWARMFORGE Stage 2 P12 Art: seven rebuilt 3D prefabs generated, " +
                      "shared metal palette plus true PBR albedo/normal/mask PNG maps. " +
                      "Use Game View in Unity for actual visual acceptance.");
        }

        private static Renderer[] BuildVisualLayers(
            GameObject root, string model, string layerName,
            Dictionary<string, MeshChunk> chunks, Dictionary<string, Material> materials)
        {
            var renderers = new List<Renderer>();
            foreach (var pair in chunks)
            {
                Mesh mesh = LoadOrCreateMesh(model + "_" + layerName, pair.Key, pair.Value);
                var child = new GameObject(layerName + "_" + pair.Key);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[pair.Key];
                renderers.Add(renderer);
            }
            return renderers.ToArray();
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

        /// <summary>
        /// A deterministic editor-authored tiled texture (no external downloads).
        /// 0 asphalt, 1 grass, 2 paving stone, 3 dark tinted window glass.
        /// Written into Unity project as a real Texture2D .asset.
        /// </summary>
        private static Texture2D GetCityTexture(string name, int size, int kind)
        {
            string path = Output + "Materials/" + name + "_Texture.asset";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool create = tex == null;
            if (create) tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            // The Owner may re-run the menu in an existing P9 Unity folder.
            // Upgrade previously generated 128px textures without requiring a fresh project.
            if (tex.width != size || tex.height != size)
                tex.Reinitialize(size, size, TextureFormat.RGBA32, true);
            tex.name = name + "_Texture";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 2;
            var pixels = new Color32[size * size];
            // Three scales of reproducible texture variation; authored once in
            // Unity Editor, NO runtime procedural work, NO new gameplay RNG.
            // Rough asphalt: aggregate pores, tire staining and faint wet patches.
            // Grass: multi-tonal clumps; stone: grout joints; facade: reflected sky.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    uint fine = DeterministicHash(x, y, 17 + kind * 31);
                    uint medium = DeterministicHash(x / 4, y / 4, 119 + kind * 67);
                    uint large = DeterministicHash(x / 23, y / 23, 811 + kind * 17);
                    int small = (int)(fine & 31) - 16;
                    int middle = (int)(medium & 31) - 16;
                    int broad = (int)(large & 31) - 16;
                    int r, g, b;
                    if (kind == 0)
                    {
                        bool aggregate = ((fine >> 8) & 1023) < 21;
                        bool narrowJoint = (x * 7 + y * 11) % 251 == 0;
                        int road = small / 4 + middle / 4 + broad / 3 +
                            (aggregate ? 22 : 0) - (narrowJoint ? 12 : 0);
                        int darkPatch = ((large >> 9) & 7) <= 1 ? -8 : 0;
                        r = 49 + road + darkPatch;
                        g = 56 + road + darkPatch;
                        b = 62 + road + darkPatch;
                    }
                    else if (kind == 1)
                    {
                        // Remove uniform bright green; local lawn patches with blades.
                        int chlorophyll = small / 3 + middle + broad +
                            (((fine >> 11) & 3) == 0 ? 6 : 0);
                        int brown = ((large >> 11) & 31) == 1 ? 13 : 0;
                        r = 69 + chlorophyll + brown;
                        g = 115 + chlorophyll - brown / 2;
                        b = 50 + chlorophyll / 2;
                    }
                    else if (kind == 2)
                    {
                        // Small irregular limestone paving and restrained grout.
                        int ox = ((y / 32) & 1) == 0 ? 0 : 16;
                        bool seam = (x + ox) % 32 < 2 || y % 32 < 2;
                        int stone = small / 5 + middle / 4 + broad / 3 +
                            (seam ? -38 : 0);
                        r = 163 + stone; g = 173 + stone; b = 180 + stone;
                    }
                    else
                    {
                        // Reflective blue-grey glazed curtain wall: vertical
                        // sky glints, frame mullions and varied interior strips.
                        bool mullion = x % 32 < 2 || y % 48 < 2;
                        int glint = (x + y / 3) % 57 < 5 ? 21 : 0;
                        int pane = ((medium >> 11) & 7) == 1 ? 17 : 0;
                        int shade = small / 7 + middle / 4 + broad / 4;
                        r = (mullion ? 40 : 47) + shade + glint + pane;
                        g = (mullion ? 68 : 100) + shade + glint + pane;
                        b = (mullion ? 81 : 125) + shade + glint + pane;
                    }
                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(r, 0, 255),
                        (byte)Mathf.Clamp(g, 0, 255),
                        (byte)Mathf.Clamp(b, 0, 255), 255);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            if (create) AssetDatabase.CreateAsset(tex, path);
            else EditorUtility.SetDirty(tex);
            return tex;
        }

        private static uint DeterministicHash(int x, int y, int seed)
        {
            unchecked
            {
                uint a = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^
                    (uint)(seed * 83492791);
                a ^= a >> 16;
                a *= 0x7feb352d;
                a ^= a >> 15;
                a *= 0x846ca68b;
                return a ^ (a >> 16);
            }
        }

        /// <summary>
        /// Real P12 Albedo/Normal/Mask PNG sources; each map is 512x512 RGBA.
        /// URP Lit metallic workflow: Mask.R=metallic, Mask.G=occlusion (packed
        /// for future shader use), Mask.A=smoothness (inverse of roughness).
        /// NORMALMAP / METALLICSPECGLOSSMAP keywords are required for URP.
        /// Files remain source-controlled under Art/Stage2/Textures.
        /// </summary>
        private static Material GetP12CityMaterial(string type, Shader shader)
        {
            string source = Root + "/Textures/P12_" + type + "_";
            Texture2D albedo = RequireTexture(source + "Albedo.png", false, false);
            Texture2D normal = RequireTexture(source + "Normal.png", true, false);
            Texture2D mask = RequireTexture(source + "Mask.png", false, true);
            string assetPath = Output + "Materials/P12_" + type + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "P12_" + type };
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else mat.shader = shader;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normal);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", type == "Grass" ? .35f : .70f);
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", mask);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1f);
            // Multiply packed Mask.A smoothness by 1 (no extra suppression).
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
            if (mat.HasProperty("_SmoothnessTextureChannel"))
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D RequireTexture(string path, bool normal, bool mask)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
                throw new FileNotFoundException("Missing P12 PBR texture. Extract full PR #26 ZIP.", path);

            bool dirty = importer.maxTextureSize != 512 ||
                importer.mipmapEnabled != true ||
                importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) ||
                importer.sRGBTexture != (!normal && !mask) ||
                importer.wrapMode != TextureWrapMode.Repeat;
            if (dirty)
            {
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal && !mask;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                throw new FileNotFoundException("P12 PBR texture not imported.", path);
            return tex;
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
            float metalness = name.Contains("CityGlass") ? .35f :
                name.Contains("Asphalt") ? .04f :
                name.Contains("Grass") || name.Contains("Pavement") ? 0f :
                emissive ? .03f : .16f;
            float smoothness = name.Contains("CityGlass") ? .83f :
                name.Contains("Asphalt") ? .33f :
                name.Contains("Pavement") ? .23f : 
                name.Contains("Grass") ? .03f : emissive ? .65f : .39f;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
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
                    string key = colorName == "RoadAsphalt" ? "Asphalt" :
                        (colorName == "Grass" || colorName == "GrassDeep" ||
                        colorName == "GrassFresh") ? "Grass" :
                        (colorName == "Pavement" || colorName == "PavementDark") ?
                        "Pavement" : colorName == "CityGlass" ? "CityGlass" :
                        glows ? "Glow" : "Surface";
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
                        Vector3 a = sourceVertices[first];
                        Vector3 b = sourceVertices[second];
                        Vector3 c = sourceVertices[third];
                        if (key == "Asphalt" || key == "Grass" ||
                            key == "Pavement" || key == "CityGlass")
                        {
                            bool vertical = key == "CityGlass";
                            Vector3 normal = Vector3.Cross(b - a, c - a);
                            bool xFace = Mathf.Abs(normal.x) > Mathf.Abs(normal.z);
                            chunk.AddTriangle(a, b, c,
                                CityUV(a, vertical, xFace),
                                CityUV(b, vertical, xFace),
                                CityUV(c, vertical, xFace));
                        }
                        else chunk.AddTriangle(a, b, c, uvForColor[colorName]);
                    }
                }
            }
            if (sourceVertices.Count == 0 || result.Count == 0)
                throw new InvalidOperationException("No polygons in Stage 2 OBJ: " + relativePath);
            return result;
        }

        private static Vector2 CityUV(Vector3 v, bool vertical, bool xFace)
        {
            if (vertical)
                return xFace ? new Vector2(v.z * .65f, v.y * .36f) :
                    new Vector2(v.x * .65f, v.y * .36f);
            return new Vector2(v.x * .70f, v.z * .70f);
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

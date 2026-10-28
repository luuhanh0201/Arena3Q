using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arena3Q.Editor.Chapter01
{
    /// <summary>
    /// Chapter 1 kitbash pieces no free pack offers in a matching style (village gate, camp gate, soldier tent,
    /// war drum, cliffs, altar plateau, stone stairs; ArtSource/Chapter01/3D/Kitbash/ch01_kitbash.py) plus bamboo
    /// clumps combined from the CC0 Nobiax stalk. The FBX files carry world-scale UVs and material names; this
    /// builder gives every name one shared URP Lit material from the CC0 Quaternius and Codex textures, and saves
    /// prefabs with a simple box collider. Materials and prefabs reference git-ignored textures, so they live in
    /// the git-ignored Assets/ThirdParty/Chapter01/_Kit and are rebuilt from this menu.
    /// </summary>
    public static class Chapter01Kitbash
    {
        internal const string Models = "Assets/Arena3Q/Art/Chapter01/Kitbash";
        internal const string Kit = "Assets/ThirdParty/Chapter01/_Kit";
        internal const string Prefabs = Kit + "/Prefabs";
        private const string MegaKit = "ArtSource/Chapter01/3D/Sources/Quaternius/medieval_village_megakit_standard/Textures";
        private const string Codex = "ArtSource/Chapter01/2D/Assets/Export/1024";
        private const string Textures = Kit + "/Textures";

        private readonly struct Look
        {
            public readonly string texture; public readonly Color tint; public readonly float smoothness;
            public Look(string texture, Color tint, float smoothness = .12f) { this.texture = texture; this.tint = tint; this.smoothness = smoothness; }
        }

        // Material name in the FBX -> albedo (copied into _Kit/Textures) and tint. Roof tiles and wood are tinted
        // towards the CS Studio houses of the colour reference scene.
        private static readonly Dictionary<string, Look> Looks = new()
        {
            ["M_Wood"] = new("06_wood-floor.png", new Color(.95f, .88f, .8f)),
            ["M_WoodDark"] = new("06_wood-floor.png", new Color(.55f, .45f, .4f)),
            ["M_Lacquer"] = new("06_wood-floor.png", new Color(.82f, .3f, .22f)),
            ["M_Roof"] = new("T_RoundTiles_Grey.png", new Color(.66f, .5f, .43f)),
            ["M_RoofDark"] = new("T_RoundTiles_Grey.png", new Color(.48f, .5f, .55f)),
            ["M_Ridge"] = new("T_Plaster_BaseColor.png", new Color(.8f, .78f, .74f)),
            ["M_Cloth"] = new("17_soldier-tent-cloth.png", Color.white),
            ["M_Dark"] = new(null, new Color(.12f, .1f, .08f)),
            ["M_Rope"] = new(null, new Color(.62f, .52f, .38f)),
            ["M_Iron"] = new(null, new Color(.22f, .22f, .24f), .3f),
            ["M_Brass"] = new(null, new Color(.7f, .55f, .28f), .35f),
            ["M_Skin"] = new("T_Plaster_BaseColor.png", new Color(1f, .9f, .75f)),
            ["M_Stone"] = new("T_UnevenBrick_BaseColor.png", Color.white),
            ["M_StoneBlock"] = new("T_RockTrim_BaseColor.png", Color.white),
            ["M_Rock"] = new("Rocks_Grey.png", new Color(.9f, .84f, .75f)),
            ["M_GroundTop"] = new("01_ground-earth.png", Color.white),
        };

        private static readonly Dictionary<string, string> TextureSources = new()
        {
            ["06_wood-floor.png"] = Codex + "/06_wood-floor.png",
            ["17_soldier-tent-cloth.png"] = Codex + "/17_soldier-tent-cloth.png",
            ["01_ground-earth.png"] = Codex + "/01_ground-earth.png",
            ["T_RoundTiles_BaseColor.png"] = MegaKit + "/T_RoundTiles_BaseColor.png",
            ["T_Plaster_BaseColor.png"] = MegaKit + "/T_Plaster_BaseColor.png",
            ["T_UnevenBrick_BaseColor.png"] = MegaKit + "/T_UnevenBrick_BaseColor.png",
            ["T_RockTrim_BaseColor.png"] = MegaKit + "/T_RockTrim_BaseColor.png",
            ["Rocks_Diffuse.png"] = "Assets/ThirdParty/Quaternius/StylizedNatureStandard/Textures/Rocks_Diffuse.png",
        };

        [MenuItem("Arena3Q/Chapter 01/Build Kitbash Prefabs")]
        public static void Build()
        {
            foreach (var folder in new[] { Kit, Textures, Kit + "/Materials", Kit + "/Meshes", Prefabs }) Directory.CreateDirectory(folder);
            foreach (var (name, source) in TextureSources)
                if (!File.Exists(Textures + "/" + name)) File.Copy(source, Textures + "/" + name);
            // The CC0 roof tiles are terracotta and the rock is mossy green; greyscale copies take the tint of
            // the CS Studio roofs and the Chapter 1 earth instead.
            Grey("T_RoundTiles_BaseColor.png", "T_RoundTiles_Grey.png"); Grey("Rocks_Diffuse.png", "Rocks_Grey.png");
            AssetDatabase.Refresh();
            Chapter01ColorReference.PrepareTextures();

            var materials = new Dictionary<string, Material>();
            foreach (var (name, look) in Looks)
            {
                string path = Kit + "/Materials/" + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
                m.SetTexture("_BaseMap", look.texture != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + look.texture) : null);
                m.SetColor("_BaseColor", look.tint); m.SetFloat("_Smoothness", look.smoothness); m.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(m); materials[name] = m;
            }

            var report = new List<string>();
            foreach (var path in Directory.GetFiles(Models, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = (GameObject)Object.Instantiate(model); instance.name = model.name;
                var missing = new HashSet<string>();
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var shared = renderer.sharedMaterials;
                    for (int i = 0; i < shared.Length; i++)
                    {
                        string key = shared[i] != null ? shared[i].name : "";
                        if (materials.TryGetValue(key, out var m)) shared[i] = m; else missing.Add(key);
                    }
                    renderer.sharedMaterials = shared;
                }
                AddCollider(instance);
                Save(instance, model.name);
                report.Add(model.name + (missing.Count > 0 ? "  missing materials: " + string.Join(", ", missing) : ""));
                if (model.name == "CH01_WarDrum")
                {
                    // Trống trận lớn of the Đại Doanh (Ải 6): the same drum, larger.
                    var big = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/CH01_WarDrum.prefab"));
                    big.transform.localScale = Vector3.one * 1.6f; Save(big, "CH01_WarDrumLarge");
                }
            }
            for (int v = 0; v < 3; v++) report.Add(BambooClump(v));
            AssetDatabase.SaveAssets();
            File.WriteAllLines(Kit + "/build-report.txt", report);
            Debug.Log("[Chapter01] Kitbash prefabs: " + string.Join("; ", report));
        }

        /// <summary>Rừng trúc: 6 to 10 Nobiax stalks (CC0) of 5.5 to 8 m combined into one mesh per clump.</summary>
        private static string BambooClump(int variant)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Chapter01/Gap/Bamboo01.fbx");
            var filter = source.GetComponentInChildren<MeshFilter>();
            var probe = (GameObject)Object.Instantiate(source);
            float native = probe.GetComponentInChildren<Renderer>().bounds.size.y; Object.DestroyImmediate(probe);
            var rng = new System.Random(11 + variant); int count = 6 + 2 * variant;
            var parts = new CombineInstance[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2), radius = (float)(.25 + rng.NextDouble() * .9);
                float height = 5.5f + (float)rng.NextDouble() * 2.5f;
                var tilt = Quaternion.Euler((float)(rng.NextDouble() * 8 - 4), (float)(rng.NextDouble() * 360), (float)(rng.NextDouble() * 8 - 4));
                parts[i] = new CombineInstance { mesh = filter.sharedMesh, transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius), tilt, new Vector3(1.5f, 1f, 1.5f) * (height / native)) * filter.transform.localToWorldMatrix };
            }
            string meshPath = Kit + "/Meshes/CH01_BambooClump_" + variant + ".asset";
            var mesh = new Mesh { name = "CH01_BambooClump_" + variant, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, true, true);
            var min = mesh.bounds.min; var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i].y -= min.y;
            mesh.vertices = vertices; mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);

            string matPath = Kit + "/Materials/M_Bamboo.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, matPath); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/Chapter01/Gap/Bamboo01_diffuse.tga"));
            m.SetColor("_BaseColor", new Color(1.25f, 1.3f, 1.1f)); m.SetFloat("_Smoothness", .1f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

            var go = new GameObject("CH01_BambooClump_" + variant, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = m;
            var collider = go.AddComponent<CapsuleCollider>(); collider.radius = .9f; collider.height = 3f; collider.center = new Vector3(0f, 1.5f, 0f);
            string name = go.name; Save(go, name);
            return name + " " + mesh.triangles.Length / 3 + " tris";
        }

        private static void Grey(string source, string target)
        {
            if (File.Exists(Textures + "/" + target)) return;
            var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(Textures + "/" + source));
            var pixels = t.GetPixels();
            for (int i = 0; i < pixels.Length; i++) { float l = pixels[i].r * .3f + pixels[i].g * .59f + pixels[i].b * .11f; l = Mathf.Clamp01(.5f + (l - .5f) * 1.15f) * 1.25f; pixels[i] = new Color(l, l, l, 1f); }
            t.SetPixels(pixels); File.WriteAllBytes(Textures + "/" + target, t.EncodeToPNG()); Object.DestroyImmediate(t);
        }

        /// <summary>One box around what blocks movement (spec §8: simple colliders for buildings).</summary>
        private static void AddCollider(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) return;
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            var box = instance.AddComponent<BoxCollider>();
            box.center = instance.transform.InverseTransformPoint(b.center); box.size = b.size;
        }

        private static void Save(GameObject instance, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(instance);
        }
    }
}

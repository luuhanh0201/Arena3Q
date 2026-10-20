using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Imports the Mặc Ảnh flipbook VFX rendered by docs/art/concepts/MuuSi_main-hero/MacAnh/vfx_macanh_flipbooks.py
    /// (pages + recipe JSON per effect) into sprites, a <see cref="MacAnhVfxRecipe"/> and a pooled battle prefab, and
    /// binds them into MacAnhPerformance. Effects without flipbooks keep their current prefab; "Bind New VFX to
    /// Battle" (MacAnhBattleFxBuilder) puts the 3D effects back.
    /// </summary>
    public static class MacAnhFlipbookBuilder
    {
        public const string Root = "Assets/Arena3Q/Art/Heroes/MacAnh/VFX/Flipbook";
        private const string PerformancePath = "Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset";

        [Serializable] private sealed class FrameJson { public int page, x, y, width, height; public float pivot_x, pivot_y; }

        [Serializable]
        private sealed class LayerJson
        {
            public string name, plane, phase;
            public float ppu, framesPerSecond, start, end, fadeIn, fadeOut, scaleFrom, scaleTo, spin, height, glow, underlay;
            public bool loop, highQualityOnly, stretchToGround;
            public float[] underlayColour, tint;
            public int sortingOffset;
            public FrameJson[] frames;
        }

        [Serializable]
        private sealed class RecipeJson
        {
            public string name; public float referenceRadius, renderProjection, screenDim, dimFadeIn = .25f, dimFadeOut = .4f;
            public float[] dimColour; public string[] pages; public LayerJson[] layers;
        }

        [MenuItem("Arena3Q/Mac Anh/Graphics/Import Flipbook VFX")]
        public static void Import() => ImportAll(bind: false);

        [MenuItem("Arena3Q/Mac Anh/Graphics/Import and Bind Flipbook VFX to Battle")]
        public static void ImportAndBind() => ImportAll(bind: true);

        private static void ImportAll(bool bind)
        {
            AssetDatabase.Refresh();
            var material = EnsureMaterial();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var folder in Directory.GetDirectories(Root))
            {
                var name = Path.GetFileName(folder);
                var json = Path.Combine(folder, name + ".recipe.json").Replace('\\', '/');
                if (!File.Exists(json))
                    continue;
                var data = JsonUtility.FromJson<RecipeJson>(File.ReadAllText(json));
                var recipe = BuildRecipe(folder.Replace('\\', '/'), data);
                prefabs[name] = BuildPrefab(name, recipe, material);
                Debug.Log($"[MacAnh Flipbook] {name}: {recipe.layers.Length} layers, {recipe.layers.Sum(l => l.frames.Length)} frames.");
            }
            if (bind)
                Bind(prefabs, material);
            AssetDatabase.SaveAssets();
        }

        private static Material EnsureMaterial()
        {
            var path = Root + "/MacAnhFlipbook.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            var shader = Shader.Find("Arena3Q/MacAnh Flipbook 2D") ?? throw new InvalidOperationException("Flipbook shader missing.");
            material = new Material(shader) { name = "MacAnhFlipbook" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static MacAnhVfxRecipe BuildRecipe(string folder, RecipeJson data)
        {
            if (data.layers == null || data.layers.Length == 0)
                throw new InvalidOperationException($"{data.name}: recipe has no layers.");
            var ppu = data.layers[0].ppu;
            if (data.layers.Any(l => !Mathf.Approximately(l.ppu, ppu)))
                throw new InvalidOperationException($"{data.name}: all layers of one effect must share one pixels-per-unit.");
            var sprites = new Dictionary<string, Sprite>();
            for (var page = 0; page < data.pages.Length; page++)
            {
                var path = folder + "/" + data.pages[page];
                var importer = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new InvalidOperationException("Missing page " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = ppu;
                importer.mipmapEnabled = false;
                // RGB carries light where alpha is 0 (it is added on top), so it must not be bled or cleared.
                importer.alphaIsTransparency = false;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = 2048;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                var slices = new List<SpriteMetaData>();
                foreach (var layer in data.layers)
                    for (var i = 0; i < layer.frames.Length; i++)
                    {
                        var f = layer.frames[i];
                        if (f.page != page)
                            continue;
                        slices.Add(new SpriteMetaData
                        {
                            name = $"{data.name}_{layer.name}_{i:D3}", rect = new Rect(f.x, f.y, f.width, f.height),
                            alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(f.pivot_x, f.pivot_y),
                        });
                    }
#pragma warning disable CS0618
                importer.spritesheet = slices.ToArray();
#pragma warning restore CS0618
                importer.SaveAndReimport();
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    sprites[sprite.name] = sprite;
            }

            var assetPath = folder + "/" + data.name + ".asset";
            var recipe = AssetDatabase.LoadAssetAtPath<MacAnhVfxRecipe>(assetPath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<MacAnhVfxRecipe>();
                AssetDatabase.CreateAsset(recipe, assetPath);
            }
            recipe.referenceRadius = Mathf.Max(.01f, data.referenceRadius);
            recipe.renderProjection = data.renderProjection > 0f ? data.renderProjection : .75f;
            recipe.screenDim = Mathf.Clamp01(data.screenDim);
            recipe.dimFadeIn = data.dimFadeIn;
            recipe.dimFadeOut = data.dimFadeOut;
            recipe.dimColour = ToColour(data.dimColour, new Color(.06f, .02f, .12f, 1f));
            recipe.layers = data.layers.Select(l => new MacAnhVfxRecipe.Layer
            {
                name = l.name,
                frames = Enumerable.Range(0, l.frames.Length).Select(i => sprites[$"{data.name}_{l.name}_{i:D3}"]).ToArray(),
                framesPerSecond = Mathf.Max(1f, l.framesPerSecond),
                loop = l.loop,
                plane = (MacAnhVfxPlane)Enum.Parse(typeof(MacAnhVfxPlane), l.plane),
                phase = (MacAnhVfxPhase)Enum.Parse(typeof(MacAnhVfxPhase), l.phase),
                start = l.start, end = l.end, fadeIn = l.fadeIn, fadeOut = l.fadeOut,
                scaleFrom = l.scaleFrom, scaleTo = l.scaleTo, spinDegreesPerSecond = l.spin, height = l.height,
                stretchToGround = l.stretchToGround, glow = l.glow, underlay = l.underlay,
                underlayColour = ToColour(l.underlayColour, new Color(.17f, .05f, .36f, 1f)),
                tint = ToColour(l.tint, Color.white), sortingOffset = l.sortingOffset, highQualityOnly = l.highQualityOnly,
            }).ToArray();
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static Color ToColour(float[] c, Color fallback) =>
            c == null || c.Length < 3 ? fallback : new Color(c[0], c[1], c[2], c.Length > 3 ? c[3] : 1f);

        private static GameObject BuildPrefab(string name, MacAnhVfxRecipe recipe, Material material)
        {
            var root = new GameObject("FX_MacAnh_" + name + "Flipbook_Battle");
            var player = new GameObject("Player").AddComponent<MacAnhVfxPlayer>();
            player.transform.SetParent(root.transform, false);
            player.Configure(recipe, material);
            var kind = (MacAnhFxKind)Enum.Parse(typeof(MacAnhFxKind), name);
            root.AddComponent<MacAnhFx>().ConfigureSpatial(kind, player);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + root.name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Bind(Dictionary<string, GameObject> prefabs, Material material)
        {
            var profile = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(PerformancePath);
            var data = new SerializedObject(profile);
            var effects = data.FindProperty("effects");
            var bound = new HashSet<string>();
            for (var i = 0; i < effects.arraySize; i++)
            {
                var item = effects.GetArrayElementAtIndex(i);
                var key = item.FindPropertyRelative("key").stringValue;
                if (prefabs.TryGetValue(key, out var prefab))
                {
                    item.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    bound.Add(key);
                }
            }
            // Effects that only exist as flipbooks (UltimateAura, DodgeStreak) get a new entry.
            foreach (var pair in prefabs)
            {
                if (bound.Contains(pair.Key))
                    continue;
                effects.arraySize++;
                var item = effects.GetArrayElementAtIndex(effects.arraySize - 1);
                item.FindPropertyRelative("key").stringValue = pair.Key;
                item.FindPropertyRelative("prefab").objectReferenceValue = pair.Value;
            }
            data.FindProperty("afterimageGlowMaterial").objectReferenceValue = material;
            var skills = data.FindProperty("skills");
            for (var i = 0; i < skills.arraySize; i++)
            {
                var item = skills.GetArrayElementAtIndex(i);
                var action = (MacAnhAction)item.FindPropertyRelative("action").enumValueIndex;
                var key = action == MacAnhAction.Attack02 ? "TripleFeather" : action == MacAnhAction.Skill01 ? "MagicArray" :
                    action == MacAnhAction.Skill02 ? "Cyclone" : action == MacAnhAction.Skill03 ? "Shield" :
                    action == MacAnhAction.Ultimate ? "Ultimate" : action == MacAnhAction.DodgeAttack ? "CounterSlash" : "WindSlash";
                if (prefabs.TryGetValue(key, out var prefab))
                    item.FindPropertyRelative("vfx").objectReferenceValue = prefab;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[MacAnh Flipbook] Bound flipbook VFX: " + string.Join(", ", prefabs.Keys));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    public static class LuBuContentBuilder
    {
        private const string Art = "Assets/Arena3Q/Art/Heroes/LuBu";
        private const string Content = "Assets/Arena3Q/Content";
        [Serializable] private sealed class Manifest { public int directions; public ClipInfo[] clips; public CaptureIssue[] clipped; }
        [Serializable] private sealed class CaptureIssue { public string clip; public int direction, frame; }
        [Serializable] private sealed class ClipInfo
        {
            public string name;
            public int frames;
            public float duration;
            public bool loop;
            public float ortho_scale;
            public string[] pages;
            public FrameInfo[] sprites;
        }
        [Serializable] private sealed class FrameInfo
        {
            public string name;
            public int page, x, y, width, height;
            public float pivot_x, pivot_y;
        }

        [MenuItem("Arena3Q/Lu Bu/Import Sprites and Add Hero")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var captureRoot = File.Exists(Art + "/Smooth/animations.json") ? Art + "/Smooth" : Art;
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(captureRoot + "/animations.json"));
            if (manifest?.clips == null || manifest.clips.Length != 7 || manifest.clips.Select(c => c.name).Distinct().Count() != 7)
                throw new InvalidOperationException("Export all seven Lu Bu animations before importing content.");
            if (manifest.clipped != null && manifest.clipped.Length > 0)
                throw new InvalidOperationException("Fix cropped frames before importing Lu Bu animations.");
            var animation = LoadOrCreate<HeroAnimationSet>(Art + "/LuBuAnimations.asset");
            var data = new SerializedObject(animation);
            var directionCount = manifest.directions > 0 ? manifest.directions : 4;
            data.FindProperty("directionCount").intValue = directionCount;
            var clips = data.FindProperty("clips");
            clips.arraySize = manifest.clips.Length;
            Sprite portrait = null;
            for (var c = 0; c < manifest.clips.Length; c++)
            {
                var info = manifest.clips[c];
                var orthoScale = info.ortho_scale > 0f ? info.ortho_scale : 2.15f;
                var packed = info.pages != null && info.pages.Length > 0;
                var allFrames = packed ? info.sprites : Enumerable.Range(0, directionCount * info.frames).Select(i =>
                    new FrameInfo
                    {
                        name = $"{info.name}_{i / info.frames}_{i % info.frames:D2}", page = 0,
                        x = i % info.frames * 256, y = (directionCount - 1 - i / info.frames) * 256,
                        width = 256, height = 256, pivot_x = 0.5f,
                        pivot_y = 0.5f - 0.55f * 5f / Mathf.Sqrt(34f) / orthoScale
                    }).ToArray();
                if (allFrames == null || allFrames.Length != directionCount * info.frames)
                    throw new InvalidOperationException($"Incomplete frames for {info.name}.");
                var pages = packed ? info.pages : new[] { info.name + ".png" };
                var sprites = new Dictionary<string, Sprite>();
                for (var page = 0; page < pages.Length; page++)
                {
                    var path = captureRoot + "/" + pages[page];
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = 128f * 2.15f / orthoScale;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.maxTextureSize = packed ? 2048 : 8192;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.filterMode = FilterMode.Bilinear;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteGenerateFallbackPhysicsShape = false;
                    importer.SetTextureSettings(settings);
                    var slices = allFrames.Where(f => f.page == page).Select(f => new SpriteMetaData
                    {
                        name = f.name, rect = new Rect(f.x, f.y, f.width, f.height),
                        alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(f.pivot_x, f.pivot_y)
                    }).ToArray();
#pragma warning disable CS0618
                    importer.spritesheet = slices;
#pragma warning restore CS0618
                    importer.SaveAndReimport();
                    foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                        sprites.Add(sprite.name, sprite);
                }
                var entry = clips.GetArrayElementAtIndex(c);
                entry.FindPropertyRelative("state").enumValueIndex = (int)Enum.Parse<HeroAnimationState>(info.name);
                entry.FindPropertyRelative("framesPerDirection").intValue = info.frames;
                entry.FindPropertyRelative("duration").floatValue = info.duration;
                entry.FindPropertyRelative("loop").boolValue = info.loop;
                var frames = entry.FindPropertyRelative("frames");
                frames.arraySize = allFrames.Length;
                for (var i = 0; i < allFrames.Length; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[allFrames[i].name];
                if (info.name == "Idle") portrait = sprites[allFrames[0].name];
            }
            data.ApplyModifiedPropertiesWithoutUndo();

            var heroPath = Content + "/Heroes/Hero_LuBu.asset";
            var newHero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroPath) == null;
            if (newHero)
                AssetDatabase.CopyAsset(Content + "/Heroes/Hero_Swordsman.asset", heroPath);
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroPath);
            hero.name = "Hero_LuBu";
            var so = new SerializedObject(hero);
            so.FindProperty("heroId").stringValue = "lu_bu";
            so.FindProperty("displayName").stringValue = "Lữ Bố";
            so.FindProperty("kind").enumValueIndex = (int)HeroKind.Named;
            so.FindProperty("faction").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FactionDefinition>(Content + "/Factions/Faction_Qun.asset");
            so.FindProperty("sprite").objectReferenceValue = portrait;
            var portraitPath = Art + "/Portrait.png";
            var portraitImporter = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            portraitImporter.textureType = TextureImporterType.Sprite;
            portraitImporter.spriteImportMode = SpriteImportMode.Single;
            portraitImporter.mipmapEnabled = false;
            portraitImporter.alphaIsTransparency = true;
            portraitImporter.SaveAndReimport();
            so.FindProperty("portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            so.FindProperty("animations").objectReferenceValue = animation;
            so.FindProperty("placeholderColor").colorValue = Color.white;
            // Provisional melee general numbers; all values remain editable in the asset.
            if (newHero)
            {
                var stats = so.FindProperty("stats");
                stats.FindPropertyRelative("maxHealth").floatValue = 850f;
                stats.FindPropertyRelative("damage").floatValue = 48f;
                stats.FindPropertyRelative("attackRange").floatValue = 0.9f;
                stats.FindPropertyRelative("attackInterval").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Include him in the existing five-card battle preview.
            var loadout = new SerializedObject(AssetDatabase.LoadAssetAtPath<TestLoadout>(Content + "/TestLoadout.asset"));
            loadout.FindProperty("heroes").GetArrayElementAtIndex(0).FindPropertyRelative("hero").objectReferenceValue = hero;
            loadout.ApplyModifiedPropertiesWithoutUndo();
            var frontend = new SerializedObject(AssetDatabase.LoadAssetAtPath<FrontendConfig>(Content + "/Frontend/FrontendConfig.asset"));
            AppendReference(frontend.FindProperty("heroCatalog"), hero);
            var gifts = frontend.FindProperty("starterGifts");
            if (!Enumerable.Range(0, gifts.arraySize).Any(i => gifts.GetArrayElementAtIndex(i).FindPropertyRelative("hero").objectReferenceValue == hero))
            {
                gifts.arraySize++;
                var gift = gifts.GetArrayElementAtIndex(gifts.arraySize - 1);
                gift.FindPropertyRelative("hero").objectReferenceValue = hero;
                gift.FindPropertyRelative("level").intValue = 1;
                gift.FindPropertyRelative("stars").intValue = 3;
            }
            frontend.ApplyModifiedPropertiesWithoutUndo();
            var errors = new List<string>();
            hero.Validate(errors);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            AssetDatabase.SaveAssets();
            Debug.Log($"[LuBu] Imported {manifest.clips.Length} clips, {directionCount} directions each. Hero ready in Battle and new-player gifts.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void AppendReference(SerializedProperty array, UnityEngine.Object value)
        {
            for (var i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        }
    }
}

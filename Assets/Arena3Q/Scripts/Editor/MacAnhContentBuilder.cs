using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arena3Q.Editor
{
    public static class MacAnhContentBuilder
    {
        public const string Art = "Assets/Arena3Q/Art/Heroes/MacAnh";
        private const string Content = "Assets/Arena3Q/Content";
        [Serializable] private sealed class Capture
        {
            public string name; public int frames, directions; public float duration, pixelsPerUnit;
            public bool loop; public string[] pages, clipped; public Frame[] sprites; public Mark[] events;
        }
        [Serializable] private sealed class Mark { public string @event; public int frame; }
        [Serializable] private sealed class Frame
        {
            public string name; public int page, x, y, width, height; public float pivot_x, pivot_y;
            public MacAnhPerformance.SocketFrame sockets;
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var item = AssetDatabase.LoadAssetAtPath<T>(path); if (item != null) return item;
            Directory.CreateDirectory(Path.GetDirectoryName(path)); item = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(item, path); return item;
        }
        [MenuItem("Arena3Q/Mac Anh/Build VFX")]
        public static void BuildEffects()
        {
            var root = Art + "/VFX"; Directory.CreateDirectory(root + "/Meshes"); AssetDatabase.Refresh();
            var shader = Shader.Find("Arena3Q/MacAnh VFX 2D"); if (shader == null) throw new InvalidOperationException("Missing MacAnh URP 2D shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(root + "/MacAnhVfx.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, root + "/MacAnhVfx.mat"); }
            foreach (MacAnhFxKind kind in Enum.GetValues(typeof(MacAnhFxKind)))
            {
                var meshes = new Mesh[3][];
                for (var quality = 0; quality < 3; quality++)
                {
                    meshes[quality] = new Mesh[3];
                    for (var layer = 0; layer < 3; layer++)
                    {
                        var mesh = MacAnhFxGeometry.Create(kind, (MacAnhFxQuality)quality, layer);
                        var path = root + "/Meshes/" + mesh.name + ".asset";
                        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
                        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); }
                        meshes[quality][layer] = existing;
                    }
                }
                var go = new GameObject("FX_MacAnh_" + kind);
                go.AddComponent<MacAnhFx>().Configure(kind, material, meshes[2], meshes[1], meshes[0]);
                PrefabUtility.SaveAsPrefabAsset(go, root + "/" + go.name + ".prefab"); UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets(); Debug.Log("[MacAnh] Built " + Enum.GetValues(typeof(MacAnhFxKind)).Length + " pooled effect prefabs, each with 3 quality levels.");
        }
        static Material PaintedMaterial()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/MacAnhSprite.mat");
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if(shader==null)throw new InvalidOperationException("Missing URP 2D unlit sprite shader.");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Art+"/MacAnhSprite.mat");}
            else if(material.shader!=shader){material.shader=shader;EditorUtility.SetDirty(material);}
            return material;
        }
        public static void UpdatePresentationMaterial()
        {
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Art+"/MacAnhPerformance.asset");
            if(profile==null)throw new InvalidOperationException("Import Mặc Ảnh first.");
            var data=new SerializedObject(profile);data.FindProperty("spriteMaterial").objectReferenceValue=PaintedMaterial();
            data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
        }
        [MenuItem("Arena3Q/Mac Anh/Import Rendered Animation")]
        public static void Import()
        {
            ImportAction(null);
        }
        /// <summary>Refresh one rendered action without rebinding skills, effects or other clips.</summary>
        public static void ImportAction(string action)
        {
            AssetDatabase.Refresh();
            var captures = Directory.GetFiles(Art + "/Sprites", "*.json").Select(p => JsonUtility.FromJson<Capture>(File.ReadAllText(p)))
                .OrderBy(c => (int)Enum.Parse<MacAnhAction>(c.name)).ToArray();
            if (action != null) captures = captures.Where(c => c.name == action).ToArray();
            if (action != null && captures.Length != 1) throw new InvalidOperationException("Missing capture " + action);
            if (action == null && (!captures.Any(c => c.name == "Idle") || !captures.Any(c => c.name == "Attack01")))
                throw new InvalidOperationException("Complete Idle and Attack01 captures first.");
            var profile = Asset<MacAnhPerformance>(Art + "/MacAnhPerformance.asset"); var so = new SerializedObject(profile);
            so.FindProperty("spriteMaterial").objectReferenceValue=PaintedMaterial();
            var clips = so.FindProperty("clips"); if (action == null) clips.arraySize = captures.Length; Sprite portrait = null;
            for (var c = 0; c < captures.Length; c++)
            {
                var info = captures[c];
                if (info.directions != 8 || info.sprites.Length != info.frames * 8 || info.clipped.Length != 0)
                    throw new InvalidOperationException("Incomplete/cropped capture " + info.name);
                var loaded = new Dictionary<string, Sprite>();
                for (var page = 0; page < info.pages.Length; page++)
                {
                    var path = Art + "/Sprites/" + info.pages[page]; var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.spritePixelsPerUnit = info.pixelsPerUnit; importer.alphaIsTransparency = true;
                    importer.sRGBTexture=true;
                    importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Compressed;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
                    importer.spritesheet = info.sprites.Where(f => f.page == page).Select(f => new SpriteMetaData { name = f.name,
                        rect = new Rect(f.x, f.y, f.width, f.height), alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(f.pivot_x, f.pivot_y) }).ToArray();
#pragma warning restore CS0618
                    importer.SaveAndReimport();
                    foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) loaded.Add(sprite.name, sprite);
                }
                int index = c;
                if (action != null)
                {
                    index = -1;
                    for (int j = 0; j < clips.arraySize; j++)
                        if (clips.GetArrayElementAtIndex(j).FindPropertyRelative("action").enumValueIndex == (int)Enum.Parse<MacAnhAction>(info.name)) index = j;
                    if (index < 0) throw new InvalidOperationException("Action is not bound: " + action);
                }
                var clip = clips.GetArrayElementAtIndex(index);
                clip.FindPropertyRelative("action").enumValueIndex = (int)Enum.Parse<MacAnhAction>(info.name);
                clip.FindPropertyRelative("duration").floatValue = info.duration; clip.FindPropertyRelative("loop").boolValue = info.loop;
                clip.FindPropertyRelative("framesPerDirection").intValue = info.frames;
                var frames = clip.FindPropertyRelative("frames"); var sockets = clip.FindPropertyRelative("sockets");
                frames.arraySize = sockets.arraySize = info.sprites.Length;
                for (var i = 0; i < info.sprites.Length; i++)
                {
                    var f = info.sprites[i]; frames.GetArrayElementAtIndex(i).objectReferenceValue = loaded[f.name];
                    var s = sockets.GetArrayElementAtIndex(i); s.FindPropertyRelative("fanTip").vector2Value = f.sockets.fanTip;
                    s.FindPropertyRelative("leftHand").vector2Value = f.sockets.leftHand; s.FindPropertyRelative("cast").vector2Value = f.sockets.cast;
                }
                var marks = clip.FindPropertyRelative("events"); marks.arraySize = info.events.Length;
                for (var i = 0; i < info.events.Length; i++)
                {
                    var mark = marks.GetArrayElementAtIndex(i); var key = info.events[i].@event;
                    mark.FindPropertyRelative("key").stringValue = key.StartsWith("SpawnTripleFeather") ? "SpawnTripleFeather" : key;
                    mark.FindPropertyRelative("frame").intValue = info.events[i].frame;
                    mark.FindPropertyRelative("comboIndex").intValue = key.StartsWith("SpawnTripleFeather") ? i : 0;
                }
                if (info.name == "Idle") portrait = loaded[info.sprites[6 * info.frames].name];
            }
            if (action != null)
            {
                so.ApplyModifiedPropertiesWithoutUndo(); MacAnhRealtimeBattleBuilder.BuildSpriteLibrary(profile);AssetDatabase.SaveAssets();
                Debug.Log("[MacAnh] Refreshed only " + action + "; existing skill and VFX bindings preserved."); return;
            }
            var effectNames = Enum.GetNames(typeof(MacAnhFxKind)); var effects = so.FindProperty("effects"); effects.arraySize = effectNames.Length;
            for (var i = 0; i < effectNames.Length; i++)
            {
                effects.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue = effectNames[i];
                effects.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/VFX/FX_MacAnh_" + effectNames[i] + ".prefab");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var previewPath = Content + "/Lords/Lord_MacAnhPreview.asset";
            if (AssetDatabase.LoadAssetAtPath<HeroDefinition>(previewPath) == null)
                AssetDatabase.CopyAsset(Content + "/Lords/Lord_Strategist.asset", previewPath);
            var preview = new SerializedObject(AssetDatabase.LoadAssetAtPath<HeroDefinition>(previewPath));
            preview.FindProperty("heroId").stringValue = "mac_anh_art_preview";
            preview.FindProperty("macAnhPerformance").objectReferenceValue = profile; preview.FindProperty("sprite").objectReferenceValue = portrait;
            preview.FindProperty("placeholderColor").colorValue = Color.white;
            for (var i = 1; i < 4; i++) preview.FindProperty("skills").GetArrayElementAtIndex(i).objectReferenceValue = null;
            preview.ApplyModifiedPropertiesWithoutUndo();
            if (captures.Length == 11) BindFinalContent(profile, portrait);
            MacAnhRealtimeBattleBuilder.BuildSpriteLibrary(profile);
            AssetDatabase.SaveAssets(); Debug.Log("[MacAnh] Imported " + captures.Length + " actions / 8 directions; exact frame sockets retained.");
        }
        private static SkillDefinition Skill(string source, string id, string title, SkillSlot slot, int eventFrame)
        {
            var directory = Content + "/Skills/MacAnh"; Directory.CreateDirectory(directory); AssetDatabase.Refresh();
            var path = directory + "/Skill_" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<SkillDefinition>(path) == null && !AssetDatabase.CopyAsset(Content + "/Skills/" + source + ".asset", path))
                throw new InvalidOperationException("Missing skill baseline " + source);
            var asset = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path); var so = new SerializedObject(asset);
            so.FindProperty("skillId").stringValue = id; so.FindProperty("displayName").stringValue = title;
            so.FindProperty("slot").enumValueIndex = (int)slot; so.FindProperty("castTime").floatValue = eventFrame / 30f;
            so.FindProperty("description").stringValue = title + ". Thông số chiến đấu có thể chỉnh trong asset.";
            so.FindProperty("iconColor").colorValue = new Color(.65f, .35f, .9f);
            so.ApplyModifiedPropertiesWithoutUndo(); return asset;
        }
        private static void BindFinalContent(MacAnhPerformance profile, Sprite portrait)
        {
            var array = Skill("Lord_Strategist/Skill_hoa_ke", "huyen_co_tran", "Huyền Cơ Trận", SkillSlot.Skill2, 33);
            var cyclone = Skill("Lord_Strategist/Skill_bat_quai_tran", "phong_vu_khon_tran", "Phong Vũ Khốn Trận", SkillSlot.Skill3, 30);
            var shield = Skill("Hero_Guardian/Skill_ho_ve_tran", "thien_muu_ho_tran", "Thiên Mưu Hộ Trận", SkillSlot.Skill3, 27);
            var triple = Skill("Lord_Archer/Skill_lien_chau_tien", "tam_vu_lien_kich", "Tam Vũ Liên Kích", SkillSlot.Skill2, 15);
            var ultimate = Skill("Lord_Swordsman/Skill_van_kiem_quy_tong", "van_tuong_thien_co", "Vạn Tượng Thiên Cơ", SkillSlot.Ultimate, 75);
            var counter = Skill("Hero_Swordsman/Skill_dao_khi", "anh_bo_phan_kich", "Ảnh Bộ Phản Kích", SkillSlot.Skill2, 24);
            var t = new SerializedObject(triple); t.FindProperty("repeatInterval").floatValue = .3f; t.ApplyModifiedPropertiesWithoutUndo();
            // Enemies caught in the cyclone sway in its wind (pictures only).
            var c = new SerializedObject(cyclone); c.FindProperty("zoneSway").floatValue = .12f; c.ApplyModifiedPropertiesWithoutUndo();
            var s = new SerializedObject(shield);
            foreach (var i in Enumerable.Range(0, s.FindProperty("effects").arraySize))
                s.FindProperty("effects").GetArrayElementAtIndex(i).FindPropertyRelative("affects").enumValueIndex = (int)SkillAffects.Self;
            s.ApplyModifiedPropertiesWithoutUndo();
            var u = new SerializedObject(ultimate); u.FindProperty("followCaster").boolValue = false;
            u.FindProperty("shape").enumValueIndex = (int)SkillShape.Circle;
            u.FindProperty("targeting").enumValueIndex = (int)SkillTargeting.Point; u.ApplyModifiedPropertiesWithoutUndo();
            var k = new SerializedObject(counter); k.FindProperty("shape").enumValueIndex = (int)SkillShape.Single;
            k.FindProperty("targeting").enumValueIndex = (int)SkillTargeting.CurrentTarget; k.ApplyModifiedPropertiesWithoutUndo();
            var bindings = new[] { triple, array, cyclone, shield, ultimate, counter };
            var actions = new[] { MacAnhAction.Attack02, MacAnhAction.Skill01, MacAnhAction.Skill02, MacAnhAction.Skill03, MacAnhAction.Ultimate, MacAnhAction.DodgeAttack };
            var fx = new[] { "TripleFeather", "MagicArray", "Cyclone", "Shield", "Ultimate", "CounterSlash" };
            var p = new SerializedObject(profile); var entries = p.FindProperty("skills"); entries.arraySize = bindings.Length;
            for (var i = 0; i < bindings.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i); entry.FindPropertyRelative("skill").objectReferenceValue = bindings[i];
                entry.FindPropertyRelative("action").enumValueIndex = (int)actions[i];
                entry.FindPropertyRelative("vfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/VFX/FX_MacAnh_" + fx[i] + ".prefab");
            }
            p.ApplyModifiedPropertiesWithoutUndo();
            foreach (var path in new[] { Content + "/Lords/Lord_Strategist.asset", Content + "/Lords/Lord_MacAnhPreview.asset" })
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path); var data = new SerializedObject(hero);
                data.FindProperty("sprite").objectReferenceValue = portrait; data.FindProperty("portrait").objectReferenceValue = portrait;
                data.FindProperty("macAnhPerformance").objectReferenceValue = profile; data.FindProperty("placeholderColor").colorValue = Color.white;
                var slots = data.FindProperty("skills"); slots.GetArrayElementAtIndex(1).objectReferenceValue = array;
                slots.GetArrayElementAtIndex(2).objectReferenceValue = cyclone; slots.GetArrayElementAtIndex(3).objectReferenceValue = ultimate;
                data.ApplyModifiedPropertiesWithoutUndo();
                var errors = new List<string>(); hero.Validate(errors); if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        [MenuItem("Arena3Q/Mac Anh/Build Preview Scene")]
        public static void BuildPreview()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the preview scene.");
            var profile = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Art + "/MacAnhPerformance.asset");
            if (profile == null) throw new InvalidOperationException("Import MacAnh animations first.");
            var path = "Assets/Arena3Q/Scenes/MacAnhPreview.unity";
            var existingScene = SceneManager.GetSceneByPath(path);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                var roots = existingScene.GetRootGameObjects();
                var showcase = roots.Select(go => go.GetComponent<MacAnhShowcase>()).FirstOrDefault(c => c != null);
                var existingCamera = roots.Select(go => go.GetComponent<Camera>()).FirstOrDefault(c => c != null);
                if (showcase != null && existingCamera != null)
                {
                    showcase.Configure(profile, AssetDatabase.LoadAssetAtPath<Material>(Art + "/MacAnhSprite.mat"));
                    existingCamera.transform.position = new Vector3(.35f, .6f, -10); existingCamera.orthographicSize = 1.8f;
                    EditorSceneManager.MarkSceneDirty(existingScene); EditorSceneManager.SaveScene(existingScene); return;
                }
                throw new InvalidOperationException("Preview scene is missing its camera/showcase component.");
            }
            var previous = SceneManager.GetActiveScene(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(.35f, .6f, -10);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.8f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .025f, .065f);
            var light = new GameObject("Main Light", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/MacAnhSprite.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Art + "/MacAnhSprite.mat"); }
            new GameObject("MacAnh Showcase", typeof(MacAnhShowcase)).GetComponent<MacAnhShowcase>().Configure(profile, material);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); AssetDatabase.SaveAssets();
            Debug.Log("[MacAnh] Preview scene ready: " + path);
        }
        [MenuItem("Arena3Q/Mac Anh/Open Preview Scene")]
        public static void OpenPreview() => EditorSceneManager.OpenScene("Assets/Arena3Q/Scenes/MacAnhPreview.unity");

        [InitializeOnLoadMethod]
        private static void RegisterCombatPreview()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }
        [MenuItem("Arena3Q/Mac Anh/Play Combat Test")]
        public static void PlayCombatTest()
        {
            if (EditorApplication.isPlaying) { StartCombatPreview(); return; }
            EditorSceneManager.OpenScene("Assets/Arena3Q/Scenes/Battle.unity");
            SessionState.SetBool("Arena3Q.MacAnhCombatPreview", true); EditorApplication.isPlaying = true;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Arena3Q.MacAnhCombatPreview", false)) return;
            SessionState.SetBool("Arena3Q.MacAnhCombatPreview", false);
            EditorApplication.delayCall += StartCombatPreview;
        }
        private static void StartCombatPreview()
        {
            var battle = UnityEngine.Object.FindAnyObjectByType<BattleController>();
            if (battle == null)
            {
                SceneManager.sceneLoaded -= OnCombatSceneLoaded; SceneManager.sceneLoaded += OnCombatSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Battle"); return;
            }
            var stage = battle.Stages[0]; var lineup = stage.TestLoadout.GetLineup().ToArray();
            lineup[0] = new LineupEntry(AssetDatabase.LoadAssetAtPath<HeroDefinition>(Content + "/Lords/Lord_Strategist.asset"), 15);
            battle.StartBattle(stage, lineup); battle.TrySummon(SummonRequest.Quick(0));
            if (battle.SelectedHero != null) battle.SelectedHero.Skills.Rage.Add(battle.SelectedHero.Skills.Rage.Max);
        }
        private static void OnCombatSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnCombatSceneLoaded;
            EditorApplication.delayCall += StartCombatPreview;
        }
    }
}

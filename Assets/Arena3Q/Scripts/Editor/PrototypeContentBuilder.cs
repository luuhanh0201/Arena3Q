using System;
using System.Collections.Generic;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Creates the prototype's placeholder content: troops, heroes, a test loadout, one Attack and one Defense stage,
    /// and the Battle scene. Existing assets are kept (so tuning survives); only missing ones are created.
    /// Batch mode: Unity.exe -batchmode -quit -projectPath . -executeMethod Arena3Q.Editor.PrototypeContentBuilder.BuildAll
    /// </summary>
    public static class PrototypeContentBuilder
    {
        public const string ContentRoot = "Assets/Arena3Q/Content";
        public const string ScenePath = "Assets/Arena3Q/Scenes/Battle.unity";
        private const string SceneTemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [MenuItem("Arena3Q/Build Prototype Content")]
        public static void BuildAll()
        {
            try
            {
                var stages = BuildAssets();
                BuildScene(stages);
                AddSceneToBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("[Arena3Q] Prototype content is ready.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("Arena3Q/Validate Stages")]
        public static void ValidateStages()
        {
            var errors = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:StageConfig"))
                AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid)).Validate(errors);
            foreach (var error in errors)
                Debug.LogError($"[Arena3Q] {error}");
            Debug.Log($"[Arena3Q] Stage validation finished with {errors.Count} error(s).");
            if (Application.isBatchMode && errors.Count > 0)
                EditorApplication.Exit(1);
        }

        // ---- Assets ----

        private static StageConfig[] BuildAssets()
        {
            EnsureFolder(ContentRoot);
            EnsureFolder(ContentRoot + "/Troops");
            EnsureFolder(ContentRoot + "/Heroes");
            EnsureFolder(ContentRoot + "/Stages");
            EnsureFolder(ContentRoot + "/Factions");

            var faction = GetOrCreate<FactionDefinition>(ContentRoot + "/Factions/Faction_Placeholder.asset", _ => { });

            var soldier = Troop("Troop_Soldier", "soldier", "Lính", new Color(0.85f, 0.85f, 0.9f),
                120f, 12f, 2.7f, 0.6f, 1.0f, 1.0f, 4.5f);
            var swordsman = Troop("Enemy_Swordsman", "enemy_swordsman", "Kiếm binh địch", new Color(0.6f, 0.12f, 0.15f),
                160f, 16f, 2.2f, 0.6f, 1.0f, 1.0f, 5f);
            var archer = Troop("Enemy_Archer", "enemy_archer", "Cung thủ địch", new Color(0.9f, 0.45f, 0.15f),
                90f, 13f, 2.2f, 4.5f, 1.3f, 1.0f, 6f);
            var brute = Troop("Enemy_Brute", "enemy_brute", "Lực sĩ địch", new Color(0.45f, 0.15f, 0.45f),
                600f, 35f, 1.6f, 0.7f, 1.5f, 1.0f, 5f);

            var lord = Hero("Hero_Lord", "lord", "Chủ Tướng", new Color(1f, 0.8f, 0.2f), faction, soldier,
                900f, 45f, 2.6f, 0.7f, 1.0f, 1.0f, 6f);
            var heroes = new[]
            {
                Hero("Hero_Swordsman", "swordsman", "Kiếm Sĩ", new Color(0.95f, 0.35f, 0.3f), faction, soldier,
                    700f, 40f, 2.8f, 0.6f, 0.9f, 1.0f, 6f),
                Hero("Hero_Archer", "archer", "Cung Thủ", new Color(0.35f, 0.85f, 0.4f), faction, soldier,
                    450f, 30f, 2.6f, 4.5f, 1.1f, 1.0f, 7f),
                Hero("Hero_Cavalry", "cavalry", "Kỵ Binh", new Color(0.6f, 0.45f, 1f), faction, soldier,
                    650f, 35f, 3.6f, 0.6f, 1.0f, 1.0f, 6f),
                Hero("Hero_Guardian", "guardian", "Giáp Sĩ", new Color(0.3f, 0.8f, 0.95f), faction, soldier,
                    1200f, 25f, 2.2f, 0.6f, 1.2f, 1.0f, 5f)
            };

            var loadout = GetOrCreate<TestLoadout>(ContentRoot + "/TestLoadout.asset", so =>
            {
                so.FindProperty("lordHero").objectReferenceValue = lord;
                var list = so.FindProperty("heroes");
                list.arraySize = heroes.Length;
                for (var i = 0; i < heroes.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = heroes[i];
            });

            var attack = GetOrCreate<StageConfig>(ContentRoot + "/Stages/Stage_Attack_01.asset", so =>
            {
                CommonStage(so, "attack_01", StageType.Attack, loadout);
                SetCastle(so.FindProperty("playerCastle"), new Vector2(-20f, 0f), 3000f, false);
                SetCastle(so.FindProperty("enemyCastle"), new Vector2(19f, 0f), 10000f, true);
                so.FindProperty("timeLimit").floatValue = 180f;

                var defenders = new List<(TroopDefinition unit, Vector2 position)>
                {
                    (brute, new Vector2(14.5f, 0f)), (brute, new Vector2(15.5f, 6.5f)), (brute, new Vector2(15.5f, -6.5f)),
                    (swordsman, new Vector2(13.5f, 3f)), (swordsman, new Vector2(13.5f, -3f)),
                    (swordsman, new Vector2(15.5f, 2f)), (swordsman, new Vector2(15.5f, -2f)),
                    (swordsman, new Vector2(16.5f, 4.5f)), (swordsman, new Vector2(16.5f, -4.5f)),
                    (swordsman, new Vector2(19f, 4.5f)), (swordsman, new Vector2(19f, -4.5f)),
                    (archer, new Vector2(17.5f, 2.6f)), (archer, new Vector2(17.5f, -2.6f)),
                    (archer, new Vector2(21.5f, 2.5f)), (archer, new Vector2(21.5f, -2.5f)),
                    // Forward outpost in the middle of the field.
                    (swordsman, new Vector2(4f, -5f)), (swordsman, new Vector2(5f, -6.5f)), (archer, new Vector2(6.5f, -5.5f))
                };
                var list = so.FindProperty("defenders");
                list.arraySize = defenders.Count;
                for (var i = 0; i < defenders.Count; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("unit").objectReferenceValue = defenders[i].unit;
                    element.FindPropertyRelative("position").vector2Value = defenders[i].position;
                    element.FindPropertyRelative("guardRadius").floatValue = 4.5f;
                }
            });

            var defense = GetOrCreate<StageConfig>(ContentRoot + "/Stages/Stage_Defense_01.asset", so =>
            {
                CommonStage(so, "defense_01", StageType.Defense, loadout);
                SetCastle(so.FindProperty("playerCastle"), new Vector2(-20f, 0f), 3000f, true);
                SetCastle(so.FindProperty("enemyCastle"), new Vector2(19f, 0f), 4000f, false);
                so.FindProperty("timeLimit").floatValue = 150f;

                var waves = new[]
                {
                    (5f, new[] { (swordsman, 6, new Vector2(22f, 2f)) }),
                    (30f, new[] { (swordsman, 6, new Vector2(22f, 4f)), (archer, 4, new Vector2(22.5f, -3f)) }),
                    (60f, new[] { (swordsman, 8, new Vector2(22f, -5f)), (archer, 4, new Vector2(22.5f, 5f)), (brute, 2, new Vector2(21f, 0f)) }),
                    (90f, new[] { (swordsman, 10, new Vector2(22f, 6f)), (archer, 6, new Vector2(22.5f, 0f)), (brute, 3, new Vector2(21f, -6f)) })
                };
                var list = so.FindProperty("waves");
                list.arraySize = waves.Length;
                for (var w = 0; w < waves.Length; w++)
                {
                    var wave = list.GetArrayElementAtIndex(w);
                    wave.FindPropertyRelative("startTime").floatValue = waves[w].Item1;
                    var spawns = wave.FindPropertyRelative("spawns");
                    var entries = waves[w].Item2;
                    spawns.arraySize = entries.Length;
                    for (var s = 0; s < entries.Length; s++)
                    {
                        var spawn = spawns.GetArrayElementAtIndex(s);
                        spawn.FindPropertyRelative("unit").objectReferenceValue = entries[s].Item1;
                        spawn.FindPropertyRelative("count").intValue = entries[s].Item2;
                        spawn.FindPropertyRelative("spawnPoint").vector2Value = entries[s].Item3;
                    }
                }
            });

            return new[] { attack, defense };
        }

        private static void CommonStage(SerializedObject so, string stageId, StageType type, TestLoadout loadout)
        {
            so.FindProperty("stageId").stringValue = stageId;
            so.FindProperty("stageType").enumValueIndex = (int)type;
            so.FindProperty("mapBounds").rectValue = new Rect(-24f, -13f, 48f, 26f);
            var forbidden = so.FindProperty("forbiddenZones");
            forbidden.arraySize = 1;
            forbidden.GetArrayElementAtIndex(0).rectValue = new Rect(-2f, 5f, 5f, 8f);
            so.FindProperty("summonZone").rectValue = new Rect(-18f, -9f, 7f, 18f);
            var points = new[]
            {
                new Vector2(-15.5f, 0f), new Vector2(-15.5f, 3.5f), new Vector2(-15.5f, -3.5f),
                new Vector2(-13f, 1.75f), new Vector2(-13f, -1.75f)
            };
            var quick = so.FindProperty("quickSummonPoints");
            quick.arraySize = points.Length;
            for (var i = 0; i < points.Length; i++)
                quick.GetArrayElementAtIndex(i).vector2Value = points[i];
            so.FindProperty("testLoadout").objectReferenceValue = loadout;
        }

        private static void SetCastle(SerializedProperty castle, Vector2 position, float health, bool canAttack)
        {
            castle.FindPropertyRelative("position").vector2Value = position;
            castle.FindPropertyRelative("maxHealth").floatValue = health;
            castle.FindPropertyRelative("canAttack").boolValue = canAttack;
            castle.FindPropertyRelative("attackRange").floatValue = 6f;
            castle.FindPropertyRelative("damage").floatValue = 30f;
            castle.FindPropertyRelative("attackInterval").floatValue = 1.5f;
        }

        private static TroopDefinition Troop(string file, string id, string displayName, Color color, float health,
            float damage, float speed, float range, float interval, float reaction, float aggro) =>
            GetOrCreate<TroopDefinition>($"{ContentRoot}/Troops/{file}.asset", so =>
            {
                so.FindProperty("troopId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("placeholderColor").colorValue = color;
                SetStats(so.FindProperty("stats"), health, damage, speed, range, interval, reaction, aggro);
            });

        private static HeroDefinition Hero(string file, string id, string displayName, Color color, FactionDefinition faction,
            TroopDefinition troop, float health, float damage, float speed, float range, float interval, float reaction,
            float aggro) =>
            GetOrCreate<HeroDefinition>($"{ContentRoot}/Heroes/{file}.asset", so =>
            {
                so.FindProperty("heroId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("placeholderColor").colorValue = color;
                so.FindProperty("faction").objectReferenceValue = faction;
                so.FindProperty("troop").objectReferenceValue = troop;
                so.FindProperty("baseSquadSize").intValue = 4;
                so.FindProperty("maxSquadSize").intValue = 8;
                so.FindProperty("leashRadius").floatValue = 4f;
                SetStats(so.FindProperty("stats"), health, damage, speed, range, interval, reaction, aggro);
            });

        private static void SetStats(SerializedProperty stats, float health, float damage, float speed, float range,
            float interval, float reaction, float aggro)
        {
            stats.FindPropertyRelative("maxHealth").floatValue = health;
            stats.FindPropertyRelative("damage").floatValue = damage;
            stats.FindPropertyRelative("moveSpeed").floatValue = speed;
            stats.FindPropertyRelative("attackRange").floatValue = range;
            stats.FindPropertyRelative("attackInterval").floatValue = interval;
            stats.FindPropertyRelative("reactionRange").floatValue = reaction;
            stats.FindPropertyRelative("aggroRange").floatValue = aggro;
        }

        private static T GetOrCreate<T>(string path, Action<SerializedObject> fill) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---- Scene ----

        private static void BuildScene(StageConfig[] stages)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log($"[Arena3Q] {ScenePath} already exists; leaving it unchanged.");
                return;
            }

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            if (!AssetDatabase.CopyAsset(SceneTemplatePath, ScenePath))
                throw new InvalidOperationException($"Could not copy {SceneTemplatePath} to {ScenePath}.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            if (camera == null)
                throw new InvalidOperationException("The scene template has no Main Camera.");
            camera.backgroundColor = new Color(0.16f, 0.22f, 0.15f);
            camera.orthographicSize = 7f;
            var cameraController = camera.gameObject.AddComponent<BattleCameraController>();

            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            if (material == null)
                Debug.LogWarning($"[Arena3Q] {SpriteMaterialPath} not found; sprites will use the default material.");

            var battleObject = new GameObject("Battle");
            var battle = battleObject.AddComponent<BattleController>();
            battle.Configure(stages, material, startAutomatically: true, simulateAutomatically: true);
            var input = battleObject.AddComponent<BattleInputController>();
            SetReference(input, "battle", battle);
            SetReference(input, "cameraController", cameraController);
            SetReference(cameraController, "battle", battle);

            var hudObject = new GameObject("HUD");
            var hud = hudObject.AddComponent<BattleHud>();
            SetReference(hud, "battle", battle);
            SetReference(hud, "cameraController", cameraController);

            EditorUtility.SetDirty(battle);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} not found.");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings()
        {
            // Re-add (refreshing the GUID if the scene was recreated) as the first, startup scene.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

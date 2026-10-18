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
        internal const string SceneTemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [MenuItem("Arena3Q/Build Prototype Content")]
        public static void BuildAll()
        {
            try
            {
                var stages = BuildAssets(out var progression);
                BuildScene(stages, progression);
                EnsureBattleSceneReferences(progression, stages);
                AssetDatabase.SaveAssets();
                var frontend = FrontendContentBuilder.Build(progression, stages[0], stages[1]);
                AssetDatabase.SaveAssets();
                FrontendContentBuilder.BuildScene(frontend);
                BatQuaiTranMapBuilder.ApplyToAllStages();
                AddScenesToBuildSettings();
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

        /// <summary>Opens the Battle scene (also usable as -executeMethod when launching the Editor).</summary>
        [MenuItem("Arena3Q/Open Battle Scene")]
        public static void OpenBattleScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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

        private static StageConfig[] BuildAssets(out ProgressionConfig progression)
        {
            EnsureFolder(ContentRoot);
            EnsureFolder(ContentRoot + "/Troops");
            EnsureFolder(ContentRoot + "/Heroes");
            EnsureFolder(ContentRoot + "/Lords");
            EnsureFolder(ContentRoot + "/Stages");
            EnsureFolder(ContentRoot + "/Factions");
            EnsureFolder(ContentRoot + "/Progression");

            // Levels, stars, breakthroughs: the field defaults are the design numbers (docs/KeHoach/HeroProgression.md).
            progression = GetOrCreate<ProgressionConfig>(ContentRoot + "/Progression/ProgressionConfig.asset", _ => { });
            // Class growth (§4): strength grows faster, (1 + Công) × (1 + Máu) per level is the same for every class.
            var growthOffense = Growth("Growth_Offense", "offense", "Công kích", 0.036f, 0.024f);
            var growthBalanced = Growth("Growth_Balanced", "balanced", "Cân bằng", 0.033f, 0.027f);
            var growthDefense = Growth("Growth_Defense", "defense", "Phòng thủ", 0.024f, 0.036f);
            BuildFactions();

            var soldier = Troop("Troop_Soldier", "soldier", "Lính", new Color(0.85f, 0.85f, 0.9f),
                120f, 12f, 2.7f, 0.6f, 1.0f, 0.6f, 4.5f);
            var swordsman = Troop<EnemyDefinition>("Enemy_Swordsman", "enemy_swordsman", "Lâu La Đao", new Color(0.6f, 0.12f, 0.15f),
                160f, 16f, 2.2f, 0.6f, 1.0f, 0.6f, 5f);
            var archer = Troop<EnemyDefinition>("Enemy_Archer", "enemy_archer", "Lâu La Cung", new Color(0.9f, 0.45f, 0.15f),
                90f, 13f, 2.2f, 4.5f, 1.3f, 0.7f, 6f);
            var brute = Troop<EnemyDefinition>("Enemy_Brute", "enemy_brute", "Đại Hán Búa", new Color(0.45f, 0.15f, 0.45f),
                600f, 35f, 1.6f, 0.7f, 1.5f, 0.7f, 5f);

            // The prototype's placeholder lord (spec §2: "Prototype dùng một chủ tướng placeholder").
            var lord = Hero("Hero_Lord", "lord", "Chủ Tướng", new Color(1f, 0.8f, 0.2f), HeroKind.Lord, growthBalanced, soldier,
                900f, 45f, 2.6f, 0.7f, 1.0f, 0.7f, 6f);
            // Starter heroes (docs/Tuong/NormalHeroes.md): prototype stats kept as they are (balance comes later).
            var heroes = new[]
            {
                Hero("Hero_Swordsman", "swordsman", "Đao Khách", new Color(0.95f, 0.35f, 0.3f), HeroKind.Starter, growthBalanced,
                    soldier, 700f, 40f, 2.8f, 0.6f, 0.9f, 0.6f, 6f),
                Hero("Hero_Archer", "archer", "Xạ Thủ", new Color(0.35f, 0.85f, 0.4f), HeroKind.Starter, growthOffense,
                    soldier, 450f, 30f, 2.6f, 4.5f, 1.1f, 0.7f, 7f, roleBonus: 0.18f),
                Hero("Hero_Cavalry", "cavalry", "Kỵ Binh", new Color(0.6f, 0.45f, 1f), HeroKind.Starter, growthBalanced,
                    soldier, 650f, 35f, 3.6f, 0.6f, 1.0f, 0.6f, 6f, roleBonus: 0.10f),
                Hero("Hero_Guardian", "guardian", "Giáp Sĩ", new Color(0.3f, 0.8f, 0.95f), HeroKind.Starter, growthDefense,
                    soldier, 1200f, 25f, 2.2f, 0.6f, 1.2f, 0.6f, 5f, roleBonus: 0.15f)
            };

            // The four lords the player picks from (docs/KeHoach/HeroProgression.md §5.1, level-1 numbers).
            Hero("Lord_Archer", "lord_archer", "Cung thủ", new Color(0.36f, 0.67f, 0.54f), HeroKind.Lord, growthOffense, soldier,
                560f, 58f, 2.9f, 4.5f, 1f / 0.9f, 0.7f, 7f, roleBonus: 0.18f, folder: "Lords");
            Hero("Lord_Swordsman", "lord_swordsman", "Kiếm sĩ", new Color(0.62f, 0.72f, 0.9f), HeroKind.Lord, growthBalanced, soldier,
                760f, 36f, 3.0f, 0.6f, 1f / 1.4f, 0.6f, 6f, damageReduction: 0.05f, folder: "Lords");
            Hero("Lord_Strategist", "lord_strategist", "Mưu sĩ", new Color(0.73f, 0.64f, 0.84f), HeroKind.Lord, growthOffense, soldier,
                600f, 32f, 2.6f, 3.0f, 1f / 0.8f, 0.7f, 6.5f, roleBonus: 0.62f, folder: "Lords");
            Hero("Lord_Marshal", "lord_marshal", "Thống soái", new Color(0.85f, 0.32f, 0.27f), HeroKind.Lord, growthDefense, soldier,
                950f, 26f, 2.3f, 0.9f, 1f, 0.7f, 6f, damageReduction: 0.10f, leadership: 110f, roleBonus: 0.22f,
                folder: "Lords");
            // Passive + skills 2, 3 + ultimate of every lord and starter hero (spec §3).
            SkillContentBuilder.Build();

            var loadout = GetOrCreate<TestLoadout>(ContentRoot + "/TestLoadout.asset", so =>
            {
                so.FindProperty("lordHero").objectReferenceValue = lord;
                so.FindProperty("lordLevel").intValue = 1;
                var list = so.FindProperty("heroes");
                list.arraySize = heroes.Length;
                for (var i = 0; i < heroes.Length; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("hero").objectReferenceValue = heroes[i];
                    entry.FindPropertyRelative("level").intValue = 1;
                    entry.FindPropertyRelative("stars").intValue = 1;
                }
            });

            var attack = GetOrCreate<StageConfig>(ContentRoot + "/Stages/Stage_Attack_01.asset", so =>
            {
                CommonStage(so, "attack_01", StageType.Attack, loadout);
                SetCastle(so.FindProperty("playerCastle"), new Vector2(-19.4f, 0f), 3000f, false);
                SetCastle(so.FindProperty("enemyCastle"), new Vector2(19.4f, 0f), 10000f, true);
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
                    // Inner ring in front of and behind the castle (spec §8: defenders concentrate around it).
                    (swordsman, new Vector2(12.5f, -1.5f)), (swordsman, new Vector2(12.5f, 1.5f)), (archer, new Vector2(22.5f, 0f))
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
                SetCastle(so.FindProperty("playerCastle"), new Vector2(-19.4f, 0f), 3000f, true);
                SetCastle(so.FindProperty("enemyCastle"), new Vector2(19.4f, 0f), 4000f, false);
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

            // Chapters 1–3 of enemies (docs/QuanDich) and two stages with a boss; the templates must be on disk to be copied.
            AssetDatabase.SaveAssets();
            var bosses = EnemyContentBuilder.Build(swordsman, archer, brute, attack, defense);
            return new[] { attack, defense, bosses.AttackBossStage, bosses.DefenseBossStage };
        }

        // Standard size-M layout (docs/BanDo/MapSpecification.md), measured from the map art map1.png so logic and art match:
        // one winding river crossing the map from top to bottom through the middle, with two bridges (two lanes).
        // The centre line runs straight down under each bridge because the bridges are drawn spanning left-right.
        public static readonly Vector2[] StandardRiverPath =
        {
            new Vector2(1.5f, 14f), new Vector2(0.4f, 11.8f), new Vector2(-0.4f, 9.3f), new Vector2(-0.2f, 8.6f),
            new Vector2(0.85f, 8f), new Vector2(0.85f, 5.4f), new Vector2(1.9f, 4.6f), new Vector2(1.5f, 3.5f),
            new Vector2(1.1f, 1.6f), new Vector2(0.8f, 0.2f), new Vector2(0.3f, -1.2f), new Vector2(-0.4f, -2.5f),
            new Vector2(-0.2f, -3.6f), new Vector2(-0.2f, -6.1f), new Vector2(1.1f, -7f), new Vector2(1.25f, -8.3f),
            new Vector2(0.7f, -9.6f), new Vector2(0.4f, -10.6f), new Vector2(0.1f, -11.7f), new Vector2(-0.2f, -14f)
        };
        // Bright water in the art is ~2.0 wide plus stony banks; 2.3 keeps units on the bank, never on the water.
        public const float StandardRiverWidth = 2.3f;
        public static readonly Vector2[] StandardBridges = { new Vector2(0.85f, 6.7f), new Vector2(-0.2f, -4.85f) };
        // The painted bridge decks are ~2.4 long along the river (narrower than the 4 proposed in the map spec).
        public const float StandardBridgeWidth = 2.4f;

        /// <summary>
        /// Replaces the forbidden zones of the shipped stages with the standard river, keeping each asset (and its
        /// GUID, so the Battle scene keeps its references). Batch mode:
        /// -executeMethod Arena3Q.Editor.PrototypeContentBuilder.ApplyStandardRiverLayout
        /// </summary>
        [MenuItem("Arena3Q/Apply Standard River Layout")]
        public static void ApplyStandardRiverLayout()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:StageConfig", new[] { ContentRoot }))
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(stage);
                ApplyStandardRiver(so);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
                Debug.Log($"[Arena3Q] Standard river applied to {stage.name}.");
            }
            AssetDatabase.SaveAssets();
        }

        private static void ApplyStandardRiver(SerializedObject so)
        {
            so.FindProperty("forbiddenZones").arraySize = 0;
            var rivers = so.FindProperty("rivers");
            rivers.arraySize = 1;
            var river = rivers.GetArrayElementAtIndex(0);
            river.FindPropertyRelative("name").stringValue = "Sông";
            river.FindPropertyRelative("width").floatValue = StandardRiverWidth;
            var path = river.FindPropertyRelative("path");
            path.arraySize = StandardRiverPath.Length;
            for (var i = 0; i < StandardRiverPath.Length; i++)
                path.GetArrayElementAtIndex(i).vector2Value = StandardRiverPath[i];
            var crossings = river.FindPropertyRelative("crossings");
            crossings.arraySize = StandardBridges.Length;
            for (var i = 0; i < StandardBridges.Length; i++)
            {
                var crossing = crossings.GetArrayElementAtIndex(i);
                crossing.FindPropertyRelative("position").vector2Value = StandardBridges[i];
                crossing.FindPropertyRelative("width").floatValue = StandardBridgeWidth;
            }
        }

        internal static void CommonStage(SerializedObject so, string stageId, StageType type, TestLoadout loadout)
        {
            so.FindProperty("stageId").stringValue = stageId;
            so.FindProperty("stageType").enumValueIndex = (int)type;
            so.FindProperty("mapBounds").rectValue = new Rect(-24f, -13f, 48f, 26f);
            // The map itself (Bát Quái Trận) is put on every stage at the end of BuildAll.
            so.FindProperty("summonZone").rectValue = new Rect(-17.5f, -9f, 6.5f, 18f);
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

        internal static void SetCastle(SerializedProperty castle, Vector2 position, float health, bool canAttack)
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
            Troop<TroopDefinition>(file, id, displayName, color, health, damage, speed, range, interval, reaction, aggro);

        // Chapter-1 soldiers (docs/QuanDich/Chuong1_SonTac.md) are normal-rank EnemyDefinitions.
        internal static T Troop<T>(string file, string id, string displayName, Color color, float health,
            float damage, float speed, float range, float interval, float reaction, float aggro) where T : TroopDefinition =>
            GetOrCreate<T>($"{ContentRoot}/Troops/{file}.asset", so =>
            {
                so.FindProperty("troopId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("placeholderColor").colorValue = color;
                SetStats(so.FindProperty("stats"), health, damage, speed, range, interval, reaction, aggro);
            });

        private static HeroDefinition Hero(string file, string id, string displayName, Color color, HeroKind kind,
            GrowthProfile growth, TroopDefinition troop, float health, float damage, float speed, float range, float interval,
            float reaction, float aggro, float damageReduction = 0f, float leadership = 100f, float roleBonus = 0f,
            string folder = "Heroes")
        {
            var hero = GetOrCreate<HeroDefinition>($"{ContentRoot}/{folder}/{file}.asset", so =>
            {
                so.FindProperty("heroId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("placeholderColor").colorValue = color;
                so.FindProperty("troop").objectReferenceValue = troop;
                so.FindProperty("baseSquadSize").intValue = 4;
                so.FindProperty("maxSquadSize").intValue = 8;
                so.FindProperty("leashRadius").floatValue = 4f;
                SetStats(so.FindProperty("stats"), health, damage, speed, range, interval, reaction, aggro);
                SetProgressionFields(so, kind, growth, damageReduction, leadership, roleBonus);
            });

            // Heroes created before progression existed: add the progression fields once, keep their tuned stats.
            if (hero.GrowthProfile == null)
            {
                var so = new SerializedObject(hero);
                SetProgressionFields(so, kind, growth, damageReduction, leadership, roleBonus);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hero);
                Debug.Log($"[Arena3Q] {hero.name}: progression fields added ({kind}, {growth.name}).");
            }
            return hero;
        }

        private static void SetProgressionFields(SerializedObject so, HeroKind kind, GrowthProfile growth,
            float damageReduction, float leadership, float roleBonus)
        {
            so.FindProperty("kind").enumValueIndex = (int)kind;
            // Spec §2: lords and starter heroes never belong to a faction.
            so.FindProperty("faction").objectReferenceValue = null;
            so.FindProperty("growthProfile").objectReferenceValue = growth;
            so.FindProperty("damageReduction").floatValue = damageReduction;
            so.FindProperty("leadership").floatValue = leadership;
            so.FindProperty("roleBonus").floatValue = roleBonus;
        }

        private static GrowthProfile Growth(string file, string id, string displayName, float attack, float health) =>
            GetOrCreate<GrowthProfile>($"{ContentRoot}/Progression/{file}.asset", so =>
            {
                so.FindProperty("profileId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("attackGrowthPerLevel").floatValue = attack;
                so.FindProperty("healthGrowthPerLevel").floatValue = health;
            });

        // Faction synergy (docs/KeHoach/HeroProgression.md §9): each tier is worth about the same for every faction.
        private static void BuildFactions()
        {
            Faction("Faction_Shu", "shu", "Thục", new Color(0.25f, 0.62f, 0.35f), "Tấn công",
                new FactionDefinition.Tier { minHeroes = 2, attackBonus = 0.06f },
                new FactionDefinition.Tier { minHeroes = 3, attackBonus = 0.12f },
                new FactionDefinition.Tier { minHeroes = 4, attackBonus = 0.20f });
            Faction("Faction_Wei", "wei", "Ngụy", new Color(0.27f, 0.42f, 0.78f), "Phòng thủ",
                new FactionDefinition.Tier { minHeroes = 2, healthBonus = 0.03f, damageReduction = 0.03f },
                new FactionDefinition.Tier { minHeroes = 3, healthBonus = 0.06f, damageReduction = 0.06f },
                new FactionDefinition.Tier { minHeroes = 4, healthBonus = 0.10f, damageReduction = 0.10f });
            Faction("Faction_Wu", "wu", "Ngô", new Color(0.82f, 0.27f, 0.22f), "Tốc độ và kỹ năng",
                new FactionDefinition.Tier { minHeroes = 2, attackSpeedBonus = 0.04f, rageGainBonus = 0.10f },
                new FactionDefinition.Tier { minHeroes = 3, attackSpeedBonus = 0.08f, rageGainBonus = 0.20f },
                new FactionDefinition.Tier { minHeroes = 4, attackSpeedBonus = 0.12f, rageGainBonus = 0.30f, cooldownReduction = 0.10f });
            Faction("Faction_Qun", "qun", "Quần", new Color(0.8f, 0.66f, 0.22f), "Chí mạng",
                new FactionDefinition.Tier { minHeroes = 2, attackBonus = 0.03f, critChance = 0.06f },
                new FactionDefinition.Tier { minHeroes = 3, attackBonus = 0.06f, critChance = 0.12f },
                new FactionDefinition.Tier { minHeroes = 4, attackBonus = 0.08f, critChance = 0.15f, critMultiplier = 1.75f });
        }

        private static FactionDefinition Faction(string file, string id, string displayName, Color color, string strength,
            params FactionDefinition.Tier[] tiers) =>
            GetOrCreate<FactionDefinition>($"{ContentRoot}/Factions/{file}.asset", so =>
            {
                so.FindProperty("factionId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("color").colorValue = color;
                so.FindProperty("strength").stringValue = strength;
                var list = so.FindProperty("synergyTiers");
                list.arraySize = tiers.Length;
                for (var i = 0; i < tiers.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    var tier = tiers[i];
                    element.FindPropertyRelative("minHeroes").intValue = tier.minHeroes;
                    element.FindPropertyRelative("attackBonus").floatValue = tier.attackBonus;
                    element.FindPropertyRelative("healthBonus").floatValue = tier.healthBonus;
                    element.FindPropertyRelative("damageReduction").floatValue = tier.damageReduction;
                    element.FindPropertyRelative("attackSpeedBonus").floatValue = tier.attackSpeedBonus;
                    element.FindPropertyRelative("rageGainBonus").floatValue = tier.rageGainBonus;
                    element.FindPropertyRelative("cooldownReduction").floatValue = tier.cooldownReduction;
                    element.FindPropertyRelative("critChance").floatValue = tier.critChance;
                    element.FindPropertyRelative("critMultiplier").floatValue = tier.critMultiplier;
                }
            });

        internal static void SetStats(SerializedProperty stats, float health, float damage, float speed, float range,
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

        internal static T GetOrCreate<T>(string path, Action<SerializedObject> fill) where T : ScriptableObject
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

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---- Scene ----

        /// <summary>Wires assets added after the Battle scene was created (fills empty references only).</summary>
        private static void EnsureBattleSceneReferences(ProgressionConfig progression, StageConfig[] stages)
        {
            // OpenScene(Single) can unload saved assets nothing references: reload by path afterwards.
            var progressionPath = AssetDatabase.GetAssetPath(progression);
            var stagePaths = Array.ConvertAll(stages, s => AssetDatabase.GetAssetPath(s));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            progression = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(progressionPath);
            var battle = UnityEngine.Object.FindAnyObjectByType<BattleController>();
            if (battle == null)
                throw new InvalidOperationException($"{ScenePath} has no BattleController.");
            var so = new SerializedObject(battle);
            var changed = false;
            var property = so.FindProperty("progression");
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = progression;
                changed = true;
                Debug.Log($"[Arena3Q] {ScenePath}: progression config assigned.");
            }

            // Stages added to the content later (boss stages) join the scene's list, after the ones it has.
            var list = so.FindProperty("stages");
            var present = new HashSet<UnityEngine.Object>();
            for (var i = 0; i < list.arraySize; i++)
                present.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var path in stagePaths)
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
                if (stage == null || present.Contains(stage))
                    continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = stage;
                changed = true;
                Debug.Log($"[Arena3Q] {ScenePath}: stage {stage.name} added.");
            }
            if (!changed)
                return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void BuildScene(StageConfig[] stages, ProgressionConfig progression)
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
            battle.UseProgression(progression);
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

        internal static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new InvalidOperationException($"{target.GetType().Name}.{field} not found.");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddScenesToBuildSettings()
        {
            // Re-add (refreshing the GUIDs if a scene was recreated): the game flow starts the app, then the battle.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath || s.path == FrontendContentBuilder.FrontendScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(FrontendContentBuilder.FrontendScenePath, true));
            scenes.Insert(1, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

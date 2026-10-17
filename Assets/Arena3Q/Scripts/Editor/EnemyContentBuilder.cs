using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Creates the enemies of chapters 1–3 (docs/QuanDich: soldiers, elites, bosses and their skills) and two stages
    /// with a boss. Numbers are the chapter files' drafts at the chapter's first level; the assets hold the level-1
    /// values (each stage level adds 3 %, chapter N starts at level 1 + 9 × (N − 1)). Like the rest of the builder it
    /// only creates what is missing.
    /// </summary>
    internal static class EnemyContentBuilder
    {
        private const string Root = PrototypeContentBuilder.ContentRoot + "/Enemies";
        private const float EnemyGrowthPerLevel = 0.03f;

        public static readonly int[] ChapterLevels = { 1, 10, 19 };

        public sealed class Result
        {
            public StageConfig AttackBossStage;
            public StageConfig DefenseBossStage;
        }

        /// <summary>Builds every chapter; the chapter-1 soldiers are the existing troop assets.</summary>
        public static Result Build(EnemyDefinition saber, EnemyDefinition bowman, EnemyDefinition brute,
            StageConfig attackTemplate, StageConfig defenseTemplate)
        {
            PrototypeContentBuilder.EnsureFolder(Root);
            var c1 = Chapter1(saber);
            Chapter2();
            var c3 = Chapter3();
            return new Result
            {
                AttackBossStage = AttackBossStage(attackTemplate, saber, bowman, brute, c1),
                DefenseBossStage = DefenseBossStage(defenseTemplate, c3)
            };
        }

        // ---- Chapter 1 – Hắc Phong Trại (docs/QuanDich/Chuong1_SonTac.md), level 1 ----

        private sealed class Chapter1Units
        {
            public EnemyDefinition Hound, Axeman, Advisor, Boss;
        }

        private static Chapter1Units Chapter1(EnemyDefinition saber)
        {
            const int c = 0;
            var folder = Folder("C1");
            var color = new Color(0.25f, 0.2f, 0.2f);
            var units = new Chapter1Units
            {
                Hound = Enemy(folder, "c1_hound", "Chó Săn Trại", new Color(0.2f, 0.2f, 0.22f), EnemyTier.Normal, 0.30f, c,
                    70f, 8f, 1.5f, 0.5f, 3.6f)
            };

            var whirl = Skill(folder, "c1_cuong_phu_xoay", "Cuồng Phủ Xoáy", color, so =>
            {
                Use(so, cooldown: 8f, castTime: 0f, condition: CastCondition.EnemyInArea);
                Area(so, SkillShape.Circle, radius: 1.8f);
                OverTime(so, zone: 1.5f, tick: 0.25f, follow: true);
                Enemy(so, EnemySkillAim.Self, telegraph: 1f, initialCooldown: 3f, channel: 1.5f);
                Effects(so, Damage(0.5f));
            });
            units.Axeman = Enemy(folder, "c1_axeman", "Đầu Mục Song Phủ", new Color(0.35f, 0.12f, 0.12f), EnemyTier.Elite,
                0.40f, c, 900f, 40f, 0.8f, 0.8f, 2.0f, whirl);

            var wine = Skill(folder, "c1_ruou_manh", "Rượu Mạnh Tiếp Sức", new Color(0.95f, 0.6f, 0.25f), so =>
            {
                Use(so, cooldown: 10f, castTime: 0.6f, condition: CastCondition.EnemyInAggroRange);
                Area(so, SkillShape.Circle, radius: 4f);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 4f);
                Effects(so, Status(SkillEffectKind.Heal, 0.15f, 0f, SkillAffects.Allies),
                    Status(SkillEffectKind.AttackSpeedUp, 0.2f, 5f, SkillAffects.Allies));
            });
            units.Advisor = Enemy(folder, "c1_advisor", "Quân Sư Sơn Trại", new Color(0.4f, 0.3f, 0.25f), EnemyTier.Elite,
                0.40f, c, 650f, 18f, 0.7f, 3.5f, 1.9f, wine);

            var slash = Skill(folder, "c1_hac_phong_tram", "Hắc Phong Trảm", color, so =>
            {
                Use(so, cooldown: 7f, castTime: 0f, range: 3f, condition: CastCondition.EnemyInArea);
                Area(so, SkillShape.Cone, length: 3f, angle: 90f);
                Enemy(so, EnemySkillAim.CurrentTarget, telegraph: 1f, initialCooldown: 3f);
                Effects(so, Damage(2f), Status(SkillEffectKind.Knockback, 1.5f, 0f));
            });
            var slash360 = Skill(folder, "c1_hac_phong_tram_360", "Hắc Phong Trảm (360°)", color, so =>
            {
                Use(so, cooldown: 7f, castTime: 0f, condition: CastCondition.EnemyInArea);
                Area(so, SkillShape.Circle, radius: 3f);
                Enemy(so, EnemySkillAim.Self, telegraph: 1f, initialCooldown: 2f);
                Effects(so, Damage(2f), Status(SkillEffectKind.Knockback, 1.5f, 0f));
            });
            var callBandits = Skill(folder, "c1_goi_lau_la", "Gọi Lâu La", color, so =>
            {
                Use(so, cooldown: 15f, castTime: 1f, condition: CastCondition.EnemyInAggroRange);
                Area(so, SkillShape.Circle, radius: 0.5f);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 6f);
                Summon(so, saber, 4);
            });
            units.Boss = Enemy(folder, "c1_boss", "Hắc Phong Trại Chủ", new Color(0.15f, 0.12f, 0.12f), EnemyTier.Boss,
                0.80f, c, 6000f, 70f, 0.7f, 1.2f, 1.8f, slash, callBandits);
            Phase(units.Boss, attackSpeed: 0.3f, moveSpeed: 0f, skills: new[] { slash360, callBandits });
            return units;
        }

        // ---- Chapter 2 – Hoàng Cân Tà Đạo (docs/QuanDich/Chuong2_HoangCan.md), level 10 ----

        private static void Chapter2()
        {
            const int c = 1;
            var folder = Folder("C2");
            var color = new Color(0.85f, 0.7f, 0.2f);
            Enemy(folder, "c2_follower", "Tín Đồ Khăn Vàng", new Color(0.8f, 0.68f, 0.3f), EnemyTier.Normal, 0.32f, c,
                200f, 18f, 0.9f, 0.9f, 2.2f);
            Enemy(folder, "c2_talisman", "Phù Thuỷ Tán Bùa", new Color(0.7f, 0.6f, 0.25f), EnemyTier.Normal, 0.30f, c,
                110f, 17f, 0.7f, 4.0f, 2.0f);
            Enemy(folder, "c2_zealot", "Lực Sĩ Cuồng Tín", new Color(0.6f, 0.45f, 0.15f), EnemyTier.Normal, 0.32f, c,
                800f, 40f, 0.6f, 0.7f, 1.6f);
            var straw = Enemy(folder, "c2_straw", "Bù Nhìn Rơm", new Color(0.85f, 0.78f, 0.45f), EnemyTier.Normal, 0.30f, c,
                50f, 7f, 1.2f, 0.5f, 2.6f);

            var guard = Skill(folder, "c2_kim_cuong_ho_the", "Kim Cương Hộ Thể", new Color(0.9f, 0.7f, 0.3f), so =>
            {
                Use(so, cooldown: 12f, castTime: 0.3f, condition: CastCondition.EnemyInArea);
                Area(so, SkillShape.Circle, radius: 2.5f);
                OverTime(so, zone: 4f, tick: 0.5f, follow: true);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 3f, channel: 4f);
                // Spec §8: the taunt only moves player units that are not carrying out an order.
                Effects(so, Status(SkillEffectKind.DamageTakenDown, 0.6f, 0f, SkillAffects.Self),
                    Status(SkillEffectKind.Taunt, 1f, 0f));
            });
            Enemy(folder, "c2_bronze_guard", "Hộ Pháp Đồng Giáp", new Color(0.7f, 0.5f, 0.2f), EnemyTier.Elite, 0.40f, c,
                1600f, 30f, 0.6f, 0.8f, 1.5f, guard);

            var talisman = Skill(folder, "c2_thien_loi_phu", "Thiên Lôi Phù", color, so =>
            {
                Use(so, cooldown: 9f, castTime: 0f, range: 6f);
                Area(so, SkillShape.Circle, radius: 1.2f);
                Enemy(so, EnemySkillAim.AroundPlayerUnits, telegraph: 1f, initialCooldown: 3f, zones: 3);
                Effects(so, Damage(1.5f), Status(SkillEffectKind.Stun, 1f, 1f));
            });
            Enemy(folder, "c2_sorcerer", "Đại Phương Sư", new Color(0.75f, 0.6f, 0.2f), EnemyTier.Elite, 0.40f, c,
                800f, 30f, 0.6f, 4.5f, 1.8f, talisman);

            var lightning = Skill(folder, "c2_hoang_thien_loi", "Hoàng Thiên Lôi", color, so =>
            {
                Use(so, cooldown: 6f, castTime: 0f, range: 8f);
                Area(so, SkillShape.Circle, radius: 1.2f);
                Enemy(so, EnemySkillAim.AroundPlayerUnits, telegraph: 1f, initialCooldown: 2f, zones: 5);
                Effects(so, Damage(1.8f));
            });
            var strawmen = Skill(folder, "c2_trieu_hoi_rom_binh", "Triệu Hồi Rơm Binh", color, so =>
            {
                Use(so, cooldown: 12f, castTime: 1.2f, condition: CastCondition.EnemyInAggroRange);
                Area(so, SkillShape.Circle, radius: 0.5f);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 5f);
                Summon(so, straw, 8);
            });
            var storm = Skill(folder, "c2_ta_khi_phong_bao", "Tà Khí Phong Bạo", new Color(0.5f, 0.75f, 0.3f), so =>
            {
                Use(so, cooldown: 0f, castTime: 0f);
                Area(so, SkillShape.Circle, radius: 3f);
                OverTime(so, zone: 600f, tick: 0.5f, follow: true);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 0f);
                Effects(so, Status(SkillEffectKind.Slow, 0.3f, 0f), Damage(0.1f));
            });
            var shield = Skill(folder, "c2_khien_ta_khi", "Khiên Tà Khí", new Color(0.5f, 0.75f, 0.3f), so =>
            {
                Use(so, cooldown: 0f, castTime: 0f);
                Area(so, SkillShape.Circle, radius: 0.5f);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 0f);
                // 1000 of the boss's 8000 health (docs: "hấp thụ 1000 sát thương").
                Effects(so, Status(SkillEffectKind.Shield, 0.125f, 600f, SkillAffects.Self));
            });
            var boss = Enemy(folder, "c2_boss", "Thiên Công Tế Tửu", new Color(0.95f, 0.8f, 0.2f), EnemyTier.Boss, 0.80f, c,
                8000f, 60f, 0.6f, 4.0f, 1.6f, lightning, strawmen);
            Phase(boss, 0f, 0f, Array.Empty<EnemySkillDefinition>(), storm, shield);
        }

        // ---- Chapter 3 – Yêu quái Âm Sơn (docs/QuanDich/Chuong3_YeuQuai.md), level 19 ----

        private sealed class Chapter3Units
        {
            public EnemyDefinition Imp, Jiangshi, WillOWisp, StoneSpirit, OxHead, HorseFace, Boss;
        }

        private static Chapter3Units Chapter3()
        {
            const int c = 2;
            var folder = Folder("C3");
            var color = new Color(0.55f, 0.35f, 0.85f);
            var units = new Chapter3Units
            {
                Imp = Enemy(folder, "c3_imp", "Tiểu Quỷ", new Color(0.45f, 0.35f, 0.6f), EnemyTier.Normal, 0.30f, c,
                    90f, 11f, 1.5f, 0.5f, 3.4f),
                Jiangshi = Enemy(folder, "c3_jiangshi", "Cương Thi", new Color(0.35f, 0.5f, 0.45f), EnemyTier.Normal, 0.32f, c,
                    380f, 24f, 0.8f, 0.6f, 1.5f),
                WillOWisp = Enemy(folder, "c3_wisp", "Quỷ Hoả", new Color(0.45f, 0.75f, 1f), EnemyTier.Normal, 0.30f, c,
                    120f, 20f, 0.7f, 4.0f, 2.4f),
                StoneSpirit = Enemy(folder, "c3_stone", "Thạch Tinh", new Color(0.3f, 0.28f, 0.35f), EnemyTier.Normal, 0.32f, c,
                    1100f, 48f, 0.5f, 0.8f, 1.3f)
            };
            Traits(units.Jiangshi, flying: false, reviveChance: 0.3f, reviveHealth: 0.3f);
            Traits(units.WillOWisp, flying: true, reviveChance: 0f, reviveHealth: 0.3f);

            var charge = Skill(folder, "c3_nguu_ma_xung", "Ngưu Ma Xung", color, so =>
            {
                Use(so, cooldown: 10f, castTime: 0f, range: 5f, condition: CastCondition.EnemyInArea);
                Move(so, CasterMove.ChargeLine, 5f);
                Area(so, SkillShape.Line, length: 5f, width: 1.2f);
                Enemy(so, EnemySkillAim.CurrentTarget, telegraph: 1f, initialCooldown: 3f);
                Effects(so, Damage(1.5f), Status(SkillEffectKind.Knockback, 1.5f, 0f));
            });
            units.OxHead = Enemy(folder, "c3_ox_head", "Ngưu Đầu", new Color(0.4f, 0.2f, 0.25f), EnemyTier.Elite, 0.40f, c,
                2200f, 60f, 0.6f, 0.9f, 1.8f, charge);

            var chain = Skill(folder, "c3_cau_hon_toa", "Câu Hồn Tỏa", color, so =>
            {
                Use(so, cooldown: 9f, castTime: 0f, range: 5f);
                Area(so, SkillShape.Single);
                Enemy(so, EnemySkillAim.NearestHero, telegraph: 0.5f, initialCooldown: 3f);
                Effects(so, Damage(0.8f), Status(SkillEffectKind.Pull, 5f, 0f), Status(SkillEffectKind.Stun, 1f, 1.5f));
            });
            units.HorseFace = Enemy(folder, "c3_horse_face", "Mã Diện", new Color(0.3f, 0.25f, 0.4f), EnemyTier.Elite, 0.40f, c,
                1500f, 40f, 0.8f, 1.2f, 2.0f, chain);

            var foxfire = Skill(folder, "c3_ho_hoa_vu", "Hồ Hoả Vũ", new Color(0.4f, 0.8f, 1f), so =>
            {
                Use(so, cooldown: 6f, castTime: 0f, range: 8f);
                Area(so, SkillShape.Circle, radius: 1.2f);
                Enemy(so, EnemySkillAim.AroundPlayerUnits, telegraph: 1f, initialCooldown: 2f, zones: 6);
                Effects(so, Damage(1.5f));
            });
            var illusion = Skill(folder, "c3_huyen_anh", "Huyễn Ảnh", new Color(0.9f, 0.55f, 0.85f), so =>
            {
                Use(so, cooldown: 14f, castTime: 0.8f, condition: CastCondition.EnemyInAggroRange);
                Area(so, SkillShape.Circle, radius: 0.5f);
                Enemy(so, EnemySkillAim.Self, telegraph: 0f, initialCooldown: 6f);
                Clones(so, 2, 0.1f, 0.3f);
            });
            var charm = Skill(folder, "c3_mi_hoac", "Mị Hoặc", new Color(0.95f, 0.5f, 0.8f), so =>
            {
                Use(so, cooldown: 12f, castTime: 0f, range: 5f);
                Area(so, SkillShape.Single);
                Enemy(so, EnemySkillAim.RandomHero, telegraph: 0.5f, initialCooldown: 0f);
                // "Đứng yên 2 s, không đổi phe" (spec §8: crowd control never changes sides).
                Effects(so, Status(SkillEffectKind.Stun, 1f, 2f));
            });
            units.Boss = Enemy(folder, "c3_boss", "Cửu Vĩ Yêu Hồ", new Color(0.95f, 0.9f, 0.95f), EnemyTier.Boss, 0.80f, c,
                12000f, 80f, 0.8f, 3.5f, 2.4f, foxfire, illusion);
            Phase(units.Boss, attackSpeed: 0f, moveSpeed: 0.25f, skills: new[] { foxfire, illusion, charm });
            return units;
        }

        // ---- Stages ----

        private static StageConfig AttackBossStage(StageConfig template, EnemyDefinition saber, EnemyDefinition bowman,
            EnemyDefinition brute, Chapter1Units c1) =>
            CopyStage(template, "Stage_Attack_C1_Boss", "c1_boss_attack", 1, so =>
            {
                // Spec §8: the boss guards the front of the enemy castle like a defender.
                var defenders = new List<(TroopDefinition unit, Vector2 position)>
                {
                    (c1.Boss, new Vector2(15f, 0f)),
                    (c1.Axeman, new Vector2(13.5f, 3.5f)), (c1.Advisor, new Vector2(17.5f, -2.5f)),
                    (brute, new Vector2(15.5f, 6.5f)), (brute, new Vector2(15.5f, -6.5f)),
                    (saber, new Vector2(13.5f, -3f)), (saber, new Vector2(16.5f, 4.5f)), (saber, new Vector2(16.5f, -4.5f)),
                    (saber, new Vector2(12.5f, 1.5f)), (saber, new Vector2(12.5f, -1.5f)),
                    (c1.Hound, new Vector2(13f, 4.2f)), (c1.Hound, new Vector2(13f, -4.2f)), (c1.Hound, new Vector2(12f, 0f)),
                    (bowman, new Vector2(17.5f, 2.6f)), (bowman, new Vector2(21.5f, 2.5f)), (bowman, new Vector2(21.5f, -2.5f))
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

        private static StageConfig DefenseBossStage(StageConfig template, Chapter3Units c3) =>
            CopyStage(template, "Stage_Defense_C3_Boss", "c3_boss_defense", ChapterLevels[2], so =>
            {
                // Spec §8: the boss comes with the last wave. Quỷ Hoả fly over the river (spec §8 flying).
                var waves = new[]
                {
                    (5f, new[] { (c3.Imp, 6, new Vector2(22f, 2f)) }),
                    (30f, new[] { (c3.Jiangshi, 4, new Vector2(22f, 4f)), (c3.WillOWisp, 3, new Vector2(22.5f, -3f)) }),
                    (60f, new[] { (c3.StoneSpirit, 2, new Vector2(21f, 0f)), (c3.OxHead, 1, new Vector2(22f, -5f)),
                        (c3.WillOWisp, 3, new Vector2(22.5f, 5f)) }),
                    (90f, new[] { (c3.HorseFace, 1, new Vector2(22f, 6f)), (c3.Imp, 6, new Vector2(22.5f, -6f)),
                        (c3.Boss, 1, new Vector2(21f, 0f)) })
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

        /// <summary>A copy of a shipped stage (same map, castles and art) with its own enemies.</summary>
        private static StageConfig CopyStage(StageConfig template, string file, string stageId, int enemyLevel,
            Action<SerializedObject> edit)
        {
            var path = $"{PrototypeContentBuilder.ContentRoot}/Stages/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (existing != null)
                return existing;
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), path))
                throw new InvalidOperationException($"Could not copy {template.name} to {path}.");
            var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            var so = new SerializedObject(stage);
            so.FindProperty("stageId").stringValue = stageId;
            so.FindProperty("enemyLevel").intValue = enemyLevel;
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            return stage;
        }

        // ---- Asset helpers ----

        private static string Folder(string chapter)
        {
            var folder = $"{Root}/{chapter}";
            PrototypeContentBuilder.EnsureFolder(folder);
            PrototypeContentBuilder.EnsureFolder(folder + "/Skills");
            return folder;
        }

        /// <summary>
        /// An enemy whose numbers are given at its chapter's first level (as in the chapter files); stored as level-1
        /// values so the stage's enemy level gives them back.
        /// </summary>
        private static EnemyDefinition Enemy(string folder, string id, string displayName, Color color, EnemyTier tier,
            float radius, int chapter, float health, float damage, float attacksPerSecond, float range, float speed,
            params EnemySkillDefinition[] skills)
        {
            var growth = Mathf.Pow(1f + EnemyGrowthPerLevel, ChapterLevels[chapter] - 1);
            return PrototypeContentBuilder.GetOrCreate<EnemyDefinition>($"{folder}/Enemy_{id}.asset", so =>
            {
                so.FindProperty("troopId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("placeholderColor").colorValue = color;
                PrototypeContentBuilder.SetStats(so.FindProperty("stats"), Round(health / growth), Round(damage / growth),
                    speed, range, 1f / attacksPerSecond, Mathf.Min(range, 1f), Mathf.Max(5f, range + 1.5f));
                so.FindProperty("tier").enumValueIndex = (int)tier;
                so.FindProperty("bodyRadius").floatValue = radius;
                var list = so.FindProperty("skills");
                list.arraySize = skills.Length;
                for (var i = 0; i < skills.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            });
        }

        private static float Round(float value) => Mathf.Round(value * 10f) / 10f;

        private static void Traits(EnemyDefinition enemy, bool flying, float reviveChance, float reviveHealth) =>
            Edit(enemy, so =>
            {
                so.FindProperty("flying").boolValue = flying;
                so.FindProperty("reviveChance").floatValue = reviveChance;
                so.FindProperty("reviveHealth").floatValue = reviveHealth;
            });

        private static void Phase(EnemyDefinition boss, float attackSpeed, float moveSpeed, EnemySkillDefinition[] skills,
            params EnemySkillDefinition[] onEnter) =>
            Edit(boss, so =>
            {
                var phase = so.FindProperty("phaseTwo");
                phase.FindPropertyRelative("healthThreshold").floatValue = 0.5f;
                phase.FindPropertyRelative("attackSpeedBonus").floatValue = attackSpeed;
                phase.FindPropertyRelative("moveSpeedBonus").floatValue = moveSpeed;
                SetList(phase.FindPropertyRelative("skills"), skills);
                SetList(phase.FindPropertyRelative("onEnter"), onEnter);
            });

        // Only fills an asset this run created (its phase is still empty), so a designer's later edits are kept.
        private static void Edit(EnemyDefinition enemy, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(enemy);
            var phase = so.FindProperty("phaseTwo");
            var untouched = !so.FindProperty("flying").boolValue && so.FindProperty("reviveChance").floatValue <= 0f &&
                            phase.FindPropertyRelative("skills").arraySize == 0 &&
                            phase.FindPropertyRelative("onEnter").arraySize == 0;
            if (!untouched)
                return;
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
        }

        private static void SetList(SerializedProperty list, IReadOnlyList<EnemySkillDefinition> skills)
        {
            list.arraySize = skills.Count;
            for (var i = 0; i < skills.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
        }

        private static EnemySkillDefinition Skill(string folder, string id, string displayName, Color color,
            Action<SerializedObject> fill) =>
            PrototypeContentBuilder.GetOrCreate<EnemySkillDefinition>($"{folder}/Skills/EnemySkill_{id}.asset", so =>
            {
                so.FindProperty("skillId").stringValue = id;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("iconColor").colorValue = color;
                fill(so);
            });

        private static void Use(SerializedObject so, float cooldown, float castTime, float range = 0f,
            CastCondition condition = CastCondition.HasTarget)
        {
            so.FindProperty("cooldown").floatValue = cooldown;
            so.FindProperty("castTime").floatValue = castTime;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("condition").enumValueIndex = (int)condition;
        }

        private static void Move(SerializedObject so, CasterMove move, float distance)
        {
            so.FindProperty("casterMove").enumValueIndex = (int)move;
            so.FindProperty("moveDistance").floatValue = distance;
        }

        private static void Area(SerializedObject so, SkillShape shape, float radius = 1.5f, float length = 5f,
            float width = 1f, float angle = 60f)
        {
            so.FindProperty("shape").enumValueIndex = (int)shape;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("length").floatValue = length;
            so.FindProperty("width").floatValue = width;
            so.FindProperty("angle").floatValue = angle;
        }

        private static void OverTime(SerializedObject so, float zone, float tick, bool follow)
        {
            so.FindProperty("zoneDuration").floatValue = zone;
            so.FindProperty("tickInterval").floatValue = tick;
            so.FindProperty("followCaster").boolValue = follow;
        }

        private static void Enemy(SerializedObject so, EnemySkillAim aim, float telegraph, float initialCooldown,
            float channel = 0f, int zones = 1)
        {
            so.FindProperty("aim").enumValueIndex = (int)aim;
            so.FindProperty("telegraphSeconds").floatValue = telegraph;
            so.FindProperty("initialCooldown").floatValue = initialCooldown;
            so.FindProperty("channelSeconds").floatValue = channel;
            so.FindProperty("zoneCount").intValue = zones;
        }

        private static void Summon(SerializedObject so, TroopDefinition unit, int count)
        {
            so.FindProperty("summonUnit").objectReferenceValue = unit;
            so.FindProperty("summonCount").intValue = count;
            so.FindProperty("summonLimit").intValue = 8;
        }

        private static void Clones(SerializedObject so, int count, float health, float damage)
        {
            so.FindProperty("summonClones").boolValue = true;
            so.FindProperty("summonCount").intValue = count;
            so.FindProperty("cloneHealthFactor").floatValue = health;
            so.FindProperty("cloneDamageFactor").floatValue = damage;
            so.FindProperty("summonRadius").floatValue = 2f;
        }

        private static SkillAreaDefinition.Effect Damage(float fraction) =>
            new SkillAreaDefinition.Effect { kind = SkillEffectKind.Damage, value = fraction };

        private static SkillAreaDefinition.Effect Status(SkillEffectKind kind, float value, float seconds,
            SkillAffects affects = SkillAffects.Enemies) =>
            new SkillAreaDefinition.Effect { kind = kind, affects = affects, value = value, duration = seconds };

        private static void Effects(SerializedObject so, params SkillAreaDefinition.Effect[] effects)
        {
            var list = so.FindProperty("effects");
            list.arraySize = effects.Length;
            for (var i = 0; i < effects.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("kind").enumValueIndex = (int)effects[i].kind;
                element.FindPropertyRelative("affects").enumValueIndex = (int)effects[i].affects;
                element.FindPropertyRelative("value").floatValue = effects[i].value;
                element.FindPropertyRelative("duration").floatValue = effects[i].duration;
            }
        }
    }
}

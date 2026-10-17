using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Creates the skill assets of the 4 lords and the 4 starter heroes (docs/Tuong/LordHeroes.md §2,
    /// docs/Tuong/NormalHeroes.md §2; numbers are the drafts there, balance comes later) and gives each hero its four
    /// skills when it has none yet. Like the rest of the builder it only creates what is missing: delete a skill asset to
    /// rebuild it, or clear a hero's skills to reassign them.
    /// </summary>
    internal static class SkillContentBuilder
    {
        private const string SkillsRoot = PrototypeContentBuilder.ContentRoot + "/Skills";

        /// <summary>A skill as plain data; written into a SkillDefinition asset by <see cref="Create"/>.</summary>
        private sealed class Spec
        {
            public string Id;
            public string Name;
            public SkillSlot Slot;
            public string Description = "";
            public float Cooldown;
            public float CastTime = 0.3f;
            public SkillTargeting Targeting = SkillTargeting.CurrentTarget;
            public float Range;
            public CastCondition Condition = CastCondition.HasTarget;
            public CasterMove Move = CasterMove.None;
            public float MoveDistance;
            public bool CastWhileMoving;
            public SkillShape Shape = SkillShape.Single;
            public float Radius = 1.5f;
            public float Length = 5f;
            public float Width = 1f;
            public float Angle = 60f;
            public bool FirstHitOnly;
            public float CastleMultiplier = 1f;
            public float Zone;
            public float Tick = 0.5f;
            public bool Follow;
            public int Repeat = 1;
            public float RepeatInterval = 0.25f;
            public float FinalHit = 1f;
            public SkillDefinition.Effect[] Effects = new SkillDefinition.Effect[0];
            public SkillDefinition.Passive[] Passives = new SkillDefinition.Passive[0];
        }

        public static void Build()
        {
            PrototypeContentBuilder.EnsureFolder(SkillsRoot);
            var lordArcher = Set("Lord_Archer", new Color(0.36f, 0.67f, 0.54f), LordArcher());
            var lordSwordsman = Set("Lord_Swordsman", new Color(0.62f, 0.72f, 0.9f), LordSwordsman());
            var lordStrategist = Set("Lord_Strategist", new Color(0.73f, 0.64f, 0.84f), LordStrategist());
            var lordMarshal = Set("Lord_Marshal", new Color(0.85f, 0.32f, 0.27f), LordMarshal());
            var marksman = Set("Hero_Archer", new Color(0.35f, 0.85f, 0.4f), Marksman());
            var cavalry = Set("Hero_Cavalry", new Color(0.6f, 0.45f, 1f), Cavalry());
            var guardian = Set("Hero_Guardian", new Color(0.3f, 0.8f, 0.95f), Guardian());
            var blademaster = Set("Hero_Swordsman", new Color(0.95f, 0.35f, 0.3f), Blademaster());

            var root = PrototypeContentBuilder.ContentRoot;
            Assign($"{root}/Lords/Lord_Archer.asset", lordArcher);
            Assign($"{root}/Lords/Lord_Swordsman.asset", lordSwordsman);
            Assign($"{root}/Lords/Lord_Strategist.asset", lordStrategist);
            Assign($"{root}/Lords/Lord_Marshal.asset", lordMarshal);
            // The prototype's placeholder lord borrows the swordsman lord's set.
            Assign($"{root}/Heroes/Hero_Lord.asset", lordSwordsman);
            // Starter heroes ↔ docs/Tuong/NormalHeroes.md (names: spec §13 conflict 4, still open).
            Assign($"{root}/Heroes/Hero_Archer.asset", marksman);
            Assign($"{root}/Heroes/Hero_Cavalry.asset", cavalry);
            Assign($"{root}/Heroes/Hero_Guardian.asset", guardian);
            Assign($"{root}/Heroes/Hero_Swordsman.asset", blademaster);
        }

        // ---- Lords (docs/Tuong/LordHeroes.md §2) ----

        private static Spec[] LordArcher() => new[]
        {
            Passive("ung_nhan", "Ưng Nhãn", "+15 % tầm đánh; mỗi đòn thứ 4 xuyên, trúng thêm 1 địch phía sau.",
                new SkillDefinition.Passive { kind = PassiveKind.AttackRangeBonus, value = 0.15f },
                new SkillDefinition.Passive { kind = PassiveKind.PierceEveryNthAttack, count = 4 }),
            new Spec
            {
                Id = "lien_chau_tien", Name = "Liên Châu Tiễn", Slot = SkillSlot.Skill2, Cooldown = 8f,
                Description = "3 mũi tên liên tiếp vào mục tiêu đang đánh, mỗi mũi 80 % sát thương.",
                Repeat = 3, RepeatInterval = 0.25f, Effects = new[] { Damage(0.8f) }
            },
            new Spec
            {
                Id = "tien_vu", Name = "Tiễn Vũ", Slot = SkillSlot.Skill3, Cooldown = 14f,
                Description = "Mưa tên vùng tròn bán kính 2.5 trong 3 giây.",
                Targeting = SkillTargeting.Point, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Circle,
                Radius = 2.5f, Zone = 3f, Tick = 0.5f, Effects = new[] { Damage(0.35f) }
            },
            new Spec
            {
                Id = "pha_thanh_hoa_tien", Name = "Phá Thành Hỏa Tiễn", Slot = SkillSlot.Ultimate, CastTime = 0.5f,
                Description = "Mũi tên lửa bay thẳng, xuyên mọi quân trên đường; trúng thành ×3.",
                Targeting = SkillTargeting.Direction, Range = 10f, Shape = SkillShape.Line, Length = 12f, Width = 1f,
                CastleMultiplier = 3f, Effects = new[] { Damage(3f) }
            }
        };

        private static Spec[] LordSwordsman() => new[]
        {
            Passive("kiem_tam", "Kiếm Tâm", "Hồi 3 % sát thương gây ra; dưới 30 % máu +25 % tốc đánh.",
                new SkillDefinition.Passive { kind = PassiveKind.Lifesteal, value = 0.03f },
                new SkillDefinition.Passive { kind = PassiveKind.LowHealthAttackSpeed, value = 0.25f, threshold = 0.3f }),
            new Spec
            {
                Id = "phi_anh_dot_kich", Name = "Phi Ảnh Đột Kích", Slot = SkillSlot.Skill2, Cooldown = 10f,
                Description = "Lướt tới mục tiêu trong bán kính 6, sát thương và choáng 1 giây.",
                Range = 6f, Move = CasterMove.DashToTarget,
                Effects = new[] { Damage(1.5f), Status(SkillEffectKind.Stun, 1f, 1f) }
            },
            new Spec
            {
                Id = "toan_phong_tram", Name = "Toàn Phong Trảm", Slot = SkillSlot.Skill3, Cooldown = 12f,
                Description = "Xoay song kiếm 1.5 giây, sát thương liên tục bán kính 1.5.",
                Targeting = SkillTargeting.Self, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Circle,
                Radius = 1.5f, Zone = 1.5f, Tick = 0.25f, Follow = true, Effects = new[] { Damage(0.3f) }
            },
            new Spec
            {
                Id = "van_kiem_quy_tong", Name = "Vạn Kiếm Quy Tông", Slot = SkillSlot.Ultimate, CastTime = 0.5f,
                Description = "Kiếm khí rơi xuống vùng quạt phía trước dài 5, sát thương lớn.",
                Targeting = SkillTargeting.Direction, Range = 5f, Shape = SkillShape.Cone, Length = 5f, Angle = 70f,
                Effects = new[] { Damage(3f) }
            }
        };

        private static Spec[] LordStrategist() => new[]
        {
            Passive("thao_luoc", "Thao Lược", "Lính phe ta trong bán kính 5 +10 % sát thương.",
                new SkillDefinition.Passive { kind = PassiveKind.AllyTroopDamageAura, value = 0.1f, radius = 5f }),
            new Spec
            {
                Id = "hoa_ke", Name = "Hỏa Kế", Slot = SkillSlot.Skill2, Cooldown = 12f,
                Description = "Trận lửa bán kính 2 trong 5 giây.",
                Targeting = SkillTargeting.Point, Range = 6f, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Circle,
                Radius = 2f, Zone = 5f, Tick = 0.5f, Effects = new[] { Damage(0.25f) }
            },
            new Spec
            {
                Id = "bat_quai_tran", Name = "Bát Quái Trận", Slot = SkillSlot.Skill3, Cooldown = 16f,
                Description = "Trận đồ bán kính 3 trong 6 giây: địch chậm 40 %, giảm 20 % sát thương.",
                Targeting = SkillTargeting.Point, Range = 6f, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Circle,
                Radius = 3f, Zone = 6f, Tick = 0.5f,
                Effects = new[] { Status(SkillEffectKind.Slow, 0.4f, 0f), Status(SkillEffectKind.Weaken, 0.2f, 0f) }
            },
            new Spec
            {
                Id = "dong_phong", Name = "Đông Phong", Slot = SkillSlot.Ultimate, CastTime = 0.5f,
                Description = "Gió quét cả chiến trường: địch bị đẩy lùi 3, quân ta +30 % tốc chạy 6 giây.",
                Targeting = SkillTargeting.Global, Shape = SkillShape.Global,
                Effects = new[]
                {
                    Status(SkillEffectKind.Knockback, 3f, 0f),
                    Status(SkillEffectKind.MoveSpeedUp, 0.3f, 6f, SkillAffects.Allies)
                }
            }
        };

        private static Spec[] LordMarshal() => new[]
        {
            Passive("quan_lenh_nhu_son", "Quân Lệnh Như Sơn", "Đội lính +2 lính; toàn quân +5 % máu tối đa.",
                new SkillDefinition.Passive { kind = PassiveKind.ExtraSoldiers, count = 2 },
                new SkillDefinition.Passive { kind = PassiveKind.ArmyMaxHealth, value = 0.05f }),
            new Spec
            {
                Id = "hieu_trieu", Name = "Hiệu Triệu", Slot = SkillSlot.Skill2, Cooldown = 15f,
                Description = "Bổ sung tối đa 3 lính đã chết cho đội gần nhất.",
                Targeting = SkillTargeting.AllyNeedingSoldiers, Range = 8f,
                Effects = new[] { Status(SkillEffectKind.RefillSquad, 3f, 0f, SkillAffects.Allies) }
            },
            new Spec
            {
                Id = "trong_tran", Name = "Trống Trận", Slot = SkillSlot.Skill3, Cooldown = 14f,
                Description = "Quân ta bán kính 6: +20 % tốc đánh và tốc chạy 6 giây.",
                Targeting = SkillTargeting.Self, Condition = CastCondition.EnemyInAggroRange, Shape = SkillShape.Circle,
                Radius = 6f,
                Effects = new[]
                {
                    Status(SkillEffectKind.AttackSpeedUp, 0.2f, 6f, SkillAffects.Allies),
                    Status(SkillEffectKind.MoveSpeedUp, 0.2f, 6f, SkillAffects.Allies)
                }
            },
            new Spec
            {
                Id = "thiet_ky_xung_phong", Name = "Thiết Kỵ Xung Phong", Slot = SkillSlot.Ultimate, CastTime = 0.5f,
                Description = "Hàng kỵ binh lao qua đường thẳng dài 12, sát thương và hất văng.",
                Targeting = SkillTargeting.Direction, Range = 10f, Shape = SkillShape.Line, Length = 12f, Width = 1.5f,
                Effects = new[] { Damage(2f), Status(SkillEffectKind.Knockback, 1.5f, 0f) }
            }
        };

        // ---- Starter heroes (docs/Tuong/NormalHeroes.md §2) ----

        private static Spec[] Marksman() => new[]
        {
            Passive("liep_sat", "Liệp Sát", "Mục tiêu dưới 40 % máu nhận thêm 30 % sát thương.",
                new SkillDefinition.Passive { kind = PassiveKind.LowHealthTargetBonus, value = 0.3f, threshold = 0.4f }),
            new Spec
            {
                Id = "doc_tien", Name = "Độc Tiễn", Slot = SkillSlot.Skill2, Cooldown = 9f,
                Description = "120 % sát thương + độc 4 giây (15 %/giây) + chậm 20 %.",
                Effects = new[]
                {
                    Damage(1.2f), Status(SkillEffectKind.Poison, 0.15f, 4f), Status(SkillEffectKind.Slow, 0.2f, 4f)
                }
            },
            new Spec
            {
                Id = "ung_kich", Name = "Ưng Kích", Slot = SkillSlot.Skill3, Cooldown = 13f,
                Description = "Chim ưng lao vào mục tiêu trong tầm 6: 150 % sát thương, −30 % tốc đánh 4 giây.",
                Range = 6f, Effects = new[] { Damage(1.5f), Status(SkillEffectKind.AttackSpeedDown, 0.3f, 4f) }
            },
            new Spec
            {
                Id = "xuyen_duong_tien", Name = "Xuyên Dương Tiễn", Slot = SkillSlot.Ultimate, CastTime = 0.5f,
                Description = "Mũi tên bay thẳng dài 9, trúng địch đầu tiên: 400 % sát thương (thành 200 %).",
                Targeting = SkillTargeting.Direction, Range = 9f, Shape = SkillShape.Line, Length = 9f, Width = 0.8f,
                FirstHitOnly = true, CastleMultiplier = 0.5f, Effects = new[] { Damage(4f) }
            }
        };

        private static Spec[] Cavalry() => new[]
        {
            Passive("xung_phong", "Xung Phong", "Chạy liên tục ≥ 2 giây: đòn đầu 200 % sát thương, choáng 0.5 giây.",
                new SkillDefinition.Passive { kind = PassiveKind.ChargedFirstHit, value = 2f, secondValue = 0.5f, threshold = 2f }),
            new Spec
            {
                Id = "ma_dap", Name = "Mã Đạp", Slot = SkillSlot.Skill2, Cooldown = 10f,
                Description = "Vòng bán kính 1.5 quanh mình: 120 % sát thương + choáng 0.8 giây.",
                Targeting = SkillTargeting.Self, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Circle, Radius = 1.5f,
                Effects = new[] { Damage(1.2f), Status(SkillEffectKind.Stun, 1f, 0.8f) }
            },
            new Spec
            {
                Id = "phong_ma", Name = "Phong Mã", Slot = SkillSlot.Skill3, Cooldown = 14f,
                Description = "Bản thân +40 % tốc chạy, +20 % sát thương 5 giây.",
                Targeting = SkillTargeting.Self, Condition = CastCondition.EnemyInAggroRange, Shape = SkillShape.Circle,
                Radius = 0.5f, CastTime = 0.1f,
                Effects = new[]
                {
                    Status(SkillEffectKind.MoveSpeedUp, 0.4f, 5f, SkillAffects.Self),
                    Status(SkillEffectKind.DamageUp, 0.2f, 5f, SkillAffects.Self)
                }
            },
            new Spec
            {
                Id = "thien_quan_dot_kich", Name = "Thiên Quân Đột Kích", Slot = SkillSlot.Ultimate, CastTime = 0.4f,
                Description = "Phi ngựa lao thẳng dài 8: 250 % sát thương, hất văng 1.5.",
                Targeting = SkillTargeting.Direction, Range = 8f, Move = CasterMove.ChargeLine, MoveDistance = 8f,
                Shape = SkillShape.Line, Length = 8f, Width = 1.4f,
                Effects = new[] { Damage(2.5f), Status(SkillEffectKind.Knockback, 1.5f, 0f) }
            }
        };

        private static Spec[] Guardian() => new[]
        {
            Passive("kien_thu", "Kiên Thủ", "Giảm 15 % sát thương nhận vào; dưới 50 % máu giảm 30 %.",
                new SkillDefinition.Passive { kind = PassiveKind.DamageReduction, value = 0.15f, secondValue = 0.3f, threshold = 0.5f }),
            new Spec
            {
                Id = "thuan_kich", Name = "Thuẫn Kích", Slot = SkillSlot.Skill2, Cooldown = 9f,
                Description = "Đập khiên: 150 % sát thương + choáng 1 giây.",
                Effects = new[] { Damage(1.5f), Status(SkillEffectKind.Stun, 1f, 1f) }
            },
            new Spec
            {
                Id = "ho_ve_tran", Name = "Hộ Vệ Trận", Slot = SkillSlot.Skill3, Cooldown = 15f,
                Description = "Quân ta bán kính 3 nhận khiên 10 % máu tối đa của Giáp Sĩ, 5 giây.",
                Targeting = SkillTargeting.Self, Condition = CastCondition.EnemyInAggroRange, Shape = SkillShape.Circle, Radius = 3f,
                Effects = new[] { Status(SkillEffectKind.Shield, 0.1f, 5f, SkillAffects.Allies) }
            },
            new Spec
            {
                Id = "kim_thanh_thiet_bich", Name = "Kim Thành Thiết Bích", Slot = SkillSlot.Ultimate, CastTime = 0.4f,
                Description = "Vùng bảo vệ bán kính 3.5 trong 6 giây: quân ta −50 % sát thương nhận vào, miễn khống chế.",
                Targeting = SkillTargeting.Self, Range = 4f, Shape = SkillShape.Circle, Radius = 3.5f, Zone = 6f, Tick = 0.5f,
                Effects = new[]
                {
                    Status(SkillEffectKind.DamageTakenDown, 0.5f, 0f, SkillAffects.Allies),
                    Status(SkillEffectKind.CrowdControlImmunity, 1f, 0f, SkillAffects.Allies)
                }
            }
        };

        private static Spec[] Blademaster() => new[]
        {
            Passive("lien_tram", "Liên Trảm", "Đánh liên tiếp cùng mục tiêu +6 % sát thương mỗi đòn, tối đa 5 lần.",
                new SkillDefinition.Passive { kind = PassiveKind.ComboStacks, value = 0.06f, count = 5 }),
            new Spec
            {
                Id = "bat_dao_tram", Name = "Bạt Đao Trảm", Slot = SkillSlot.Skill2, Cooldown = 8f,
                Description = "Nhát chém dồn lực: 250 % sát thương.", Effects = new[] { Damage(2.5f) }
            },
            new Spec
            {
                Id = "dao_khi", Name = "Đao Khí", Slot = SkillSlot.Skill3, Cooldown = 11f,
                Description = "Lưỡi đao khí bay thẳng dài 5, xuyên mọi địch: 140 % sát thương.",
                Targeting = SkillTargeting.Direction, Range = 5f, Condition = CastCondition.EnemyInArea, Shape = SkillShape.Line,
                Length = 5f, Width = 0.8f, Effects = new[] { Damage(1.4f) }
            },
            new Spec
            {
                Id = "cuong_dao_luc_tram", Name = "Cuồng Đao Lục Trảm", Slot = SkillSlot.Ultimate, CastTime = 0.2f,
                Description = "6 nhát trong 1.5 giây vào mọi địch bán kính 1.8 (5 × 80 %, nhát cuối 250 %); miễn khống chế.",
                Targeting = SkillTargeting.Self, Shape = SkillShape.Circle, Radius = 1.8f,
                Repeat = 6, RepeatInterval = 0.26f, FinalHit = 2.5f / 0.8f,
                Effects = new[]
                {
                    Status(SkillEffectKind.CrowdControlImmunity, 1f, 1.6f, SkillAffects.Self),
                    Damage(0.8f)
                }
            }
        };

        // ---- Helpers ----

        private static Spec Passive(string id, string name, string description, params SkillDefinition.Passive[] passives) =>
            new Spec { Id = id, Name = name, Slot = SkillSlot.Passive, Description = description, Passives = passives };

        private static SkillDefinition.Effect Damage(float fraction) =>
            new SkillDefinition.Effect { kind = SkillEffectKind.Damage, value = fraction };

        private static SkillDefinition.Effect Status(SkillEffectKind kind, float value, float seconds,
            SkillAffects affects = SkillAffects.Enemies) =>
            new SkillDefinition.Effect { kind = kind, affects = affects, value = value, duration = seconds };

        private static SkillDefinition[] Set(string owner, Color color, Spec[] specs)
        {
            var folder = $"{SkillsRoot}/{owner}";
            PrototypeContentBuilder.EnsureFolder(folder);
            var set = new SkillDefinition[4];
            foreach (var spec in specs)
            {
                // Icons: placeholder colours from the hero's, a little lighter for each slot.
                var tint = Color.Lerp(color, Color.white, 0.15f * (int)spec.Slot);
                set[(int)spec.Slot] = Create($"{folder}/Skill_{spec.Id}.asset", spec, tint);
            }
            return set;
        }

        private static SkillDefinition Create(string path, Spec spec, Color color) =>
            PrototypeContentBuilder.GetOrCreate<SkillDefinition>(path, so =>
            {
                so.FindProperty("skillId").stringValue = spec.Id;
                so.FindProperty("displayName").stringValue = spec.Name;
                so.FindProperty("slot").enumValueIndex = (int)spec.Slot;
                so.FindProperty("description").stringValue = spec.Description;
                so.FindProperty("iconColor").colorValue = color;
                so.FindProperty("cooldown").floatValue = spec.Cooldown;
                so.FindProperty("castTime").floatValue = spec.CastTime;
                so.FindProperty("targeting").enumValueIndex = (int)spec.Targeting;
                so.FindProperty("range").floatValue = spec.Range;
                so.FindProperty("condition").enumValueIndex = (int)spec.Condition;
                so.FindProperty("casterMove").enumValueIndex = (int)spec.Move;
                so.FindProperty("moveDistance").floatValue = spec.MoveDistance;
                so.FindProperty("autoCastWhileMoving").boolValue = spec.CastWhileMoving;
                so.FindProperty("shape").enumValueIndex = (int)spec.Shape;
                so.FindProperty("radius").floatValue = spec.Radius;
                so.FindProperty("length").floatValue = spec.Length;
                so.FindProperty("width").floatValue = spec.Width;
                so.FindProperty("angle").floatValue = spec.Angle;
                so.FindProperty("firstHitOnly").boolValue = spec.FirstHitOnly;
                so.FindProperty("castleMultiplier").floatValue = spec.CastleMultiplier;
                so.FindProperty("zoneDuration").floatValue = spec.Zone;
                so.FindProperty("tickInterval").floatValue = spec.Tick;
                so.FindProperty("followCaster").boolValue = spec.Follow;
                so.FindProperty("repeatCount").intValue = spec.Repeat;
                so.FindProperty("repeatInterval").floatValue = spec.RepeatInterval;
                so.FindProperty("finalHitMultiplier").floatValue = spec.FinalHit;

                var effects = so.FindProperty("effects");
                effects.arraySize = spec.Effects.Length;
                for (var i = 0; i < spec.Effects.Length; i++)
                {
                    var element = effects.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("kind").enumValueIndex = (int)spec.Effects[i].kind;
                    element.FindPropertyRelative("affects").enumValueIndex = (int)spec.Effects[i].affects;
                    element.FindPropertyRelative("value").floatValue = spec.Effects[i].value;
                    element.FindPropertyRelative("duration").floatValue = spec.Effects[i].duration;
                }

                var passives = so.FindProperty("passives");
                passives.arraySize = spec.Passives.Length;
                for (var i = 0; i < spec.Passives.Length; i++)
                {
                    var element = passives.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("kind").enumValueIndex = (int)spec.Passives[i].kind;
                    element.FindPropertyRelative("value").floatValue = spec.Passives[i].value;
                    element.FindPropertyRelative("secondValue").floatValue = spec.Passives[i].secondValue;
                    element.FindPropertyRelative("threshold").floatValue = spec.Passives[i].threshold;
                    element.FindPropertyRelative("radius").floatValue = spec.Passives[i].radius;
                    element.FindPropertyRelative("count").intValue = spec.Passives[i].count;
                }
            });

        /// <summary>Gives a hero its four skills, unless it already has some (a designer's choice is kept).</summary>
        private static void Assign(string heroPath, IReadOnlyList<SkillDefinition> set)
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroPath);
            if (hero == null)
                return;
            var so = new SerializedObject(hero);
            var skills = so.FindProperty("skills");
            for (var i = 0; i < skills.arraySize; i++)
            {
                if (skills.GetArrayElementAtIndex(i).objectReferenceValue != null)
                    return;
            }
            skills.arraySize = set.Count;
            for (var i = 0; i < set.Count; i++)
                skills.GetArrayElementAtIndex(i).objectReferenceValue = set[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hero);
            Debug.Log($"[Arena3Q] {hero.name}: skills assigned.");
        }
    }
}

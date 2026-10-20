using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Arena3Q > Lu Bu > Build Skills: Lữ Bố's four skills from
    /// docs/art/concepts/LuBu_Arena3Q/Arena3Q_LuBu_4Skills_VFX_Production_Spec_v1_0.md §9.1, applied to Arena3Q's rules
    /// (skills 2 and 3 auto-cast, the ultimate on the rage bar, unlocked by stars). The spec's settled numbers are used
    /// as they are; the ones it leaves open (sweep area, charge distance, ultimate radius and timings) are 🟡 defaults
    /// in the assets, to tune there. Rebuilding overwrites the four assets.
    /// </summary>
    public static class LuBuSkillsBuilder
    {
        private const string Folder = "Assets/Arena3Q/Content/Skills/LuBu";
        private const string HeroPath = "Assets/Arena3Q/Content/Heroes/Hero_LuBu.asset";

        [MenuItem("Arena3Q/Lu Bu/Build Skills")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var passive = Skill("LuBu_0_ChienThanCuongHuyet", s =>
            {
                Identity(s, "lubu_cuong_huyet", "Chiến Thần Cuồng Huyết",
                    "Mỗi đòn đánh thường hoặc kỹ năng trúng địch: +1 tầng Cuồng Huyết (tối đa 5), mỗi tầng +3% sát thương. " +
                    "Đủ 5 tầng: hút máu 15% và miễn làm chậm. Tầng giữ 5 giây, làm mới khi gây sát thương.",
                    new Color(0.67f, 0.13f, 0.19f));
                s.FindProperty("slot").enumValueIndex = (int)SkillSlot.Passive;
                s.FindProperty("cooldown").floatValue = 0f;
                var passives = s.FindProperty("passives");
                passives.arraySize = 1;
                var p = passives.GetArrayElementAtIndex(0);
                p.FindPropertyRelative("kind").enumValueIndex = (int)PassiveKind.FrenzyStacks;
                p.FindPropertyRelative("count").intValue = 5;
                p.FindPropertyRelative("value").floatValue = 0.03f;
                p.FindPropertyRelative("secondValue").floatValue = 0.15f;
                p.FindPropertyRelative("threshold").floatValue = 5f;
            });

            var sweep = Skill("LuBu_1_PhuongThienTram", s =>
            {
                Identity(s, "lubu_phuong_thien_tram", "Phương Thiên Trảm",
                    "Quét Phương Thiên Họa Kích một vòng phía trước: 160% sát thương, làm chậm 30% trong 2 giây. " +
                    "Trúng từ 2 địch: thêm 1 tầng Cuồng Huyết.", new Color(0.9f, 0.3f, 0.31f));
                s.FindProperty("slot").enumValueIndex = (int)SkillSlot.Skill2;
                s.FindProperty("targeting").enumValueIndex = (int)SkillTargeting.Direction;
                s.FindProperty("cooldown").floatValue = 8f;
                s.FindProperty("castTime").floatValue = 17f / 30f; // EVENT Skill01_Hit at frame 17 of 36
                s.FindProperty("range").floatValue = 2.4f;
                s.FindProperty("condition").enumValueIndex = (int)CastCondition.EnemyInArea;
                s.FindProperty("shape").enumValueIndex = (int)SkillShape.Cone;
                s.FindProperty("length").floatValue = 2.4f;  // 🟡 spec: radius TBD
                s.FindProperty("angle").floatValue = 150f;   // 🟡 spec: sector angle TBD ("quét một vòng phía trước")
                Effects(s,
                    (SkillEffectKind.Damage, SkillAffects.Enemies, 1.6f, 0f, 0),
                    (SkillEffectKind.Slow, SkillAffects.Enemies, 0.3f, 2f, 0),
                    (SkillEffectKind.FrenzyStacks, SkillAffects.Self, 1f, 0f, 2));
            });

            var charge = Skill("LuBu_2_XichThoXungPhong", s =>
            {
                Identity(s, "lubu_xich_tho_xung_phong", "Xích Thố Xung Phong",
                    "Lao thẳng về phía trước cùng bóng Xích Thố: 130% sát thương lên địch trên đường lao, hất tung 0,8 giây. " +
                    "Kết thúc lao nhận khiên bằng 12% máu tối đa trong 3 giây.", new Color(0.97f, 0.45f, 0.2f));
                s.FindProperty("slot").enumValueIndex = (int)SkillSlot.Skill3;
                s.FindProperty("targeting").enumValueIndex = (int)SkillTargeting.Direction;
                s.FindProperty("cooldown").floatValue = 12f;
                s.FindProperty("castTime").floatValue = 14f / 30f; // EVENT Charge_Start at frame 14 of 45
                s.FindProperty("range").floatValue = 4.5f;
                s.FindProperty("condition").enumValueIndex = (int)CastCondition.HasTarget;
                s.FindProperty("casterMove").enumValueIndex = (int)CasterMove.ChargeLine;
                s.FindProperty("moveDistance").floatValue = 4.5f; // 🟡 spec: dash range TBD
                s.FindProperty("shape").enumValueIndex = (int)SkillShape.Line;
                s.FindProperty("width").floatValue = 1.2f;
                Effects(s,
                    (SkillEffectKind.Damage, SkillAffects.Enemies, 1.3f, 0f, 0),
                    (SkillEffectKind.KnockUp, SkillAffects.Enemies, 0f, 0.8f, 0),
                    (SkillEffectKind.Shield, SkillAffects.Self, 0.12f, 3f, 0));
            });

            var ultimate = Skill("LuBu_3_ThienHaVoSong", s =>
            {
                Identity(s, "lubu_thien_ha_vo_song", "Thiên Hạ Vô Song",
                    "Tuyệt kỹ: nhảy lên, giáng Phương Thiên Họa Kích xuống đất: 300% sát thương diện rộng, hất tung 1 giây; " +
                    "địch dưới 30% máu chịu thêm 25% sát thương từ chiêu này. Sau chiêu nhận đủ 5 tầng Cuồng Huyết.",
                    new Color(0.98f, 0.9f, 0.84f));
                s.FindProperty("slot").enumValueIndex = (int)SkillSlot.Ultimate;
                s.FindProperty("targeting").enumValueIndex = (int)SkillTargeting.Point;
                s.FindProperty("cooldown").floatValue = 0f;
                s.FindProperty("castTime").floatValue = 55f / 30f; // EVENT Ultimate_Impact at frame 55 of 90
                s.FindProperty("range").floatValue = 5f;
                s.FindProperty("condition").enumValueIndex = (int)CastCondition.HasTarget;
                s.FindProperty("casterMove").enumValueIndex = (int)CasterMove.LeapToPoint;
                s.FindProperty("moveDistance").floatValue = 5f;
                s.FindProperty("shape").enumValueIndex = (int)SkillShape.Circle;
                s.FindProperty("radius").floatValue = 2.5f; // 🟡 spec: radius TBD
                s.FindProperty("executeThreshold").floatValue = 0.3f;
                s.FindProperty("executeBonus").floatValue = 0.25f;
                Effects(s,
                    (SkillEffectKind.Damage, SkillAffects.Enemies, 3f, 0f, 0),
                    (SkillEffectKind.KnockUp, SkillAffects.Enemies, 0f, 1f, 0),
                    (SkillEffectKind.FrenzyStacks, SkillAffects.Self, 5f, 0f, 0));
            });

            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(HeroPath);
            if (hero != null)
            {
                var so = new SerializedObject(hero);
                var skills = so.FindProperty("skills");
                skills.arraySize = 4;
                skills.GetArrayElementAtIndex(0).objectReferenceValue = passive;
                skills.GetArrayElementAtIndex(1).objectReferenceValue = sweep;
                skills.GetArrayElementAtIndex(2).objectReferenceValue = charge;
                skills.GetArrayElementAtIndex(3).objectReferenceValue = ultimate;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LuBu] Skills built: Chiến Thần Cuồng Huyết, Phương Thiên Trảm, Xích Thố Xung Phong, Thiên Hạ Vô Song.");
        }

        private static SkillDefinition Skill(string file, System.Action<SerializedObject> fill)
        {
            var path = $"{Folder}/{file}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }
            var so = new SerializedObject(skill);
            fill(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void Identity(SerializedObject s, string id, string name, string description, Color color)
        {
            s.FindProperty("skillId").stringValue = id;
            s.FindProperty("displayName").stringValue = name;
            s.FindProperty("description").stringValue = description;
            s.FindProperty("iconColor").colorValue = color;
        }

        private static void Effects(SerializedObject s,
            params (SkillEffectKind kind, SkillAffects affects, float value, float duration, int minimumTargets)[] effects)
        {
            var list = s.FindProperty("effects");
            list.arraySize = effects.Length;
            for (var i = 0; i < effects.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("kind").enumValueIndex = (int)effects[i].kind;
                e.FindPropertyRelative("affects").enumValueIndex = (int)effects[i].affects;
                e.FindPropertyRelative("value").floatValue = effects[i].value;
                e.FindPropertyRelative("duration").floatValue = effects[i].duration;
                e.FindPropertyRelative("minimumTargets").intValue = effects[i].minimumTargets;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>The shipped skills: every lord and starter hero has a valid passive, skills 2, 3 and an ultimate.</summary>
    public class SkillContentTests
    {
        private const string ContentRoot = "Assets/Arena3Q/Content";

        private static List<T> LoadAll<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ContentRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();

        [Test]
        public void EveryHero_HasFourSkillsInTheRightSlots()
        {
            var heroes = LoadAll<HeroDefinition>();
            Assert.That(heroes, Is.Not.Empty);
            foreach (var hero in heroes)
            {
                foreach (SkillSlot slot in System.Enum.GetValues(typeof(SkillSlot)))
                {
                    var skill = hero.GetSkill(slot);
                    Assert.That(skill, Is.Not.Null, $"{hero.name} has a {slot}");
                    Assert.That(skill.Slot, Is.EqualTo(slot), $"{hero.name}: {skill.name}");
                }
                var errors = new List<string>();
                hero.Validate(errors);
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
        }

        [Test]
        public void Skills_AreValid_AndIdsAreUnique()
        {
            var skills = LoadAll<SkillDefinition>();
            Assert.That(skills, Has.Count.EqualTo(32), "4 lords + 4 starter heroes × 4 skills");
            var errors = new List<string>();
            foreach (var skill in skills)
                skill.Validate(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(skills.Select(s => s.SkillId).Distinct().Count(), Is.EqualTo(skills.Count));
        }

        [Test]
        public void SkillValidation_CatchesMissingCooldownAndEffects()
        {
            var skill = UnityEngine.ScriptableObject.CreateInstance<SkillDefinition>();
            try
            {
                var so = new SerializedObject(skill);
                so.FindProperty("cooldown").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                var errors = new List<string>();
                skill.Validate(errors);
                Assert.That(errors, Has.Some.Contains("cooldown"));
                Assert.That(errors, Has.Some.Contains("no effects"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(skill);
            }
        }
    }
}

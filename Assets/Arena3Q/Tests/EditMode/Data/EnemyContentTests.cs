using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>The shipped enemies of chapters 1–3 (docs/QuanDich) and the boss stages (spec §8).</summary>
    public class EnemyContentTests
    {
        private const string ContentRoot = "Assets/Arena3Q/Content";

        private static List<T> LoadAll<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ContentRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .ToList();

        private static EnemyDefinition Enemy(string id) => LoadAll<EnemyDefinition>().Single(e => e.TroopId == id);

        [Test]
        public void Enemies_AreValid_WithThreeBossesAndSixElites()
        {
            var enemies = LoadAll<EnemyDefinition>();
            var errors = new List<string>();
            foreach (var enemy in enemies)
                enemy.Validate(errors);
            foreach (var skill in LoadAll<EnemySkillDefinition>())
                skill.Validate(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));

            Assert.That(enemies.Count(e => e.Tier == EnemyTier.Boss), Is.EqualTo(3));
            Assert.That(enemies.Count(e => e.Tier == EnemyTier.Elite), Is.EqualTo(6));
            Assert.That(enemies.Count(e => e.Tier == EnemyTier.Normal), Is.EqualTo(12));
            Assert.That(enemies.Select(e => e.TroopId).Distinct().Count(), Is.EqualTo(enemies.Count));
        }

        [Test]
        public void Enemies_AtTheirChapterLevel_HaveTheChapterFileNumbers()
        {
            // docs/QuanDich/README.md §1: numbers are given at the chapter's first level (1, 10, 19), +3 % per level.
            var rules = new ProgressionRules(new HeroProgressionSettings());
            void Check(string id, int level, float health, float damage)
            {
                var enemy = Enemy(id);
                var multiplier = rules.EnemyMultiplier(level);
                Assert.That(enemy.Stats.MaxHealth * multiplier, Is.EqualTo(health).Within(health * 0.01f), id);
                Assert.That(enemy.Stats.Damage * multiplier, Is.EqualTo(damage).Within(damage * 0.01f + 0.1f), id);
            }
            Check("c1_boss", 1, 6000f, 70f);
            Check("c2_boss", 10, 8000f, 60f);
            Check("c3_boss", 19, 12000f, 80f);
            Check("c3_imp", 19, 90f, 11f);
        }

        [Test]
        public void Ranks_HaveTheirBodySize_AndTraits()
        {
            foreach (var enemy in LoadAll<EnemyDefinition>())
            {
                var expected = enemy.Tier == EnemyTier.Boss ? 0.8f : enemy.Tier == EnemyTier.Elite ? 0.4f : 0.32f;
                Assert.That(enemy.BodyRadius, Is.EqualTo(expected).Within(0.021f), enemy.name);
            }
            Assert.That(Enemy("c3_wisp").Flying, Is.True, "Quỷ Hoả flies (spec §8)");
            Assert.That(Enemy("c3_jiangshi").ReviveChance, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(Enemy("c1_boss").Phase.attackSpeedBonus, Is.EqualTo(0.3f).Within(1e-4f), "Cuồng Nộ");
            Assert.That(Enemy("c2_boss").Phase.onEnter, Has.Length.EqualTo(2), "storm aura + shield");
        }

        [Test]
        public void BossStages_PlaceTheBossAsSpecified()
        {
            var stages = LoadAll<StageConfig>();
            var attack = stages.Single(s => s.StageId == "c1_boss_attack");
            Assert.That(attack.StageType, Is.EqualTo(StageType.Attack));
            Assert.That(attack.Defenders.Count(d => d.Unit is EnemyDefinition e && e.Tier == EnemyTier.Boss), Is.EqualTo(1),
                "spec §8: the boss guards the enemy castle");

            var defense = stages.Single(s => s.StageId == "c3_boss_defense");
            Assert.That(defense.EnemyLevel, Is.EqualTo(19));
            var bossWaves = defense.Waves.Select((wave, index) => (wave, index))
                .Where(w => w.wave.Spawns.Any(s => s.Unit is EnemyDefinition e && e.Tier == EnemyTier.Boss))
                .Select(w => w.index).ToList();
            Assert.That(bossWaves, Is.EqualTo(new[] { defense.Waves.Count() - 1 }), "spec §8: the boss comes with the last wave");
            Assert.That(defense.LargestEnemyRadius, Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(defense.CreateLayout().HasPath(new Vector2(21f, 0f), defense.PlayerCastle.Position + new Vector2(3f, 0f), 0.8f),
                Is.True, "the boss can reach the player castle over a bridge");
        }

        [Test]
        public void EnemySkillValidation_CatchesEmptySkills()
        {
            var skill = ScriptableObject.CreateInstance<EnemySkillDefinition>();
            try
            {
                var errors = new List<string>();
                skill.Validate(errors);
                Assert.That(errors, Has.Some.Contains("no effects and nothing summoned"));
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }
    }
}

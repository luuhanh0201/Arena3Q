#if UNITY_EDITOR
// Loads the hero and enemy assets and edits copies through SerializedObject, so these tests run in the Editor only.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>
    /// Lữ Bố's four skills in a real battle (docs/art/concepts/LuBu_Arena3Q/Arena3Q_LuBu_4Skills_VFX_Production_Spec_v1_0.md
    /// §4–§7, §13): Cuồng Huyết stacks, the sweep's slow and bonus stack, the charge's knock-up and shield, the
    /// ultimate's leap, execute and full stacks.
    /// </summary>
    public class LuBuSkillsTests
    {
        private const float Step = 0.05f;
        private const string ContentRoot = "Assets/Arena3Q/Content";

        private BattleController _battle;
        private HeroUnit _hero;
        private readonly List<Object> _created = new List<Object>();
        private readonly List<SkillSlot> _used = new List<SkillSlot>();
        private readonly List<EnemyUnit> _dummies = new List<EnemyUnit>();
        // State right when the skill resolved: a hero without a cast animation may basic-attack in the same step.
        private int _stacksAtUse;
        private readonly Dictionary<EnemyUnit, float> _healthAtUse = new Dictionary<EnemyUnit, float>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _battle.AutoSimulate = false;

            var stage = Copy(_battle.Stages.First(s => s.StageId == "attack_01"), so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                so.FindProperty("waves").arraySize = 0;
                so.FindProperty("enemyLevel").intValue = 1;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
                so.FindProperty("playerCastle.canAttack").boolValue = false;
                so.FindProperty("timeLimit").floatValue = 600f;
            });
            // Lữ Bố alone (no soldiers), 3 stars: every skill unlocked.
            var luBu = Copy(Load<HeroDefinition>("Heroes/Hero_LuBu"), so => so.FindProperty("baseSquadSize").intValue = 0);
            _battle.StartBattle(stage, new[] { new LineupEntry(stage.TestLoadout.LordHero), new LineupEntry(luBu, 1, 3) });
            Assert.That(_battle.TrySummon(SummonRequest.Quick(1)).Accepted, Is.True);
            _hero = _battle.GetHero(_battle.State.Cards[1].Hero);
            _hero.Skills.Used += slot =>
            {
                _used.Add(slot);
                _stacksAtUse = _hero.Skills.FrenzyStacks;
                foreach (var dummy in _dummies)
                    _healthAtUse[dummy] = dummy.Health;
            };
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null)
                    Object.Destroy(obj);
            _created.Clear();
            _used.Clear();
            _dummies.Clear();
            _healthAtUse.Clear();
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{ContentRoot}/{path}.asset");

        private T Copy<T>(T original, System.Action<SerializedObject> edit) where T : Object
        {
            var copy = Object.Instantiate(original);
            _created.Add(copy);
            var so = new SerializedObject(copy);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        /// <summary>A harmless, sturdy soldier standing still where it is put.</summary>
        private EnemyUnit Dummy(Vector2 offset, float maxHealth = 100000f)
        {
            var definition = Copy(Load<TroopDefinition>("Troops/Enemy_Swordsman"), so =>
            {
                so.FindProperty("stats.damage").floatValue = 0f;
                so.FindProperty("stats.moveSpeed").floatValue = 0f;
                so.FindProperty("stats.maxHealth").floatValue = maxHealth;
            });
            var position = _hero.Position + offset;
            Assert.That(_battle.Layout.IsWalkable(position), Is.True, $"test setup: {position} is walkable");
            var enemy = _battle.SpawnEnemy(definition, EnemyRole.Guard, position, 0f, -1);
            _dummies.Add(enemy);
            return enemy;
        }

        /// <summary>Only <paramref name="slot"/> may auto-cast.</summary>
        private void Only(SkillSlot slot)
        {
            foreach (var other in new[] { SkillSlot.Skill2, SkillSlot.Skill3 })
                _hero.Skills.Get(other).CooldownLeft = other == slot ? 0f : 1000f;
        }

        private void Run(float seconds, System.Func<bool> until = null)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            for (var i = 0; i < steps && !_battle.IsOver; i++)
            {
                if (until != null && until())
                    return;
                _battle.Simulate(Step);
            }
        }

        [UnityTest]
        public IEnumerator Skills_AreTheFourFromTheSpec()
        {
            yield return null;
            var definition = _hero.Definition;
            Assert.That(definition.GetSkill(SkillSlot.Passive).Passives.Single().kind, Is.EqualTo(PassiveKind.FrenzyStacks));
            Assert.That(definition.GetSkill(SkillSlot.Skill2).Cooldown, Is.EqualTo(8f));
            Assert.That(definition.GetSkill(SkillSlot.Skill3).Cooldown, Is.EqualTo(12f));
            Assert.That(definition.GetSkill(SkillSlot.Ultimate).CasterMove, Is.EqualTo(CasterMove.LeapToPoint));
            var errors = new List<string>();
            definition.Validate(errors);
            Assert.That(errors, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Sweep_HitsTheConeInFront_Slows_AndGivesTheBonusStackForTwoTargets()
        {
            var front = new[] { Dummy(new Vector2(1.2f, 0.35f)), Dummy(new Vector2(1.2f, -0.35f)) };
            var behind = Dummy(new Vector2(-1.6f, 0f));
            Only(SkillSlot.Skill2);
            yield return null;
            Run(3f, () => _used.Contains(SkillSlot.Skill2));
            Assert.That(_used, Does.Contain(SkillSlot.Skill2));
            foreach (var enemy in front)
            {
                Assert.That(enemy.Health, Is.LessThan(enemy.MaxHealth), "160% to everything in the sweep");
                Assert.That(enemy.Statuses.Value(StatusKind.Slow), Is.EqualTo(0.3f).Within(1e-4f), "30% slow");
                Assert.That(enemy.Statuses.Remaining(StatusKind.Slow), Is.EqualTo(2f).Within(0.06f));
            }
            Assert.That(behind.Health, Is.EqualTo(behind.MaxHealth), "nothing behind Lữ Bố is hit");
            Assert.That(_stacksAtUse, Is.EqualTo(2), "1 for the cast + 1 bonus for 2+ enemies");
        }

        [UnityTest]
        public IEnumerator Sweep_OnOneEnemy_GivesNoBonusStack()
        {
            Dummy(new Vector2(1.2f, 0f));
            Only(SkillSlot.Skill2);
            yield return null;
            Run(3f, () => _used.Contains(SkillSlot.Skill2));
            Assert.That(_stacksAtUse, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Charge_KnocksUpWhatItPasses_AndEndsWithAShield()
        {
            var enemy = Dummy(new Vector2(2.5f, 0f));
            var start = _hero.Position;
            Only(SkillSlot.Skill3);
            yield return null;
            Run(3f, () => _used.Contains(SkillSlot.Skill3));
            Assert.That(_used, Does.Contain(SkillSlot.Skill3));
            Assert.That(Vector2.Distance(start, _hero.Position), Is.GreaterThan(3f), "dashed forward");
            Assert.That(enemy.Health, Is.LessThan(enemy.MaxHealth), "130% to what it passed");
            Assert.That(enemy.Statuses.Has(StatusKind.Airborne), Is.True, "knocked up");
            Assert.That(enemy.IsStunned, Is.True, "cannot act while in the air");
            Assert.That(enemy.Statuses.Remaining(StatusKind.Airborne), Is.EqualTo(0.8f).Within(0.06f));
            Assert.That(_hero.Statuses.Shield, Is.GreaterThanOrEqualTo(_hero.MaxHealth * 0.12f - 0.01f), "12% max health shield");
            Assert.That(_hero.Statuses.Remaining(StatusKind.Shield), Is.EqualTo(3f).Within(0.06f));
            // The knocked-up unit rises on an arc, its shadow stays down.
            Run(0.3f);
            yield return null;
            Assert.That(Vector3.Distance(enemy.transform.position, _battle.ToView(enemy.Position)), Is.GreaterThan(0.1f));
            var shadow = enemy.transform.Find("Shadow");
            if (shadow != null)
                Assert.That(Vector3.Distance(shadow.position, _battle.ToView(enemy.Position)), Is.LessThan(0.01f), "shadow on the ground");
        }

        [UnityTest]
        public IEnumerator Ultimate_LeapsAndSlams_ExecutesBelowThirtyPercent_AndFillsTheStacks()
        {
            var low = Dummy(new Vector2(3.4f, 0.4f));
            var high = Dummy(new Vector2(3.4f, -0.4f));
            low.TakeDamage(low.MaxHealth * 0.71f, null, ignoreReduction: true);   // 29 %
            high.TakeDamage(high.MaxHealth * 0.68f, null, ignoreReduction: true); // 32 %: no bonus
            var lowBefore = low.Health;
            var highBefore = high.Health;
            yield return null;
            _hero.Skills.Rage.Add(1000f);
            var point = _hero.Position + new Vector2(3.4f, 0f);
            Assert.That(_hero.Skills.TryUltimate(point), Is.EqualTo(RejectReason.None));
            Run(4f, () => _used.Contains(SkillSlot.Ultimate));
            Assert.That(_used, Does.Contain(SkillSlot.Ultimate));
            Assert.That(Vector2.Distance(_hero.Position, point), Is.LessThan(1f), "leapt onto the aimed point");
            var lowDamage = lowBefore - _healthAtUse[low];
            var highDamage = highBefore - _healthAtUse[high];
            Assert.That(highDamage, Is.GreaterThan(0f));
            Assert.That(lowDamage / highDamage, Is.EqualTo(1.25f).Within(0.01f), "+25% only below 30%");
            Assert.That(low.Statuses.Remaining(StatusKind.Airborne), Is.EqualTo(1f).Within(0.06f), "knocked up 1 s");
            Assert.That(_stacksAtUse, Is.EqualTo(5), "full Cuồng Huyết after the ultimate");
            Assert.That(_hero.Skills.IsFrenzyFull, Is.True);
            Assert.That(_hero.ApplyStatus(StatusKind.Slow, 0.5f, 2f, null), Is.False, "no slows at 5 stacks");
        }

        [UnityTest]
        public IEnumerator FullFrenzy_HealsFromDamage_ThenEveryStackFallsOffAfterFiveQuietSeconds()
        {
            var enemy = Dummy(new Vector2(0.9f, 0f));
            Only(SkillSlot.Skill2);
            _hero.Skills.Get(SkillSlot.Skill2).CooldownLeft = 1000f;
            _hero.Skills.AddFrenzyStacks(5);
            _hero.TakeDamage(_hero.MaxHealth * 0.5f, null, ignoreReduction: true);
            var wounded = _hero.Health;
            yield return null;
            Run(2f, () => enemy.Health < enemy.MaxHealth);
            Assert.That(enemy.Health, Is.LessThan(enemy.MaxHealth), "a basic attack landed");
            Assert.That(_hero.Health, Is.GreaterThan(wounded), "15% lifesteal at full stacks");
            Assert.That(_hero.Skills.FrenzyStacks, Is.EqualTo(5));

            enemy.TakeDamage(enemy.MaxHealth * 10f, null, ignoreReduction: true);
            Run(5.3f);
            Assert.That(_hero.Skills.FrenzyStacks, Is.Zero, "5 s without damage: all stacks fall off together");
            Assert.That(_hero.ApplyStatus(StatusKind.Slow, 0.5f, 2f, null), Is.True, "slows work again");
        }
    }
}
#endif

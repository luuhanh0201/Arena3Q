#if UNITY_EDITOR
// Uses SerializedObject to build hero variants, so these tests run in the Editor only.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>Levels, stars, faction synergy, damage reduction and crits in a real battle (spec §2 "Cấp, sao và phe").</summary>
    public class ProgressionBattleTests
    {
        private const float Step = 0.05f;

        private BattleController _battle;
        private BattleHud _hud;
        private readonly List<Object> _created = new List<Object>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _hud = Object.FindAnyObjectByType<BattleHud>();
            _battle.AutoSimulate = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            _created.Clear();
        }

        private StageConfig Stage(int index) => _battle.Stages[index];
        private TestLoadout Loadout => Stage(0).TestLoadout;
        private HeroDefinition Starter(string heroId) =>
            AssetDatabase.FindAssets("t:HeroDefinition", new[] { "Assets/Arena3Q/Content/Heroes" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<HeroDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .First(hero => hero.Kind == HeroKind.Starter && hero.HeroId == heroId);

        private T Copy<T>(T original, System.Action<SerializedObject> edit) where T : Object
        {
            var copy = Object.Instantiate(original);
            _created.Add(copy);
            var so = new SerializedObject(copy);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private HeroUnit Summon(int card)
        {
            var decision = _battle.TrySummon(SummonRequest.Quick(card));
            Assert.That(decision.Accepted, Is.True, decision.Result.ToString());
            return _battle.GetHero(_battle.State.Cards[card].Hero);
        }

        private static float Grow(float rate, int level) => Mathf.Pow(1f + rate, level - 1);

        [UnityTest]
        public IEnumerator Lineup_LevelAndStars_ScaleTheHeroItsSquadAndTheCard()
        {
            var lordDefinition = Loadout.LordHero;
            var guardianDefinition = Starter("guardian");
            _battle.StartBattle(Stage(0), new[] { new LineupEntry(lordDefinition, 10), new LineupEntry(guardianDefinition, 1, 3) });
            yield return null;

            var lord = Summon(0);
            var guardian = Summon(1);

            var rates = lordDefinition.GrowthRates;
            Assert.That(lord.Level, Is.EqualTo(10));
            Assert.That(lord.MaxHealth, Is.EqualTo(lordDefinition.Stats.MaxHealth * Grow(rates.Health, 10)).Within(0.01f));
            Assert.That(lord.Damage, Is.EqualTo(lordDefinition.Stats.Damage * Grow(rates.Attack, 10)).Within(0.01f));
            Assert.That(lord.Runtime.MaxHealth, Is.EqualTo(lord.MaxHealth), "the Core record uses the scaled health");

            var troop = lord.Squad.Members[0];
            Assert.That(troop.MaxHealth, Is.EqualTo(lordDefinition.Troop.Stats.MaxHealth * Grow(0.03f, 10)).Within(0.01f),
                "soldiers grow with their hero's level");

            var starBonus = 1f + _battle.Rules.GetStarRule(3).StatBonus;
            Assert.That(guardian.Stars, Is.EqualTo(3));
            Assert.That(guardian.MaxHealth, Is.EqualTo(guardianDefinition.Stats.MaxHealth * starBonus).Within(0.01f));

            var levelLabels = _hud.CardViews.Select(card => card.transform.Find("Level").GetComponent<Text>().text).ToList();
            Assert.That(levelLabels[0], Is.EqualTo("Lv10"), "lords show the level only");
            Assert.That(levelLabels[1], Is.EqualTo("Lv1 ★★★"));
        }

        [UnityTest]
        public IEnumerator StageEnemyLevel_ScalesTheEnemies()
        {
            var stage = Copy(Stage(0), so => so.FindProperty("enemyLevel").intValue = 10);
            _battle.StartBattle(stage);
            yield return null;

            var enemy = _battle.Registry.Units.OfType<EnemyUnit>().First();
            var definition = stage.Defenders[0].Unit;
            Assert.That(enemy.MaxHealth, Is.EqualTo(definition.Stats.MaxHealth * Grow(0.03f, 10)).Within(0.01f));
            Assert.That(enemy.Damage, Is.EqualTo(definition.Stats.Damage * Grow(0.03f, 10)).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator FactionSynergy_TwoHeroesOfOneFaction_BuffTheWholeArmy()
        {
            var faction = ScriptableObject.CreateInstance<FactionDefinition>();
            _created.Add(faction);
            var factionSo = new SerializedObject(faction);
            factionSo.FindProperty("factionId").stringValue = "test";
            var tiers = factionSo.FindProperty("synergyTiers");
            tiers.arraySize = 1;
            tiers.GetArrayElementAtIndex(0).FindPropertyRelative("minHeroes").intValue = 2;
            tiers.GetArrayElementAtIndex(0).FindPropertyRelative("attackBonus").floatValue = 0.5f;
            factionSo.ApplyModifiedPropertiesWithoutUndo();

            System.Action<SerializedObject> named = so =>
            {
                so.FindProperty("kind").enumValueIndex = (int)HeroKind.Named;
                so.FindProperty("faction").objectReferenceValue = faction;
            };
            var first = Copy(Starter("swordsman"), named);
            var second = Copy(Starter("cavalry"), named);
            var lordDefinition = Loadout.LordHero;
            _battle.StartBattle(Stage(0), new[] { new LineupEntry(lordDefinition), new LineupEntry(first), new LineupEntry(second) });
            yield return null;

            Assert.That(_battle.PlayerModifiers.AttackBonus, Is.EqualTo(0.5f).Within(1e-5f));
            var lord = Summon(0);
            Assert.That(lord.Damage, Is.EqualTo(lordDefinition.Stats.Damage * 1.5f).Within(0.01f), "the lord has no faction but gets the bonus");
            Assert.That(lord.Squad.Members[0].Damage, Is.EqualTo(lordDefinition.Troop.Stats.Damage * 1.5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator DamageReduction_CutsIncomingDamage()
        {
            var tough = Copy(Loadout.LordHero, so => so.FindProperty("damageReduction").floatValue = 0.2f);
            _battle.StartBattle(Stage(0), new[] { new LineupEntry(tough) });
            yield return null;

            var lord = Summon(0);
            lord.TakeDamage(100f, null);

            Assert.That(lord.Health, Is.EqualTo(lord.MaxHealth - 80f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator CriticalHit_WithCertainChance_DealsMultipliedDamage()
        {
            // No squad, no defenders, a castle that does not shoot back: every hit on the castle is the lord's.
            var lucky = Copy(Loadout.LordHero, so =>
            {
                so.FindProperty("critChanceBonus").floatValue = 1f;
                so.FindProperty("baseSquadSize").intValue = 0;
            });
            var stage = Copy(Stage(0), so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
            });
            _battle.StartBattle(stage, new[] { new LineupEntry(lucky) });
            yield return null;

            var lord = Summon(0);
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            var before = castle.Health;
            Assert.That(_battle.AttackWithHero(lord.Id, castle).Accepted, Is.True);
            for (var i = 0; i < 2000 && Mathf.Approximately(castle.Health, before); i++)
                _battle.Simulate(Step);

            Assert.That(lord.CritChance, Is.EqualTo(1f));
            Assert.That(before - castle.Health, Is.EqualTo(lord.Damage * _battle.Rules.Settings.CritMultiplier).Within(0.01f));
        }
    }
}
#endif

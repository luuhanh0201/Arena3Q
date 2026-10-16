using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class StageConfigTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
        }

        [Test]
        public void HeroDefinition_DefaultsMatchSpec()
        {
            var hero = Create<HeroDefinition>("Hero");

            Assert.That(hero.IdleBeforeAdvanceSeconds, Is.EqualTo(5f));
            Assert.That(hero.IdleTimerResetsOnOutOfRangeDamage, Is.False);
            Assert.That(hero.SummonCost, Is.Zero);
            Assert.That(hero.SummonCooldown, Is.Zero);
        }

        [Test]
        public void StageConfig_DefaultsMatchSpec()
        {
            var stage = Create<StageConfig>("Stage");

            Assert.That(stage.TimeLimit, Is.EqualTo(180f));
            Assert.That(stage.AllowResummonDeadHero, Is.False);
            Assert.That(stage.OnHeroDeathTroopBehavior, Is.EqualTo(OnHeroDeathTroopBehavior.KeepFighting));
            Assert.That(stage.PlayerCastle.CanAttack, Is.True);
            Assert.That(stage.StarThresholds.maxHeroesLostForThreeStars, Is.Zero);
            Assert.That(stage.StarThresholds.maxHeroesLostForTwoStars, Is.EqualTo(1));
        }

        [Test]
        public void ValidStage_HasNoErrors()
        {
            var stage = CreateValidStage();

            var errors = new List<string>();
            stage.Validate(errors);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Stage_WithoutLoadout_ReportsError()
        {
            var stage = Create<StageConfig>("Stage");

            var errors = new List<string>();
            stage.Validate(errors);

            Assert.That(errors, Has.Some.Contains("testLoadout"));
        }

        [Test]
        public void Stage_QuickSummonPointInForbiddenZone_ReportsError()
        {
            var stage = CreateValidStage();
            var so = new SerializedObject(stage);
            var zones = so.FindProperty("forbiddenZones");
            zones.arraySize = 1;
            zones.GetArrayElementAtIndex(0).rectValue = new Rect(-13f, -1f, 2f, 2f);
            so.ApplyModifiedPropertiesWithoutUndo();

            var errors = new List<string>();
            stage.Validate(errors);

            Assert.That(errors, Has.Some.Contains("quickSummonPoints[0]"));
        }

        [Test]
        public void Loadout_WithEmptySlot_ReportsError()
        {
            var loadout = Create<TestLoadout>("Loadout");

            var errors = new List<string>();
            loadout.Validate(errors);

            Assert.That(errors, Has.Some.Contains("lordHero"));
            Assert.That(errors, Has.Some.Contains("heroes[0]"));
        }

        [Test]
        public void CreateBattleSetup_PutsLordCardFirstAndCopiesRules()
        {
            var stage = CreateValidStage();

            var setup = stage.CreateBattleSetup();
            var state = new BattleRuntimeState(setup);

            Assert.That(setup.CardHeroIds, Is.EqualTo(new[] { "lord", "hero0", "hero1", "hero2", "hero3" }));
            Assert.That(setup.TimeLimit, Is.EqualTo(180f));
            Assert.That(setup.StageType, Is.EqualTo(StageType.Attack));
            Assert.That(state.EnemyCastle.MaxHealth, Is.EqualTo(stage.EnemyCastle.MaxHealth));
        }

        [Test]
        public void IdleOverride_AppliesOnlyWhenEnabled()
        {
            var stage = CreateValidStage();
            var hero = stage.TestLoadout.LordHero;
            Assert.That(stage.GetIdleBeforeAdvanceSeconds(hero), Is.EqualTo(5f));

            var so = new SerializedObject(stage);
            so.FindProperty("overrideIdleBeforeAdvance").boolValue = true;
            so.FindProperty("idleBeforeAdvanceSeconds").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(stage.GetIdleBeforeAdvanceSeconds(hero), Is.EqualTo(2f));
        }

        private StageConfig CreateValidStage()
        {
            var troop = Create<TroopDefinition>("Troop");

            var lord = CreateHero("lord", troop);
            var heroes = new HeroDefinition[4];
            for (var i = 0; i < heroes.Length; i++)
                heroes[i] = CreateHero($"hero{i}", troop);

            var loadout = Create<TestLoadout>("Loadout");
            var loadoutSo = new SerializedObject(loadout);
            loadoutSo.FindProperty("lordHero").objectReferenceValue = lord;
            var heroesProp = loadoutSo.FindProperty("heroes");
            heroesProp.arraySize = heroes.Length;
            for (var i = 0; i < heroes.Length; i++)
                heroesProp.GetArrayElementAtIndex(i).objectReferenceValue = heroes[i];
            loadoutSo.ApplyModifiedPropertiesWithoutUndo();

            var stage = Create<StageConfig>("Stage");
            var stageSo = new SerializedObject(stage);
            stageSo.FindProperty("testLoadout").objectReferenceValue = loadout;
            stageSo.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private HeroDefinition CreateHero(string heroId, TroopDefinition troop)
        {
            var hero = Create<HeroDefinition>(heroId);
            var so = new SerializedObject(hero);
            so.FindProperty("heroId").stringValue = heroId;
            so.FindProperty("troop").objectReferenceValue = troop;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hero;
        }

        private T Create<T>(string objectName) where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            obj.name = objectName;
            _created.Add(obj);
            return obj;
        }
    }
}

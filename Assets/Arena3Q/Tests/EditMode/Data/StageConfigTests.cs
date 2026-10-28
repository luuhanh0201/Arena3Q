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
        public void River_WithoutBridgeAcrossTheMap_ReportsNoRoute()
        {
            var stage = CreateValidStage();
            SetRiver(stage, new[] { new Vector2(0f, -14f), new Vector2(0f, 14f) });

            var errors = new List<string>();
            stage.Validate(errors);

            Assert.That(errors, Has.Some.Contains("no walking route"));
        }

        [Test]
        public void River_WithBridge_IsValid_AndBridgeOffTheRiverIsReported()
        {
            var stage = CreateValidStage();
            SetRiver(stage, new[] { new Vector2(0f, -14f), new Vector2(0f, 14f) }, (Vector2.zero, 4f));
            var errors = new List<string>();
            stage.Validate(errors);
            Assert.That(errors, Is.Empty);

            SetRiver(stage, new[] { new Vector2(0f, -14f), new Vector2(0f, 14f) }, (new Vector2(6f, 0f), 4f));
            errors.Clear();
            stage.Validate(errors);
            Assert.That(errors, Has.Some.Contains("is not on the river"));
        }

        private static void SetRiver(StageConfig stage, Vector2[] path, params (Vector2 position, float width)[] bridges)
        {
            var so = new SerializedObject(stage);
            var rivers = so.FindProperty("rivers");
            rivers.arraySize = 1;
            var river = rivers.GetArrayElementAtIndex(0);
            river.FindPropertyRelative("width").floatValue = 2.5f;
            var points = river.FindPropertyRelative("path");
            points.arraySize = path.Length;
            for (var i = 0; i < path.Length; i++)
                points.GetArrayElementAtIndex(i).vector2Value = path[i];
            var crossings = river.FindPropertyRelative("crossings");
            crossings.arraySize = bridges.Length;
            for (var i = 0; i < bridges.Length; i++)
            {
                crossings.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value = bridges[i].position;
                crossings.GetArrayElementAtIndex(i).FindPropertyRelative("width").floatValue = bridges[i].width;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
        public void ArmyStance_StartsInHold_AndDefendsTheSummonZone()
        {
            var stage = CreateValidStage();
            var hero = stage.TestLoadout.LordHero;

            Assert.That(stage.StartStance, Is.EqualTo(ArmyStance.Hold), "user 2026-10-28: every battle starts in Giữ");
            Assert.That(stage.CreateBattleSetup().StartStance, Is.EqualTo(ArmyStance.Hold));
            var settings = stage.CreateHeroBrainSettings(hero);
            Assert.That(settings.DefendArea, Is.EqualTo(stage.SummonZone));
            Assert.That(settings.DefendChaseMargin, Is.EqualTo(stage.DefendChaseMargin));

            var so = new SerializedObject(stage);
            so.FindProperty("startStance").enumValueIndex = (int)ArmyStance.Attack;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(new BattleRuntimeState(stage.CreateBattleSetup()).Stance, Is.EqualTo(ArmyStance.Attack));
        }

        [Test]
        public void Hero_ReactionRangeAboveAttackRange_ReportsError()
        {
            var hero = CreateHero("hero", Create<TroopDefinition>("Troop"));
            var so = new SerializedObject(hero);
            so.FindProperty("stats.attackRange").floatValue = 0.6f;
            so.FindProperty("stats.reactionRange").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var errors = new List<string>();
            hero.Validate(errors);

            Assert.That(errors, Has.Some.Contains("reactionRange must be <= attackRange"));
        }

        [Test]
        public void ShippedContent_PassesValidation()
        {
            var errors = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:StageConfig", new[] { "Assets/Arena3Q/Content" }))
                AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid)).Validate(errors);

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ShippedAttackStages_DefendersStayAroundTheEnemyCastle()
        {
            // Spec §8 ✅: in Attack stages the defenders concentrate around the enemy castle.
            const float maxDistance = 8f;
            foreach (var guid in AssetDatabase.FindAssets("t:StageConfig", new[] { "Assets/Arena3Q/Content" }))
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (stage.StageType != StageType.Attack)
                    continue;
                foreach (var defender in stage.Defenders)
                {
                    Assert.That(Vector2.Distance(defender.Position, stage.EnemyCastle.Position), Is.LessThanOrEqualTo(maxDistance),
                        $"{stage.name}: defender at {defender.Position} is far from the castle");
                }
            }
        }

        [Test]
        public void CreateHeroBrainSettings_CombinesHeroAndStage()
        {
            var stage = CreateValidStage();
            var hero = stage.TestLoadout.LordHero;
            var so = new SerializedObject(stage);
            so.FindProperty("stageType").enumValueIndex = (int)StageType.Defense;
            so.ApplyModifiedPropertiesWithoutUndo();

            var settings = stage.CreateHeroBrainSettings(hero);

            Assert.That(settings.StageType, Is.EqualTo(StageType.Defense));
            Assert.That(settings.SelfDefendWhileAttacking, Is.False, "attack orders take priority by default");
        }

        private StageConfig CreateValidStage()
        {
            var troop = Create<TroopDefinition>("Troop");

            var lord = CreateHero("lord", troop, HeroKind.Lord);
            var heroes = new HeroDefinition[4];
            for (var i = 0; i < heroes.Length; i++)
                heroes[i] = CreateHero($"hero{i}", troop);

            var loadout = Create<TestLoadout>("Loadout");
            var loadoutSo = new SerializedObject(loadout);
            loadoutSo.FindProperty("lordHero").objectReferenceValue = lord;
            var heroesProp = loadoutSo.FindProperty("heroes");
            heroesProp.arraySize = heroes.Length;
            for (var i = 0; i < heroes.Length; i++)
                heroesProp.GetArrayElementAtIndex(i).FindPropertyRelative("hero").objectReferenceValue = heroes[i];
            loadoutSo.ApplyModifiedPropertiesWithoutUndo();

            var stage = Create<StageConfig>("Stage");
            var stageSo = new SerializedObject(stage);
            stageSo.FindProperty("testLoadout").objectReferenceValue = loadout;
            stageSo.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private HeroDefinition CreateHero(string heroId, TroopDefinition troop, HeroKind kind = HeroKind.Starter)
        {
            var hero = Create<HeroDefinition>(heroId);
            var so = new SerializedObject(hero);
            so.FindProperty("heroId").stringValue = heroId;
            so.FindProperty("kind").enumValueIndex = (int)kind;
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

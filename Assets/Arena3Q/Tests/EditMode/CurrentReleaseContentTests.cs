using System.Collections.Generic;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public sealed class CurrentReleaseContentTests
    {
        [TestCase("Stage_Tutorial_01", "Stage_Defense")]
        [TestCase("Stage_Attack_01", "Stage_Attack")]
        [TestCase("Stage_Defense_01", "Stage_Defense")]
        public void CurrentMap_PreservesCampaignRulesAndHasValidRoutes(string sourceName, string layoutName)
        {
            var source = AssetDatabase.LoadAssetAtPath<StageConfig>($"Assets/Arena3Q/Content/Stages/{sourceName}.asset");
            var stage = AssetDatabase.LoadAssetAtPath<StageConfig>($"Assets/Arena3Q/Content/Battlefield/Campaign/{sourceName}.asset");
            var map = AssetDatabase.LoadAssetAtPath<StageConfig>($"Assets/Arena3Q/Content/Battlefield/{layoutName}.asset");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.StageId, Is.EqualTo(source.StageId));
            Assert.That(stage.StageType, Is.EqualTo(source.StageType));
            Assert.That(stage.TimeLimit, Is.EqualTo(source.TimeLimit));
            Assert.That(stage.EnemyLevel, Is.EqualTo(source.EnemyLevel));
            Assert.That(stage.AllowResummonDeadHero, Is.EqualTo(source.AllowResummonDeadHero));
            Assert.That(stage.OnHeroDeathTroopBehavior, Is.EqualTo(source.OnHeroDeathTroopBehavior));
            Assert.That(JsonUtility.ToJson(stage.LossRules), Is.EqualTo(JsonUtility.ToJson(source.LossRules)));
            Assert.That(JsonUtility.ToJson(stage.StarThresholds), Is.EqualTo(JsonUtility.ToJson(source.StarThresholds)));
            Assert.That(stage.TestLoadout, Is.SameAs(source.TestLoadout));
            Assert.That(stage.MapBounds, Is.EqualTo(map.MapBounds));
            Assert.That(stage.Formation, Is.SameAs(map.Formation));
            Assert.That(stage.BackgroundArt, Is.Null);
            Assert.That(stage.PlayerCastle.Position, Is.EqualTo(map.PlayerCastle.Position));
            Assert.That(stage.EnemyCastle.Position, Is.EqualTo(map.EnemyCastle.Position));
            foreach (var field in new[] { "maxHealth", "canAttack", "attackRange", "damage", "attackInterval" })
                foreach (var castle in new[] { "playerCastle", "enemyCastle" })
                {
                    var a = new SerializedObject(stage).FindProperty(castle).FindPropertyRelative(field);
                    var b = new SerializedObject(source).FindProperty(castle).FindPropertyRelative(field);
                    Assert.That(SerializedProperty.DataEquals(a, b), Is.True, castle + "." + field);
                }
            Assert.That(stage.Defenders.Count, Is.EqualTo(source.Defenders.Count));
            for (int i = 0; i < stage.Defenders.Count; i++) Assert.That(stage.Defenders[i].Unit, Is.SameAs(source.Defenders[i].Unit));
            Assert.That(stage.Waves.Count, Is.EqualTo(source.Waves.Count));
            var layout = stage.CreateLayout();
            for (int i = 0; i < stage.Waves.Count; i++)
            {
                Assert.That(stage.Waves[i].StartTime, Is.EqualTo(source.Waves[i].StartTime));
                Assert.That(stage.Waves[i].Spawns.Length, Is.EqualTo(source.Waves[i].Spawns.Length));
                for (int j = 0; j < stage.Waves[i].Spawns.Length; j++)
                {
                    var spawn = stage.Waves[i].Spawns[j];
                    Assert.That(spawn.Unit, Is.SameAs(source.Waves[i].Spawns[j].Unit));
                    Assert.That(spawn.Count, Is.EqualTo(source.Waves[i].Spawns[j].Count));
                    Assert.That(layout.IsWalkable(spawn.SpawnPoint), Is.True);
                }
            }
            var errors = new List<string>(); stage.Validate(errors);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void FrontendAndBuildOrder_OpenTheCurrentMapFirst()
        {
            var config = AssetDatabase.LoadAssetAtPath<FrontendConfig>("Assets/Arena3Q/Content/Frontend/FrontendConfig.asset");
            Assert.That(config.BattleSceneName, Is.EqualTo("Battlefield_HighQuality"));
            Assert.That(EditorBuildSettings.scenes[0].path, Does.EndWith("/Frontend.unity"));
            Assert.That(EditorBuildSettings.scenes[1].path, Does.EndWith("/Battlefield_HighQuality.unity"));
            foreach (var stage in new[] { config.TutorialStage, config.DefaultStage })
                Assert.That(AssetDatabase.GetAssetPath(stage), Does.Contain("/Battlefield/Campaign/"));
        }
    }
}

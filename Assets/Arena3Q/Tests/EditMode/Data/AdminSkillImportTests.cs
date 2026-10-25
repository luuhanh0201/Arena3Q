using System.Collections.Generic;
using System.IO;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class AdminSkillImportTests
    {
        [Test]
        public void AppliedApiYaml_ImportsAsARealSkill_WithUpdatedCombatValues()
        {
            var project = Directory.GetParent(Application.dataPath).FullName;
            var fixture = Path.Combine(project, "Admin", ".local", "unity-skill-roundtrip.asset");
            if (!File.Exists(fixture)) Assert.Ignore("Run npm test in Admin to produce the API apply fixture first.");
            var folder = "Assets/Arena3Q/Tests/EditMode/Data/AdminSkillRoundTrip_Temporary";
            Assert.That(AssetDatabase.IsValidFolder(folder), Is.False, "A previous fixture must be inspected before replacing it.");
            AssetDatabase.CreateFolder("Assets/Arena3Q/Tests/EditMode/Data", "AdminSkillRoundTrip_Temporary");
            var target = folder + "/AppliedSkill.asset";
            try
            {
                File.Copy(fixture, Path.Combine(project, target));
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(target);
                Assert.That(skill, Is.Not.Null, "API output must be a valid Unity SkillDefinition.");
                Assert.That(skill.Cooldown, Is.EqualTo(9f));
                Assert.That(skill.Effects[0].value, Is.EqualTo(2f));
                Assert.That(skill.Description, Is.EqualTo("Fixture updated description"));
                Assert.That(skill.SkillId, Is.EqualTo("lubu_phuong_thien_tram"));
                var errors = new List<string>();
                skill.Validate(errors);
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}

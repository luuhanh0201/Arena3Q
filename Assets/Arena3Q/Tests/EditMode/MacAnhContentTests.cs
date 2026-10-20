#if UNITY_EDITOR
using System;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class MacAnhContentTests
    {
        private const string Root = "Assets/Arena3Q/Art/Heroes/MacAnh";
        [Test] public void EveryActionHasEightCompleteDirectionsAndBoneSockets()
        {
            var data = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Root + "/MacAnhPerformance.asset");
            Assert.That(data.Clips.Count, Is.EqualTo(11));
            foreach (MacAnhAction action in Enum.GetValues(typeof(MacAnhAction)))
            {
                var clip = data.Find(action); Assert.That(clip, Is.Not.Null, action.ToString());
                Assert.That(clip.frames.Length, Is.EqualTo(8 * clip.framesPerDirection));
                Assert.That(clip.sockets.Length, Is.EqualTo(clip.frames.Length));
                Assert.That(clip.frames.All(f => f != null), Is.True);
                Assert.That(clip.sockets.All(s => s.fanTip.sqrMagnitude > .01f && s.fanTip.sqrMagnitude < 9f), Is.True);
                for (var direction = 0; direction < 8; direction++)
                {
                    Assert.That(clip.Index(direction, 0), Is.EqualTo(direction * clip.framesPerDirection));
                    Assert.That(clip.Index(direction, clip.duration), Is.EqualTo(direction * clip.framesPerDirection + (clip.loop ? 0 : clip.framesPerDirection - 1)));
                }
            }
        }
        [Test] public void GameplayCastAndComboTimesMatchTheAuthoredFrameEvents()
        {
            var data = AssetDatabase.LoadAssetAtPath<MacAnhPerformance>(Root + "/MacAnhPerformance.asset");
            foreach (var binding in data.Skills)
            {
                var clip = data.Find(binding.action); Assert.That(clip.events.Length, Is.GreaterThan(0));
                Assert.That(binding.skill.CastTime, Is.EqualTo(clip.events[0].Seconds).Within(.00001f));
                for (var i = 0; i < clip.events.Length; i++)
                    Assert.That(binding.skill.CastTime + i * binding.skill.RepeatInterval, Is.EqualTo(clip.events[i].Seconds).Within(.00001f));
                Assert.That(clip.duration, Is.GreaterThanOrEqualTo(clip.events.Last().Seconds));
            }
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/Arena3Q/Content/Lords/Lord_Strategist.asset");
            Assert.That(hero.HeroId, Is.EqualTo("lord_strategist")); Assert.That(hero.Kind, Is.EqualTo(HeroKind.Lord));
            Assert.That(hero.Faction, Is.Null); Assert.That(hero.MacAnhPerformance, Is.SameAs(data));
            Assert.That(hero.GetSkill(SkillSlot.Skill2).SkillId, Is.EqualTo("huyen_co_tran"));
            Assert.That(hero.GetSkill(SkillSlot.Skill3).SkillId, Is.EqualTo("phong_vu_khon_tran"));
        }
        [Test] public void EveryEffectHasThreeQualityLevelsAndUrpMaterial()
        {
            foreach (var kind in new[] { "WindSlash", "TripleFeather", "MagicArray", "Cyclone", "Shield", "Ultimate", "Afterimage", "CounterSlash", "Impact" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/VFX/FX_MacAnh_" + kind + ".prefab");
                Assert.That(prefab, Is.Not.Null, kind);
                var component = prefab.GetComponents<MonoBehaviour>().Single();
                Assert.That(component.GetType().Name, Is.EqualTo("MacAnhFx"));
                var data = new SerializedObject(component);
                foreach (var name in new[] { "high", "medium", "low" })
                {
                    var meshes = data.FindProperty(name); Assert.That(meshes.arraySize, Is.EqualTo(3));
                    for (var i = 0; i < 3; i++) Assert.That(meshes.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                }
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
                    Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Arena3Q/MacAnh VFX 2D"));
            }
        }
    }
}
#endif

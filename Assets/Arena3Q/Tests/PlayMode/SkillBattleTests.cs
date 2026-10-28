#if UNITY_EDITOR
// Loads hero assets and edits stage copies through SerializedObject, so these tests run in the Editor only.
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
    /// <summary>Skills in a real battle (spec §3 "Kỹ năng trên thẻ"): passives, auto-cast, rage, ultimates and the card.</summary>
    public class SkillBattleTests
    {
        private const float Step = 0.05f;
        private const string ContentRoot = "Assets/Arena3Q/Content";

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

        private static HeroDefinition Hero(string path) =>
            AssetDatabase.LoadAssetAtPath<HeroDefinition>($"{ContentRoot}/{path}.asset");

        /// <summary>The test loadout's level-1 lord (no skills yet) first, as every lineup needs, then the given heroes.</summary>
        private LineupEntry[] WithLord(params LineupEntry[] heroes) =>
            new[] { new LineupEntry(Stage(0).TestLoadout.LordHero) }.Concat(heroes).ToArray();

        private StageConfig Stage(int index) => _battle.Stages[index];

        /// <summary>Attack stage without defenders and with a castle that does not shoot: only the enemies a test adds.</summary>
        private StageConfig EmptyStage()
        {
            var copy = Object.Instantiate(Stage(0));
            _created.Add(copy);
            var so = new SerializedObject(copy);
            so.FindProperty("defenders").arraySize = 0;
            so.FindProperty("enemyCastle.canAttack").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private HeroUnit Summon(int card)
        {
            var decision = _battle.TrySummon(SummonRequest.Quick(card));
            Assert.That(decision.Accepted, Is.True, decision.Result.ToString());
            return _battle.GetHero(_battle.State.Cards[card].Hero);
        }

        private EnemyUnit Enemy(Vector2 position)
        {
            Assert.That(_battle.Layout.IsWalkable(position), Is.True, "test setup: the enemy stands on walkable ground");
            return _battle.SpawnEnemy(Stage(0).Defenders[0].Unit, EnemyRole.Guard, position, 0.5f, -1);
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

        // ---- Passives ----

        [UnityTest]
        public IEnumerator Passive_Marshal_AddsTwoSoldiers_AndRaisesTheWholeArmysHealth()
        {
            var marshal = Hero("Lords/Lord_Marshal");
            _battle.StartBattle(EmptyStage(), new[] { new LineupEntry(marshal, 2), new LineupEntry(Hero("Heroes/Hero_Guardian")) });
            yield return null;

            var guardian = Summon(1);
            var before = guardian.MaxHealth;
            var lord = Summon(0);

            Assert.That(lord.Skills.Get(SkillSlot.Passive).Unlocked, Is.True, "learned at level 2");
            Assert.That(lord.Squad.Members, Has.Count.EqualTo(marshal.BaseSquadSize + 2));
            Assert.That(guardian.MaxHealth, Is.EqualTo(before * 1.05f).Within(0.01f), "+5 % for units already on the field");
        }

        [UnityTest]
        public IEnumerator Passive_NotLearnedYet_DoesNothing()
        {
            var marshal = Hero("Lords/Lord_Marshal");
            _battle.StartBattle(EmptyStage(), new[] { new LineupEntry(marshal, 1) });
            yield return null;

            var lord = Summon(0);
            Assert.That(lord.Skills.Get(SkillSlot.Passive).Unlocked, Is.False);
            Assert.That(lord.Squad.Members, Has.Count.EqualTo(marshal.BaseSquadSize));
        }

        // ---- Skills 2 and 3 ----

        [UnityTest]
        public IEnumerator SkillTwo_AutoCastsOnTheTarget_StunsIt_AndStartsItsCooldown()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"))));
            yield return null;
            var hero = Summon(1);
            var enemy = Enemy(hero.Position + new Vector2(1.5f, 0f));

            var stunnedWhenUsed = false;
            var used = new List<SkillSlot>();
            hero.Skills.Used += slot =>
            {
                used.Add(slot);
                stunnedWhenUsed = enemy.IsStunned;
            };
            Assert.That(_battle.AttackWithHero(hero.Id, enemy).Accepted, Is.True);
            Run(5f, () => used.Count > 0);

            Assert.That(used, Is.EqualTo(new[] { SkillSlot.Skill2 }), "1★: only skill 2 is learned");
            Assert.That(stunnedWhenUsed, Is.True, "Thuẫn Kích stuns");
            var slotState = hero.Skills.Get(SkillSlot.Skill2);
            Assert.That(slotState.CooldownLeft, Is.GreaterThan(slotState.Definition.Cooldown - 0.2f));
        }

        [UnityTest]
        public IEnumerator SkillTwo_WithoutAnyEnemy_Waits()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"))));
            yield return null;
            var hero = Summon(1);
            var used = 0;
            hero.Skills.Used += _ => used++;

            Run(2f);
            Assert.That(used, Is.Zero, "no target in range: the skill is not wasted");
            Assert.That(hero.Skills.Get(SkillSlot.Skill2).CooldownLeft, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DashSkill_DoesNotAutoCastWhileMovingOnOrder_ButDoesWhenAttacking()
        {
            _battle.StartBattle(EmptyStage(), new[] { new LineupEntry(Hero("Lords/Lord_Swordsman"), 5) });
            yield return null;
            var hero = Summon(0);
            var enemy = Enemy(hero.Position + new Vector2(3f, 0f));
            var used = new List<SkillSlot>();
            hero.Skills.Used += used.Add;

            Assert.That(_battle.MoveSelectedHero(hero.Position + new Vector2(0f, 3f)).Accepted, Is.True);
            Run(0.5f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Moving));
            Assert.That(used, Is.Empty, "Phi Ảnh waits while the hero walks to its move order (spec §3)");

            Assert.That(_battle.AttackWithHero(hero.Id, enemy).Accepted, Is.True);
            Run(2f, () => used.Count > 0);
            Assert.That(used, Does.Contain(SkillSlot.Skill2));
            Assert.That(hero.EdgeDistance(enemy), Is.LessThan(0.3f), "dashed next to the target");
        }

        // ---- Rage and ultimate ----

        [UnityTest]
        public IEnumerator Rage_ComesOnlyFromTheHerosOwnDamage()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            var enemy = Enemy(hero.Position + new Vector2(4f, 0f));

            hero.TakeDamage(50f, enemy);
            enemy.TakeDamage(10f, hero.Squad.Members[0]);
            Assert.That(hero.Skills.Rage.Value, Is.Zero, "taking damage and the squad's hits do not count");

            enemy.TakeDamage(hero.Damage, hero);
            Assert.That(hero.Skills.Rage.Value,
                Is.EqualTo(hero.Skills.Rage.Max / hero.Definition.RageHitsToFill).Within(0.5f), "one hit at the hero's Công");
        }

        [UnityTest]
        public IEnumerator Ultimate_EmptyRageIsRejected_FullRageCastsAndEmptiesTheBar()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            Enemy(hero.Position + new Vector2(4f, 0f));

            Assert.That(_battle.TryUseUltimate(1).Reason, Is.EqualTo(RejectReason.SkillNotReady));

            hero.Skills.Rage.Add(1000f);
            Assert.That(_battle.TryUseUltimate(1).Accepted, Is.True);
            Run(0.6f);

            Assert.That(hero.Skills.Rage.Value, Is.Zero);
            Assert.That(_battle.ActiveZoneCount, Is.EqualTo(1), "Kim Thành Thiết Bích stays on the ground");
            Assert.That(hero.Statuses.DamageTakenReduction, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(hero.Statuses.IsImmune, Is.True);

            Run(6.5f);
            Assert.That(_battle.ActiveZoneCount, Is.Zero, "the zone ends after 6 s");
        }

        [UnityTest]
        public IEnumerator Ultimate_StunnedBeforeTheCastMark_IsNotUsed_AndRageIsKept()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            Enemy(hero.Position + new Vector2(4f, 0f));
            hero.Skills.Rage.Add(1000f);

            Assert.That(_battle.TryUseUltimate(1).Accepted, Is.True);
            hero.ApplyStatus(StatusKind.Stun, 1f, 1f);
            Run(0.6f);

            Assert.That(_battle.ActiveZoneCount, Is.Zero);
            Assert.That(hero.Skills.Rage.IsFull, Is.True);
            Assert.That(_battle.TryUseUltimate(1).Reason, Is.EqualTo(RejectReason.SkillNotReady), "stunned heroes cannot cast");
        }

        [UnityTest]
        public IEnumerator Ultimate_DraggedToAPoint_PlacesTheAreaThere_WithinItsRange()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            Enemy(hero.Position + new Vector2(6f, 0f));
            hero.Skills.Rage.Add(1000f);

            // Kim Thành Thiết Bích: a point within 4 of the hero; dropped farther, it is pulled back to 4.
            Assert.That(_battle.TryUseUltimate(1, hero.Position + new Vector2(10f, 0f)).Accepted, Is.True);
            Run(0.6f);
            var area = Object.FindObjectsByType<SkillAreaVisual>(FindObjectsSortMode.None)
                .Single(v => v.name == "Skill kim_thanh_thiet_bich");
            Assert.That(Vector2.Distance(area.transform.localPosition, hero.Position + new Vector2(4f, 0f)), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator Ultimate_EastWind_PushesEnemiesBack_AndSpeedsUpTheArmy()
        {
            // Exercise the retained Đông Phong skill explicitly; the strategist's current art kit is Mặc Ảnh.
            var eastWindLord = Object.Instantiate(Hero("Lords/Lord_Strategist"));
            _created.Add(eastWindLord);
            var data = new SerializedObject(eastWindLord);
            data.FindProperty("macAnhPerformance").objectReferenceValue = null;
            data.FindProperty("skills").GetArrayElementAtIndex((int)SkillSlot.Ultimate).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SkillDefinition>(ContentRoot + "/Skills/Lord_Strategist/Skill_dong_phong.asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            _battle.StartBattle(EmptyStage(), new[] { new LineupEntry(eastWindLord, 15) });
            yield return null;
            var hero = Summon(0);
            var enemy = Enemy(hero.Position + new Vector2(2f, 0f));
            var before = Vector2.Distance(enemy.Position, hero.Position);
            hero.Skills.Rage.Add(1000f);

            Assert.That(_battle.TryUseUltimate(0).Accepted, Is.True);
            Run(0.6f);

            Assert.That(Vector2.Distance(enemy.Position, hero.Position), Is.GreaterThan(before + 1.5f), "knocked back");
            Assert.That(_battle.Layout.IsWalkable(enemy.Position), Is.True, "never pushed into a forbidden zone");
            Assert.That(hero.Statuses.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(1e-4f));
            Assert.That(hero.Squad.Members[0].Statuses.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator Ultimate_HeroDies_LosesItsRage()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            hero.Skills.Rage.Add(1000f);
            hero.TakeDamage(float.MaxValue, null);
            Assert.That(hero.Skills.Rage.Value, Is.Zero);
        }

        // ---- Card ----

        private static string IconLabel(HeroCardView card, SkillSlot slot) =>
            card.transform.Find($"Skill {slot}/Label").GetComponent<Text>().text;

        [UnityTest]
        public IEnumerator Card_ShowsLockedSkills_TapUsesTheUltimate_NothingElse()
        {
            _battle.StartBattle(Stage(0));
            yield return null;
            Summon(0);
            Summon(1);
            yield return null;
            var lordCard = _hud.CardViews[0];
            var heroCard = _hud.CardViews[1];

            Assert.That(IconLabel(lordCard, SkillSlot.Skill2), Is.EqualTo("Lv5"), "a level-1 lord has not learned skill 2");
            Assert.That(IconLabel(heroCard, SkillSlot.Skill3), Is.EqualTo("2"));
            Assert.That(heroCard.transform.Find($"Skill {SkillSlot.Skill3}/LockStar").gameObject.activeSelf, Is.True,
                "the star icon follows the number");
            Assert.That(lordCard.transform.Find($"Skill {SkillSlot.Skill2}/LockStar").gameObject.activeSelf, Is.False,
                "lords unlock by level: no star");
            Assert.That(heroCard.transform.Find("Rage").gameObject.activeSelf, Is.False, "no ultimate learned: no rage bar");
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1));

            var rejected = new List<RejectReason>();
            _battle.ActionRejected += rejected.Add;
            lordCard.Tap();
            Assert.That(rejected, Is.EqualTo(new[] { RejectReason.SkillNotReady }));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1), "a tap no longer selects");
            yield return null;
            var toast = _hud.GetComponentsInChildren<Text>().First(t => t.name == "Toast");
            Assert.That(toast.text, Is.EqualTo("Kỹ năng chưa thể sử dụng"));

        }

        [UnityTest]
        public IEnumerator Card_ShowsCooldownAndRage_AndTapCastsWithFullRage()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            Enemy(hero.Position + new Vector2(1.5f, 0f));
            var card = _hud.CardViews[1];

            hero.Skills.Rage.Add(hero.Skills.Rage.Max * 0.5f);
            hero.Skills.Get(SkillSlot.Skill2).CooldownLeft = 4.2f;
            yield return null;
            Assert.That(card.RageShown, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(IconLabel(card, SkillSlot.Skill2), Is.EqualTo("5"));

            hero.Skills.Rage.Add(1000f);
            card.Tap();
            Run(0.6f);
            Assert.That(hero.Skills.Rage.Value, Is.Zero);
            Assert.That(_battle.ActiveZoneCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeployedCard_Drag_DoesNotAimTheUltimate()
        {
            _battle.StartBattle(EmptyStage(), WithLord(new LineupEntry(Hero("Heroes/Hero_Guardian"), 1, 4)));
            yield return null;
            var hero = Summon(1);
            Enemy(hero.Position + new Vector2(1.5f, 0f));
            hero.Skills.Rage.Add(1000f);
            var card = _hud.CardViews[1];

            // Spec §5 "Cơ chế mới": only a tap uses the ultimate; dragging a deployed card does nothing.
            var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, card.transform.position) };
            card.OnBeginDrag(data);
            card.ReleaseDrag(_hud.Camera.WorldToScreen(hero.Position + new Vector2(1.5f, 0f)));
            Run(0.6f);
            Assert.That(hero.Skills.Rage.IsFull, Is.True, "no aimed ultimate from a drag");
        }
    }
}
#endif

#if UNITY_EDITOR
// Loads enemy assets and edits copies through SerializedObject, so these tests run in the Editor only.
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

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>Elites and bosses in a real battle (spec §8): warnings, crowd control, taunt, phase 2, summons, traits.</summary>
    public class EnemyBattleTests
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

        private StageConfig Stage(string stageId) => _battle.Stages.First(s => s.StageId == stageId);

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{ContentRoot}/{path}.asset");
        private static EnemyDefinition EnemyAsset(string chapter, string id) => Load<EnemyDefinition>($"Enemies/{chapter}/Enemy_{id}");
        private static EnemySkillDefinition SkillAsset(string chapter, string id) =>
            Load<EnemySkillDefinition>($"Enemies/{chapter}/Skills/EnemySkill_{id}");

        private T Copy<T>(T original, System.Action<SerializedObject> edit) where T : Object
        {
            var copy = Object.Instantiate(original);
            _created.Add(copy);
            var so = new SerializedObject(copy);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        /// <summary>A stage of the given type without enemies, the enemy castle harmless, enemy level 1.</summary>
        private StageConfig Empty(string stageId) => Copy(Stage(stageId), so =>
        {
            so.FindProperty("defenders").arraySize = 0;
            so.FindProperty("waves").arraySize = 0;
            so.FindProperty("enemyLevel").intValue = 1;
            so.FindProperty("enemyCastle.canAttack").boolValue = false;
            so.FindProperty("playerCastle.canAttack").boolValue = false;
            so.FindProperty("timeLimit").floatValue = 600f;
        });

        /// <summary>The lord (unused) and a guardian without soldiers, so area skills can only pick the hero.</summary>
        private HeroUnit StartWithLoneHero(StageConfig stage, float aggroRange = 0f)
        {
            var loadout = stage.TestLoadout;
            var guardian = Copy(loadout.GetCards().First(h => h.HeroId == "guardian"), so =>
            {
                so.FindProperty("baseSquadSize").intValue = 0;
                if (aggroRange > 0f)
                    so.FindProperty("stats.aggroRange").floatValue = aggroRange;
            });
            _battle.StartBattle(stage, new[] { new LineupEntry(loadout.LordHero), new LineupEntry(guardian) });
            var decision = _battle.TrySummon(SummonRequest.Quick(1));
            Assert.That(decision.Accepted, Is.True, decision.Result.ToString());
            return _battle.GetHero(_battle.State.Cards[1].Hero);
        }

        private EnemyUnit Spawn(TroopDefinition definition, Vector2 position, EnemyRole role = EnemyRole.Guard,
            float guardRadius = 0.5f)
        {
            Assert.That(_battle.Layout.IsWalkable(position), Is.True, $"test setup: {position} is walkable");
            return _battle.SpawnEnemy(definition, role, position, guardRadius, -1);
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

        // ---- Warning, then effect ----

        [UnityTest]
        public IEnumerator AreaSkill_WarnsFirst_ThenHitsWhoStayed()
        {
            var hero = StartWithLoneHero(Empty("attack_01"));
            yield return null;
            // Out of the hero's aggro range (5) but in the skill's (6): the hero stays idle.
            var sorcerer = Spawn(EnemyAsset("C2", "c2_sorcerer"), hero.Position + new Vector2(6.3f, 0f));
            Run(5f, () => sorcerer.Skills.Warning != null);
            Assert.That(sorcerer.Skills.Warning, Is.Not.Null, "Thiên Lôi Phù starts after its first cooldown");
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth), "nothing lands during the warning");

            Run(1.1f);
            Assert.That(hero.IsStunned, Is.True, "stunned 1 s");
            Assert.That(hero.Health, Is.LessThan(hero.MaxHealth));
        }

        [UnityTest]
        public IEnumerator AreaSkill_StaysWhereItWasPut_LeavingTheZoneDodgesIt()
        {
            var hero = StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var sorcerer = Spawn(EnemyAsset("C2", "c2_sorcerer"), hero.Position + new Vector2(6.3f, 0f));
            Run(5f, () => sorcerer.Skills.Warning != null);
            Assert.That(sorcerer.Skills.Warning, Is.Not.Null);

            hero.SetPosition(hero.Position + new Vector2(0f, 5f));
            Run(1.2f);
            Assert.That(hero.IsStunned, Is.False);
            Assert.That(hero.Health, Is.EqualTo(hero.MaxHealth));
            Assert.That(sorcerer.Skills.Slots[0].CooldownLeft, Is.GreaterThan(0f), "the skill was used, it just missed");
        }

        [UnityTest]
        public IEnumerator Warning_CasterStunned_SkillCancelledWithoutCooldown()
        {
            var hero = StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var sorcerer = Spawn(EnemyAsset("C2", "c2_sorcerer"), hero.Position + new Vector2(6.3f, 0f));
            Run(5f, () => sorcerer.Skills.Warning != null);

            sorcerer.ApplyStatus(StatusKind.Stun, 1f, 2f);
            Run(0.2f);
            Assert.That(sorcerer.Skills.Warning, Is.Null);
            Assert.That(sorcerer.Skills.Slots[0].CooldownLeft, Is.Zero);
            Run(1f);
            Assert.That(hero.IsStunned, Is.False);
        }

        // ---- Crowd control on heroes ----

        [UnityTest]
        public IEnumerator Chain_PullsTheNearestHeroAndRootsIt()
        {
            var hero = StartWithLoneHero(Empty("attack_01"), aggroRange: 3f);
            yield return null;
            var horse = Spawn(EnemyAsset("C3", "c3_horse_face"), hero.Position + new Vector2(5f, 0f));
            var before = hero.EdgeDistance(horse);
            var used = 0;
            horse.Skills.Used += _ => used++;
            Run(5f, () => used > 0);

            Assert.That(used, Is.EqualTo(1), "Câu Hồn Tỏa");
            Assert.That(hero.EdgeDistance(horse), Is.LessThan(before - 2f), "pulled towards Mã Diện");
            Assert.That(hero.IsStunned, Is.True);
        }

        [UnityTest]
        public IEnumerator Stun_KeepsTheOrder_AndLongLocksBreakAfterTwoSeconds()
        {
            var hero = StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var destination = hero.Position + new Vector2(0f, 4f);
            Assert.That(_battle.MoveSelectedHero(destination).Accepted, Is.True);
            hero.ApplyStatus(StatusKind.Stun, 1f, 6f);

            Run(1f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Moving), "spec §8: crowd control does not erase the order");
            Run(1.2f);
            Assert.That(hero.IsStunned, Is.False, "anti-lock: free after 2 s");
            Assert.That(hero.Statuses.IsImmune, Is.True);
            Run(3f);
            Assert.That(Vector2.Distance(hero.Position, destination), Is.LessThan(0.3f), "then the order goes on");
        }

        // ---- Taunt ----

        [UnityTest]
        public IEnumerator Taunt_PullsAFreeHero_ButNotOneOnAnOrder_AndANewOrderEndsIt()
        {
            var hero = StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var bandit = Spawn(Load<EnemyDefinition>("Troops/Enemy_Brute"), hero.Position + new Vector2(-1.1f, 0f));
            var guard = Spawn(EnemyAsset("C2", "c2_bronze_guard"), hero.Position + new Vector2(2.2f, 0f));
            Run(1f);
            Assert.That(hero.EngagedTarget, Is.EqualTo(bandit.Id), "test setup: the hero fights the nearer enemy");

            Run(4f, () => hero.Statuses.Has(StatusKind.Taunt));
            Assert.That(hero.TauntSource, Is.EqualTo(guard), "Kim Cương Hộ Thể taunts");
            Run(0.3f);
            Assert.That(hero.EngagedTarget, Is.EqualTo(guard.Id), "a free-fighting hero goes for the taunter");

            Assert.That(_battle.MoveSelectedHero(hero.Position + new Vector2(-4f, 3f)).Accepted, Is.True);
            Assert.That(hero.Statuses.Has(StatusKind.Taunt), Is.False, "a new order wins over the taunt");
            Run(0.5f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Moving));
            Assert.That(hero.EngagedTarget, Is.Not.EqualTo(guard.Id));
        }

        // ---- Boss ----

        [UnityTest]
        public IEnumerator Boss_PhaseTwoBelowHalfHealth_FasterNewSkills_AndTheHudBarShowsIt()
        {
            StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var bossDefinition = EnemyAsset("C1", "c1_boss");
            var boss = Spawn(bossDefinition, new Vector2(15f, 0f));
            var interval = boss.CurrentAttackInterval;
            yield return null;
            Assert.That(_hud.IsBossBarVisible, Is.True);
            Assert.That(_hud.BossBarText, Is.EqualTo(bossDefinition.DisplayName));

            boss.TakeDamage(boss.MaxHealth * 0.4f, null);
            Assert.That(boss.InPhaseTwo, Is.False);
            boss.TakeDamage(boss.MaxHealth * 0.2f, null);
            Assert.That(boss.InPhaseTwo, Is.True);
            Assert.That(boss.CurrentAttackInterval, Is.EqualTo(interval / 1.3f).Within(1e-3f), "Cuồng Nộ +30 % attack speed");
            Assert.That(boss.Skills.Slots.Select(s => s.Definition.SkillId),
                Does.Contain("c1_hac_phong_tram_360"), "the slash becomes a full circle");
            yield return null;
            Assert.That(_hud.BossBarFraction, Is.EqualTo(0.4f).Within(0.01f));
            Assert.That(_hud.BossBarText, Does.Contain("Pha 2"));

            boss.TakeDamage(boss.MaxHealth, null);
            yield return null;
            Assert.That(_hud.IsBossBarVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Boss_PhaseTwoStart_StormAuraAndShield()
        {
            StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var boss = Spawn(EnemyAsset("C2", "c2_boss"), new Vector2(15f, 0f));
            var zones = _battle.ActiveZoneCount;
            boss.TakeDamage(boss.MaxHealth * 0.6f, null);

            Assert.That(boss.InPhaseTwo, Is.True);
            Assert.That(boss.Statuses.Shield, Is.EqualTo(boss.MaxHealth * 0.125f).Within(1f), "khiên tà khí");
            Assert.That(_battle.ActiveZoneCount, Is.EqualTo(zones + 1), "the storm follows the boss");
            var health = boss.Health;
            boss.TakeDamage(100f, null);
            Assert.That(boss.Health, Is.EqualTo(health), "the shield takes the hit first");
        }

        [UnityTest]
        public IEnumerator Summons_AreCappedAtEightAlive()
        {
            StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var boss = Spawn(EnemyAsset("C1", "c1_boss"), new Vector2(15f, 0f));
            var call = SkillAsset("C1", "c1_goi_lau_la");
            int Enemies() => _battle.Registry.Units.Count(u => u is EnemyUnit && u != boss);

            boss.Skills.UseNow(call);
            Assert.That(Enemies(), Is.EqualTo(4));
            boss.Skills.UseNow(call);
            boss.Skills.UseNow(call);
            Assert.That(Enemies(), Is.EqualTo(8), "spec §8 🟡: at most 8 alive per skill");
            Assert.That(_battle.Registry.Units.OfType<EnemyUnit>().Where(u => u != boss).All(u => u.Tier == EnemyTier.Normal), Is.True);
        }

        [UnityTest]
        public IEnumerator Clones_AreWeakCopies_WithoutSkillsOrBossBar()
        {
            StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var boss = Spawn(EnemyAsset("C3", "c3_boss"), new Vector2(15f, 0f));
            boss.Skills.UseNow(SkillAsset("C3", "c3_huyen_anh"));

            var clones = _battle.Registry.Units.OfType<EnemyUnit>().Where(u => u.IsClone).ToList();
            Assert.That(clones, Has.Count.EqualTo(2));
            foreach (var clone in clones)
            {
                Assert.That(clone.MaxHealth, Is.EqualTo(boss.MaxHealth * 0.1f).Within(0.5f));
                Assert.That(clone.Damage, Is.EqualTo(boss.Damage * 0.3f).Within(0.05f));
                Assert.That(clone.Skills.Slots, Is.Empty);
                Assert.That(clone.IsBoss, Is.False);
            }
            Assert.That(_battle.LivingBosses.Count(), Is.EqualTo(1));
        }

        // ---- Traits ----

        [UnityTest]
        public IEnumerator Revive_StandsUpOnceWithPartOfItsHealth()
        {
            StartWithLoneHero(Empty("attack_01"));
            yield return null;
            var sure = Copy(EnemyAsset("C3", "c3_jiangshi"), so => so.FindProperty("reviveChance").floatValue = 1f);
            var jiangshi = Spawn(sure, new Vector2(15f, 3f));

            jiangshi.TakeDamage(jiangshi.MaxHealth * 2f, null);
            Assert.That(jiangshi.IsAlive, Is.True);
            Assert.That(jiangshi.Health, Is.EqualTo(jiangshi.MaxHealth * 0.3f).Within(0.5f));
            jiangshi.TakeDamage(jiangshi.MaxHealth * 2f, null);
            Assert.That(jiangshi.IsAlive, Is.False, "only once");
        }

        [UnityTest]
        public IEnumerator Flyer_CrossesTheRiverAnywhere_WalkersNeedABridge()
        {
            StartWithLoneHero(Copy(Empty("defense_01"), TestStages.UseRiverInsteadOfFormation));
            yield return null;
            // Between the two bridges (y ≈ 6.7 and −4.85), east of the river.
            var start = new Vector2(4f, 0.5f);
            var wisp = Spawn(EnemyAsset("C3", "c3_wisp"), start, EnemyRole.Assault, 0f);
            var imp = Spawn(EnemyAsset("C3", "c3_imp"), start + new Vector2(0f, 1f), EnemyRole.Assault, 0f);
            Assert.That(wisp.IsFlying, Is.True);

            Run(3f);
            Assert.That(wisp.Position.x, Is.LessThan(-1f), "straight over the water");
            Assert.That(Mathf.Abs(wisp.Position.y - start.y), Is.LessThan(1.5f));
            Assert.That(imp.Position.x > -1f || Mathf.Abs(imp.Position.y - (start.y + 1f)) > 1.5f, Is.True,
                "the walker goes round by a bridge");
        }

        // ---- Stages ----

        [UnityTest]
        public IEnumerator BossStages_ShowTheBoss()
        {
            _battle.StartBattle(Stage("c1_boss_attack"));
            yield return null;
            Assert.That(_battle.LivingBosses.Count(), Is.EqualTo(1));
            Assert.That(_battle.Registry.Units.OfType<EnemyUnit>().Count(u => u.Tier == EnemyTier.Elite), Is.EqualTo(2));
            yield return null;
            Assert.That(_hud.IsBossBarVisible, Is.True);

            _battle.StartBattle(Stage("c3_boss_defense"));
            yield return null;
            Assert.That(_battle.LivingBosses, Is.Empty, "the boss comes with the last wave");
        }
    }
}
#endif

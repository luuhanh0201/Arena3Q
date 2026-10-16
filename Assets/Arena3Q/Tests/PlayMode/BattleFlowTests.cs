#if UNITY_EDITOR
// Uses SerializedObject to build stage variants, so these tests run in the Editor only.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Tests.PlayMode
{
    /// <summary>
    /// End-to-end checks in the real Battle scene against the acceptance list in spec §12. The battle is stepped
    /// manually with <see cref="BattleController.Simulate"/> so the tests are fast and deterministic.
    /// </summary>
    public class BattleFlowTests
    {
        private const string SceneName = "Battle";
        private const float Step = 0.05f;

        private BattleController _battle;
        private BattleHud _hud;
        private BattleCameraController _camera;
        private readonly List<Object> _created = new List<Object>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            _battle = Object.FindAnyObjectByType<BattleController>();
            _hud = Object.FindAnyObjectByType<BattleHud>();
            _camera = Object.FindAnyObjectByType<BattleCameraController>();
            Assert.That(_battle, Is.Not.Null, "Battle scene has a BattleController");
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

        private void Run(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            for (var i = 0; i < steps && !_battle.IsOver; i++)
                _battle.Simulate(Step);
        }

        private StageConfig Stage(int index) => _battle.Stages[index];

        /// <summary>A copy of a stage asset tweaked through its serialized fields.</summary>
        private StageConfig Variant(int index, System.Action<SerializedObject> edit)
        {
            var copy = Object.Instantiate(Stage(index));
            _created.Add(copy);
            var so = new SerializedObject(copy);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private HeroUnit SummonQuick(int card)
        {
            var decision = _battle.TrySummon(SummonRequest.Quick(card));
            Assert.That(decision.Accepted, Is.True, decision.Result.ToString());
            return _battle.GetHero(_battle.State.Cards[card].Hero);
        }

        // ---- Scene and start ----

        [UnityTest]
        public IEnumerator BattleScene_StartsAttackStageWithCardsCastlesAndDefenders()
        {
            _battle.StartBattle(0);
            yield return null;

            Assert.That(_battle.Stage.StageType, Is.EqualTo(StageType.Attack));
            Assert.That(_battle.Cards, Has.Count.EqualTo(5), "1 lord + 4 heroes");
            Assert.That(_battle.PlayerCastleId.IsValid && _battle.EnemyCastleId.IsValid, Is.True);
            Assert.That(_battle.Registry.Count(u => u is EnemyUnit), Is.EqualTo(Stage(0).Defenders.Count));
            Assert.That(_hud.CardViews, Has.Count.EqualTo(5));
            Assert.That(Camera.main.orthographic, Is.True);
        }

        // ---- Summon ----

        [UnityTest]
        public IEnumerator QuickSummon_SpawnsIdleHeroWithSquad_ThenAdvancesAfterFiveSeconds()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;

            Assert.That(hero.State, Is.EqualTo(HeroState.Idle));
            Assert.That(hero.Squad.Members, Has.Count.EqualTo(hero.Definition.BaseSquadSize));
            Assert.That(_battle.Layout.IsWalkable(hero.Position), Is.True);
            Assert.That(_battle.SelectedHero, Is.EqualTo(hero));

            Run(4.8f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Idle), "no target nearby, still waiting");
            Run(0.4f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Advancing));
        }

        [UnityTest]
        public IEnumerator FreeSummon_OutsideZoneIsRejectedAndCardKept_InsideZoneSpawnsAtDropPoint()
        {
            _battle.StartBattle(0);
            yield return null;

            var outside = _battle.TrySummon(SummonRequest.Free(1, new Vector2(0f, 0f)));
            Assert.That(outside.Result.Reason, Is.EqualTo(RejectReason.OutsideSummonZone));
            Assert.That(_battle.State.Cards[1].State, Is.EqualTo(CardState.Available));

            var drop = Stage(0).SummonZone.center + new Vector2(1f, 2f);
            var inside = _battle.TrySummon(SummonRequest.Free(1, drop));
            Assert.That(inside.Accepted, Is.True);
            var hero = _battle.GetHero(_battle.State.Cards[1].Hero);
            Assert.That(Vector2.Distance(hero.Position, drop), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator SixthHero_IsRejected()
        {
            var lord = Stage(0).TestLoadout.LordHero;
            var loadout = Object.Instantiate(Stage(0).TestLoadout);
            _created.Add(loadout);
            var loadoutSo = new SerializedObject(loadout);
            var heroes = loadoutSo.FindProperty("heroes");
            heroes.arraySize = 5;
            heroes.GetArrayElementAtIndex(4).objectReferenceValue = Object.Instantiate(lord);
            _created.Add(heroes.GetArrayElementAtIndex(4).objectReferenceValue);
            loadoutSo.ApplyModifiedPropertiesWithoutUndo();
            _battle.StartBattle(Variant(0, so => so.FindProperty("testLoadout").objectReferenceValue = loadout));
            yield return null;

            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
                SummonQuick(i);
            var sixth = _battle.TrySummon(SummonRequest.Quick(5));

            Assert.That(_battle.Cards, Has.Count.EqualTo(6));
            Assert.That(sixth.Result.Reason, Is.EqualTo(RejectReason.HeroLimitReached));
            Assert.That(_battle.State.Cards[5].State, Is.EqualTo(CardState.Available));
        }

        [UnityTest]
        public IEnumerator RuntimeState_TracksEverySoldiersHealthAndDeath()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;
            var state = _battle.State;

            Assert.That(state.CountAliveUnits(Team.Player, UnitKind.Troop), Is.EqualTo(hero.Definition.BaseSquadSize));
            Assert.That(state.CountAliveUnits(Team.Enemy, UnitKind.Enemy), Is.EqualTo(Stage(0).Defenders.Count));
            Assert.That(state.Units.Where(u => u.Kind == UnitKind.Troop).All(u => u.OwnerHero == hero.Id), Is.True);

            var troop = hero.Squad.Members[0];
            troop.TakeDamage(10f, null);
            Assert.That(troop.Record.Health, Is.EqualTo(troop.MaxHealth - 10f));

            var enemy = _battle.Registry.Units.OfType<EnemyUnit>().First();
            var enemyId = enemy.Id;
            enemy.TakeDamage(float.MaxValue, null);
            Assert.That(state.TryGetUnit(enemyId, out var record) && !record.IsAlive, Is.True);
            Assert.That(state.CountAliveUnits(Team.Enemy, UnitKind.Enemy), Is.EqualTo(Stage(0).Defenders.Count - 1));
        }

        [UnityTest]
        public IEnumerator DeadHero_CardGreysOut_CannotBeReused_SquadKeepsFighting()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            var squad = hero.Squad;
            yield return null;

            hero.TakeDamage(float.MaxValue, null);
            Run(Step);

            Assert.That(_battle.State.Cards[0].State, Is.EqualTo(CardState.Dead));
            Assert.That(_battle.State.HeroesLost, Is.EqualTo(1));
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Result.Reason, Is.EqualTo(RejectReason.HeroDeadCannotResummon));
            Assert.That(_battle.TrySelectCard(0), Is.False);
            Assert.That(squad.Mode, Is.EqualTo(SquadMode.Leaderless));
            Assert.That(squad.Members, Has.Count.GreaterThan(0));
        }

        // ---- Selection and commands ----

        [UnityTest]
        public IEnumerator SelectByCard_AndMoveCommand_HeroWalksThereThenIdles_TroopsFollow()
        {
            _battle.StartBattle(0);
            var first = SummonQuick(0);
            var second = SummonQuick(1);
            yield return null;
            Assert.That(_battle.SelectedHero, Is.EqualTo(second));

            Assert.That(_battle.TrySelectCard(0), Is.True);
            Assert.That(_battle.SelectedHero, Is.EqualTo(first));

            var destination = new Vector2(-6f, -8f);
            Assert.That(_battle.MoveSelectedHero(destination).Accepted, Is.True);
            Assert.That(first.State, Is.EqualTo(HeroState.Moving));
            Run(8f);

            Assert.That(Vector2.Distance(first.Position, destination), Is.LessThan(0.2f));
            Assert.That(first.State, Is.EqualTo(HeroState.Idle).Or.EqualTo(HeroState.Advancing));
            foreach (var troop in first.Squad.Members)
                Assert.That(Vector2.Distance(troop.Position, first.Position), Is.LessThan(3f));
        }

        [UnityTest]
        public IEnumerator Moving_HitOnceByCloseEnemy_DefendsInPlace_ThenWalksOnAfterTwoSeconds()
        {
            _battle.StartBattle(Variant(0, so => so.FindProperty("defenders").arraySize = 0));
            var hero = SummonQuick(0);
            // No squad: soldiers rushing the enemy would shove the hero and blur "fights in place".
            foreach (var troop in hero.Squad.Members.ToArray())
                troop.Die();
            yield return null;
            // A harmless enemy right next to the hero: it never hurts the hero by itself.
            var harmless = Object.Instantiate(Stage(0).Defenders[1].Unit);
            _created.Add(harmless);
            var harmlessSo = new SerializedObject(harmless);
            harmlessSo.FindProperty("stats.damage").floatValue = 0f;
            harmlessSo.ApplyModifiedPropertiesWithoutUndo();
            var enemy = _battle.SpawnEnemy(harmless, EnemyRole.Guard, hero.Position + new Vector2(0.9f, 0f), 0f, -1);

            Assert.That(_battle.MoveSelectedHero(hero.Position + new Vector2(0f, 8f)).Accepted, Is.True);
            hero.TakeDamage(1f, enemy);
            Run(Step);
            Assert.That(hero.State, Is.EqualTo(HeroState.Combat));
            var standing = hero.Position;

            Run(1.5f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Combat), "still a fresh threat");
            Assert.That(Vector2.Distance(hero.Position, standing), Is.LessThan(0.15f), "fights in place");

            Run(1f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Moving), "no hit for over 2 s: back to the move order");
            Assert.That(enemy.IsAlive, Is.True);
        }

        [UnityTest]
        public IEnumerator MoveCommand_PastForbiddenZone_WalksAroundIt()
        {
            _battle.StartBattle(Variant(0, so => so.FindProperty("defenders").arraySize = 0));
            yield return null;
            Assert.That(_battle.TrySummon(SummonRequest.Free(0, new Vector2(-12f, 8f))).Accepted, Is.True);
            var hero = _battle.SelectedHero;
            var zone = Stage(0).ForbiddenZones[0];
            var destination = new Vector2(zone.xMax + 3f, zone.center.y);
            Assert.That(_battle.MoveSelectedHero(destination).Accepted, Is.True);

            var entered = false;
            for (var t = 0f; t < 25f && hero.State == HeroState.Moving; t += Step)
            {
                _battle.Simulate(Step);
                entered |= zone.Contains(hero.Position);
                foreach (var troop in hero.Squad.Members)
                    entered |= zone.Contains(troop.Position);
            }

            Assert.That(entered, Is.False, "nobody walked through the forbidden zone");
            Assert.That(Vector2.Distance(hero.Position, destination), Is.LessThan(0.2f), "arrived on the other side");
        }

        [UnityTest]
        public IEnumerator Idle_NextToEnemyCastle_HitsUnitsFirstThenTheCastle()
        {
            _battle.StartBattle(Variant(0, so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
            }));
            var hero = SummonQuick(0);
            yield return null;
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            var spot = castle.Position + new Vector2(-(CastleUnit.BodyRadius + HeroUnit.BodyRadius + 0.3f), 0f);
            Assert.That(_battle.MoveSelectedHero(spot).Accepted, Is.True);
            for (var t = 0f; t < 30f && hero.State != HeroState.Idle; t += Step)
                _battle.Simulate(Step);
            Assert.That(hero.State, Is.EqualTo(HeroState.Idle));

            // An enemy soldier in reach at the same time as the castle: the soldier comes first.
            var enemy = _battle.SpawnEnemy(Stage(0).Defenders[1].Unit, EnemyRole.Guard, hero.Position + new Vector2(0f, 0.9f), 0f, -1);
            Run(Step);
            Assert.That(hero.EngagedTarget, Is.EqualTo(enemy.Id));

            enemy.Die();
            var castleHealth = castle.Health;
            Run(2f);
            Assert.That(hero.EngagedTarget, Is.EqualTo(castle.Id));
            Assert.That(castle.Health, Is.LessThan(castleHealth), "the idle hero hits the castle in reach");
            Assert.That(hero.State, Is.EqualTo(HeroState.Idle), "still idle: a target in reach keeps resetting the timer");
        }

        [UnityTest]
        public IEnumerator Tap_SelectsHero_AttacksTappedEnemyOrCastle_MovesOnGround()
        {
            _battle.StartBattle(0);
            var first = SummonQuick(0);
            var second = SummonQuick(1);
            yield return null;
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            var defender = _battle.Registry.Units.OfType<EnemyUnit>().First();

            Assert.That(_battle.TapAt(first.Position), Is.EqualTo(BattleController.TapResult.Selected));
            Assert.That(_battle.SelectedHero, Is.EqualTo(first));

            Assert.That(_battle.TapAt(defender.Position), Is.EqualTo(BattleController.TapResult.Attack));
            Assert.That(first.Brain.AttackTarget, Is.EqualTo(defender.Id));

            Assert.That(_battle.TapAt(castle.Position + new Vector2(0.5f, 0.5f)), Is.EqualTo(BattleController.TapResult.Attack),
                "anywhere on the castle counts");
            Assert.That(first.Brain.AttackTarget, Is.EqualTo(castle.Id));

            Assert.That(_battle.TapAt(new Vector2(-8f, -6f)), Is.EqualTo(BattleController.TapResult.Move));
            Assert.That(first.State, Is.EqualTo(HeroState.Moving));
            Assert.That(second.State, Is.EqualTo(HeroState.Idle), "only the selected hero gets the order");
        }

        [UnityTest]
        public IEnumerator MoveCommand_IntoForbiddenZoneOrOffMap_IsRejected()
        {
            _battle.StartBattle(0);
            SummonQuick(0);
            yield return null;

            var forbidden = Stage(0).ForbiddenZones[0].center;
            Assert.That(_battle.MoveSelectedHero(forbidden).Reason, Is.EqualTo(RejectReason.InForbiddenZone));
            Assert.That(_battle.MoveSelectedHero(new Vector2(500f, 0f)).Reason, Is.EqualTo(RejectReason.OutsideBattlefield));
            Assert.That(_battle.SelectedHero.State, Is.EqualTo(HeroState.Idle));
        }

        [UnityTest]
        public IEnumerator AttackCommand_OnEnemyCastle_HeroAndSquadDestroyIt_VictoryWithThreeStars()
        {
            // Command flow only: no defenders and a weak, passive castle so one hero can finish it.
            _battle.StartBattle(Variant(0, so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                so.FindProperty("enemyCastle.maxHealth").floatValue = 1500f;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
            }));
            var hero = SummonQuick(0);
            yield return null;

            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            Assert.That(_battle.AttackWithHero(hero.Id, castle).Accepted, Is.True);
            Assert.That(hero.State, Is.EqualTo(HeroState.Attacking));

            Run(25f);
            Assert.That(hero.Squad.Mode, Is.EqualTo(SquadMode.FocusTarget));
            Run(150f);

            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(_battle.LastOutcome.Reason, Is.EqualTo(OutcomeReason.EnemyCastleDestroyed));
            Assert.That(_battle.State.Stars, Is.EqualTo(3));
            yield return null;
            Assert.That(_hud.IsResultVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator AttackCommand_SoldiersMakingNoProgress_AttackEnemyNearestToTarget()
        {
            // Soldiers that crawl (speed ~0) can never close in on the ordered target, a deterministic stand-in for
            // a target that is surrounded. A harmless defender stands next to the target.
            var slowTroop = Object.Instantiate(Stage(0).TestLoadout.LordHero.Troop);
            _created.Add(slowTroop);
            var troopSo = new SerializedObject(slowTroop);
            troopSo.FindProperty("stats.moveSpeed").floatValue = 0.01f;
            troopSo.ApplyModifiedPropertiesWithoutUndo();

            var archer = Object.Instantiate(Stage(0).TestLoadout.GetCards()[2]);
            _created.Add(archer);
            var heroSo = new SerializedObject(archer);
            heroSo.FindProperty("troop").objectReferenceValue = slowTroop;
            heroSo.ApplyModifiedPropertiesWithoutUndo();

            var loadout = Object.Instantiate(Stage(0).TestLoadout);
            _created.Add(loadout);
            var loadoutSo = new SerializedObject(loadout);
            loadoutSo.FindProperty("heroes").GetArrayElementAtIndex(1).objectReferenceValue = archer;
            loadoutSo.ApplyModifiedPropertiesWithoutUndo();

            var harmless = Object.Instantiate(Stage(0).Defenders[1].Unit);
            _created.Add(harmless);
            var harmlessSo = new SerializedObject(harmless);
            harmlessSo.FindProperty("stats.damage").floatValue = 0f;
            harmlessSo.ApplyModifiedPropertiesWithoutUndo();

            var castlePosition = Stage(0).EnemyCastle.Position;
            _battle.StartBattle(Variant(0, so =>
            {
                so.FindProperty("testLoadout").objectReferenceValue = loadout;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
                var defenders = so.FindProperty("defenders");
                defenders.arraySize = 1;
                var element = defenders.GetArrayElementAtIndex(0);
                element.FindPropertyRelative("unit").objectReferenceValue = harmless;
                element.FindPropertyRelative("guardRadius").floatValue = 0f;
                element.FindPropertyRelative("position").vector2Value = castlePosition + new Vector2(-2.2f, 0f);
            }));
            yield return null;
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            var defender = _battle.Registry.Units.OfType<EnemyUnit>().Single();
            var hero = SummonQuick(2);
            Assert.That(hero.Definition, Is.SameAs(archer));
            Assert.That(_battle.AttackWithHero(hero.Id, castle).Accepted, Is.True);

            Run(1.2f);
            Assert.That(hero.Squad.Members.All(m => !m.FallbackTarget.IsValid), Is.True, "not before the 1.5 s timeout");

            // The soldier standing in the hero's path gets shoved forward by the hero, which is real progress
            // towards the castle, so it rightly keeps the ordered target. The others are stuck and must switch.
            var start = hero.Squad.Members.ToDictionary(m => m, m => m.Position);
            Run(1f);
            Assert.That(hero.Squad.Mode, Is.EqualTo(SquadMode.FocusTarget));
            Assert.That(hero.Squad.Members, Has.Count.EqualTo(archer.BaseSquadSize));
            var stationary = hero.Squad.Members.Where(m => Vector2.Distance(start[m], m.Position) < 0.05f).ToList();
            Assert.That(stationary, Has.Count.GreaterThanOrEqualTo(archer.BaseSquadSize - 1));
            foreach (var troop in stationary)
            {
                Assert.That(troop.FallbackTarget, Is.EqualTo(defender.Id), "the enemy nearest to the castle");
                Assert.That(troop.EngagedTarget, Is.EqualTo(defender.Id));
            }
            Assert.That(hero.Brain.AttackTarget, Is.EqualTo(castle.Id), "the hero's own order is unchanged");
        }

        [UnityTest]
        public IEnumerator AttackOrder_OnCastle_TakesPriorityOverCloseAttackers()
        {
            _battle.StartBattle(Variant(0, so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                so.FindProperty("enemyCastle.canAttack").boolValue = false;
            }));
            var hero = SummonQuick(0);
            foreach (var troop in hero.Squad.Members.ToArray())
                troop.Die();
            yield return null;
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            Assert.That(_battle.AttackWithHero(hero.Id, castle).Accepted, Is.True);
            for (var t = 0f; t < 30f && !hero.IsInAttackRange(castle); t += Step)
                _battle.Simulate(Step);

            // A close enemy (on the side away from the castle) hits the hero every half second.
            var enemy = _battle.SpawnEnemy(Stage(0).Defenders[1].Unit, EnemyRole.Guard, hero.Position + new Vector2(-0.9f, 0f), 0f, -1);
            var castleHealth = castle.Health;
            for (var i = 0; i < 6; i++)
            {
                hero.TakeDamage(1f, enemy);
                Run(0.5f);
                Assert.That(hero.EdgeDistance(enemy), Is.LessThanOrEqualTo(hero.ReactionRange), "the attacker is close");
            }

            Assert.That(hero.State, Is.EqualTo(HeroState.Attacking), "no switch to Combat");
            Assert.That(hero.EngagedTarget, Is.EqualTo(castle.Id));
            Assert.That(castle.Health, Is.LessThan(castleHealth));
        }

        [UnityTest]
        public IEnumerator AttackStage_TimeUp_IsDefeatWithZeroStars()
        {
            _battle.StartBattle(Variant(0, so => so.FindProperty("timeLimit").floatValue = 3f));
            yield return null;

            Run(3.2f);

            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(_battle.LastOutcome.Reason, Is.EqualTo(OutcomeReason.TimeUp));
            Assert.That(_battle.State.Stars, Is.Zero);
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Result.Reason, Is.EqualTo(RejectReason.BattleOver));
        }

        [UnityTest]
        public IEnumerator Defenders_ChargeIntruders_ThenReturnToPost()
        {
            var post = new Vector2(0f, -6f);
            _battle.StartBattle(Variant(0, so =>
            {
                // A single sturdy defender (the brute) moved to the middle of the map.
                var defenders = so.FindProperty("defenders");
                defenders.arraySize = 1;
                defenders.GetArrayElementAtIndex(0).FindPropertyRelative("position").vector2Value = post;
            }));
            yield return null;
            var defender = _battle.Registry.Units.OfType<EnemyUnit>().Single();

            var hero = SummonQuick(2);
            Assert.That(_battle.MoveSelectedHero(post + new Vector2(-4f, 0f)).Accepted, Is.True);
            var left = false;
            for (var t = 0f; t < 20f && !left; t += Step)
            {
                _battle.Simulate(Step);
                left = Vector2.Distance(defender.Position, post) > 0.5f;
            }
            Assert.That(left, Is.True, "defender left its post to charge the intruder");

            foreach (var troop in hero.Squad.Members.ToArray())
                troop.Die();
            hero.Die();
            Run(8f);
            Assert.That(defender.IsAlive, Is.True);
            Assert.That(Vector2.Distance(defender.Position, post), Is.LessThan(0.3f), "back at its post");
        }

        // ---- Defense stage ----

        [UnityTest]
        public IEnumerator DefenseStage_WavesSpawn_SurvivingTheTimerWins()
        {
            _battle.StartBattle(Variant(1, so => so.FindProperty("timeLimit").floatValue = 10f));
            yield return null;
            Assert.That(_battle.EnemyCastleId.IsValid, Is.False, "no enemy castle in Defense stages");

            Run(6f);
            Assert.That(_battle.State.CurrentWave, Is.EqualTo(0));
            Assert.That(_battle.Registry.Count(u => u is EnemyUnit), Is.GreaterThan(0));

            Run(5f);
            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(_battle.LastOutcome.Reason, Is.EqualTo(OutcomeReason.SurvivedTimeLimit));
        }

        [UnityTest]
        public IEnumerator DefenseStage_PlayerCastleDestroyed_IsDefeat()
        {
            _battle.StartBattle(1);
            yield return null;

            _battle.Registry.Get(_battle.PlayerCastleId).TakeDamage(float.MaxValue, null);
            Run(Step);

            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(_battle.LastOutcome.Reason, Is.EqualTo(OutcomeReason.PlayerCastleDestroyed));
        }

        [UnityTest]
        public IEnumerator DefenseStage_PlayerCastleShootsEnemiesInRange()
        {
            _battle.StartBattle(1);
            yield return null;
            Run(Step);
            var castle = _battle.Registry.Get(_battle.PlayerCastleId);
            var enemy = _battle.SpawnEnemy(Stage(1).Waves[0].Spawns[0].Unit, EnemyRole.Guard,
                castle.Position + new Vector2(3f, 0f), 0f, -1);
            var health = enemy.Health;

            Run(2f);

            Assert.That(enemy == null || enemy.Health < health, Is.True);
        }

        // ---- Balance smoke tests: the placeholder stages are winnable with a full army ----

        [UnityTest]
        public IEnumerator Balance_AttackStage_FiveHeroesAdvancing_Win()
        {
            // No orders: heroes wait 5 s, then advance and fight their way to the castle. (Ordering a straight
            // attack on the castle ignores the defenders by design, so it is not the winning strategy.)
            _battle.StartBattle(0);
            yield return null;
            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
                SummonQuick(i);

            Run(Stage(0).TimeLimit + 1f);

            Debug.Log($"[Arena3Q] Attack balance: {_battle.LastOutcome} at {_battle.State.ElapsedTime:F0}s, " +
                      $"heroes lost {_battle.State.HeroesLost}, stars {_battle.State.Stars}");
            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [UnityTest]
        public IEnumerator Balance_DefenseStage_FiveHeroesHoldTheLine_Win()
        {
            _battle.StartBattle(1);
            yield return null;
            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
                SummonQuick(i);

            Run(Stage(1).TimeLimit + 1f);

            Debug.Log($"[Arena3Q] Defense balance: {_battle.LastOutcome} at {_battle.State.ElapsedTime:F0}s, " +
                      $"heroes lost {_battle.State.HeroesLost}, stars {_battle.State.Stars}, " +
                      $"castle {_battle.State.PlayerCastle.Health:F0}/{_battle.State.PlayerCastle.MaxHealth:F0}");
            Assert.That(_battle.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        // ---- Replay, camera, UI ----

        [UnityTest]
        public IEnumerator Restart_StartsFreshBattle()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            Run(2f);
            hero.TakeDamage(float.MaxValue, null);
            Run(Step);
            var oldState = _battle.State;

            _battle.Restart();
            yield return null;

            Assert.That(_battle.State, Is.Not.SameAs(oldState));
            Assert.That(_battle.State.SummonedCount, Is.Zero);
            Assert.That(_battle.State.HeroesLost, Is.Zero);
            Assert.That(_battle.State.Cards.All(c => c.State == CardState.Available), Is.True);
            Assert.That(_battle.LivingHeroes.Any(), Is.False);
            Assert.That(_battle.Registry.Count(u => u is EnemyUnit), Is.EqualTo(Stage(0).Defenders.Count));
        }

        [UnityTest]
        public IEnumerator Camera_StaysInsideMapBounds_AndOffscreenHeroGetsEdgeIcon()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;

            _camera.Pan(new Vector2(-100000f, 0f));
            yield return null;
            yield return null;

            var cam = Camera.main;
            var bounds = Stage(0).MapBounds;
            Assert.That(cam.transform.position.x + cam.orthographicSize * cam.aspect, Is.LessThanOrEqualTo(bounds.xMax + 0.01f));
            Assert.That(_camera.IsFollowPaused, Is.True);

            var indicators = Object.FindAnyObjectByType<OffscreenHeroIndicators>();
            Assert.That(indicators.TryGetIndicatorViewport(hero.Id, out var edge), Is.True);
            Assert.That(edge.x, Is.LessThan(0.1f), "icon on the left edge, towards the hero");

            _battle.TrySelectHero(hero.Id);
            Assert.That(_camera.IsFollowPaused, Is.False, "selecting again resumes following");
        }

        [UnityTest]
        public IEnumerator CardViews_ReflectCardStates()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;

            var deployed = _hud.CardViews[0].GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Assert.That(deployed, Has.Some.EqualTo("Trên sân"));

            hero.TakeDamage(float.MaxValue, null);
            Run(Step);
            yield return null;
            var dead = _hud.CardViews[0].GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Assert.That(dead, Has.Some.EqualTo("Đã chết"));
        }

        private static Vector2 ScreenPositionOf(Component uiElement) =>
            RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)uiElement.transform).position);

        private static PointerEventData Pointer(Vector2 screen) =>
            new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };

        [UnityTest]
        public IEnumerator CardTap_QuickSummons_ThenTapSelectsThatHero()
        {
            _battle.StartBattle(0);
            yield return null;
            var first = _hud.CardViews[0];
            var second = _hud.CardViews[1];

            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(second.gameObject, Pointer(ScreenPositionOf(second)), ExecuteEvents.pointerClickHandler);
            Assert.That(_battle.State.Cards[0].State, Is.EqualTo(CardState.Deployed));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1));

            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(0), "tapping a deployed card selects, not summons");
            Assert.That(_battle.State.SummonedCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator CardDrag_IntoSummonZoneSummons_ReleasedOverUiCancels()
        {
            _battle.StartBattle(0);
            yield return null;
            var card = _hud.CardViews[1];
            var zoneCenter = Stage(0).SummonZone.center;
            var zoneScreen = (Vector2)_camera.Camera.WorldToScreenPoint(zoneCenter);
            Assert.That(_hud.IsOverUi(zoneScreen), Is.False, "test setup: the zone centre is not under the HUD");

            ExecuteEvents.Execute(card.gameObject, Pointer(ScreenPositionOf(card)), ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(card.gameObject, Pointer(zoneScreen), ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(card.gameObject, Pointer(zoneScreen), ExecuteEvents.endDragHandler);

            Assert.That(_battle.State.Cards[1].State, Is.EqualTo(CardState.Deployed));
            var hero = _battle.GetHero(_battle.State.Cards[1].Hero);
            Assert.That(Vector2.Distance(hero.Position, zoneCenter), Is.LessThan(0.05f));

            var other = _hud.CardViews[2];
            ExecuteEvents.Execute(other.gameObject, Pointer(ScreenPositionOf(other)), ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(other.gameObject, Pointer(ScreenPositionOf(other)), ExecuteEvents.endDragHandler);
            Assert.That(_battle.State.Cards[2].State, Is.EqualTo(CardState.Available), "dropped back on the card bar");
        }

        /// <summary>Renders the battle (world + HUD) to Logs/Screenshots for a visual check of the placeholder look.</summary>
        [UnityTest]
        public IEnumerator Screenshot_AttackAndDefense()
        {
            _battle.StartBattle(0);
            SummonQuick(0);
            SummonQuick(1);
            _battle.MoveSelectedHero(new Vector2(-6f, 2f));
            Run(3f);
            _battle.ShowSummonZone(true);
            yield return null;
            Capture("attack.png");
            _battle.ShowSummonZone(false);

            _battle.StartBattle(1);
            SummonQuick(0);
            Run(12f);
            _camera.FocusOn(new Vector2(0f, 0f));
            for (var i = 0; i < 30; i++)
                yield return null;
            Capture("defense.png");
        }

        private void Capture(string fileName)
        {
            var cam = Camera.main;
            var canvas = _hud.GetComponentInChildren<Canvas>();
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previousTarget = cam.targetTexture;
            var previousMode = canvas.renderMode;
            try
            {
                cam.targetTexture = rt;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var directory = Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
                Object.Destroy(texture);
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = previousTarget;
                canvas.renderMode = previousMode;
                Object.Destroy(rt);
            }
        }
    }
}
#endif

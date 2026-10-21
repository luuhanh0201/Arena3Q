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
            // A sixth card: a copy of a starter hero (a lineup has exactly one lord).
            var extra = Object.Instantiate(Stage(0).TestLoadout.GetCards()[1]);
            _created.Add(extra);
            var loadout = Object.Instantiate(Stage(0).TestLoadout);
            _created.Add(loadout);
            var loadoutSo = new SerializedObject(loadout);
            var heroes = loadoutSo.FindProperty("heroes");
            heroes.arraySize = 5;
            heroes.GetArrayElementAtIndex(4).FindPropertyRelative("hero").objectReferenceValue = extra;
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
        public IEnumerator MoveCommand_AcrossTheRiver_GoesOverABridge()
        {
            _battle.StartBattle(Variant(0, so =>
            {
                so.FindProperty("defenders").arraySize = 0;
                TestStages.UseRiverInsteadOfFormation(so);
            }));
            yield return null;
            var river = _battle.Layout.Obstacles.OfType<RiverObstacle>().Single();
            Assert.That(river.Name, Is.EqualTo("Sông"));
            Assert.That(river.Crossings, Has.Count.EqualTo(2));

            Assert.That(_battle.TrySummon(SummonRequest.Free(0, new Vector2(-12f, 0f))).Accepted, Is.True);
            var hero = _battle.SelectedHero;
            var destination = new Vector2(8f, 0f);
            Assert.That(_battle.MoveSelectedHero(destination).Accepted, Is.True);

            var wet = false;
            var closestToBridge = float.PositiveInfinity;
            for (var t = 0f; t < 30f && hero.State == HeroState.Moving; t += Step)
            {
                _battle.Simulate(Step);
                wet |= river.Contains(hero.Position, 0f);
                foreach (var troop in hero.Squad.Members)
                    wet |= river.Contains(troop.Position, 0f);
                foreach (var crossing in river.Crossings)
                    closestToBridge = Mathf.Min(closestToBridge, Vector2.Distance(hero.Position, crossing.Center));
            }

            Assert.That(wet, Is.False, "nobody waded through the river");
            Assert.That(closestToBridge, Is.LessThan(1.5f), "the hero crossed on a bridge");
            Assert.That(Vector2.Distance(hero.Position, destination), Is.LessThan(0.2f), "arrived on the far bank");
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

            // The shipped map: a Bát Quái pillar blocks, an open gate (4, west, Khai at the start) does not.
            var formation = Stage(0).Formation;
            Assert.That(_battle.MoveSelectedHero(formation.Pillars[0]).Reason, Is.EqualTo(RejectReason.InForbiddenZone));
            Assert.That(_battle.MoveSelectedHero(formation.Gates[4].position).Accepted, Is.True, "an open gate is walkable");
            _battle.SelectedHero.Brain.Issue(new MoveCommand(_battle.SelectedHero.Id, _battle.SelectedHero.Position));
            Run(Step);
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
            // The result screen waits for the castle's fall animation.
            for (var t = 0f; t < _battle.ResultDelaySeconds + 1f && !_hud.IsResultVisible; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.That(_hud.IsResultVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator CastleFall_ShakesThenLeavesTheRuin_ThenTheResultShows()
        {
            _battle.StartBattle(Variant(0, so => so.FindProperty("defenders").arraySize = 0));
            yield return null;
            var art = Stage(0).EnemyCastle.Art;
            Assert.That(art, Is.Not.Null, "the shipped castles use the command post art");
            Assert.That(art.IntactSprite, Is.Not.Null);
            Assert.That(art.DestroyedSprite, Is.Not.Null);
            var castle = _battle.Registry.Get(_battle.EnemyCastleId);
            Assert.That(castle.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite == art.IntactSprite), Is.True,
                "the standing castle shows the intact command post");

            castle.TakeDamage(castle.MaxHealth * 2f, null);
            _battle.Simulate(Step);
            Assert.That(_battle.IsOver, Is.True);
            Assert.That(_battle.CastleFalls, Has.Count.EqualTo(1));
            var fall = _battle.CastleFalls[0];
            yield return null;
            Assert.That(_hud.IsResultVisible, Is.False, "the fall is shown first");

            for (var t = 0f; t < art.ShakeSeconds + 1f && !fall.HasCollapsed; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.That(fall.HasCollapsed, Is.True);
            Assert.That(fall.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite == art.DestroyedSprite), Is.True,
                "the ruin replaces the post");
            for (var t = 0f; t < art.ResultDelaySeconds + 1f && !_hud.IsResultVisible; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.That(_hud.IsResultVisible, Is.True);
            Assert.That(fall != null, Is.True, "the ruin stays on the field");
        }

        [UnityTest]
        public IEnumerator AttackCommand_SoldiersMakingNoProgress_AttackEnemyNearestToTarget()
        {
            // Soldiers that crawl (speed ~0) can never close in on the ordered target, a deterministic stand-in for
            // a target that is surrounded. A harmless defender stands next to the target.
            var slowTroop = Object.Instantiate(Stage(0).TestLoadout.LordHero.Troop);
            _created.Add(slowTroop);
            var troopSo = new SerializedObject(slowTroop);
            // 0.001 units/s: far below the progress threshold (0.05 per 1.5 s), so only real shoves count as progress.
            troopSo.FindProperty("stats.moveSpeed").floatValue = 0.001f;
            troopSo.ApplyModifiedPropertiesWithoutUndo();

            var archer = Object.Instantiate(Stage(0).TestLoadout.GetCards()[2]);
            _created.Add(archer);
            var heroSo = new SerializedObject(archer);
            heroSo.FindProperty("troop").objectReferenceValue = slowTroop;
            heroSo.ApplyModifiedPropertiesWithoutUndo();

            var loadout = Object.Instantiate(Stage(0).TestLoadout);
            _created.Add(loadout);
            var loadoutSo = new SerializedObject(loadout);
            loadoutSo.FindProperty("heroes").GetArrayElementAtIndex(1).FindPropertyRelative("hero").objectReferenceValue = archer;
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
            Run(2f); // longer than the 1.5 s timeout, measured from when the hero has walked off
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
            var post = new Vector2(-6f, -6f); // open meadow on the player side, clear of the river
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
        public IEnumerator EdgeIcon_ForHeroBelowTheScreen_IsAboveTheCardBar()
        {
            _battle.StartBattle(0);
            Assert.That(_battle.TrySummon(SummonRequest.Free(0, new Vector2(-12f, -8.5f))).Accepted, Is.True);
            var hero = _battle.SelectedHero;
            yield return null;

            _camera.Pan(new Vector2(0f, -100000f)); // view moves up as far as the map allows
            yield return null;
            yield return null;

            var viewport = _camera.WorldToViewport(hero.Position);
            Assert.That(viewport.y, Is.LessThan(0f), "test setup: the hero is below the screen");
            var indicators = Object.FindAnyObjectByType<OffscreenHeroIndicators>();
            Assert.That(indicators.TryGetIndicatorViewport(hero.Id, out var edge), Is.True);
            Assert.That(edge.y, Is.GreaterThanOrEqualTo(_hud.CardBarTopViewport), "not hidden behind the cards");
        }

        [UnityTest]
        public IEnumerator Presentation3D_UnitsWithoutArtStandAsLitBodies_FacingTheirWay()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;
            var enemy = _battle.Registry.Units.OfType<EnemyUnit>().First();
            Assert.That(enemy.Definition.Sprite, Is.Null, "placeholder enemy");
            var body = enemy.GetComponentsInChildren<MeshRenderer>().Where(r => r.transform.parent.name == "Body3D").ToArray();
            Assert.That(body.Length, Is.EqualTo(2), "capsule + nose");
            Assert.That(enemy.transform.Find("Body").GetComponent<SpriteRenderer>().enabled, Is.False, "no flat circle");
            Assert.That(enemy.transform.Find("Shadow"), Is.Not.Null, "soft ground shadow");
            // The body stands up from the ground (up = -Z), whatever the root's billboard rotation.
            var capsule = body.Single(r => r.name == "Capsule").transform;
            Assert.That(Vector3.Dot(capsule.up, Vector3.back), Is.GreaterThan(0.999f));

            // Walking turns it: after moving, its forward points along the move on the ground.
            var start = enemy.Position;
            Run(1f);
            yield return null;
            var moved = enemy.Position - start;
            if (moved.sqrMagnitude > 0.01f)
            {
                var forward = (Vector2)capsule.forward;
                Assert.That(Vector2.Dot(forward.normalized, moved.normalized), Is.GreaterThan(0.7f));
            }
            // A hero with a picture keeps it (billboard), no capsule.
            if (hero.Definition.Sprite != null)
                Assert.That(hero.transform.Find("Body3D"), Is.Null);
        }

        [UnityTest]
        public IEnumerator ViewTilt_SquashesTheGround_PickingStillHitsLogicPositions()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;
            // A stage with painted map art starts at the art's fixed 3/4 angle; without art it starts top-down.
            var expectedStart = Stage(0).BackgroundArt != null ? Stage(0).BackgroundArtTiltDegrees : 0f;
            Assert.That(_battle.ViewTiltDegrees, Is.EqualTo(expectedStart).Within(0.01f));
            Assert.That(_hud.TiltSlider.value, Is.EqualTo(Mathf.Round(expectedStart)).Within(1f), "slider shows the angle");

            _hud.TiltSlider.value = 45f; // as if the player dragged the HUD slider
            yield return null;
            yield return null;

            var scale = Mathf.Cos(45f * Mathf.Deg2Rad);
            Assert.That(_battle.ViewTiltDegrees, Is.EqualTo(45f));
            Assert.That(_battle.GroundProjection, Is.EqualTo(scale).Within(1e-4f));
            Assert.That(hero.transform.position, Is.EqualTo(_battle.ToView(hero.Position)), "drawn at its logic position");
            Assert.That(hero.transform.position.x, Is.EqualTo(hero.Position.x).Within(1e-3f));
            if (_battle.Presentation3D)
            {
                // 3D: the ground keeps its size, the tilted camera squashes it, and the unit faces the camera.
                Assert.That(hero.transform.position.y, Is.EqualTo(hero.Position.y).Within(1e-3f));
                Assert.That(Quaternion.Angle(hero.transform.rotation, Camera.main.transform.rotation), Is.LessThan(0.01f));
            }
            else
                Assert.That(hero.transform.position.y, Is.EqualTo(hero.Position.y * scale).Within(1e-3f), "drawn on the tilted ground");
            // On screen one metre north is cos(tilt) as long as one metre east, in both presentations.
            var east = _camera.WorldToScreen(hero.Position + Vector2.right) - _camera.WorldToScreen(hero.Position);
            var north = _camera.WorldToScreen(hero.Position + Vector2.up) - _camera.WorldToScreen(hero.Position);
            Assert.That(north.y / east.x, Is.EqualTo(scale).Within(1e-3f), "ground squashed by the tilt on screen");
            Assert.That(Mathf.Abs(north.x), Is.LessThan(0.01f));

            // Screen → logic round trip, and a tap on an on-screen enemy still finds it.
            var screen = _camera.WorldToScreen(hero.Position);
            Assert.That(Vector2.Distance(_camera.ScreenToWorld(screen), hero.Position), Is.LessThan(0.01f));
            var defender = _battle.Registry.Units.OfType<EnemyUnit>().First();
            _battle.TrySelectHero(hero.Id);
            Assert.That(_battle.TapAt(_camera.ScreenToWorld(_camera.WorldToScreen(defender.Position))),
                Is.EqualTo(BattleController.TapResult.Attack));

            // Gameplay is unchanged by the tilt: logic positions, not the drawing, drive the rules.
            var bounds = Stage(0).MapBounds;
            Assert.That(_camera.WorldToViewport(new Vector2(bounds.center.x, bounds.yMax)).y, Is.GreaterThanOrEqualTo(0.999f),
                "camera limits follow the squashed map");
            Assert.That(_camera.WorldToViewport(new Vector2(bounds.center.x, bounds.yMin)).y, Is.LessThanOrEqualTo(0.001f));

            _hud.TiltSlider.value = 0f;
            yield return null;
            Assert.That(hero.transform.position.y, Is.EqualTo(hero.Position.y).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator Presentation3D_GroundIsAMesh_FlatUnderTheUnits_WithHillsOutsideTheMap()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var ground = _battle.GetComponentsInChildren<MeshRenderer>().SingleOrDefault(r => r.name == "Ground3D");
            Assert.That(ground, Is.Not.Null, "decision 2: the ground is a mesh with the painted texture");
            Assert.That(_battle.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "MapArt").enabled, Is.False,
                "the flat painting is not drawn twice");
            var art = Stage(0).BackgroundArtArea;
            Assert.That(ground.bounds.min.x, Is.LessThanOrEqualTo(art.xMin), "covers the painting and its skirt");
            Assert.That(ground.bounds.max.y, Is.GreaterThanOrEqualTo(art.yMax));

            var map = Stage(0).MapBounds;
            var view = _battle.View;
            foreach (var point in new[] { map.center, map.min, map.max, new Vector2(map.xMin, map.yMax), new Vector2(map.xMax, map.yMin) })
                Assert.That(Ground3DView.Height(point, map, view), Is.Zero, $"flat where units stand ({point})");
            var outside = new Vector2(map.xMin - view.GroundRimRamp, map.center.y);
            Assert.That(Ground3DView.Height(outside, map, view), Is.GreaterThan(0.1f), "low hills outside the map");
        }

        [UnityTest]
        public IEnumerator Presentation3D_TentsAndCommandPostsFaceTheOpponent()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var centre = Stage(0).MapBounds.center.x;
            // The Meshy props' front (door, drum) is the model's -Z at yaw 0.
            var faced = _battle.GetComponentsInChildren<Transform>()
                .Where(t => (t.name.Contains("ThreeKingdomsSquare") && t.name.EndsWith(" 3D")) || t.name == "Body3D Prop")
                .ToArray();
            Assert.That(faced.Count(t => t.name.EndsWith(" 3D")), Is.EqualTo(4), "the four tents");
            Assert.That(faced.Count(t => t.name == "Body3D Prop"), Is.EqualTo(2), "both command posts");
            foreach (var model in faced)
            {
                var front = model.rotation * Vector3.back;
                var toward = model.position.x <= centre ? 1f : -1f;
                Assert.That(front.x * toward, Is.GreaterThan(0.99f), $"{model.name} at x={model.position.x:0.0} faces the other side");
            }
        }

        [UnityTest]
        public IEnumerator Presentation3D_DecorAndCommandPostsAreEnlarged_TheFormationKeepsItsSize()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var size = _battle.View.DecorModelScale;
            Assert.That(size, Is.GreaterThan(1f), "the decorations are drawn larger than their sprites");
            var props = _battle.View.MapProps.Props;
            float SpriteScale(Transform model) =>
                props.Single(p => p.mesh == model.GetComponentInChildren<MeshFilter>().sharedMesh).scale;
            var models = _battle.GetComponentsInChildren<Transform>()
                .Where(t => t.name.EndsWith(" 3D") || t.name == "Body3D Prop").ToArray();
            var decor = models.Where(t => t.name.StartsWith("Decor ") || t.name == "Body3D Prop").ToArray();
            var formation = models.Where(t => t.name.StartsWith("Pillar ") || t.name.StartsWith("Altar")).ToArray();
            Assert.That(decor.Length, Is.GreaterThan(2), "decorations and both command posts");
            Assert.That(formation.Length, Is.EqualTo(9), "8 pillars and the altar");
            foreach (var model in decor)
                Assert.That(model.localScale.x, Is.EqualTo(SpriteScale(model) * size).Within(1e-4f), model.name);
            foreach (var model in formation)
                Assert.That(model.localScale.x, Is.EqualTo(SpriteScale(model)).Within(1e-4f), $"{model.name} matches its obstacle");
        }

        [UnityTest]
        public IEnumerator MapArt_CoversTheMap_AndHidesPlaceholderTerrain()
        {
            _battle.StartBattle(0);
            yield return null;
            Assert.That(Stage(0).BackgroundArt, Is.Not.Null, "the shipped stages use the Bát Quái Trận ground as background art");

            var art = _battle.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "MapArt");
            var bounds = Stage(0).BackgroundArtArea;
            Assert.That(bounds.width, Is.GreaterThan(Stage(0).MapBounds.width), "the painting has a rim outside the map");
            var scale = _battle.ViewYScale;
            Assert.That(art.bounds.size.x, Is.EqualTo(bounds.width).Within(0.01f), "art spans the map width");
            Assert.That(art.bounds.size.y, Is.EqualTo(bounds.height * scale).Within(0.01f), "and the tilted map height");
            // 3D: the ground keeps its true size and the tilted camera squashes it on screen.
            Assert.That(_battle.GroundProjection, Is.EqualTo(Stage(0).BackgroundArtVerticalScale).Within(1e-3f), "at the art's own angle");
            Assert.That(_battle.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Water" || r.name == "Ground"),
                Is.False, "no placeholder water/ground over the painting");
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
        public IEnumerator CardTap_QuickSummons_ThenTapUsesTheUltimate_HoldSelects()
        {
            _battle.StartBattle(0);
            yield return null;
            var first = _hud.CardViews[0];
            var second = _hud.CardViews[1];

            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(second.gameObject, Pointer(ScreenPositionOf(second)), ExecuteEvents.pointerClickHandler);
            Assert.That(_battle.State.Cards[0].State, Is.EqualTo(CardState.Deployed));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1));

            // Spec §3: tapping a deployed card uses its ultimate (none learned yet here), holding it selects the hero.
            var rejected = new List<RejectReason>();
            _battle.ActionRejected += rejected.Add;
            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            Assert.That(rejected, Is.EqualTo(new[] { RejectReason.SkillNotReady }));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1), "a tap does not select any more");

            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerDownHandler);
            first.SelectByHold();
            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(0), "holding a deployed card selects its hero");
            Assert.That(rejected, Has.Count.EqualTo(1), "the click that ends a hold does nothing");
            Assert.That(_battle.State.SummonedCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator CardDrag_IntoSummonZoneSummons_ReleasedOverUiCancels()
        {
            _battle.StartBattle(0);
            yield return null;
            var card = _hud.CardViews[1];
            var zoneCenter = Stage(0).SummonZone.center;
            var zoneScreen = _camera.WorldToScreen(zoneCenter);
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
        public IEnumerator Screenshot_Props3D_CloseUps()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var geometry = _battle.Formation.Geometry;
            // A closed gate shows its slab standing between two pillars.
            _battle.DevToggleGate(0);
            Run(2f);
            yield return null;
            var cam = Camera.main;
            var controller = cam.GetComponent<BattleCameraController>();
            var size = cam.orthographicSize;
            controller.enabled = false;
            try
            {
                cam.orthographicSize = 3.2f;
                foreach (var (file, point) in new[]
                         {
                             ("props3d_gate.png", geometry.Gates[0].Position), ("props3d_altar.png", geometry.AltarCenter),
                             ("props3d_castle.png", Stage(0).EnemyCastle.Position)
                         })
                {
                    BattleView3D.PlaceCamera(cam.transform, _battle, point + Vector2.up * 0.8f);
                    Capture(file);
                }
            }
            finally
            {
                cam.orthographicSize = size;
                controller.enabled = true;
            }
        }

        [UnityTest]
        public IEnumerator Screenshot_AttackAndDefense()
        {
            _battle.StartBattle(0);
            SummonQuick(0);
            SummonQuick(1);
            _battle.MoveSelectedHero(new Vector2(-8f, 2f));
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

            // The whole map at once (both camps, the ring, the decorations), beyond the in-game zoom limit.
            _battle.StartBattle(0);
            SummonQuick(0);
            Run(2f);
            yield return null;
            var overview = Camera.main;
            var savedSize = overview.orthographicSize;
            var savedPosition = overview.transform.position;
            var view = _battle.ViewBounds;
            overview.orthographicSize = Mathf.Max(view.height * 0.5f, view.width * 0.5f / overview.aspect) + 0.2f;
            if (_battle.Presentation3D)
                BattleView3D.PlaceCamera(overview.transform, _battle, TiltedViewMath.PlaneToGround(view.center, _battle.GroundProjection));
            else
                overview.transform.position = new Vector3(view.center.x, view.center.y, savedPosition.z);
            Capture("map_overview.png");
            overview.orthographicSize = savedSize;
            overview.transform.position = savedPosition;

            _battle.StartBattle(0);
            SummonQuick(0);
            SummonQuick(1);
            Run(3f);
            _battle.ViewTiltDegrees = 45f;
            for (var i = 0; i < 30; i++)
                yield return null;
            Capture("attack_tilt45.png");

            // Skills on the cards: a level-15 lord (all skills, rage bar) and starters at 1–3 stars, then a fight.
            var loadout = Stage(1).TestLoadout;
            var heroes = loadout.GetCards().Skip(1).ToList();
            _battle.StartBattle(Stage(1), new[]
            {
                new LineupEntry(loadout.LordHero, 15), new LineupEntry(heroes[0], 1, 3), new LineupEntry(heroes[1], 1, 2),
                new LineupEntry(heroes[2]), new LineupEntry(heroes[3], 1, 3)
            });
            _battle.ViewTiltDegrees = 41f;
            SummonQuick(0);
            SummonQuick(1);
            SummonQuick(4);
            Run(14f);
            _camera.FocusOn(new Vector2(0f, 0f));
            for (var i = 0; i < 30; i++)
                yield return null;
            Capture("skills.png");

            // Chapter-1 boss stage: elites with their outline, the boss bar, a red warning.
            _battle.StartBattle(_battle.Stages.First(s => s.StageId == "c1_boss_attack"));
            SummonQuick(0);
            SummonQuick(1);
            SummonQuick(2);
            foreach (var hero in _battle.LivingHeroes.ToList())
                _battle.AttackWithHero(hero.Id, _battle.LivingBosses.First());
            var boss = _battle.LivingBosses.First();
            Run(25f);
            for (var i = 0; i < 200 && boss.IsAlive && boss.Skills.Warning == null; i++)
                _battle.Simulate(Step);
            _camera.FocusOn(boss.Position);
            for (var i = 0; i < 30; i++)
                yield return null;
            Capture("boss.png");

            // The enemy command post falling: dust and debris just after it gives way, then the smoking ruin.
            _battle.StartBattle(0);
            yield return null;
            var post = _battle.Registry.Get(_battle.EnemyCastleId);
            var postPosition = post.Position;
            _camera.FocusOn(postPosition);
            post.TakeDamage(post.MaxHealth * 2f, null);
            _battle.Simulate(Step);
            var fall = _battle.CastleFalls[0];
            while (!fall.HasCollapsed)
                yield return null;
            for (var t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
                yield return null;
            _camera.FocusOn(postPosition);
            Capture("castle_fall.png");
            for (var t = 0f; t < 2f; t += Time.unscaledDeltaTime)
                yield return null;
            Capture("castle_ruin.png");
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

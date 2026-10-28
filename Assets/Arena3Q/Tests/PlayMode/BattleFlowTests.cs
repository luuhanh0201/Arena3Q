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
        public IEnumerator QuickSummon_SpawnsIdleHeroWithSquad_ThatHoldsUntilTheStanceChanges()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;

            Assert.That(hero.State, Is.EqualTo(HeroState.Idle));
            Assert.That(hero.Squad.Members, Has.Count.EqualTo(hero.Definition.BaseSquadSize));
            Assert.That(_battle.Layout.IsWalkable(hero.Position), Is.True);
            Assert.That(_battle.SelectedHero, Is.EqualTo(hero));

            Run(8f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Idle), "Giữ: no automatic advance after 5 seconds any more");
            _battle.SetStance(ArmyStance.Attack);
            Run(0.2f);
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
        public IEnumerator Stance_StartsInHold_SwitchesEveryHero_AndLaterSummonsFollowIt()
        {
            _battle.StartBattle(0);
            var first = SummonQuick(0);
            yield return null;
            Assert.That(_battle.Stance, Is.EqualTo(ArmyStance.Hold), "spec §5: every battle starts in Giữ");
            Assert.That(first.Brain.Stance, Is.EqualTo(ArmyStance.Hold));
            Run(8f);
            Assert.That(first.State, Is.EqualTo(HeroState.Idle), "Giữ: no automatic advance after 5 seconds any more");

            var changes = new List<ArmyStance>();
            _battle.StanceChanged += changes.Add;
            Assert.That(_battle.SetStance(ArmyStance.Attack), Is.True);
            Assert.That(_battle.SetStance(ArmyStance.Attack), Is.False, "choosing the active stance again does nothing");
            Assert.That(first.State, Is.EqualTo(HeroState.Advancing));
            var second = SummonQuick(1);
            Assert.That(second.Brain.Stance, Is.EqualTo(ArmyStance.Attack), "a later summon follows the active stance");
            Assert.That(second.State, Is.EqualTo(HeroState.Advancing));

            Assert.That(_battle.SetStance(ArmyStance.Defend), Is.True);
            var zone = Stage(0).SummonZone;
            foreach (var hero in new[] { first, second })
            {
                Assert.That(hero.Brain.Stance, Is.EqualTo(ArmyStance.Defend));
                Assert.That(zone.Contains(hero.Brain.DefendPoint), Is.True, "falls back into the area in front of the own castle");
            }
            Assert.That(changes, Is.EqualTo(new[] { ArmyStance.Attack, ArmyStance.Defend }));
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
            // The player summons five heroes and picks Tấn công (spec §5 "Cơ chế mới"): they advance and fight their way
            // to the castle, as the old 5-second rule did.
            _battle.StartBattle(0);
            yield return null;
            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
                SummonQuick(i);
            _battle.SetStance(ArmyStance.Attack);

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
            // Tấn công meets the waves like the old automatic advance did (balance numbers stay comparable).
            _battle.SetStance(ArmyStance.Attack);

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

        // The whole view lies on the ground the map builds (map plus outer ground): no empty background (user 2026-10-28).
        private void AssertViewOnTheGround()
        {
            var cam = _camera.Camera;
            var ground = _camera.GroundBounds;
            foreach (var corner in new[] { Vector2.zero, new Vector2(cam.pixelWidth, 0f), new Vector2(0f, cam.pixelHeight),
                         new Vector2(cam.pixelWidth, cam.pixelHeight) })
            {
                var point = _camera.ScreenToWorld(corner);
                Assert.That(point.x, Is.InRange(ground.xMin - 0.05f, ground.xMax + 0.05f), $"corner {corner} at {point}");
                Assert.That(point.y, Is.InRange(ground.yMin - 0.05f, ground.yMax + 0.05f), $"corner {corner} at {point}");
            }
        }

        [UnityTest]
        public IEnumerator Camera_ZoomAndPanStayOnTheGround_EdgeIconBringsTheHeroIntoView()
        {
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;
            var cam = _camera.Camera;
            var centre = new Vector2(cam.pixelWidth, cam.pixelHeight) * 0.5f;
            Assert.That(cam.orthographicSize, Is.EqualTo(_camera.DefaultOrthographicSize).Within(1e-3f));
            AssertViewOnTheGround();

            // Zoom in to the limit (asset flaws must not show up close), then drag far away from the hero.
            _camera.Zoom(0.01f, centre);
            Assert.That(cam.orthographicSize, Is.EqualTo(_camera.MinOrthographicSize).Within(1e-3f));
            Assert.That(_camera.MinOrthographicSize, Is.EqualTo(_camera.DefaultOrthographicSize * _battle.View.ZoomInLimit).Within(1e-3f));
            _camera.Pan(new Vector2(100000f, 100000f));
            yield return null;
            yield return null;
            AssertViewOnTheGround();

            var indicators = Object.FindAnyObjectByType<OffscreenHeroIndicators>();
            Assert.That(EdgeIndicatorMath.IsOnScreen(_camera.WorldToViewport(hero.Position)), Is.False, "test setup: hero off screen");
            Assert.That(indicators.TryGetIndicatorViewport(hero.Id, out _), Is.True);

            // Tapping the edge icon only brings the hero into view (no selection any more).
            _hud.FocusHero(hero.Id);
            for (var t = 0f; t < 2f; t += Time.unscaledDeltaTime)
                yield return null;
            var viewport = _camera.WorldToViewport(hero.Position);
            Assert.That(EdgeIndicatorMath.IsOnScreen(viewport), Is.True, $"hero back in view at {viewport}");

            // Zoom out to the limit: still no empty background.
            _camera.Zoom(1000f, centre);
            Assert.That(cam.orthographicSize, Is.EqualTo(_camera.MaxOrthographicSize).Within(1e-3f));
            yield return null;
            AssertViewOnTheGround();
        }

        [UnityTest]
        public IEnumerator EdgeIcon_ForHeroBelowTheScreen_IsAboveTheCardBar()
        {
            _battle.StartBattle(0);
            Assert.That(_battle.TrySummon(SummonRequest.Free(0, new Vector2(-12f, -8.5f))).Accepted, Is.True);
            var hero = _battle.SelectedHero;
            yield return null;

            var cam = _camera.Camera;
            _camera.Zoom(0.01f, new Vector2(cam.pixelWidth, cam.pixelHeight) * 0.5f); // closest zoom
            _camera.Pan(new Vector2(0f, -100000f)); // view moves up as far as the ground allows
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
        public IEnumerator View_IsAFixedDiagonal_NoAngleControl_PickingStillHitsLogicPositions()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            var hero = SummonQuick(0);
            yield return null;
            // User 2026-10-28: tilt 40° from straight down (50° above the ground), turned 45°, nothing changes it.
            Assert.That(_battle.ViewTiltDegrees, Is.EqualTo(_battle.View.DefaultTiltDegrees).Within(0.01f));
            Assert.That(_battle.View.DefaultTiltDegrees, Is.EqualTo(40f).Within(0.01f));
            Assert.That(_battle.ViewYawDegrees, Is.EqualTo(45f).Within(0.01f));
            Assert.That(_hud.GetComponentsInChildren<Slider>(true), Is.Empty, "no view angle slider any more");

            Assert.That(hero.transform.position, Is.EqualTo(_battle.ToView(hero.Position)), "drawn at its logic position");
            Assert.That(Quaternion.Angle(hero.transform.rotation, Camera.main.transform.rotation), Is.LessThan(0.01f),
                "upright pictures face the turned camera");

            // Diagonal: the map's +x (towards the enemy) runs to the top right of the screen, +y to the top left.
            var east = _camera.WorldToScreen(hero.Position + Vector2.right) - _camera.WorldToScreen(hero.Position);
            var north = _camera.WorldToScreen(hero.Position + Vector2.up) - _camera.WorldToScreen(hero.Position);
            Assert.That(east.x, Is.GreaterThan(0f));
            Assert.That(east.y, Is.GreaterThan(0f));
            Assert.That(north.x, Is.LessThan(0f));
            Assert.That(north.y, Is.GreaterThan(0f));
            var own = _camera.WorldToScreen(Stage(0).PlayerCastle.Position);
            var enemy = _camera.WorldToScreen(Stage(0).EnemyCastle.Position);
            Assert.That(own.x < enemy.x && own.y < enemy.y, Is.True, "own camp bottom left, enemy top right");

            // Screen → logic round trip (a ray down to the ground), so taps and drags still land on logic positions.
            var screen = _camera.WorldToScreen(hero.Position);
            Assert.That(Vector2.Distance(_camera.ScreenToWorld(screen), hero.Position), Is.LessThan(0.01f));
            var defender = _battle.Registry.Units.OfType<EnemyUnit>().First();
            Assert.That(Vector2.Distance(_camera.ScreenToWorld(_camera.WorldToScreen(defender.Position)), defender.Position),
                Is.LessThan(0.01f));
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
        public IEnumerator MacAnhUltimate_BuffsAlliesAroundHer_HealsAndFillsOthersRage_WithoutDamage()
        {
            var macAnh = AssetDatabase.FindAssets("t:HeroDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<HeroDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .First(h => h != null && h.MacAnhPerformance != null);
            var ultimate = macAnh.GetSkill(SkillSlot.Ultimate);
            Assert.That(ultimate.Targeting, Is.EqualTo(SkillTargeting.Self), "centred on Mặc Ảnh");
            Assert.That(ultimate.Radius, Is.EqualTo(5f));
            Assert.That(ultimate.Effects.Any(e => e.kind == SkillEffectKind.Damage), Is.False, "no damage any more");
            var other = Stage(0).TestLoadout.GetLineup().First(e => e.Hero.MacAnhPerformance == null && e.Hero.Kind != HeroKind.Lord);
            _battle.StartBattle(Stage(0), new[] { new LineupEntry(macAnh, 60), other });
            Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted, Is.True);
            Assert.That(_battle.TrySummon(SummonRequest.Quick(1)).Accepted, Is.True);
            var caster = _battle.GetHero(_battle.State.Cards[0].Hero);
            var ally = _battle.GetHero(_battle.State.Cards[1].Hero);
            foreach (var unit in _battle.Registry.Units)
                if (unit != null && unit.Team != caster.Team)
                    unit.ApplyStatus(StatusKind.Stun, 1, 30);
            yield return null;
            // The ally stands 2 m away, hurt, with an empty bar, and holds still (no rage from its own hits).
            ally.SetPosition(caster.Position + Vector2.right * 2f);
            ally.ApplyStatus(StatusKind.Stun, 1, 10);
            ally.TakeDamage(ally.MaxHealth * .5f, null, true);
            ally.Skills.Rage.Reset();
            var hurt = ally.Health;
            caster.Skills.Rage.Add(caster.Skills.Rage.Max);
            Assert.That(_battle.TryUseUltimate(0).Accepted, Is.True);
            for (var i = 0; i < 200; i++)
            {
                _battle.Simulate(1f / 60f);
                if (i % 10 == 0)
                    yield return null;
            }
            Assert.That(ally.Statuses.Has(StatusKind.DamageUp), Is.True, "+Công");
            Assert.That(caster.Statuses.Has(StatusKind.DamageUp), Is.True, "Mặc Ảnh stands in her own circle");
            Assert.That(ally.Health, Is.EqualTo(Mathf.Min(ally.MaxHealth, hurt + ally.MaxHealth * .2f)).Within(ally.MaxHealth * .01f), "heals 20 %");
            Assert.That(ally.Skills.Rage.Fraction, Is.EqualTo(.3f).Within(.02f), "the other hero gets 30 % of its bar");
            Assert.That(caster.Skills.Rage.Fraction, Is.LessThan(.3f), "not her own bar");
            var troops = Object.FindObjectsByType<TroopUnit>(FindObjectsSortMode.None)
                .Where(t => t.Team == caster.Team && Vector2.Distance(t.Position, caster.Position) <= 5f).ToArray();
            Assume.That(troops, Is.Not.Empty, "soldiers stood in the circle");
            Assert.That(troops.All(t => t.Statuses.Has(StatusKind.DamageUp)), Is.True, "soldiers get the buff too");
        }

        [UnityTest]
        public IEnumerator StoreEffect_StandsInForSkill01_OnTheBattleClock_FlatOnTheGround()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var set = AssetDatabase.FindAssets("t:HeroDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<HeroDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .First(h => h != null && h.MacAnhPerformance != null).MacAnhPerformance;
            Assert.That(set.OwnEffect("MagicArray").GetComponentInChildren<MacAnhVfxPlayer>(), Is.Not.Null, "the own flipbook stays");
            var store = set.Effect("MagicArray");
            Assume.That(store.GetComponent<ParticleFxPresenter>(), Is.Not.Null,
                "Hovl Studio's files are on this machine (Assets/ThirdParty, not in the repository); without them the own effect plays");
            var fx = MacAnhFxPool.For(_battle).Spawn(store, Vector3.zero, new Vector3(2f, 2f, 1f), Quaternion.identity,
                MacAnhFxQuality.Medium, 3f, 100);
            var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
            fx.Advance(.5f);
            var count = systems.Sum(p => p.particleCount);
            Assert.That(count, Is.GreaterThan(0), "played by the battle clock");
            yield return null;
            Assert.That(systems.Sum(p => p.particleCount), Is.EqualTo(count), "it does not run on its own (battle pause, speed)");
            var turned = fx.transform.Find("Y up to battle up");
            Assert.That((turned.rotation * Vector3.up - Vector3.back).magnitude, Is.LessThan(1e-3f), "authored Y up, stands on the battle ground");
            fx.Stop();
        }

        [UnityTest]
        public IEnumerator WindZone_SwaysTheCaughtUnitsPicture_ThenItSettles()
        {
            _battle.StartBattle(0);
            yield return null;
            var enemy = _battle.Registry.Units.First(u => u != null && u is EnemyUnit && u.IsAlive);
            var start = enemy.Position;
            enemy.Sway(.12f, .6f);
            var largest = 0f;
            for (var i = 0; i < 20; i++)
            {
                yield return null;
                largest = Mathf.Max(largest, (enemy.transform.position - _battle.ToView(enemy.Position)).magnitude);
            }
            Assert.That(largest, Is.GreaterThan(.02f).And.LessThan(.25f), "lifted and rocked a little, not thrown");
            Assert.That(enemy.Position, Is.EqualTo(start), "pictures only: the unit does not move");
            yield return new WaitForSeconds(.8f);
            Assert.That((enemy.transform.position - _battle.ToView(enemy.Position)).magnitude, Is.LessThan(1e-3f), "settles when the wind stops");
        }

        [UnityTest]
        public IEnumerator FullRageTestOption_HeroesComeOnWithAFullBar_OnlyWhenTheLaunchAsks()
        {
            _battle.StartBattle(0);
            var normal = SummonQuick(0);
            yield return null;
            Assert.That(normal.Skills.Rage.Value, Is.Zero, "a battle played as is starts with empty rage");

            _battle.Launch(new BattleLaunchRequest { Stage = Stage(0), FullRageAtStart = true });
            var tester = SummonQuick(0);
            yield return null;
            Assert.That(tester.Skills.Rage.IsFull, Is.True, "Development test option: the ultimate is ready at once");
        }

        [UnityTest]
        public IEnumerator Presentation3D_MacAnhRunning_PlantedFeetDoNotSlide()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            var definition = AssetDatabase.FindAssets("t:HeroDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<HeroDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .First(h => h != null && h.MacAnhPerformance != null);
            foreach (var (distance, gait) in new[] { (8f, "run") })
            {
                _battle.StartBattle(Stage(0), new[] { new LineupEntry(definition, 1) });
                Assert.That(_battle.TrySummon(SummonRequest.Quick(0)).Accepted, Is.True);
                var hero = _battle.GetHero(_battle.State.Cards[0].Hero);
                foreach (var unit in _battle.Registry.Units)
                    if (unit != null && unit.Team != hero.Team)
                        unit.ApplyStatus(StatusKind.Stun, 1, 30);
                _battle.TrySelectHero(hero.Id);
                yield return null;
                var toes = hero.GetComponentsInChildren<Transform>(true).Where(t => t.name == "LeftToe" || t.name == "RightToe")
                    .ToArray();
                Assert.That(toes.Length, Is.EqualTo(2), "the 3D model is in the battle");
                _battle.MoveSelectedHero(hero.Position + Vector2.left * distance);
                const float dt = 1f / 60f;
                // Ground velocity of each toe along the run direction (+ = with the body = gliding, - = backwards).
                var direction = Vector2.left;
                var samples = new List<(int toe, float height, float groundSpeed, float bodySpeed)>();
                var last = toes.Select(t => t.position).ToArray();
                var lastBody = hero.transform.position;
                var lastClip = hero.Performance.View.Seconds;
                for (var frame = 0; frame < 80; frame++)
                {
                    _battle.Simulate(dt);
                    yield return null;
                    var body = ((Vector2)(hero.transform.position - lastBody)).magnitude / dt;
                    if (frame == 40)
                        Debug.Log($"[Arena3Q] FEET diag: body {body:0.00} m/s, locomotion {hero.LocomotionVelocity.magnitude:0.00}, " +
                                  $"clip rate {(hero.Performance.View.Seconds - lastClip) / dt:0.00} s/s, action {hero.Performance.View.Action}, " +
                                  $"drawn {hero.DrawnSize}, toe scale {toes[0].lossyScale.x:0.000}");
                    lastBody = hero.transform.position;
                    lastClip = hero.Performance.View.Seconds;
                    for (var i = 0; i < toes.Length; i++)
                    {
                        var p = toes[i].position;
                        // Ground = XY, up = -Z; only once the gait has settled.
                        if (frame >= 15 && body > .05f)
                            samples.Add((i, -p.z, Vector2.Dot(new Vector2(p.x - last[i].x, p.y - last[i].y), direction) / dt, body));
                        last[i] = p;
                    }
                }
                Assert.That(samples, Is.Not.Empty, $"{gait}: the hero moved");
                // A planted toe (within 5 mm of its lowest point, at the drawn size) must stay put while the body moves.
                var planted = samples.Where(s => s.height < samples.Where(o => o.toe == s.toe).Min(o => o.height) + .005f * hero.DrawnSize)
                    .ToArray();
                var slip = planted.Average(s => s.groundSpeed) / planted.Average(s => s.bodySpeed);
                Debug.Log($"[Arena3Q] FEET {gait}: planted toe moves at {slip:P0} of the body speed ({planted.Length} samples)");
                Assert.That(Mathf.Abs(slip), Is.LessThan(.12f), $"{gait}: planted feet slide (+ gliding, - moonwalking)");
            }
        }

        [UnityTest]
        public IEnumerator Presentation3D_Bloom_FollowsTheViewData()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            var view = _battle.View;
            var data = Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Assert.That(data.renderPostProcessing, Is.EqualTo(view.Bloom), "post-processing only when the data asks for bloom");
            var volumes = _battle.GetComponentsInChildren<UnityEngine.Rendering.Volume>();
            Assert.That(volumes.Length, Is.EqualTo(view.Bloom ? 1 : 0), "one bloom volume per battle");
            if (!view.Bloom)
                yield break;
            Assert.That(volumes[0].isGlobal, Is.True);
            Assert.That(volumes[0].sharedProfile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom), Is.True);
            Assert.That(bloom.intensity.value, Is.EqualTo(view.BloomIntensity));
            Assert.That(bloom.threshold.value, Is.EqualTo(view.BloomThreshold));
            Assert.That(BattleView3D.EffectBloomBoost, Is.EqualTo(view.EffectBloomBoost), "effects push their light");
            // A second battle reuses the profile and does not pile up volumes.
            _battle.StartBattle(0);
            yield return null;
            Assert.That(_battle.GetComponentsInChildren<UnityEngine.Rendering.Volume>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Presentation3D_AreaEffectPicturesComeForward_TheCasterAuraStaysBehind()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            yield return null;
            const string folder = "Assets/Arena3Q/Art/Heroes/MacAnh/VFX/Flipbook/";
            var forward = _battle.UprightRotation * Vector3.forward;
            // Distance towards the camera of the first upright layer, along the view (the screen position does not move).
            float Forward(string prefab, string layerName)
            {
                var fx = MacAnhFxPool.For(_battle).Spawn(AssetDatabase.LoadAssetAtPath<GameObject>(folder + prefab), Vector3.zero,
                    new Vector3(2f, 2f, 1f), Quaternion.identity, MacAnhFxQuality.Medium, 3f, 100);
                fx.Advance(.5f);
                var root = fx.GetComponentInChildren<MacAnhVfxPlayer>().transform.Find(layerName);
                var toward = -Vector3.Dot(root.position - fx.transform.position, forward);
                fx.Stop();
                return toward;
            }
            Assert.That(Forward("FX_MacAnh_UltimateFlipbook_Battle.prefab", "Beams"),
                Is.EqualTo(_battle.View.EffectDepthBias * 2f).Within(1e-3f), "the qi over a target is drawn in front of its model");
            Assert.That(Forward("FX_MacAnh_MagicArrayFlipbook_Battle.prefab", "Spikes"),
                Is.EqualTo(_battle.View.EffectDepthBias * 2f).Within(1e-3f), "the same for the circle's rising qi");
            Assert.That(Forward("FX_MacAnh_UltimateAuraFlipbook_Battle.prefab", "Circle"), Is.EqualTo(0f).Within(1e-3f),
                "the rune circle behind the caster stays behind");
            // The cyclone's far half goes behind the units caught in it, its near half in front: they stand inside it.
            Assert.That(Forward("FX_MacAnh_CycloneFlipbook_Battle.prefab", "ColumnFront"),
                Is.EqualTo(_battle.View.EffectDepthBias * 2f).Within(1e-3f));
            Assert.That(Forward("FX_MacAnh_CycloneFlipbook_Battle.prefab", "ColumnBack"),
                Is.EqualTo(-_battle.View.EffectDepthBias * 2f).Within(1e-3f));
            Assert.That(Forward("FX_MacAnh_CycloneFlipbook_Battle.prefab", "DustBack"), Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator Presentation3D_HeroesAreDrawnLarger_TroopsKeepTheirSize()
        {
            Assume.That(_battle.Presentation3D, Is.True, "3D presentation is the shipped default");
            _battle.StartBattle(0);
            var heroes = Enumerable.Range(0, _battle.State.Cards.Count).Select(SummonQuick).ToArray();
            yield return null;
            var size = _battle.View.HeroModelScale;
            Assert.That(size, Is.GreaterThan(1f), "heroes stand out from their troops");
            // The drawn body: the placeholder body if the hero has no picture, else the picture (its model hangs on it).
            Transform Drawn(Component unit) =>
                unit.transform.Find("Body3D") ?? unit.transform.Find("Body");
            foreach (var hero in heroes)
            {
                Assert.That(Drawn(hero).localScale.x, Is.EqualTo(size).Within(1e-4f), hero.name);
                Assert.That(hero.Radius, Is.EqualTo(HeroUnit.BodyRadius).Within(1e-4f), "gameplay size unchanged");
            }
            var troops = Object.FindObjectsByType<TroopUnit>(FindObjectsSortMode.None);
            Assert.That(troops, Is.Not.Empty);
            foreach (var troop in troops)
                Assert.That(Drawn(troop).localScale.x, Is.EqualTo(1f).Within(1e-4f), troop.name);
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
            // 3D: the painting lies flat on the ground mesh, so the fixed camera angle (user 2026-10-28) need not match it.
            if (_battle.Presentation3D)
                Assert.That(_battle.ViewTiltDegrees, Is.EqualTo(_battle.View.DefaultTiltDegrees).Within(1e-3f), "the fixed camera angle");
            else
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
        public IEnumerator CardTap_QuickSummons_ThenTapUsesTheUltimate_NothingElse()
        {
            _battle.StartBattle(0);
            yield return null;
            var first = _hud.CardViews[0];
            var second = _hud.CardViews[1];

            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(second.gameObject, Pointer(ScreenPositionOf(second)), ExecuteEvents.pointerClickHandler);
            Assert.That(_battle.State.Cards[0].State, Is.EqualTo(CardState.Deployed));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1));

            // Spec §5 "Cơ chế mới": tapping a deployed card uses its ultimate (none learned yet here) and nothing else;
            // there is no hold-to-select and no aimed drag any more.
            var rejected = new List<RejectReason>();
            _battle.ActionRejected += rejected.Add;
            ExecuteEvents.Execute(first.gameObject, Pointer(ScreenPositionOf(first)), ExecuteEvents.pointerClickHandler);
            Assert.That(rejected, Is.EqualTo(new[] { RejectReason.SkillNotReady }));
            Assert.That(_battle.SelectedHero.CardIndex, Is.EqualTo(1), "a tap does not select");
            Assert.That(typeof(HeroCardView).GetMethod("SelectByHold"), Is.Null, "no hold-to-select");
            Assert.That(first.GetComponent<IPointerDownHandler>(), Is.Null, "a card does not track how long it is held");
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
            var overview = _camera.Camera;
            _camera.Zoom(1000f, new Vector2(overview.pixelWidth, overview.pixelHeight) * 0.5f); // the in-game zoom-out limit
            yield return null;
            Capture("map_overview.png");

            // Closest zoom (the in-game limit) on the fight.
            _battle.StartBattle(0);
            SummonQuick(0);
            SummonQuick(1);
            _battle.SetStance(ArmyStance.Attack);
            Run(3f);
            _camera.Zoom(0.01f, new Vector2(overview.pixelWidth, overview.pixelHeight) * 0.5f);
            _camera.FocusOn(_battle.SelectedHero.Position);
            for (var i = 0; i < 30; i++)
                yield return null;
            Capture("attack_closest_zoom.png");

            // Skills on the cards: a level-15 lord (all skills, rage bar) and starters at 1–3 stars, then a fight.
            var loadout = Stage(1).TestLoadout;
            var heroes = loadout.GetCards().Skip(1).ToList();
            _battle.StartBattle(Stage(1), new[]
            {
                new LineupEntry(loadout.LordHero, 15), new LineupEntry(heroes[0], 1, 3), new LineupEntry(heroes[1], 1, 2),
                new LineupEntry(heroes[2]), new LineupEntry(heroes[3], 1, 3)
            });
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

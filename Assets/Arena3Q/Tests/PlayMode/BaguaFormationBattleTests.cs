#if UNITY_EDITOR
// Edits stage copies through SerializedObject, so these tests run in the Editor only.
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
    /// <summary>The Bát Quái Trận in a real battle (docs/BanDo/BatQuaiTran.md): routes, slabs, gate effects, altar, HUD.</summary>
    public class BaguaFormationBattleTests
    {
        private const float Step = 0.05f;

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

        /// <summary>The Attack stage without defenders and with a harmless enemy castle.</summary>
        private StageConfig Quiet()
        {
            var copy = Object.Instantiate(_battle.Stages[0]);
            _created.Add(copy);
            var so = new SerializedObject(copy);
            so.FindProperty("defenders").arraySize = 0;
            so.FindProperty("enemyCastle.canAttack").boolValue = false;
            so.FindProperty("timeLimit").floatValue = 600f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }

        private HeroUnit StartWithHero()
        {
            _battle.StartBattle(Quiet());
            var decision = _battle.TrySummon(SummonRequest.Quick(0));
            Assert.That(decision.Accepted, Is.True, decision.Result.ToString());
            return _battle.GetHero(_battle.State.Cards[0].Hero);
        }

        private void Run(float seconds, System.Func<bool> until = null, System.Action eachStep = null)
        {
            var steps = Mathf.CeilToInt(seconds / Step);
            for (var i = 0; i < steps && !_battle.IsOver; i++)
            {
                if (until != null && until())
                    return;
                _battle.Simulate(Step);
                eachStep?.Invoke();
            }
        }

        private BaguaFormation Formation => _battle.Formation;
        private BaguaGateSpot Gate(int k) => Formation.Geometry.Gates[k];

        private static Vector2 Outward(int gate) =>
            new Vector2(Mathf.Cos(gate * 45f * Mathf.Deg2Rad), Mathf.Sin(gate * 45f * Mathf.Deg2Rad));

        /// <summary>Puts the hero (and squad) outside a gate and orders it through to the inside of the ring.</summary>
        private Vector2 SendThroughGate(HeroUnit hero, int gate)
        {
            var outside = Gate(gate).Position + Outward(gate) * 2.5f;
            hero.SetPosition(outside);
            foreach (var troop in hero.Squad.Members)
                troop.SetPosition(outside + Outward(gate) * 1.2f);
            var inside = Gate(gate).Position - Outward(gate) * 2.2f;
            Assert.That(_battle.MoveSelectedHero(inside).Accepted, Is.True);
            return inside;
        }

        // ---- Map ----

        [UnityTest]
        public IEnumerator ShippedStages_UseTheFormation_AndTheMiddleLaneIs39()
        {
            foreach (var stage in _battle.Stages)
            {
                Assert.That(stage.Formation, Is.Not.Null, $"{stage.name} uses the Bát Quái Trận");
                Assert.That(stage.Rivers, Is.Empty, $"{stage.name} has no river any more");
            }
            _battle.StartBattle(0);
            yield return null;
            Assert.That(_battle.Layout.Obstacles.OfType<CircleObstacle>().Count(), Is.EqualTo(8), "8 pillar clusters");
            Assert.That(_battle.Layout.Obstacles.OfType<WallObstacle>().Count(), Is.EqualTo(8), "8 gate slabs");
            var lane = _battle.Layout.RouteLength(_battle.Stage.PlayerCastle.Position, _battle.Stage.EnemyCastle.Position,
                BattlefieldLayout.DefaultMaxUnitRadius);
            Assert.That(lane, Is.EqualTo(39f).Within(0.5f), "(e) castle to castle through gates 4 and 0");
            Assert.That(_battle.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name.StartsWith("Pillar")), Is.EqualTo(8));
        }

        // ---- (b) a gate closing on the way ----

        [UnityTest]
        public IEnumerator GateClosingOnTheWay_HeroGoesAnotherWay_AndArrives()
        {
            var hero = StartWithHero();
            yield return null;
            var destination = new Vector2(8f, 0f);
            Assert.That(_battle.MoveSelectedHero(destination).Accepted, Is.True);
            Run(20f, () => hero.Position.x > -9f);
            Assert.That(Formation.IsGateClosed(4), Is.False, "the west gate was open");
            _battle.DevToggleGate(4);
            Assert.That(Formation.IsSlabBlocking(4), Is.True);

            var throughGate = false;
            var stuckInSlab = false;
            Run(30f, () => hero.State != HeroState.Moving, () =>
            {
                throughGate |= Vector2.Distance(hero.Position, Gate(4).Position) < 0.5f;
                stuckInSlab |= Formation.Slab(4).Contains(hero.Position, 0f);
            });
            Assert.That(throughGate, Is.False, "did not walk through the closed gate");
            Assert.That(stuckInSlab, Is.False);
            Assert.That(Vector2.Distance(hero.Position, destination), Is.LessThan(0.3f), "arrived by another gate");
        }

        // ---- (c) the slab rises under a unit ----

        [UnityTest]
        public IEnumerator SlabRisingUnderAUnit_PushesItOut_AndItCanStillWalk()
        {
            var hero = StartWithHero();
            yield return null;
            var gap = Gate(4).Position;
            Assert.That(_battle.MoveSelectedHero(gap).Accepted, Is.True);
            Run(20f, () => Vector2.Distance(hero.Position, gap) < 0.1f);
            Assert.That(Vector2.Distance(hero.Position, gap), Is.LessThan(0.1f), "standing in the gap");

            _battle.DevToggleGate(4);
            foreach (var unit in _battle.Registry.Units.Where(u => !u.IsCastle))
                Assert.That(_battle.Layout.IsBlocked(unit.Position, unit.Radius), Is.False, $"{unit.name} pushed out at once");
            var target = new Vector2(-12f, 0f);
            Assert.That(_battle.MoveSelectedHero(target).Accepted, Is.True);
            Run(10f, () => hero.State != HeroState.Moving);
            Assert.That(Vector2.Distance(hero.Position, target), Is.LessThan(0.3f), "not stuck");
        }

        // ---- Gate effects ----

        [UnityTest]
        public IEnumerator SinhGate_HealsOnce_ThenCoolsDown()
        {
            var hero = StartWithHero();
            yield return null;
            Assert.That(Formation.KindOf(5), Is.EqualTo(BaguaGateKind.Life), "gates 1 & 5 start as Sinh");
            hero.TakeDamage(hero.MaxHealth * 0.5f, null, ignoreReduction: true);
            var before = hero.Health;
            SendThroughGate(hero, 5);
            Run(6f, () => hero.State != HeroState.Moving);
            Assert.That(hero.Health - before, Is.EqualTo(hero.MaxHealth * 0.08f).Within(1f), "8 % of max health");

            var healed = hero.Health;
            SendThroughGate(hero, 5);
            Run(6f, () => hero.State != HeroState.Moving);
            Assert.That(hero.Health, Is.EqualTo(healed).Within(1f), "per-unit cooldown 5 s");
        }

        [UnityTest]
        public IEnumerator TuGate_CostsTenPercent_AndSlows_KhaiGate_SpeedsUp()
        {
            var hero = StartWithHero();
            yield return null;
            Assert.That(Formation.KindOf(6), Is.EqualTo(BaguaGateKind.Death), "gates 2 & 6 start as Tử");
            var before = hero.Health;
            SendThroughGate(hero, 6);
            Run(3f, () => hero.Statuses.Has(StatusKind.Slow));
            Assert.That(hero.Statuses.Has(StatusKind.Slow), Is.True, "slowed");
            Assert.That(before - hero.Health, Is.EqualTo(hero.MaxHealth * 0.1f).Within(1f), "10 % of max health, true damage");
            Run(6f, () => hero.State != HeroState.Moving);

            Assert.That(Formation.KindOf(4), Is.EqualTo(BaguaGateKind.Haste), "gates 0 & 4 start as Khai");
            SendThroughGate(hero, 4);
            Run(3f, () => hero.Statuses.Has(StatusKind.MoveSpeedUp));
            Assert.That(hero.Statuses.Value(StatusKind.MoveSpeedUp), Is.EqualTo(0.3f).Within(1e-3f), "+30 % run speed");
        }

        // ---- Rotation, HUD warning, altar ----

        [UnityTest]
        public IEnumerator Rotation_WarnsOnTheHud_ThenShiftsTheGates_AndRaisesTheNewSlabs()
        {
            StartWithHero();
            yield return null;
            Assert.That(_hud.FormationWarningText, Is.Empty);
            Run(18f);
            yield return null;
            Assert.That(Formation.IsWarning, Is.True);
            Assert.That(_hud.FormationWarningText, Does.Contain("xoay"), "warning 3 s before");

            Run(2.5f);
            Assert.That(Formation.RotationCount, Is.EqualTo(1));
            Assert.That(Formation.KindOf(0), Is.EqualTo(BaguaGateKind.Life), "clockwise: gate 0 takes gate 1's type");
            Assert.That(Formation.IsSlabBlocking(2) && Formation.IsSlabBlocking(6), Is.True, "2 & 6 are now Đỗ");
            Assert.That(Formation.IsSlabBlocking(3), Is.True, "3 & 7 are going down, still blocking for 0.6 s");
            Run(1f);
            Assert.That(Formation.IsSlabBlocking(3), Is.False);
        }

        [UnityTest]
        public IEnumerator HeroOnTheAltar_HoldsItAfter5s_AndIsBlessedAfterTheRotation()
        {
            var hero = StartWithHero();
            yield return null;
            Assert.That(_battle.MoveSelectedHero(Vector2.zero).Accepted, Is.True);
            Run(15f, () => Formation.AltarOwner == Team.Player);
            Assert.That(Formation.AltarOwner, Is.EqualTo(Team.Player), "held the altar");
            Assert.That(Formation.BlessedTeam, Is.Null, "not before the rotation");
            Run(21f, () => Formation.RotationCount > 0);
            Assert.That(Formation.BlessedTeam, Is.EqualTo(Team.Player));
            Assert.That(Formation.TryGetEffect(BaguaGateKind.Death, Team.Player, out _), Is.False, "immune to Tử this cycle");
        }

        [UnityTest]
        public IEnumerator DevTools_RotateAndSetTheAltarHolder()
        {
            StartWithHero();
            yield return null;
            _battle.DevRotateFormation();
            Assert.That(Formation.RotationCount, Is.EqualTo(1));
            _battle.DevCycleAltarOwner();
            Assert.That(Formation.AltarOwner, Is.EqualTo(Team.Player));
            _battle.DevCycleAltarOwner();
            Assert.That(Formation.AltarOwner, Is.EqualTo(Team.Enemy));
            _battle.DevToggleGate(1);
            Assert.That(Formation.IsGateClosed(1), Is.True);
            _battle.DevToggleGate(1, clearOverride: true);
            Assert.That(Formation.ForcedClosed(1), Is.Null);
        }
    }
}
#endif

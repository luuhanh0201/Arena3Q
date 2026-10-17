using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Enemy ranks, boss phase, zone picking, taunt, anti-lock and flying (spec §8).</summary>
    public class EnemyRulesTests
    {
        [Test]
        public void SkillCount_PerTier()
        {
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Normal, 0), Is.True);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Normal, 1), Is.False);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Elite, 1), Is.True);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Elite, 2), Is.False);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Boss, 1), Is.False);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Boss, 2), Is.True);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Boss, 3), Is.True);
            Assert.That(EnemyRules.IsSkillCountValid(EnemyTier.Boss, 4), Is.False);
        }

        [Test]
        public void PhaseTwo_OnceBelowTheThreshold_NotWhenDead()
        {
            Assert.That(EnemyRules.ShouldEnterPhaseTwo(false, 600f, 1000f, 0.5f), Is.False);
            Assert.That(EnemyRules.ShouldEnterPhaseTwo(false, 499f, 1000f, 0.5f), Is.True);
            Assert.That(EnemyRules.ShouldEnterPhaseTwo(true, 499f, 1000f, 0.5f), Is.False, "only once");
            Assert.That(EnemyRules.ShouldEnterPhaseTwo(false, 0f, 1000f, 0.5f), Is.False, "killed outright");
        }

        [Test]
        public void ZonePoints_DifferentUnitsFirst_ThenAroundThem()
        {
            var units = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(20f, 0f) };
            var random = new System.Random(3);
            float Next() => (float)random.NextDouble();

            var three = EnemyRules.PickZonePoints(units, 3, 1.5f, Next);
            Assert.That(three, Is.EquivalentTo(units), "three zones on three units: one each");

            var five = EnemyRules.PickZonePoints(units, 5, 1.5f, Next);
            Assert.That(five, Has.Count.EqualTo(5));
            foreach (var point in five)
                Assert.That(units.Min(u => Vector2.Distance(u, point)), Is.LessThanOrEqualTo(1.5f + 1e-4f));

            Assert.That(EnemyRules.PickZonePoints(new List<Vector2>(), 3, 1.5f, Next), Is.Empty, "nobody in range");
        }

        [Test]
        public void AntiLock_ImmunityAfterTwoSecondsOfStun()
        {
            var seconds = 0f;
            var grant = false;
            for (var i = 0; i < 50 && !grant; i++)
                seconds = EnemyRules.TrackCrowdControl(seconds, true, 0.05f, 2f, out grant);
            Assert.That(grant, Is.True);
            Assert.That(seconds, Is.Zero);

            seconds = EnemyRules.TrackCrowdControl(1.5f, false, 0.05f, 2f, out grant);
            Assert.That(seconds, Is.Zero, "a break resets the count");
            Assert.That(grant, Is.False);
            EnemyRules.TrackCrowdControl(5f, true, 0.05f, 0f, out grant);
            Assert.That(grant, Is.False, "0 = rule off");
        }

        [Test]
        public void Taunt_OnlyUnitsNotCarryingOutAnOrder()
        {
            Assert.That(EnemyRules.HeroFollowsTaunt(HeroState.Idle), Is.True);
            Assert.That(EnemyRules.HeroFollowsTaunt(HeroState.Advancing), Is.True);
            Assert.That(EnemyRules.HeroFollowsTaunt(HeroState.Combat), Is.True);
            Assert.That(EnemyRules.HeroFollowsTaunt(HeroState.Moving), Is.False);
            Assert.That(EnemyRules.HeroFollowsTaunt(HeroState.Attacking), Is.False);

            Assert.That(EnemyRules.SquadFollowsTaunt(SquadMode.Skirmish), Is.True);
            Assert.That(EnemyRules.SquadFollowsTaunt(SquadMode.HoldFormation), Is.True);
            Assert.That(EnemyRules.SquadFollowsTaunt(SquadMode.MarchFormation), Is.False);
            Assert.That(EnemyRules.SquadFollowsTaunt(SquadMode.FocusTarget), Is.False);
        }

        [Test]
        public void TauntStatus_IsCrowdControl_FollowsTheNewestTaunter()
        {
            var statuses = new StatusSet();
            var first = new object();
            var second = new object();
            statuses.Apply(StatusKind.Taunt, 1f, 4f, first);
            statuses.Apply(StatusKind.Taunt, 1f, 2f, second);
            Assert.That(statuses.SourceOf(StatusKind.Taunt), Is.SameAs(second));

            statuses.Remove(StatusKind.Taunt);
            Assert.That(statuses.Has(StatusKind.Taunt), Is.False);

            statuses.Apply(StatusKind.CrowdControlImmunity, 1f, 2f);
            Assert.That(statuses.Apply(StatusKind.Taunt, 1f, 4f, first), Is.False, "immunity blocks taunts");
        }

        [Test]
        public void Flyers_CrossRivers_ButNotRectangularZones()
        {
            var river = new RiverObstacle(new[] { new Vector2(0f, -10f), new Vector2(0f, 10f) }, 2f,
                new[] { (new Vector2(0f, 8f), 2f) });
            var layout = new BattlefieldLayout(new Rect(-20f, -10f, 40f, 20f), new Rect(-18f, -5f, 4f, 10f),
                new[] { new Rect(5f, -2f, 2f, 4f) }, new IObstacle[] { river });
            var flyers = layout.ForFlyers();

            Assert.That(layout.IsSegmentBlocked(new Vector2(-3f, 0f), new Vector2(3f, 0f), 0.3f), Is.True);
            Assert.That(flyers.IsSegmentBlocked(new Vector2(-3f, 0f), new Vector2(3f, 0f), 0.3f), Is.False, "over the river");
            Assert.That(flyers.IsBlocked(new Vector2(6f, 0f), 0.3f), Is.True, "rectangular zones still block");
            Assert.That(flyers, Is.SameAs(layout.ForFlyers()), "built once");
        }
    }
}

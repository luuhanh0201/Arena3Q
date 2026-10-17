using System.Collections.Generic;
using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Statuses, the rage bar and the skill helpers (spec §3 "Kỹ năng trên thẻ", §8).</summary>
    public class SkillCoreTests
    {
        private const float Tolerance = 1e-4f;

        // ---- StatusSet ----

        [Test]
        public void Status_SameKindDoesNotStack_StrongestValueAndLongestTimeWin()
        {
            var statuses = new StatusSet();
            statuses.Apply(StatusKind.Slow, 0.4f, 1f);
            statuses.Apply(StatusKind.Slow, 0.2f, 3f);

            Assert.That(statuses.Count, Is.EqualTo(1));
            Assert.That(statuses.Value(StatusKind.Slow), Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(statuses.Remaining(StatusKind.Slow), Is.EqualTo(3f).Within(Tolerance));
            Assert.That(statuses.MoveSpeedMultiplier, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void Status_ExpiresAfterItsTime()
        {
            var statuses = new StatusSet();
            statuses.Apply(StatusKind.Stun, 1f, 1f);
            statuses.Tick(0.6f);
            Assert.That(statuses.IsStunned, Is.True);
            statuses.Tick(0.5f);
            Assert.That(statuses.IsStunned, Is.False);
            Assert.That(statuses.Count, Is.Zero);
        }

        [Test]
        public void Immunity_RemovesAndBlocksStunsAndSlows_NotOtherStatuses()
        {
            var statuses = new StatusSet();
            statuses.Apply(StatusKind.Stun, 1f, 2f);
            statuses.Apply(StatusKind.CrowdControlImmunity, 1f, 3f);

            Assert.That(statuses.IsStunned, Is.False, "immunity clears a stun already on");
            Assert.That(statuses.Apply(StatusKind.Stun, 1f, 1f), Is.False);
            Assert.That(statuses.Apply(StatusKind.Slow, 0.4f, 1f), Is.False);
            Assert.That(statuses.Apply(StatusKind.Weaken, 0.2f, 1f), Is.True, "weaken is not crowd control");
        }

        [Test]
        public void Shield_AbsorbsDamageUntilBroken_ThenDisappears()
        {
            var statuses = new StatusSet();
            statuses.Apply(StatusKind.Shield, 120f, 5f);

            Assert.That(statuses.AbsorbWithShield(100f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(statuses.AbsorbWithShield(50f), Is.EqualTo(30f).Within(Tolerance));
            statuses.Tick(0.01f);
            Assert.That(statuses.Has(StatusKind.Shield), Is.False);
        }

        [Test]
        public void Poison_OnePerSource()
        {
            var statuses = new StatusSet();
            var a = new object();
            var b = new object();
            statuses.Apply(StatusKind.Poison, 5f, 4f, a);
            statuses.Apply(StatusKind.Poison, 3f, 4f, a);
            statuses.Apply(StatusKind.Poison, 7f, 4f, b);

            var poisons = new List<(object source, float damagePerSecond)>();
            statuses.CollectPoison(poisons);
            Assert.That(poisons, Has.Count.EqualTo(2));
            Assert.That(poisons.Find(p => p.source == a).damagePerSecond, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Multipliers_CombineBuffsAndDebuffs()
        {
            var statuses = new StatusSet();
            statuses.Apply(StatusKind.AttackSpeedUp, 0.2f, 1f);
            statuses.Apply(StatusKind.AttackSpeedDown, 0.3f, 1f);
            statuses.Apply(StatusKind.DamageUp, 0.2f, 1f);
            statuses.Apply(StatusKind.Weaken, 0.2f, 1f);

            Assert.That(statuses.AttackSpeedMultiplier, Is.EqualTo(0.9f).Within(Tolerance));
            Assert.That(statuses.DamageMultiplier, Is.EqualTo(1f).Within(Tolerance));
        }

        // ---- Rage ----

        [Test]
        public void Rage_FillsAfterTheConfiguredNumberOfOwnHits_AndCapsAtMax()
        {
            var rage = new RageMeter(100f);
            for (var i = 0; i < 17; i++)
                rage.Add(RageMeter.GainFor(40f, 40f, 18f, rage.Max));
            Assert.That(rage.IsFull, Is.False);
            rage.Add(RageMeter.GainFor(40f, 40f, 18f, rage.Max));
            Assert.That(rage.IsFull, Is.True);
            rage.Add(50f);
            Assert.That(rage.Value, Is.EqualTo(100f).Within(Tolerance));
            rage.Reset();
            Assert.That(rage.Value, Is.Zero);
        }

        [Test]
        public void Rage_IsProportionalToDamage_AndGetsTheFactionBonus()
        {
            Assert.That(RageMeter.GainFor(80f, 40f, 20f, 100f), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(RageMeter.GainFor(40f, 40f, 20f, 100f, 0.2f), Is.EqualTo(6f).Within(Tolerance));
            Assert.That(RageMeter.GainFor(40f, 0f, 20f, 100f), Is.Zero);
        }

        // ---- Helpers ----

        [Test]
        public void DensestPoint_PicksTheBiggestGroup()
        {
            var points = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(10.5f, 0.5f), new Vector2(9.6f, -0.4f)
            };
            Assert.That(SkillMath.DensestPoint(points, 1.5f, out var best, out var count), Is.True);
            Assert.That(count, Is.EqualTo(3));
            Assert.That(Vector2.Distance(best, new Vector2(10f, 0f)), Is.LessThan(0.5f));
            Assert.That(SkillMath.DensestPoint(new List<Vector2>(), 1f, out _, out _), Is.False);
        }

        [Test]
        public void Cone_And_Line_AreaChecks()
        {
            Assert.That(SkillMath.InCone(Vector2.zero, Vector2.right, 5f, 60f, new Vector2(3f, 1f), 0.3f), Is.True);
            Assert.That(SkillMath.InCone(Vector2.zero, Vector2.right, 5f, 60f, new Vector2(0f, 3f), 0.3f), Is.False, "beside");
            Assert.That(SkillMath.InCone(Vector2.zero, Vector2.right, 5f, 60f, new Vector2(6f, 0f), 0.3f), Is.False, "too far");

            Assert.That(SkillMath.AlongLine(Vector2.zero, Vector2.right, 8f, 1f, new Vector2(4f, 0.6f), 0.3f),
                Is.EqualTo(4f).Within(Tolerance));
            Assert.That(SkillMath.AlongLine(Vector2.zero, Vector2.right, 8f, 1f, new Vector2(4f, 2f), 0.3f), Is.LessThan(0f));
            Assert.That(SkillMath.AlongLine(Vector2.zero, Vector2.right, 8f, 1f, new Vector2(-2f, 0f), 0.3f), Is.LessThan(0f));
        }

        [Test]
        public void AutoCast_NeedsUnlockedReadyFreeHero_AndDashSkillsWaitWhileMoving()
        {
            Assert.That(SkillMath.CanAutoCast(true, 0f, false, false, false, false, false), Is.True);
            Assert.That(SkillMath.CanAutoCast(false, 0f, false, false, false, false, false), Is.False, "not learned");
            Assert.That(SkillMath.CanAutoCast(true, 1f, false, false, false, false, false), Is.False, "cooling down");
            Assert.That(SkillMath.CanAutoCast(true, 0f, true, false, false, false, false), Is.False, "casting");
            Assert.That(SkillMath.CanAutoCast(true, 0f, false, true, false, false, false), Is.False, "stunned");
            Assert.That(SkillMath.CanAutoCast(true, 0f, false, false, true, false, false), Is.True, "moving, not a dash");
            Assert.That(SkillMath.CanAutoCast(true, 0f, false, false, true, true, false), Is.False, "spec §3: dash skills wait while moving");
            Assert.That(SkillMath.CanAutoCast(true, 0f, false, false, true, true, true), Is.True, "per-skill switch");
        }
    }
}

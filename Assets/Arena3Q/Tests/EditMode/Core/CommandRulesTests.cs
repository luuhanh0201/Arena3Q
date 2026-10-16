using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class CommandRulesTests
    {
        [Test]
        public void Move_ToWalkableGround_IsAccepted()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(5f, 5f)), state, TestBattles.Layout());

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void Move_OutsideBattlefield_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(50f, 0f)), state, TestBattles.Layout());

            Assert.That(result.Reason, Is.EqualTo(RejectReason.OutsideBattlefield));
        }

        [Test]
        public void Move_IntoForbiddenZone_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateMove(new MoveCommand(hero.Id, Vector2.zero), state, TestBattles.Layout());

            Assert.That(result.Reason, Is.EqualTo(RejectReason.InForbiddenZone));
        }

        [Test]
        public void Move_ForDeadOrUnknownHero_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);
            state.MarkHeroDead(hero.Id);
            var destination = new Vector2(5f, 5f);

            Assert.That(CommandRules.ValidateMove(new MoveCommand(hero.Id, destination), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.HeroNotAvailable));
            Assert.That(CommandRules.ValidateMove(new MoveCommand(new UnitId(999), destination), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.HeroNotAvailable));
        }

        [Test]
        public void Attack_LivingEnemy_IsAccepted()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateAttack(new AttackCommand(hero.Id, state.EnemyCastle.Id), state, Team.Enemy, true);

            Assert.That(result.Accepted, Is.True);
        }

        [Test]
        public void Attack_FriendlyTarget_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            var result = CommandRules.ValidateAttack(new AttackCommand(hero.Id, state.PlayerCastle.Id), state, Team.Player, true);

            Assert.That(result.Reason, Is.EqualTo(RejectReason.TargetNotHostile));
        }

        [Test]
        public void Attack_DeadOrMissingTarget_IsRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            Assert.That(CommandRules.ValidateAttack(new AttackCommand(hero.Id, new UnitId(42)), state, Team.Enemy, false).Reason,
                Is.EqualTo(RejectReason.InvalidTarget));
            Assert.That(CommandRules.ValidateAttack(new AttackCommand(hero.Id, UnitId.None), state, Team.Enemy, true).Reason,
                Is.EqualTo(RejectReason.InvalidTarget));
        }

        [Test]
        public void Commands_AfterBattleOver_AreRejected()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);
            state.Finish(BattleOutcome.Victory);

            Assert.That(CommandRules.ValidateMove(new MoveCommand(hero.Id, new Vector2(5f, 5f)), state, TestBattles.Layout()).Reason,
                Is.EqualTo(RejectReason.BattleOver));
        }

        [Test]
        public void Layout_SummonZoneExcludesForbiddenAndOutsidePoints()
        {
            var layout = new BattlefieldLayout(TestBattles.Bounds, TestBattles.SummonZone, new[] { new Rect(-12f, -1f, 2f, 2f) });

            Assert.That(layout.IsValidSummonPosition(new Vector2(-8f, 5f)), Is.True);
            Assert.That(layout.IsValidSummonPosition(new Vector2(-11f, 0f)), Is.False, "forbidden zone inside summon zone");
            Assert.That(layout.IsValidSummonPosition(new Vector2(0f, 0f)), Is.False, "outside summon zone");
        }
    }
}

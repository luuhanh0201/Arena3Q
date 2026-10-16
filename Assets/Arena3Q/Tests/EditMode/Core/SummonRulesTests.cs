using Arena3Q.Core;
using NUnit.Framework;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public class SummonRulesTests
    {
        [Test]
        public void QuickSummon_PicksFreePointNearestPlayerCastle()
        {
            var far = new Vector2(-8f, 0f);
            var near = new Vector2(-13f, 2f);
            var rules = TestBattles.CreateSummonRules(far, near);

            var decision = rules.Evaluate(SummonRequest.Quick(0), TestBattles.State());

            Assert.That(decision.Accepted, Is.True);
            Assert.That(decision.Position, Is.EqualTo(near));
        }

        [Test]
        public void QuickSummon_SkipsOccupiedPoint()
        {
            var far = new Vector2(-8f, 0f);
            var near = new Vector2(-13f, 2f);
            var rules = TestBattles.CreateSummonRules(far, near);

            var decision = rules.Evaluate(SummonRequest.Quick(0), TestBattles.State(), p => p == near);

            Assert.That(decision.Position, Is.EqualTo(far));
        }

        [Test]
        public void QuickSummon_AllPointsOccupied_FallsBackToNearestPoint()
        {
            var far = new Vector2(-8f, 0f);
            var near = new Vector2(-13f, 2f);
            var rules = TestBattles.CreateSummonRules(far, near);

            var decision = rules.Evaluate(SummonRequest.Quick(0), TestBattles.State(), _ => true);

            Assert.That(decision.Accepted, Is.True);
            Assert.That(decision.Position, Is.EqualTo(near));
        }

        [Test]
        public void QuickSummon_NoPointsConfigured_IsRejected()
        {
            var rules = new SummonRules(TestBattles.Layout(), new Vector2[0], TestBattles.PlayerCastle);

            var decision = rules.Evaluate(SummonRequest.Quick(0), TestBattles.State());

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.NoQuickSummonPoint));
        }

        [Test]
        public void FreeSummon_InsideSummonZone_IsAcceptedAtDropPosition()
        {
            var drop = new Vector2(-10f, 3f);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Free(0, drop), TestBattles.State());

            Assert.That(decision.Accepted, Is.True);
            Assert.That(decision.Position, Is.EqualTo(drop));
        }

        [Test]
        public void FreeSummon_OutsideSummonZone_IsRejectedAndCardStaysAvailable()
        {
            var state = TestBattles.State();

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Free(0, new Vector2(5f, 0f)), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.OutsideSummonZone));
            Assert.That(state.Cards[0].State, Is.EqualTo(CardState.Available));
            Assert.That(state.SummonedCount, Is.Zero);
        }

        [Test]
        public void SixthHero_IsRejected()
        {
            var state = TestBattles.State(cardCount: 6);
            var rules = TestBattles.CreateSummonRules();
            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
            {
                Assert.That(rules.Evaluate(SummonRequest.Quick(i), state).Accepted, Is.True, $"hero {i}");
                TestBattles.Summon(state, i);
            }

            var decision = rules.Evaluate(SummonRequest.Quick(5), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.HeroLimitReached));
        }

        [Test]
        public void DeadHeroes_DoNotGiveTheSlotBack()
        {
            var state = TestBattles.State(cardCount: 6, allowResummon: true);
            for (var i = 0; i < SummonRules.MaxHeroesPerBattle; i++)
                state.MarkHeroDead(TestBattles.Summon(state, i).Id);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(5), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.HeroLimitReached));
        }

        [Test]
        public void DeployedCard_IsRejected()
        {
            var state = TestBattles.State();
            TestBattles.Summon(state, 0);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(0), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.CardAlreadyDeployed));
        }

        [Test]
        public void DeadHeroCard_IsRejected_WhenResummonNotAllowed()
        {
            var state = TestBattles.State();
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(0), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.HeroDeadCannotResummon));
        }

        [Test]
        public void DeadHeroCard_IsAccepted_WhenResummonAllowed()
        {
            var state = TestBattles.State(allowResummon: true);
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(0), state);

            Assert.That(decision.Accepted, Is.True);
        }

        [Test]
        public void UnknownCard_IsRejected()
        {
            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(9), TestBattles.State());

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.UnknownCard));
        }

        [Test]
        public void Summon_AfterBattleOver_IsRejected()
        {
            var state = TestBattles.State();
            state.Finish(BattleOutcome.Defeat);

            var decision = TestBattles.CreateSummonRules().Evaluate(SummonRequest.Quick(0), state);

            Assert.That(decision.Result.Reason, Is.EqualTo(RejectReason.BattleOver));
        }

        [Test]
        public void CanSummonAnyCard_FalseWhenAllCardsUsedAndResummonOff()
        {
            var state = TestBattles.State(cardCount: 2);
            TestBattles.Summon(state, 0);
            Assert.That(SummonRules.CanSummonAnyCard(state), Is.True);

            TestBattles.Summon(state, 1);

            Assert.That(SummonRules.CanSummonAnyCard(state), Is.False);
        }
    }
}

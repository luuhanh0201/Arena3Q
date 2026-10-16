using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public class BattleOutcomeRulesTests
    {
        [Test]
        public void Attack_EnemyCastleDestroyed_IsVictory()
        {
            var state = TestBattles.State();
            state.EnemyCastle.ApplyDamage(float.MaxValue);

            var decision = BattleOutcomeRules.EvaluateAndApply(state);

            Assert.That(decision.Reason, Is.EqualTo(OutcomeReason.EnemyCastleDestroyed));
            Assert.That(state.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(state.Stars, Is.EqualTo(3));
        }

        [Test]
        public void Attack_TimeUp_IsDefeat()
        {
            var state = TestBattles.State(timeLimit: 180f);
            state.Tick(179.9f);
            Assert.That(BattleOutcomeRules.Evaluate(state).IsFinal, Is.False);

            state.Tick(0.2f);
            var decision = BattleOutcomeRules.EvaluateAndApply(state);

            Assert.That(decision.Reason, Is.EqualTo(OutcomeReason.TimeUp));
            Assert.That(state.Stars, Is.Zero);
        }

        [Test]
        public void Attack_CastleDestroyedOnLastFrame_WinsOverTimeUp()
        {
            var state = TestBattles.State(timeLimit: 10f);
            state.Tick(10f);
            state.EnemyCastle.ApplyDamage(float.MaxValue);

            Assert.That(BattleOutcomeRules.Evaluate(state).Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void Attack_AllHeroesDeadAndNoCardsLeft_IsDefeat()
        {
            var state = TestBattles.State(cardCount: 2);
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);
            Assert.That(BattleOutcomeRules.Evaluate(state).IsFinal, Is.False, "a card is still available");

            state.MarkHeroDead(TestBattles.Summon(state, 1).Id);

            Assert.That(BattleOutcomeRules.Evaluate(state).Reason, Is.EqualTo(OutcomeReason.AllHeroesLost));
        }

        [Test]
        public void Attack_AllHeroesDeadRule_CanBeDisabled()
        {
            var setup = TestBattles.Setup(cardCount: 1);
            setup.LossRules = new LossRules { loseWhenAllHeroesDeadAndNoCards = false };
            var state = new BattleRuntimeState(setup);
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);

            Assert.That(BattleOutcomeRules.Evaluate(state).IsFinal, Is.False);
        }

        [Test]
        public void Defense_SurviveTimeLimit_IsVictory()
        {
            var state = TestBattles.State(stageType: StageType.Defense, timeLimit: 60f);
            state.Tick(60f);

            Assert.That(BattleOutcomeRules.Evaluate(state).Reason, Is.EqualTo(OutcomeReason.SurvivedTimeLimit));
        }

        [Test]
        public void Defense_AllWavesCleared_IsVictory()
        {
            var state = TestBattles.State(stageType: StageType.Defense, timeLimit: 0f);
            Assert.That(BattleOutcomeRules.Evaluate(state).IsFinal, Is.False);

            state.AllWavesCleared = true;

            Assert.That(BattleOutcomeRules.Evaluate(state).Reason, Is.EqualTo(OutcomeReason.AllWavesCleared));
        }

        [Test]
        public void Defense_PlayerCastleDestroyed_IsDefeat_EvenOnLastFrame()
        {
            var state = TestBattles.State(stageType: StageType.Defense, timeLimit: 60f);
            state.Tick(60f);
            state.PlayerCastle.ApplyDamage(float.MaxValue);

            Assert.That(BattleOutcomeRules.Evaluate(state).Reason, Is.EqualTo(OutcomeReason.PlayerCastleDestroyed));
        }

        [Test]
        public void Defense_AllHeroesDead_IsNotDefeat()
        {
            var state = TestBattles.State(cardCount: 1, stageType: StageType.Defense);
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);

            Assert.That(BattleOutcomeRules.Evaluate(state).IsFinal, Is.False);
        }

        [Test]
        public void FinishedBattle_KeepsItsOutcome()
        {
            var state = TestBattles.State();
            state.EnemyCastle.ApplyDamage(float.MaxValue);
            BattleOutcomeRules.EvaluateAndApply(state);

            state.Tick(1000f);
            BattleOutcomeRules.EvaluateAndApply(state);

            Assert.That(state.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }
    }
}

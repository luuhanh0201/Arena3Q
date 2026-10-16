namespace Arena3Q.Core
{
    public enum OutcomeReason
    {
        None,
        EnemyCastleDestroyed,
        TimeUp,
        AllHeroesLost,
        PlayerCastleDestroyed,
        SurvivedTimeLimit,
        AllWavesCleared
    }

    public readonly struct OutcomeDecision
    {
        public static readonly OutcomeDecision InProgress = default;

        public readonly BattleOutcome Outcome;
        public readonly OutcomeReason Reason;

        public OutcomeDecision(BattleOutcome outcome, OutcomeReason reason)
        {
            Outcome = outcome;
            Reason = reason;
        }

        public bool IsFinal => Outcome != BattleOutcome.InProgress;

        public override string ToString() => $"{Outcome}({Reason})";
    }

    /// <summary>
    /// Win/lose conditions (spec §8). Castle destruction is checked before the timer, so destroying the enemy castle on
    /// the last frame of an Attack stage wins, and losing the castle on the last frame of a Defense stage loses.
    /// </summary>
    public static class BattleOutcomeRules
    {
        public static OutcomeDecision Evaluate(BattleRuntimeState state)
        {
            if (state.IsOver)
                return new OutcomeDecision(state.Outcome, OutcomeReason.None);

            return state.StageType == StageType.Attack ? EvaluateAttack(state) : EvaluateDefense(state);
        }

        /// <summary>Evaluates and, if final, ends the battle. Returns the decision.</summary>
        public static OutcomeDecision EvaluateAndApply(BattleRuntimeState state)
        {
            var decision = Evaluate(state);
            if (decision.IsFinal)
                state.Finish(decision.Outcome);
            return decision;
        }

        private static OutcomeDecision EvaluateAttack(BattleRuntimeState state)
        {
            if (state.EnemyCastle.IsDestroyed)
                return new OutcomeDecision(BattleOutcome.Victory, OutcomeReason.EnemyCastleDestroyed);
            if (state.IsTimeUp)
                return new OutcomeDecision(BattleOutcome.Defeat, OutcomeReason.TimeUp);
            if (state.Setup.LossRules.loseWhenAllHeroesDeadAndNoCards
                && state.AliveHeroCount == 0
                && !SummonRules.CanSummonAnyCard(state))
                return new OutcomeDecision(BattleOutcome.Defeat, OutcomeReason.AllHeroesLost);
            return OutcomeDecision.InProgress;
        }

        private static OutcomeDecision EvaluateDefense(BattleRuntimeState state)
        {
            if (state.Setup.LossRules.loseWhenPlayerCastleDestroyed && state.PlayerCastle.IsDestroyed)
                return new OutcomeDecision(BattleOutcome.Defeat, OutcomeReason.PlayerCastleDestroyed);
            if (state.AllWavesCleared)
                return new OutcomeDecision(BattleOutcome.Victory, OutcomeReason.AllWavesCleared);
            if (state.IsTimeUp)
                return new OutcomeDecision(BattleOutcome.Victory, OutcomeReason.SurvivedTimeLimit);
            return OutcomeDecision.InProgress;
        }
    }
}

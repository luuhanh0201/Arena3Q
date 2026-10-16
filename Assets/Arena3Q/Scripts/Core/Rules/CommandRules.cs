namespace Arena3Q.Core
{
    /// <summary>
    /// Validates player commands before they reach a hero (spec §5). Input-level checks, such as ignoring a pointer
    /// over UI, happen earlier in the input layer.
    /// </summary>
    public static class CommandRules
    {
        public static CommandResult ValidateMove(in MoveCommand command, BattleRuntimeState state, BattlefieldLayout layout)
        {
            var heroCheck = ValidateHero(command.Hero, state);
            if (!heroCheck.Accepted)
                return heroCheck;
            if (!layout.IsInsideBounds(command.Destination))
                return CommandResult.Reject(RejectReason.OutsideBattlefield);
            if (layout.IsInForbiddenZone(command.Destination))
                return CommandResult.Reject(RejectReason.InForbiddenZone);
            return CommandResult.Ok;
        }

        /// <param name="targetTeam">Team of the unit or castle under the swipe end.</param>
        /// <param name="targetAlive">False if the target is dead or already destroyed.</param>
        public static CommandResult ValidateAttack(in AttackCommand command, BattleRuntimeState state, Team targetTeam, bool targetAlive)
        {
            var heroCheck = ValidateHero(command.Hero, state);
            if (!heroCheck.Accepted)
                return heroCheck;
            if (!command.Target.IsValid || !targetAlive)
                return CommandResult.Reject(RejectReason.InvalidTarget);
            if (targetTeam != Team.Enemy)
                return CommandResult.Reject(RejectReason.TargetNotHostile);
            return CommandResult.Ok;
        }

        private static CommandResult ValidateHero(UnitId heroId, BattleRuntimeState state)
        {
            if (state.IsOver)
                return CommandResult.Reject(RejectReason.BattleOver);
            if (!state.TryGetHero(heroId, out var hero) || !hero.IsAlive)
                return CommandResult.Reject(RejectReason.HeroNotAvailable);
            return CommandResult.Ok;
        }
    }
}

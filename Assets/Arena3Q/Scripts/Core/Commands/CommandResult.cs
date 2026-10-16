namespace Arena3Q.Core
{
    public enum RejectReason
    {
        None,
        BattleOver,
        UnknownCard,
        CardAlreadyDeployed,
        HeroDeadCannotResummon,
        HeroLimitReached,
        OutsideSummonZone,
        NoQuickSummonPoint,
        HeroNotAvailable,
        OutsideBattlefield,
        InForbiddenZone,
        InvalidTarget,
        TargetNotHostile
    }

    public readonly struct CommandResult
    {
        public static readonly CommandResult Ok = default;

        public readonly RejectReason Reason;

        private CommandResult(RejectReason reason)
        {
            Reason = reason;
        }

        public bool Accepted => Reason == RejectReason.None;

        public static CommandResult Reject(RejectReason reason) => new CommandResult(reason);

        public override string ToString() => Accepted ? "Accepted" : $"Rejected({Reason})";
    }
}

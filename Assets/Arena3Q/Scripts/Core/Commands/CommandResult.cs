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
        TargetNotHostile,
        /// <summary>Ultimate not learned, rage not full, or the hero is stunned (spec §3: "kỹ năng chưa thể sử dụng").</summary>
        SkillNotReady,
        /// <summary>Nothing to aim the skill at.</summary>
        NoSkillTarget
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

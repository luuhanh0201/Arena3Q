namespace Arena3Q.Core
{
    /// <summary>Per-hero, per-stage tuning for <see cref="HeroBrain"/>. Built from HeroDefinition + StageConfig.</summary>
    public sealed class HeroBrainSettings
    {
        public StageType StageType { get; set; } = StageType.Attack;

        /// <summary>Seconds with no target and no command before Idle turns into Advancing (spec §6, default 5).</summary>
        public float IdleBeforeAdvanceSeconds { get; set; } = 5f;

        /// <summary>Provisional: whether damage from outside attack range also resets the idle timer (default false).</summary>
        public bool IdleTimerResetsOnOutOfRangeDamage { get; set; }

        /// <summary>Provisional: apply the Moving self-defence rule while walking to an AttackCommand target (default true).</summary>
        public bool SelfDefendWhileAttacking { get; set; } = true;

        /// <summary>Distance at which a MoveCommand destination counts as reached.</summary>
        public float ArrivalTolerance { get; set; } = 0.1f;
    }
}

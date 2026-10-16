namespace Arena3Q.Core
{
    /// <summary>Hero behaviour states (spec §6).</summary>
    public enum HeroState
    {
        Idle,
        Moving,
        Attacking,
        Advancing,
        Combat,
        Dead
    }

    /// <summary>Stage kinds (spec §8): Attack = "ải Công", Defense = "ải Thủ".</summary>
    public enum StageType
    {
        Attack,
        Defense
    }

    /// <summary>What a squad does after its hero dies (spec §7, provisional).</summary>
    public enum OnHeroDeathTroopBehavior
    {
        KeepFighting,
        Retreat,
        Disband
    }
}

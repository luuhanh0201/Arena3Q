namespace Arena3Q.Core
{
    /// <summary>
    /// The army-wide order every hero of the player follows (spec §5 "Cơ chế mới"): the player picks one with the
    /// stance button, there is no per-hero control.
    /// </summary>
    public enum ArmyStance
    {
        /// <summary>Giữ: stand where you are, hit what walks into attack range, never chase.</summary>
        Hold,
        /// <summary>Tấn công: advance and fight (the Advancing behaviour).</summary>
        Attack,
        /// <summary>Phòng thủ: fall back to the defence area in front of the own castle, fight what enters it.</summary>
        Defend
    }
}

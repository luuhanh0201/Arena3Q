using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>Optional defeat conditions (spec §8, provisional). Time-out defeat in Attack stages is always on.</summary>
    [Serializable]
    public sealed class LossRules
    {
        [Tooltip("Attack stage: lose when every summoned hero is dead and no card can still be summoned.")]
        public bool loseWhenAllHeroesDeadAndNoCards = true;

        [Tooltip("Defense stage: lose when the player's castle is destroyed.")]
        public bool loseWhenPlayerCastleDestroyed = true;
    }

    /// <summary>Star thresholds by heroes lost (spec §8). Defaults: 0 lost = 3 stars, 1 lost = 2 stars, more = 1 star.</summary>
    [Serializable]
    public sealed class StarThresholds
    {
        [Min(0)]
        [Tooltip("Win with at most this many heroes lost for 3 stars.")]
        public int maxHeroesLostForThreeStars = 0;

        [Min(0)]
        [Tooltip("Win with at most this many heroes lost for 2 stars. Anything above is 1 star.")]
        public int maxHeroesLostForTwoStars = 1;
    }

    /// <summary>Plain per-battle settings, built from a StageConfig by the Data layer so Core stays asset-free.</summary>
    public sealed class BattleSetup
    {
        public StageType StageType { get; set; } = StageType.Attack;

        /// <summary>Seconds. A value &lt;= 0 means no time limit (only meaningful for Defense stages that end on waves).</summary>
        public float TimeLimit { get; set; } = 180f;

        public int WaveCount { get; set; }

        /// <summary>Hero ids in card order. Card index = position in this list.</summary>
        public IReadOnlyList<string> CardHeroIds { get; set; } = Array.Empty<string>();

        public bool AllowResummonDeadHero { get; set; }

        public float PlayerCastleMaxHealth { get; set; } = 1f;
        public float EnemyCastleMaxHealth { get; set; } = 1f;

        public LossRules LossRules { get; set; } = new LossRules();
        public StarThresholds StarThresholds { get; set; } = new StarThresholds();

        /// <summary>The army-wide stance the battle starts in (spec §5: Giữ).</summary>
        public ArmyStance StartStance { get; set; } = ArmyStance.Hold;
    }
}

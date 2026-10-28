using UnityEngine;

namespace Arena3Q.Core
{
    public sealed class HeroBrainSettings
    {
        public StageType StageType { get; set; } = StageType.Attack;

        /// <summary>
        /// Provisional: apply the Moving self-defence rule while carrying out an AttackCommand. Default false: an attack
        /// order takes priority, the hero keeps on its target even when hit up close.
        /// </summary>
        public bool SelfDefendWhileAttacking { get; set; }

        /// <summary>Distance at which a MoveCommand destination counts as reached.</summary>
        public float ArrivalTolerance { get; set; } = 0.1f;

        /// <summary>Phòng thủ: the area in front of the own castle the heroes fall back to (the stage's summon zone).</summary>
        public Rect DefendArea { get; set; }

        /// <summary>Phòng thủ: how far beyond the defence area a hero still chases an intruder before going back.</summary>
        public float DefendChaseMargin { get; set; } = 3f;
    }
}

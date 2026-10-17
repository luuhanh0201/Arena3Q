using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// One skill of a hero (spec §3 "Kỹ năng trên thẻ"): its slot (passive, skills 2/3, ultimate), how it aims and, for
    /// the passive, its always-on effects. What it does when it lands is in <see cref="SkillAreaDefinition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Skill Definition", fileName = "Skill")]
    public sealed class SkillDefinition : SkillAreaDefinition
    {
        [Serializable]
        public sealed class Passive
        {
            public PassiveKind kind;
            public float value;
            public float secondValue;
            public float threshold;
            public float radius;
            public int count;
        }

        [Header("Hero")]
        [SerializeField] private SkillSlot slot = SkillSlot.Skill2;
        [SerializeField] private SkillTargeting targeting = SkillTargeting.CurrentTarget;
        [Tooltip("Skills that move the hero: also auto-cast while it walks to a move order (spec §3: off = settled rule, the hero keeps walking).")]
        [SerializeField] private bool autoCastWhileMoving;
        [Tooltip("Passive (kỹ năng 1) only.")]
        [SerializeField] private Passive[] passives = Array.Empty<Passive>();

        public SkillSlot Slot => slot;
        public SkillTargeting Targeting => targeting;
        public bool AutoCastWhileMoving => autoCastWhileMoving;
        public IReadOnlyList<Passive> Passives => passives;

        public bool IsPassive => slot == SkillSlot.Passive;
        public bool IsUltimate => slot == SkillSlot.Ultimate;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            var owner = $"Skill '{name}'";
            if (IsPassive)
            {
                if (passives == null || passives.Length == 0) errors.Add($"{owner}: a passive needs passive effects.");
                return;
            }
            if (!HasEffects) errors.Add($"{owner}: no effects.");
            if (slot != SkillSlot.Ultimate && Cooldown <= 0f) errors.Add($"{owner}: skills 2 and 3 need a cooldown.");
            if (Shape == SkillShape.Single && targeting != SkillTargeting.CurrentTarget && targeting != SkillTargeting.AllyNeedingSoldiers)
                errors.Add($"{owner}: a single-target skill must aim at a unit (the current target or an ally).");
        }
    }
}

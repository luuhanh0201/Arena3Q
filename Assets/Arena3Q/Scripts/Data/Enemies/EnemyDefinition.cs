using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// An enemy with a rank (spec §8 ✅, docs/QuanDich): normal (no skill), elite (one skill, golden outline) or boss
    /// (2–3 skills, a second phase, its own health bar on the HUD). Adds the body size, the skills, the boss phase and
    /// traits (flying, coming back to life) to the soldier stats of <see cref="TroopDefinition"/>. Stats are the
    /// level-1 values; the stage's enemy level scales them.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Enemy Definition", fileName = "Enemy")]
    public sealed class EnemyDefinition : TroopDefinition
    {
        /// <summary>A boss's second phase (spec §8 ✅: once, below 🟡 50 % health).</summary>
        [Serializable]
        public sealed class PhaseTwo
        {
            [Range(0.05f, 0.95f)] public float healthThreshold = 0.5f;
            [Tooltip("+fraction attack speed for the rest of the fight (Cuồng Nộ +30 %).")]
            public float attackSpeedBonus;
            public float moveSpeedBonus;
            public float damageBonus;
            [Tooltip("Skills used in phase 2 (replaces the list; empty = keep the phase-1 skills).")]
            public EnemySkillDefinition[] skills = Array.Empty<EnemySkillDefinition>();
            [Tooltip("Used once when the phase starts, without warning (storm aura, shield).")]
            public EnemySkillDefinition[] onEnter = Array.Empty<EnemySkillDefinition>();
        }

        [Header("Rank")]
        [SerializeField] private EnemyTier tier = EnemyTier.Normal;
        [Tooltip("Logic body radius (docs/QuanDich/README.md §1: normal 0.30–0.32, elite 0.40, boss 0.80).")]
        [SerializeField, Range(0.1f, 1.5f)] private float bodyRadius = 0.32f;
        [Tooltip("Placeholder look: tint applied when phase 2 starts.")]
        [SerializeField] private Color phaseTwoColor = new Color(1f, 0.25f, 0.2f);

        [Header("Skills")]
        [SerializeField] private EnemySkillDefinition[] skills = Array.Empty<EnemySkillDefinition>();
        [SerializeField] private PhaseTwo phaseTwo = new PhaseTwo();

        [Header("Traits")]
        [Tooltip("Spec §8: flies over rivers anywhere; still blocked by rectangular forbidden zones and the map edge.")]
        [SerializeField] private bool flying;
        [Tooltip("Spec §8 🟡: chance to stand up once when killed (Cương Thi 30 %).")]
        [SerializeField, Range(0f, 1f)] private float reviveChance;
        [Tooltip("Share of max health it comes back with.")]
        [SerializeField, Range(0.05f, 1f)] private float reviveHealth = 0.3f;

        public EnemyTier Tier => tier;
        public float BodyRadius => bodyRadius;
        public Color PhaseTwoColor => phaseTwoColor;
        public IReadOnlyList<EnemySkillDefinition> Skills => skills;
        public PhaseTwo Phase => tier == EnemyTier.Boss ? phaseTwo : null;
        public bool Flying => flying;
        public float ReviveChance => reviveChance;
        public float ReviveHealth => reviveHealth;

        public override void Validate(List<string> errors)
        {
            base.Validate(errors);
            var owner = $"Enemy '{name}'";
            var count = skills?.Length ?? 0;
            if (!EnemyRules.IsSkillCountValid(tier, count))
                errors.Add($"{owner}: a {tier} has {count} skill(s); spec §8: normal 0, elite 1, boss 2–3.");
            ValidateSkills(owner, skills, errors, needsCooldown: true);
            if (tier == EnemyTier.Boss && phaseTwo != null)
            {
                ValidateSkills(owner + " phase 2", phaseTwo.skills, errors, needsCooldown: true);
                ValidateSkills(owner + " phase 2 start", phaseTwo.onEnter, errors, needsCooldown: false);
                if (phaseTwo.skills != null && phaseTwo.skills.Length > 3)
                    errors.Add($"{owner}: phase 2 has more than 3 skills.");
            }
        }

        private static void ValidateSkills(string owner, EnemySkillDefinition[] list, List<string> errors, bool needsCooldown)
        {
            if (list == null)
                return;
            foreach (var skill in list)
            {
                if (skill == null)
                {
                    errors.Add($"{owner}: empty skill slot.");
                    continue;
                }
                skill.Validate(errors);
                if (needsCooldown && skill.Cooldown <= 0f)
                    errors.Add($"{owner}: skill '{skill.name}' needs a cooldown.");
            }
        }
    }
}

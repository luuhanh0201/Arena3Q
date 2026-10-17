using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// How a class of heroes grows per level (docs/KeHoach/HeroProgression.md §4): the class strength grows faster, but
    /// every profile should keep (1 + Công growth) × (1 + Máu growth) equal so classes stay balanced at every level.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Progression/Growth Profile", fileName = "Growth")]
    public sealed class GrowthProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "balanced";
        [SerializeField] private string displayName = "Cân bằng";
        [Tooltip("Công gained per level, compounding: 0.033 = +3.3 % per level.")]
        [SerializeField, Range(0f, 0.2f)] private float attackGrowthPerLevel = 0.03f;
        [Tooltip("Máu gained per level, compounding.")]
        [SerializeField, Range(0f, 0.2f)] private float healthGrowthPerLevel = 0.03f;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public GrowthRates Rates => new GrowthRates(attackGrowthPerLevel, healthGrowthPerLevel);

        /// <summary>Growth of Công × Máu per level; equal for every profile when classes are balanced.</summary>
        public float StrengthGrowthPerLevel => (1f + attackGrowthPerLevel) * (1f + healthGrowthPerLevel) - 1f;

        public void Validate(List<string> errors)
        {
            var owner = $"Growth profile '{name}'";
            if (string.IsNullOrWhiteSpace(profileId)) errors.Add($"{owner}: profileId is empty.");
            if (attackGrowthPerLevel < 0f || healthGrowthPerLevel < 0f) errors.Add($"{owner}: growth must be >= 0.");
        }
    }
}

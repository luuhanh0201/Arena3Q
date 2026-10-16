using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>A soldier type: hero squads, Attack-stage defenders and Defense-stage wave enemies (spec §10).</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Troop Definition", fileName = "Troop")]
    public sealed class TroopDefinition : ScriptableObject
    {
        [SerializeField] private string troopId = "troop";
        [SerializeField] private string displayName = "Troop";
        [SerializeField] private Sprite sprite;
        [Tooltip("Tint used when no sprite is assigned.")]
        [SerializeField] private Color placeholderColor = Color.white;

        [SerializeField] private CombatStats stats = new CombatStats(
            maxHealth: 100f, damage: 10f, moveSpeed: 2.8f, attackRange: 0.6f, attackInterval: 1f,
            reactionRange: 0.6f, aggroRange: 5f);

        public string TroopId => troopId;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color PlaceholderColor => placeholderColor;
        public CombatStats Stats => stats;

        public void Validate(List<string> errors)
        {
            var owner = $"Troop '{name}'";
            if (string.IsNullOrWhiteSpace(troopId)) errors.Add($"{owner}: troopId is empty.");
            if (stats == null) errors.Add($"{owner}: stats are missing.");
            else stats.Validate(owner, errors);
        }
    }
}

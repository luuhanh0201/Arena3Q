using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// A hero and its squad (spec §10). Hero cards are UI only and read their display data from here (spec §3).
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Hero Definition", fileName = "Hero")]
    public sealed class HeroDefinition : ScriptableObject
    {
        public const float DefaultIdleBeforeAdvanceSeconds = 5f;

        [Header("Identity")]
        [SerializeField] private string heroId = "hero";
        [SerializeField] private string displayName = "Hero";
        [SerializeField] private Sprite sprite;
        [Tooltip("Tint used when no sprite is assigned.")]
        [SerializeField] private Color placeholderColor = Color.white;
        [Tooltip("Optional. Faction data is deferred in the prototype.")]
        [SerializeField] private FactionDefinition faction;

        [Header("Combat")]
        [SerializeField] private CombatStats stats = new CombatStats(
            maxHealth: 500f, damage: 40f, moveSpeed: 2.5f, attackRange: 1.2f, attackInterval: 1f,
            reactionRange: 1.5f, aggroRange: 6f);

        [Header("Idle rule (spec §6)")]
        [Tooltip("Seconds with no target and no new command before an Idle hero starts Advancing.")]
        [SerializeField, Min(0f)] private float idleBeforeAdvanceSeconds = DefaultIdleBeforeAdvanceSeconds;
        [Tooltip("Provisional: if true, damage from an attacker outside attack range also resets the idle timer.")]
        [SerializeField] private bool idleTimerResetsOnOutOfRangeDamage;
        [Tooltip("Provisional: while walking to an AttackCommand target, defend against close attackers like when Moving.")]
        [SerializeField] private bool selfDefendWhileAttacking = true;

        [Header("Squad (spec §7)")]
        [SerializeField] private TroopDefinition troop;
        [SerializeField, Min(0)] private int baseSquadSize = 4;
        [SerializeField, Min(0)] private int maxSquadSize = 8;
        [Tooltip("Provisional: troops never stray further than this from their hero while fighting on their own.")]
        [SerializeField, Min(0f)] private float leashRadius = 4f;

        [Header("Summon (provisional, unused while 0)")]
        [SerializeField, Min(0f)] private float summonCost;
        [SerializeField, Min(0f)] private float summonCooldown;

        public string HeroId => heroId;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color PlaceholderColor => placeholderColor;
        public FactionDefinition Faction => faction;
        public CombatStats Stats => stats;
        public float IdleBeforeAdvanceSeconds => idleBeforeAdvanceSeconds;
        public bool IdleTimerResetsOnOutOfRangeDamage => idleTimerResetsOnOutOfRangeDamage;
        public bool SelfDefendWhileAttacking => selfDefendWhileAttacking;
        public TroopDefinition Troop => troop;
        public int BaseSquadSize => baseSquadSize;
        public int MaxSquadSize => maxSquadSize;
        public float LeashRadius => leashRadius;
        public float SummonCost => summonCost;
        public float SummonCooldown => summonCooldown;

        public void Validate(List<string> errors)
        {
            var owner = $"Hero '{name}'";
            if (string.IsNullOrWhiteSpace(heroId)) errors.Add($"{owner}: heroId is empty.");
            if (stats == null) errors.Add($"{owner}: stats are missing.");
            else stats.Validate(owner, errors);
            if (idleBeforeAdvanceSeconds < 0f) errors.Add($"{owner}: idleBeforeAdvanceSeconds must be >= 0.");
            if (baseSquadSize < 0) errors.Add($"{owner}: baseSquadSize must be >= 0.");
            if (maxSquadSize < baseSquadSize) errors.Add($"{owner}: maxSquadSize must be >= baseSquadSize.");
            if (baseSquadSize > 0 && troop == null) errors.Add($"{owner}: has a squad but no troop definition.");
            if (leashRadius <= 0f) errors.Add($"{owner}: leashRadius must be > 0.");
        }
    }
}

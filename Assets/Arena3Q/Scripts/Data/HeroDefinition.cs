using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// A hero and its squad (spec §10). Hero cards are UI only and read their display data from here (spec §3).
    /// Combat stats are the level-1, 1-star values; level, stars and breakthroughs scale them in battle
    /// (<see cref="CombatNumbersFactory"/>, docs/KeHoach/HeroProgression.md).
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
        [Tooltip("Lord = the player's character (skills by level, breakthroughs); Starter = no faction, cheap stars; " +
                 "Named = a general of a faction (Thục, Ngụy, Ngô, Quần).")]
        [SerializeField] private HeroKind kind = HeroKind.Starter;
        [Tooltip("Named heroes only: lords and starter heroes have no faction (spec §2).")]
        [SerializeField] private FactionDefinition faction;

        [Header("Combat (level 1, 1 star)")]
        [SerializeField] private CombatStats stats = new CombatStats(
            maxHealth: 500f, damage: 40f, moveSpeed: 2.5f, attackRange: 0.7f, attackInterval: 1f,
            reactionRange: 0.7f, aggroRange: 6f);
        [Tooltip("Share of incoming damage blocked: 0.1 = 10 % (capped by the progression config).")]
        [SerializeField, Range(0f, 0.9f)] private float damageReduction;
        [Tooltip("Added to the progression config's base crit chance.")]
        [SerializeField, Range(-1f, 1f)] private float critChanceBonus;

        [Header("Progression (docs/KeHoach/HeroProgression.md)")]
        [Tooltip("How Công and Máu grow per level. Empty = no growth.")]
        [SerializeField] private GrowthProfile growthProfile;
        [Tooltip("Thống suất: the squad's soldiers get Máu and Công × leadership / 100.")]
        [SerializeField, Min(0f)] private float leadership = 100f;
        [Tooltip("Strength not visible in the numbers (range, crowd control, extra soldiers, shielding allies): " +
                 "used by balance checks and combat power. 0.18 = worth +18 %.")]
        [SerializeField, Min(0f)] private float roleBonus;

        [Header("Idle rule (spec §6)")]
        [Tooltip("Seconds with no target and no new command before an Idle hero starts Advancing.")]
        [SerializeField, Min(0f)] private float idleBeforeAdvanceSeconds = DefaultIdleBeforeAdvanceSeconds;
        [Tooltip("Provisional: if true, damage from an attacker outside attack range also resets the idle timer.")]
        [SerializeField] private bool idleTimerResetsOnOutOfRangeDamage;
        [Tooltip("Provisional: while carrying out an AttackCommand, defend against close attackers like when Moving. Off by default: the order takes priority.")]
        [SerializeField] private bool selfDefendWhileAttacking;

        [Header("Squad (spec §7)")]
        [SerializeField] private TroopDefinition troop;
        [SerializeField, Min(0)] private int baseSquadSize = 4;
        [SerializeField, Min(0)] private int maxSquadSize = 8;
        [Tooltip("Provisional: troops never stray further than this from their hero while fighting on their own.")]
        [SerializeField, Min(0f)] private float leashRadius = 4f;
        [Tooltip("Provisional: seconds without getting closer to the ordered target before a soldier calls it unreachable.")]
        [SerializeField, Min(0.1f)] private float troopUnreachableSeconds = 1.5f;
        [Tooltip("Provisional: an unreachable target is replaced by the enemy nearest to it, within this radius.")]
        [SerializeField, Min(0f)] private float troopAssistRadius = 3f;

        [Header("Summon (provisional, unused while 0)")]
        [SerializeField, Min(0f)] private float summonCost;
        [SerializeField, Min(0f)] private float summonCooldown;

        public string HeroId => heroId;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public Color PlaceholderColor => placeholderColor;
        public HeroKind Kind => kind;
        public FactionDefinition Faction => faction;
        public CombatStats Stats => stats;
        public float DamageReduction => damageReduction;
        public float CritChanceBonus => critChanceBonus;
        public GrowthProfile GrowthProfile => growthProfile;
        public GrowthRates GrowthRates => growthProfile != null ? growthProfile.Rates : GrowthRates.None;
        public float Leadership => leadership;
        public float RoleBonus => roleBonus;
        public float IdleBeforeAdvanceSeconds => idleBeforeAdvanceSeconds;
        public bool IdleTimerResetsOnOutOfRangeDamage => idleTimerResetsOnOutOfRangeDamage;
        public bool SelfDefendWhileAttacking => selfDefendWhileAttacking;
        public TroopDefinition Troop => troop;
        public int BaseSquadSize => baseSquadSize;
        public int MaxSquadSize => maxSquadSize;
        public float LeashRadius => leashRadius;
        public float TroopUnreachableSeconds => troopUnreachableSeconds;
        public float TroopAssistRadius => troopAssistRadius;
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
            // Spec §2: only named heroes belong to a faction; lords and starter heroes never do.
            if (kind == HeroKind.Named && faction == null) errors.Add($"{owner}: a named hero needs a faction.");
            if (kind != HeroKind.Named && faction != null) errors.Add($"{owner}: only named heroes have a faction.");
            if (leadership < 0f) errors.Add($"{owner}: leadership must be >= 0.");
        }
    }
}

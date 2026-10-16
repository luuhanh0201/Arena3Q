using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Combat numbers shared by heroes and troops (spec §10).</summary>
    [Serializable]
    public sealed class CombatStats
    {
        [SerializeField, Min(1f)] private float maxHealth;
        [SerializeField, Min(0f)] private float damage;
        [SerializeField, Min(0f)] private float moveSpeed;
        [SerializeField, Min(0f)] private float attackRange;

        [Tooltip("Seconds between two attacks (attack speed / cooldown).")]
        [SerializeField, Min(0.05f)] private float attackInterval;

        [Tooltip("Self-defence radius while Moving: only attackers within this distance trigger Combat (spec §6). Default is about melee range.")]
        [SerializeField, Min(0f)] private float reactionRange;

        [Tooltip("Radius used to find targets while Advancing.")]
        [SerializeField, Min(0f)] private float aggroRange;

        public CombatStats()
        {
        }

        public CombatStats(float maxHealth, float damage, float moveSpeed, float attackRange, float attackInterval,
            float reactionRange, float aggroRange)
        {
            this.maxHealth = maxHealth;
            this.damage = damage;
            this.moveSpeed = moveSpeed;
            this.attackRange = attackRange;
            this.attackInterval = attackInterval;
            this.reactionRange = reactionRange;
            this.aggroRange = aggroRange;
        }

        public float MaxHealth => maxHealth;
        public float Damage => damage;
        public float MoveSpeed => moveSpeed;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public float ReactionRange => reactionRange;
        public float AggroRange => aggroRange;

        public void Validate(string owner, List<string> errors)
        {
            if (maxHealth <= 0f) errors.Add($"{owner}: maxHealth must be > 0.");
            if (damage < 0f) errors.Add($"{owner}: damage must be >= 0.");
            if (moveSpeed <= 0f) errors.Add($"{owner}: moveSpeed must be > 0.");
            if (attackRange <= 0f) errors.Add($"{owner}: attackRange must be > 0.");
            if (attackInterval <= 0f) errors.Add($"{owner}: attackInterval must be > 0.");
            if (reactionRange < 0f) errors.Add($"{owner}: reactionRange must be >= 0.");
            if (aggroRange < attackRange) errors.Add($"{owner}: aggroRange must be >= attackRange.");
        }
    }
}

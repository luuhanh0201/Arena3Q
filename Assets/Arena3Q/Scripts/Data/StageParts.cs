using System;
using UnityEngine;

namespace Arena3Q.Data
{
    [Serializable]
    public sealed class CastleConfig
    {
        [SerializeField] private Vector2 position;
        [SerializeField, Min(1f)] private float maxHealth = 3000f;
        [Tooltip("Whether the castle shoots enemies in range.")]
        [SerializeField] private bool canAttack;
        [SerializeField, Min(0f)] private float attackRange = 6f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0.05f)] private float attackInterval = 1.5f;

        public CastleConfig()
        {
        }

        public CastleConfig(Vector2 position, bool canAttack)
        {
            this.position = position;
            this.canAttack = canAttack;
        }

        public Vector2 Position => position;
        public float MaxHealth => maxHealth;
        public bool CanAttack => canAttack;
        public float AttackRange => attackRange;
        public float Damage => damage;
        public float AttackInterval => attackInterval;
    }

    /// <summary>An enemy defender placed around the enemy castle in an Attack stage (spec §8).</summary>
    [Serializable]
    public sealed class DefenderPlacement
    {
        [SerializeField] private TroopDefinition unit;
        [SerializeField] private Vector2 position;
        [Tooltip("Provisional: the defender charges player units entering this radius, then returns to its post.")]
        [SerializeField, Min(0f)] private float guardRadius = 4f;

        public TroopDefinition Unit => unit;
        public Vector2 Position => position;
        public float GuardRadius => guardRadius;
    }

    [Serializable]
    public sealed class WaveSpawn
    {
        [SerializeField] private TroopDefinition unit;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField] private Vector2 spawnPoint;

        public TroopDefinition Unit => unit;
        public int Count => count;
        public Vector2 SpawnPoint => spawnPoint;
    }

    /// <summary>One wave of attackers in a Defense stage (spec §8).</summary>
    [Serializable]
    public sealed class WaveConfig
    {
        [Tooltip("Seconds after battle start when this wave spawns.")]
        [SerializeField, Min(0f)] private float startTime;
        [SerializeField] private WaveSpawn[] spawns = Array.Empty<WaveSpawn>();

        public float StartTime => startTime;
        public WaveSpawn[] Spawns => spawns;
    }
}

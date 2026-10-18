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
        [Tooltip("Pictures of the castle standing and destroyed (with the fall animation). Empty = placeholder square.")]
        [SerializeField] private CastleArtConfig art;

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
        public CastleArtConfig Art => art;
    }

    /// <summary>Where a river can be crossed (bridge or ford).</summary>
    [Serializable]
    public sealed class RiverCrossingConfig
    {
        [Tooltip("A point on (or next to) the river's centre line where the crossing is.")]
        [SerializeField] private Vector2 position;
        [Tooltip("Clear walking width of the crossing, measured along the river.")]
        [SerializeField, Min(1f)] private float width = 4f;

        public RiverCrossingConfig()
        {
        }

        public RiverCrossingConfig(Vector2 position, float width)
        {
            this.position = position;
            this.width = width;
        }

        public Vector2 Position => position;
        public float Width => width;
    }

    /// <summary>A river forbidden zone (spec §9, provisional): centre line + width, crossable only at its crossings.</summary>
    [Serializable]
    public sealed class RiverConfig
    {
        [SerializeField] private string name = "Sông";
        [Tooltip("Centre line, in order. Extend it past the map edge to cut the map completely.")]
        [SerializeField] private Vector2[] path = Array.Empty<Vector2>();
        [SerializeField, Min(0.5f)] private float width = 2.5f;
        [SerializeField] private RiverCrossingConfig[] crossings = Array.Empty<RiverCrossingConfig>();

        public string Name => name;
        public Vector2[] Path => path;
        public float Width => width;
        public RiverCrossingConfig[] Crossings => crossings;
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

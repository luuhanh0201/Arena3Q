using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Shared runtime for every unit on the field: health, attacker memory, straight-line movement and attacks.
    /// Behaviour lives in a Core brain; subclasses feed it and pass the resulting intent to <see cref="Execute"/>.
    /// </summary>
    public abstract class UnitBase : MonoBehaviour
    {
        private readonly List<UnitBase> _attackers = new List<UnitBase>();
        private float _cooldown;
        private bool _dead;

        protected BattleController Battle { get; private set; }
        protected UnitVisual Visual { get; private set; }

        public UnitId Id { get; private set; }
        public Team Team { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;
        public float Radius { get; private set; }
        public virtual bool IsCastle => false;
        public Vector2 Position => transform.position;

        public float Damage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; } = 1f;
        public float MoveSpeed { get; private set; }
        public float ReactionRange { get; private set; }
        public float AggroRange { get; private set; }

        /// <summary>Damage from an attacker outside our attack range since the last brain tick.</summary>
        public bool TookDamageFromOutOfRange { get; private set; }

        /// <summary>The unit this one attacked or moved towards this tick, or None.</summary>
        public UnitId EngagedTarget { get; private set; }

        public event Action<UnitBase> Died;

        protected void InitUnit(BattleController battle, UnitId id, Team team, float maxHealth, float radius,
            Sprite sprite, Color color, bool withSelectionRing)
        {
            Battle = battle;
            Id = id;
            Team = team;
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            Radius = radius;
            Visual = new UnitVisual(transform, sprite != null ? sprite : PlaceholderArt.Circle, color, radius * 2f,
                withSelectionRing);
            Visual.SetHealth(1f, team == Team.Enemy);
        }

        protected void SetCombat(CombatStats stats) =>
            SetCombat(stats.Damage, stats.AttackRange, stats.AttackInterval, stats.MoveSpeed, stats.ReactionRange,
                stats.AggroRange);

        protected void SetCombat(float damage, float attackRange, float attackInterval, float moveSpeed,
            float reactionRange, float aggroRange)
        {
            Damage = damage;
            AttackRange = attackRange;
            AttackInterval = Mathf.Max(0.05f, attackInterval);
            MoveSpeed = moveSpeed;
            ReactionRange = reactionRange;
            AggroRange = aggroRange;
        }

        /// <summary>One simulation step. Called by <see cref="BattleController"/> only while the battle runs.</summary>
        public abstract void Simulate(float deltaTime);

        /// <summary>Gap between the two units' edges.</summary>
        public float EdgeDistance(UnitBase other) =>
            Mathf.Max(0f, Vector2.Distance(Position, other.Position) - Radius - other.Radius);

        public bool IsInAttackRange(UnitBase other) => EdgeDistance(other) <= AttackRange;

        public virtual void TakeDamage(float amount, UnitBase source)
        {
            // Spec §6: no damage once dead or after the battle has ended.
            if (!IsAlive || amount <= 0f || Battle == null || Battle.IsOver)
                return;

            Health = Mathf.Max(0f, Health - amount);
            Visual.Flash();
            Visual.SetHealth(Health / MaxHealth, Team == Team.Enemy);

            if (source != null && source.IsAlive)
            {
                if (!_attackers.Contains(source))
                    _attackers.Add(source);
                if (EdgeDistance(source) > AttackRange)
                    TookDamageFromOutOfRange = true;
            }

            OnDamaged(amount, source);
            if (Health <= 0f)
                Die();
        }

        /// <summary>Removes the unit immediately (death or squad disband).</summary>
        public void Die()
        {
            Health = 0f;
            if (_dead)
                return;
            _dead = true;
            OnDied();
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void OnDamaged(float amount, UnitBase source)
        {
        }

        protected virtual void OnDied()
        {
        }

        /// <summary>Nearest living attacker within reaction range; forgets attackers that died or moved away (spec §6).</summary>
        protected UnitId FindCloseAttacker()
        {
            UnitBase best = null;
            var bestDistance = float.MaxValue;
            for (var i = _attackers.Count - 1; i >= 0; i--)
            {
                var attacker = _attackers[i];
                if (attacker == null || !attacker.IsAlive)
                {
                    _attackers.RemoveAt(i);
                    continue;
                }
                var distance = EdgeDistance(attacker);
                if (distance > ReactionRange)
                {
                    _attackers.RemoveAt(i);
                    continue;
                }
                if (distance < bestDistance)
                {
                    best = attacker;
                    bestDistance = distance;
                }
            }
            return best != null ? best.Id : UnitId.None;
        }

        protected void ConsumeDamageFlags() => TookDamageFromOutOfRange = false;

        /// <summary>Turns a brain intent into movement and attacks.</summary>
        protected void Execute(HeroIntent intent, float deltaTime)
        {
            _cooldown -= deltaTime;
            EngagedTarget = UnitId.None;

            switch (intent.Kind)
            {
                case HeroIntentKind.MoveTo:
                    MoveTowards(intent.Destination, deltaTime, 0f);
                    break;

                case HeroIntentKind.Engage:
                    var target = Battle.Registry.Get(intent.Target);
                    if (target == null)
                        break;
                    EngagedTarget = target.Id;
                    if (IsInAttackRange(target))
                    {
                        if (_cooldown <= 0f)
                        {
                            _cooldown = AttackInterval;
                            target.TakeDamage(Damage, this);
                        }
                    }
                    else if (intent.MayChase)
                    {
                        MoveTowards(target.Position, deltaTime, Radius + target.Radius + AttackRange * 0.8f);
                    }
                    break;
            }
        }

        protected void MoveTowards(Vector2 destination, float deltaTime, float stopDistance)
        {
            var position = Position;
            var toTarget = destination - position;
            var distance = toTarget.magnitude;
            if (distance <= stopDistance || distance < 1e-5f)
                return;
            var step = Mathf.Min(MoveSpeed * deltaTime, distance - stopDistance);
            SetPosition(position + toTarget / distance * step);
        }

        /// <summary>Moves the unit, clamped to the battlefield bounds.</summary>
        public void SetPosition(Vector2 position)
        {
            if (Battle != null && Battle.Layout != null)
            {
                var b = Battle.Layout.Bounds;
                position.x = Mathf.Clamp(position.x, b.xMin + Radius, b.xMax - Radius);
                position.y = Mathf.Clamp(position.y, b.yMin + Radius, b.yMax - Radius);
            }
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        protected virtual void LateUpdate()
        {
            Visual?.Update(Time.deltaTime);
        }
    }
}

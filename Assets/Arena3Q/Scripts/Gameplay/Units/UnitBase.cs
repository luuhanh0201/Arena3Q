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
        // Who damaged us and when (battle time), for the "just took damage" rule (spec §6 rule 2).
        private readonly List<(UnitBase unit, float lastHit)> _attackers = new List<(UnitBase unit, float lastHit)>();
        private float _cooldown;
        private bool _dead;
        // Position on the logic plane. The transform shows it through the battle's view projection (2.5D tilt).
        private Vector2 _position;

        public BattleController Battle { get; private set; }
        protected UnitVisual Visual { get; private set; }

        public UnitId Id { get; private set; }
        public Team Team { get; private set; }
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;
        public float Radius { get; private set; }
        public virtual bool IsCastle => false;
        /// <summary>Logic-plane position (all gameplay uses this; the transform is only the on-screen projection).</summary>
        public Vector2 Position => _position;
        /// <summary>Voluntary movement only; crowd separation and knockback do not start a running animation.</summary>
        public Vector2 LocomotionVelocity { get; private set; }
        protected void ResetLocomotion() => LocomotionVelocity = Vector2.zero;

        public float Damage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; } = 1f;
        public float MoveSpeed { get; private set; }
        public float ReactionRange { get; private set; }
        public float AggroRange { get; private set; }

        /// <summary>Share of incoming damage blocked (already capped by the progression rules).</summary>
        public float DamageReduction { get; private set; }
        public float CritChance { get; private set; }
        public float CritMultiplier { get; private set; } = 1f;

        /// <summary>Timed statuses from skills: stun, slow, shield, poison, buffs.</summary>
        public StatusSet Statuses { get; } = new StatusSet();
        public bool IsStunned => Statuses.IsStunned;

        /// <summary>Move speed after slows, speed buffs and the unit's own bonus (boss phase 2).</summary>
        public float CurrentMoveSpeed => MoveSpeed * Statuses.MoveSpeedMultiplier * MoveSpeedFactor;

        /// <summary>Permanent move-speed factor of the unit itself (boss phase 2); 1 = none.</summary>
        protected virtual float MoveSpeedFactor => 1f;

        /// <summary>Flying units cross rivers anywhere (spec §8); rectangular zones and the map edge still block them.</summary>
        public virtual bool IsFlying => false;

        /// <summary>The forbidden zones this unit is blocked by (flyers ignore rivers).</summary>
        protected BattlefieldLayout MovementLayout => Battle != null ? Battle.LayoutFor(this) : null;

        /// <summary>The living enemy that taunts this unit, or null (spec §8).</summary>
        public UnitBase TauntSource
        {
            get
            {
                var source = Statuses.SourceOf(StatusKind.Taunt) as UnitBase;
                return source != null && source.IsAlive && source.Team != Team ? source : null;
            }
        }

        /// <summary>A new player order ends a taunt (spec §8: the player's order always wins).</summary>
        public void ClearTaunt()
        {
            if (Statuses.Has(StatusKind.Taunt))
                Statuses.Remove(StatusKind.Taunt);
        }

        /// <summary>Seconds between attacks after attack-speed statuses (heroes add their passives).</summary>
        public virtual float CurrentAttackInterval => AttackInterval / Statuses.AttackSpeedMultiplier;

        private readonly List<(object source, float damagePerSecond)> _poison = new List<(object source, float damagePerSecond)>();

        /// <summary>An attacker counts as a threat only this long after its last hit (provisional, default 2 s).</summary>
        public float AttackerMemorySeconds { get; private set; } = CombatStats.DefaultAttackerMemorySeconds;

        /// <summary>Damage from an attacker outside our attack range since the last brain tick.</summary>
        public bool TookDamageFromOutOfRange { get; private set; }

        /// <summary>The unit this one attacked or moved towards this tick, or None.</summary>
        public UnitId EngagedTarget { get; private set; }

        public event Action<UnitBase> Died;

        /// <summary>Core record of a soldier (troops and enemies; heroes and castles have their own), kept in sync.</summary>
        public UnitRuntime Record { get; internal set; }

        protected void InitUnit(BattleController battle, UnitId id, Team team, float maxHealth, float radius,
            Sprite sprite, Color color, bool withSelectionRing)
        {
            Battle = battle;
            Id = id;
            Team = team;
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            Radius = radius;
            // In 3D a unit without a picture stands as a lit placeholder body.
            Visual = new UnitVisual(transform, sprite != null ? sprite : PlaceholderArt.Circle, color, radius * 2f,
                withSelectionRing, battle.Presentation3D && sprite == null ? battle.View : null);
            Visual.SetHealth(1f, team == Team.Enemy);
            Visual.SetGroundScale(battle.ViewYScale);
            transform.rotation = battle.UprightRotation;
            Visual.SetGroundRotation(Quaternion.Inverse(battle.UprightRotation));
        }

        protected void SetCombat(CombatStats stats) =>
            SetCombat(stats.Damage, stats.AttackRange, stats.AttackInterval, stats.MoveSpeed, stats.ReactionRange,
                stats.AggroRange, stats.AttackerMemorySeconds);

        /// <summary>Final numbers after level, stars and bonuses (built by <c>CombatNumbersFactory</c>).</summary>
        protected void SetCombat(CombatNumbers numbers)
        {
            SetCombat(numbers.Damage, numbers.AttackRange, numbers.AttackInterval, numbers.MoveSpeed, numbers.ReactionRange,
                numbers.AggroRange, numbers.AttackerMemorySeconds);
            DamageReduction = Mathf.Clamp(numbers.DamageReduction, 0f, 0.95f);
            CritChance = Mathf.Clamp01(numbers.CritChance);
            CritMultiplier = Mathf.Max(1f, numbers.CritMultiplier);
        }

        protected void SetCombat(float damage, float attackRange, float attackInterval, float moveSpeed,
            float reactionRange, float aggroRange, float attackerMemorySeconds = CombatStats.DefaultAttackerMemorySeconds)
        {
            Damage = damage;
            AttackRange = attackRange;
            AttackInterval = Mathf.Max(0.05f, attackInterval);
            MoveSpeed = moveSpeed;
            ReactionRange = reactionRange;
            AggroRange = aggroRange;
            AttackerMemorySeconds = Mathf.Max(0.05f, attackerMemorySeconds);
        }

        /// <summary>One simulation step. Called by <see cref="BattleController"/> only while the battle runs.</summary>
        public abstract void Simulate(float deltaTime);

        /// <summary>Gap between the two units' edges.</summary>
        public float EdgeDistance(UnitBase other) =>
            Mathf.Max(0f, Vector2.Distance(Position, other.Position) - Radius - other.Radius);

        public bool IsInAttackRange(UnitBase other) => EdgeDistance(other) <= AttackRange;

        /// <param name="ignoreReduction">True damage (e.g. the Tử gate's share of max health): damage reduction does
        /// not apply, shields still absorb it.</param>
        public virtual void TakeDamage(float amount, UnitBase source, bool ignoreReduction = false)
        {
            // Spec §6: no damage once dead or after the battle has ended.
            if (!IsAlive || amount <= 0f || Battle == null || Battle.IsOver)
                return;

            if (!ignoreReduction)
            {
                var reduction = DamageReduction + ExtraDamageReduction + Statuses.DamageTakenReduction;
                amount *= 1f - Mathf.Clamp(reduction, 0f, Battle.Rules.Settings.MaxDamageReduction);
            }
            var dealt = amount;
            amount = Statuses.AbsorbWithShield(amount);
            Health = Mathf.Max(0f, Health - amount);
            if (source != null)
                source.OnDealtDamage(this, dealt);
            if (Record != null)
                Record.Health = Health;
            Visual.Flash();
            Visual.SetHealth(Health / MaxHealth, Team == Team.Enemy);

            if (source != null && source.IsAlive)
            {
                var index = _attackers.FindIndex(a => a.unit == source);
                if (index >= 0)
                    _attackers[index] = (source, Battle.BattleTime);
                else
                    _attackers.Add((source, Battle.BattleTime));
                if (EdgeDistance(source) > AttackRange)
                    TookDamageFromOutOfRange = true;
            }

            OnDamaged(amount, source);
            if (Health <= 0f && !TryCheatDeath())
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

        /// <summary>Called when health reaches 0: return true to stay alive (enemy trait "stands up once").</summary>
        protected virtual bool TryCheatDeath() => false;

        /// <summary>Back on its feet with this share of max health, statuses cleared.</summary>
        protected void Revive(float healthFraction)
        {
            Health = Mathf.Clamp(MaxHealth * healthFraction, 1f, MaxHealth);
            Statuses.Clear();
            _attackers.Clear();
            if (Record != null)
                Record.Health = Health;
            Visual.SetHealth(Health / MaxHealth, Team == Team.Enemy);
            RefreshStatusVisual();
            OnHealthChanged();
        }

        /// <summary>Damage reduction from passives (heroes), added to the unit's own and the statuses'.</summary>
        protected virtual float ExtraDamageReduction => 0f;

        /// <summary>This unit hurt <paramref name="target"/> (any damage, after the target's reduction). Heroes gain rage.</summary>
        protected virtual void OnDealtDamage(UnitBase target, float amount)
        {
        }

        /// <summary>Damage of one basic attack (heroes add their passives).</summary>
        protected virtual float ComputeAttackDamage(UnitBase target)
        {
            var damage = Damage * Statuses.DamageMultiplier;
            return CritChance > 0f && Battle.RollCritical(CritChance) ? damage * CritMultiplier : damage;
        }

        /// <summary>A basic attack just hit (heroes: lifesteal, combo, pierce…).</summary>
        protected virtual void OnAttackLanded(UnitBase target, float damage)
        {
        }

        /// <summary>Adds a status unless the unit is a castle (castles ignore statuses).</summary>
        public bool ApplyStatus(StatusKind kind, float value, float duration, object source = null)
        {
            if (!IsAlive || IsCastle)
                return false;
            var applied = Statuses.Apply(kind, value, duration, source);
            RefreshStatusVisual();
            return applied;
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            if (Record != null)
                Record.Health = Health;
            Visual.SetHealth(Health / MaxHealth, Team == Team.Enemy);
            OnHealthChanged();
        }

        /// <summary>Raises (or lowers) max health keeping the same share of health (army-wide bonuses).</summary>
        public void ScaleMaxHealth(float factor)
        {
            if (!IsAlive || factor <= 0f)
                return;
            MaxHealth *= factor;
            Health *= factor;
            if (Record != null)
                Record.Health = Health;
            Visual.SetHealth(Health / MaxHealth, Team == Team.Enemy);
            OnHealthChanged();
        }

        protected virtual void OnHealthChanged()
        {
        }

        /// <summary>
        /// Pushes or pulls the unit, stopped by forbidden zones and the map edge (spec §8 ✅); castles and units immune
        /// to crowd control do not move.
        /// </summary>
        public void Displace(Vector2 offset)
        {
            if (!IsAlive || IsCastle || Statuses.IsImmune || offset.sqrMagnitude < 1e-8f)
                return;
            var target = Position + offset;
            var layout = MovementLayout;
            SetPosition(layout != null ? layout.ResolveStep(Position, target, Radius) : target);
        }

        /// <summary>Advances statuses and applies poison; called by the battle every step before the unit thinks.</summary>
        internal void TickStatuses(float deltaTime)
        {
            if (Statuses.Count == 0)
                return;
            Statuses.Tick(deltaTime);
            _poison.Clear();
            Statuses.CollectPoison(_poison);
            foreach (var (source, damagePerSecond) in _poison)
                TakeDamage(damagePerSecond * deltaTime, source as UnitBase);
            RefreshStatusVisual();
        }

        private void RefreshStatusVisual() =>
            Visual?.SetStatus(Statuses.IsStunned, Statuses.Shield > 0f, Statuses.Has(StatusKind.Slow) || Statuses.Has(StatusKind.Weaken),
                Statuses.Has(StatusKind.Taunt));

        protected virtual void OnDied()
        {
        }

        /// <summary>
        /// Nearest living attacker within reaction range that hit us within the last <see cref="AttackerMemorySeconds"/>;
        /// forgets attackers that died, moved away or stopped attacking (spec §6 rules 2–3).
        /// </summary>
        protected UnitId FindCloseAttacker()
        {
            UnitBase best = null;
            var bestDistance = float.MaxValue;
            var now = Battle.BattleTime;
            for (var i = _attackers.Count - 1; i >= 0; i--)
            {
                var (attacker, lastHit) = _attackers[i];
                if (attacker == null || !attacker.IsAlive || now - lastHit > AttackerMemorySeconds)
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
            ResetLocomotion();
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
                            _cooldown = CurrentAttackInterval;
                            BeginBasicAttack(target);
                        }
                    }
                    else if (intent.MayChase)
                    {
                        MoveTowards(target.Position, deltaTime, Radius + target.Radius + AttackRange * 0.8f);
                    }
                    break;
            }
        }

        protected virtual void BeginBasicAttack(UnitBase target)
        {
            var damage = ComputeAttackDamage(target);
            target.TakeDamage(damage, this);
            OnAttackLanded(target, damage);
        }

        protected void MoveTowards(Vector2 destination, float deltaTime, float stopDistance)
        {
            var position = Position;
            var toTarget = destination - position;
            var distance = toTarget.magnitude;
            if (distance <= stopDistance || distance < 1e-5f)
                return;
            var step = Mathf.Min(CurrentMoveSpeed * deltaTime, distance - stopDistance);
            var layout = MovementLayout;
            if (layout == null)
            {
                SetPosition(position + toTarget / distance * step);
                LocomotionVelocity = deltaTime > 0f ? (Position - position) / deltaTime : Vector2.zero;
                return;
            }

            // Forbidden zones block movement (spec §9): head for a corner of the zone in the way, if any.
            var waypoint = layout.NextWaypoint(position, destination, Radius);
            var toWaypoint = waypoint - position;
            var waypointDistance = toWaypoint.magnitude;
            var next = waypointDistance < 1e-5f
                ? position
                : position + toWaypoint / waypointDistance * Mathf.Min(step, waypointDistance);
            SetPosition(layout.ResolveStep(position, next, Radius));
            LocomotionVelocity = deltaTime > 0f ? (Position - position) / deltaTime : Vector2.zero;
        }

        /// <summary>Moves the unit, clamped to the battlefield bounds and kept out of forbidden zones.</summary>
        public void SetPosition(Vector2 position)
        {
            var layout = MovementLayout;
            if (layout != null)
            {
                var b = layout.Bounds;
                position = layout.PushOut(position, Radius);
                position.x = Mathf.Clamp(position.x, b.xMin + Radius, b.xMax - Radius);
                position.y = Mathf.Clamp(position.y, b.yMin + Radius, b.yMax - Radius);
            }
            _position = position;
            ApplyViewTransform();
        }

        /// <summary>Re-applies the view projection after the camera tilt changed (in 3D the unit faces the camera).</summary>
        internal void RefreshView()
        {
            ApplyViewTransform();
            Visual?.SetGroundScale(Battle != null ? Battle.ViewYScale : 1f);
            if (Battle != null)
            {
                transform.rotation = Battle.UprightRotation;
                Visual?.SetGroundRotation(Quaternion.Inverse(Battle.UprightRotation));
            }
        }

        private void ApplyViewTransform()
        {
            transform.position = Battle != null ? Battle.ToView(_position) + Battle.WorldUp * _lift
                : new Vector3(_position.x, _position.y, 0f);
        }

        private Vector2 _lastFacingPosition;
        private float _lift;

        protected virtual void LateUpdate()
        {
            ApplyLift();
            FaceForward();
            Visual?.Update(Time.deltaTime);
        }

        // Knocked up: the unit rises and falls on an arc over the status' time; its shadow and ring stay on the ground.
        private void ApplyLift()
        {
            var lift = Battle != null && Statuses.Has(StatusKind.Airborne)
                ? Mathf.Sin(Mathf.PI * Statuses.Progress(StatusKind.Airborne)) * Battle.View.KnockUpHeight
                : 0f;
            if (lift <= 0f && _lift <= 0f)
                return;
            _lift = Mathf.Max(0f, lift);
            ApplyViewTransform();
            Visual?.SetGroundOffset(Quaternion.Inverse(transform.rotation) * (-Battle.WorldUp * _lift));
        }

        // 3D bodies look where they walk, or at what they fight while standing.
        private void FaceForward()
        {
            if (Visual?.Model == null)
                return;
            var moved = _position - _lastFacingPosition;
            _lastFacingPosition = _position;
            if (moved.sqrMagnitude > 1e-6f)
                Visual.Face(moved);
            else if (EngagedTarget.IsValid && Battle != null && Battle.Registry.Get(EngagedTarget) is UnitBase target)
                Visual.Face(target.Position - _position);
        }
    }
}

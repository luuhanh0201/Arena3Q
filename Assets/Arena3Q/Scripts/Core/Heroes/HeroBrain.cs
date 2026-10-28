using System;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// Hero behaviour state machine (spec §5 "Cơ chế mới", §6): Idle, Moving, Attacking, Advancing, Combat, Dead.
    /// The player no longer orders single heroes: every hero follows the army-wide <see cref="ArmyStance"/>
    /// (<see cref="SetStance"/>). Giữ = Idle in place, Tấn công = Advancing, Phòng thủ = Moving back to the defence area,
    /// Idle there and Advancing on an intruder. Move / attack commands remain for scripted orders and tests.
    /// Pure logic: call <see cref="Tick"/> once per simulation step, then execute the returned <see cref="HeroIntent"/>.
    /// </summary>
    public sealed class HeroBrain
    {
        private readonly HeroBrainSettings _settings;

        // State to return to when Combat ends (Moving or Attacking).
        private HeroState _resumeState;
        // Target locked while Advancing, kept until it dies (spec §6 "đánh tới khi mục tiêu chết").
        private UnitId _advanceTarget;

        public HeroBrain(HeroBrainSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Raised as (previous, next) whenever the state changes.</summary>
        public event Action<HeroState, HeroState> StateChanged;

        /// <summary>A freshly summoned hero starts Idle (spec §4) and then follows the army stance.</summary>
        public HeroState State { get; private set; } = HeroState.Idle;

        /// <summary>The army-wide order this hero follows (Giữ until told otherwise).</summary>
        public ArmyStance Stance { get; private set; } = ArmyStance.Hold;

        /// <summary>Phòng thủ: the point inside the defence area this hero guards.</summary>
        public Vector2 DefendPoint { get; private set; }

        public HeroCommandKind CurrentCommand { get; private set; }

        /// <summary>Saved MoveCommand destination (or the defence point while falling back); Combat resumes towards it.</summary>
        public Vector2 Destination { get; private set; }

        /// <summary>AttackCommand target.</summary>
        public UnitId AttackTarget { get; private set; }

        public bool IsDead => State == HeroState.Dead;

        /// <summary>
        /// Follow a new army stance from <paramref name="position"/> (spec §5): Giữ stops here, Tấn công advances,
        /// Phòng thủ walks back to the nearest point of the defence area. Replaces any scripted order.
        /// </summary>
        public void SetStance(ArmyStance stance, Vector2 position)
        {
            if (IsDead)
                return;
            Stance = stance;
            ClearCommand();
            ResetTransientState();
            switch (stance)
            {
                case ArmyStance.Attack:
                    SetState(HeroState.Advancing);
                    break;
                case ArmyStance.Defend:
                    DefendPoint = ClampInto(_settings.DefendArea, position);
                    if (IsAt(position, DefendPoint))
                        SetState(HeroState.Idle);
                    else
                        BeginReturn();
                    break;
                default:
                    SetState(HeroState.Idle);
                    break;
            }
        }

        /// <summary>A new command replaces the old one (scripted orders and tests; the player has no per-hero orders).</summary>
        public void Issue(in MoveCommand command)
        {
            if (IsDead)
                return;
            CurrentCommand = HeroCommandKind.Move;
            Destination = command.Destination;
            AttackTarget = UnitId.None;
            ResetTransientState();
            SetState(HeroState.Moving);
        }

        public void Issue(in AttackCommand command)
        {
            if (IsDead)
                return;
            CurrentCommand = HeroCommandKind.Attack;
            AttackTarget = command.Target;
            ResetTransientState();
            SetState(HeroState.Attacking);
        }

        /// <summary>Death ends everything: commands are cancelled and the brain stops acting.</summary>
        public void Kill()
        {
            if (IsDead)
                return;
            ClearCommand();
            ResetTransientState();
            SetState(HeroState.Dead);
        }

        public HeroIntent Tick(float deltaTime, IHeroSensor sensor)
        {
            if (sensor == null)
                throw new ArgumentNullException(nameof(sensor));

            switch (State)
            {
                case HeroState.Idle: return TickIdle(sensor);
                case HeroState.Moving: return TickMoving(sensor);
                case HeroState.Attacking: return TickAttacking(sensor);
                case HeroState.Combat: return TickCombat(sensor);
                case HeroState.Advancing: return TickAdvancing(sensor);
                default: return HeroIntent.Hold;
            }
        }

        // Idle: hit enemies that walk into attack range without chasing. What else happens depends on the stance:
        // Giữ waits here (no 5-second rule any more), Tấn công advances, Phòng thủ goes for intruders in the area.
        private HeroIntent TickIdle(IHeroSensor sensor)
        {
            var target = sensor.EnemyInAttackRange;
            if (target.IsValid)
                return HeroIntent.Engage(target, mayChase: false);

            switch (Stance)
            {
                case ArmyStance.Attack:
                    SetState(HeroState.Advancing);
                    return TickAdvancing(sensor);
                case ArmyStance.Defend:
                {
                    var intruder = sensor.NearestEnemyIn(ChaseArea);
                    if (intruder.IsValid)
                    {
                        _advanceTarget = intruder;
                        SetState(HeroState.Advancing);
                        return HeroIntent.Engage(intruder, mayChase: true);
                    }
                    if (!IsAt(sensor.Position, DefendPoint))
                    {
                        BeginReturn();
                        return HeroIntent.MoveTo(Destination);
                    }
                    return HeroIntent.Hold;
                }
                default:
                    return HeroIntent.Hold;
            }
        }

        // Moving (IgnoreUntilDestination): walk on regardless of enemies; only a close attacker triggers Combat.
        // Used by scripted move orders and by Phòng thủ falling back to its defence point.
        private HeroIntent TickMoving(IHeroSensor sensor)
        {
            var attacker = sensor.CloseAttacker;
            if (attacker.IsValid)
                return EnterCombat(HeroState.Moving, attacker);

            if (IsAt(sensor.Position, Destination))
            {
                EnterIdle();
                return TickIdle(sensor);
            }

            return HeroIntent.MoveTo(Destination);
        }

        private HeroIntent TickAttacking(IHeroSensor sensor)
        {
            if (!AttackTarget.IsValid || !sensor.IsAlive(AttackTarget))
            {
                EnterIdle();
                return TickIdle(sensor);
            }

            if (_settings.SelfDefendWhileAttacking)
            {
                var attacker = sensor.CloseAttacker;
                if (attacker.IsValid && attacker != AttackTarget)
                    return EnterCombat(HeroState.Attacking, attacker);
            }

            return HeroIntent.Engage(AttackTarget, mayChase: true);
        }

        // Combat: fight the close attacker in place (spec §6 rule 5, no chasing); once no close threat remains,
        // resume the interrupted order. reactionRange must not exceed attack range, or the hero would stand still
        // against an attacker it cannot hit (HeroDefinition.Validate enforces this).
        private HeroIntent TickCombat(IHeroSensor sensor)
        {
            var attacker = sensor.CloseAttacker;
            if (attacker.IsValid)
                return HeroIntent.Engage(attacker, mayChase: false);

            var resume = _resumeState;
            _resumeState = HeroState.Idle;
            SetState(resume);
            return resume == HeroState.Attacking ? TickAttacking(sensor) : TickMoving(sensor);
        }

        // Advancing. Tấn công: normal AI, fight the locked target until it dies, else the nearest enemy in aggro range,
        // else head for the stage objective (spec §6). Phòng thủ: chase an intruder while it stays near the area.
        private HeroIntent TickAdvancing(IHeroSensor sensor)
        {
            if (Stance == ArmyStance.Defend)
            {
                if (!_advanceTarget.IsValid || !sensor.IsAlive(_advanceTarget) || !sensor.IsInArea(_advanceTarget, ChaseArea))
                    _advanceTarget = sensor.NearestEnemyIn(ChaseArea);
                if (_advanceTarget.IsValid)
                    return HeroIntent.Engage(_advanceTarget, mayChase: true);
                BeginReturn();
                return TickMoving(sensor);
            }

            if (!_advanceTarget.IsValid || !sensor.IsAlive(_advanceTarget))
                _advanceTarget = sensor.EnemyInAggroRange;
            if (_advanceTarget.IsValid)
                return HeroIntent.Engage(_advanceTarget, mayChase: true);

            var objective = _settings.StageType == StageType.Attack ? sensor.EnemyCastle : sensor.NearestEnemy;
            if (objective.IsValid)
                return HeroIntent.Engage(objective, mayChase: true);

            // Nothing to advance towards (e.g. Defense stage with no enemies left): stand by, still Advancing so the
            // hero sets off again as soon as an enemy appears.
            return HeroIntent.Hold;
        }

        private Rect ChaseArea
        {
            get
            {
                var area = _settings.DefendArea;
                var margin = Mathf.Max(0f, _settings.DefendChaseMargin);
                return new Rect(area.xMin - margin, area.yMin - margin, area.width + margin * 2f, area.height + margin * 2f);
            }
        }

        private void BeginReturn()
        {
            CurrentCommand = HeroCommandKind.None;
            Destination = DefendPoint;
            _advanceTarget = UnitId.None;
            SetState(HeroState.Moving);
        }

        private bool IsAt(Vector2 position, Vector2 point) =>
            (position - point).sqrMagnitude <= _settings.ArrivalTolerance * _settings.ArrivalTolerance;

        private static Vector2 ClampInto(Rect area, Vector2 point) =>
            area.width <= 0f || area.height <= 0f
                ? point
                : new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));

        private HeroIntent EnterCombat(HeroState resumeState, UnitId attacker)
        {
            _resumeState = resumeState;
            SetState(HeroState.Combat);
            return HeroIntent.Engage(attacker, mayChase: false);
        }

        private void EnterIdle()
        {
            ClearCommand();
            ResetTransientState();
            SetState(HeroState.Idle);
        }

        private void ClearCommand()
        {
            CurrentCommand = HeroCommandKind.None;
            AttackTarget = UnitId.None;
        }

        private void ResetTransientState()
        {
            _resumeState = HeroState.Idle;
            _advanceTarget = UnitId.None;
        }

        private void SetState(HeroState next)
        {
            if (State == next)
                return;
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}

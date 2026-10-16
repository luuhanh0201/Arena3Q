using System;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// Hero behaviour state machine (spec §6): Idle, Moving, Attacking, Advancing, Combat, Dead.
    /// Pure logic: call <c>Issue</c> for player commands and <see cref="Tick"/> once per simulation step, then execute
    /// the returned <see cref="HeroIntent"/>.
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

        /// <summary>A freshly summoned hero starts Idle (spec §4).</summary>
        public HeroState State { get; private set; } = HeroState.Idle;

        public HeroCommandKind CurrentCommand { get; private set; }

        /// <summary>Saved MoveCommand destination; Combat resumes towards it.</summary>
        public Vector2 Destination { get; private set; }

        /// <summary>AttackCommand target.</summary>
        public UnitId AttackTarget { get; private set; }

        /// <summary>Seconds spent Idle without a target. Resets on any target, engagement or new command.</summary>
        public float IdleTimer { get; private set; }

        public bool IsDead => State == HeroState.Dead;

        /// <summary>A new command replaces the old one and cancels the idle timer (spec §5, §6).</summary>
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
            if (deltaTime < 0f)
                deltaTime = 0f;

            switch (State)
            {
                case HeroState.Idle: return TickIdle(deltaTime, sensor);
                case HeroState.Moving: return TickMoving(sensor);
                case HeroState.Attacking: return TickAttacking(sensor);
                case HeroState.Combat: return TickCombat(sensor);
                case HeroState.Advancing: return TickAdvancing(sensor);
                default: return HeroIntent.Hold;
            }
        }

        // Idle: hit enemies that walk into attack range without chasing; after IdleBeforeAdvanceSeconds of
        // continuous "no target", start Advancing.
        private HeroIntent TickIdle(float deltaTime, IHeroSensor sensor)
        {
            var target = sensor.EnemyInAttackRange;
            if (target.IsValid)
            {
                IdleTimer = 0f;
                return HeroIntent.Engage(target, mayChase: false);
            }

            if (sensor.TookDamageFromOutOfRange && _settings.IdleTimerResetsOnOutOfRangeDamage)
                IdleTimer = 0f;
            else
                IdleTimer += deltaTime;

            if (IdleTimer < _settings.IdleBeforeAdvanceSeconds)
                return HeroIntent.Hold;

            SetState(HeroState.Advancing);
            return TickAdvancing(sensor);
        }

        // Moving (IgnoreUntilDestination): walk on regardless of enemies; only a close attacker triggers Combat.
        private HeroIntent TickMoving(IHeroSensor sensor)
        {
            var attacker = sensor.CloseAttacker;
            if (attacker.IsValid)
                return EnterCombat(HeroState.Moving, attacker);

            if ((sensor.Position - Destination).sqrMagnitude <= _settings.ArrivalTolerance * _settings.ArrivalTolerance)
            {
                EnterIdle();
                return TickIdle(0f, sensor);
            }

            return HeroIntent.MoveTo(Destination);
        }

        private HeroIntent TickAttacking(IHeroSensor sensor)
        {
            if (!AttackTarget.IsValid || !sensor.IsAlive(AttackTarget))
            {
                EnterIdle();
                return TickIdle(0f, sensor);
            }

            if (_settings.SelfDefendWhileAttacking)
            {
                var attacker = sensor.CloseAttacker;
                if (attacker.IsValid && attacker != AttackTarget)
                    return EnterCombat(HeroState.Attacking, attacker);
            }

            return HeroIntent.Engage(AttackTarget, mayChase: true);
        }

        // Combat: fight the close attacker; once no close threat remains, resume the interrupted order.
        private HeroIntent TickCombat(IHeroSensor sensor)
        {
            var attacker = sensor.CloseAttacker;
            if (attacker.IsValid)
                return HeroIntent.Engage(attacker, mayChase: true);

            var resume = _resumeState;
            _resumeState = HeroState.Idle;
            SetState(resume);
            return resume == HeroState.Attacking ? TickAttacking(sensor) : TickMoving(sensor);
        }

        // Advancing: normal AI. Fight the locked target until it dies, else the nearest enemy in aggro range,
        // else head for the stage objective (spec §6, provisional direction).
        private HeroIntent TickAdvancing(IHeroSensor sensor)
        {
            if (!_advanceTarget.IsValid || !sensor.IsAlive(_advanceTarget))
                _advanceTarget = sensor.EnemyInAggroRange;
            if (_advanceTarget.IsValid)
                return HeroIntent.Engage(_advanceTarget, mayChase: true);

            var objective = _settings.StageType == StageType.Attack ? sensor.EnemyCastle : sensor.NearestEnemy;
            if (objective.IsValid)
                return HeroIntent.Engage(objective, mayChase: true);

            // Nothing to advance towards (e.g. Defense stage with no enemies left): stand by in Idle.
            // Return Hold rather than ticking Idle so a zero idle delay cannot bounce back here in the same tick.
            EnterIdle();
            return HeroIntent.Hold;
        }

        private HeroIntent EnterCombat(HeroState resumeState, UnitId attacker)
        {
            _resumeState = resumeState;
            SetState(HeroState.Combat);
            return HeroIntent.Engage(attacker, mayChase: true);
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
            IdleTimer = 0f;
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

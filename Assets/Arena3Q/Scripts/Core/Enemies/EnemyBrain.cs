using System;
using UnityEngine;

namespace Arena3Q.Core
{
    public enum EnemyRole
    {
        /// <summary>Attack-stage defender: holds a post near the enemy castle (spec §8).</summary>
        Guard,
        /// <summary>Defense-stage wave attacker: marches on the player castle (spec §8).</summary>
        Assault
    }

    /// <summary>What an enemy soldier perceives. "Opponent" queries return living player units (heroes, troops).</summary>
    public interface IEnemySensor
    {
        Vector2 Position { get; }
        UnitId OpponentInAttackRange { get; }
        UnitId OpponentInAggroRange { get; }
        /// <summary>Nearest player unit inside the guard radius around the post (Guard role).</summary>
        UnitId IntruderNearPost { get; }
        UnitId CloseAttacker { get; }
        /// <summary>The player castle, or None if it is gone.</summary>
        UnitId PlayerCastle { get; }
        bool IsAlive(UnitId unit);
        Vector2 GetPosition(UnitId unit);
    }

    public sealed class EnemyBrainSettings
    {
        public EnemyRole Role { get; set; } = EnemyRole.Guard;
        public Vector2 Post { get; set; }
        /// <summary>Provisional: defenders charge player units inside this radius around their post.</summary>
        public float GuardRadius { get; set; } = 4f;
        /// <summary>A charging defender gives up on a target that is this far beyond the guard radius.</summary>
        public float PursuitMargin { get; set; } = 1.5f;
        public float PostTolerance { get; set; } = 0.15f;
    }

    public sealed class EnemyBrain
    {
        private readonly EnemyBrainSettings _settings;
        private UnitId _target;

        public EnemyBrain(EnemyBrainSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public HeroIntent Tick(IEnemySensor sensor) =>
            _settings.Role == EnemyRole.Guard ? TickGuard(sensor) : TickAssault(sensor);

        // Guard: charge intruders, then return to the post (spec §8, provisional).
        private HeroIntent TickGuard(IEnemySensor sensor)
        {
            var leashFromPost = _settings.GuardRadius + _settings.PursuitMargin;
            if (_target.IsValid && (!sensor.IsAlive(_target)
                                    || (sensor.GetPosition(_target) - _settings.Post).magnitude > leashFromPost))
                _target = UnitId.None;

            if (!_target.IsValid)
                _target = sensor.IntruderNearPost;
            if (!_target.IsValid && sensor.CloseAttacker.IsValid)
                _target = sensor.CloseAttacker;
            if (_target.IsValid)
                return HeroIntent.Engage(_target, mayChase: true);

            if (sensor.OpponentInAttackRange.IsValid)
                return HeroIntent.Engage(sensor.OpponentInAttackRange, mayChase: false);

            return (sensor.Position - _settings.Post).sqrMagnitude > _settings.PostTolerance * _settings.PostTolerance
                ? HeroIntent.MoveTo(_settings.Post)
                : HeroIntent.Hold;
        }

        // Assault: fight whatever is near, otherwise march on the player castle.
        private HeroIntent TickAssault(IEnemySensor sensor)
        {
            if (_target.IsValid && !sensor.IsAlive(_target))
                _target = UnitId.None;
            if (!_target.IsValid)
                _target = sensor.OpponentInAggroRange;
            if (_target.IsValid)
                return HeroIntent.Engage(_target, mayChase: true);
            return sensor.PlayerCastle.IsValid ? HeroIntent.Engage(sensor.PlayerCastle, mayChase: true) : HeroIntent.Hold;
        }
    }
}

using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A castle. Health mirrors the Core <see cref="CastleRuntime"/>; optionally shoots enemies in range.</summary>
    public sealed class CastleUnit : UnitBase
    {
        public const float BodyRadius = 1.4f;

        private CastleRuntime _runtime;
        private bool _canAttack;
        private float _cooldown;

        public override bool IsCastle => true;

        internal void Init(BattleController battle, CastleRuntime runtime, CastleConfig config)
        {
            _runtime = runtime;
            _canAttack = config.CanAttack;
            name = runtime.Team == Team.Player ? "Player Castle" : "Enemy Castle";
            var color = runtime.Team == Team.Player ? new Color(0.35f, 0.6f, 1f) : new Color(0.85f, 0.25f, 0.25f);
            InitUnit(battle, runtime.Id, runtime.Team, runtime.MaxHealth, BodyRadius, PlaceholderArt.Square, color,
                withSelectionRing: false);
            SetCombat(config.Damage, config.AttackRange, config.AttackInterval, 0f, 0f, config.AttackRange);
            SetPosition(config.Position);
        }

        public override void Simulate(float deltaTime)
        {
            if (!_canAttack)
                return;
            _cooldown -= deltaTime;
            if (_cooldown > 0f)
                return;
            var target = Battle.Registry.Get(Battle.Registry.FindNearestHostile(this, AttackRange));
            if (target == null)
                return;
            _cooldown = AttackInterval;
            target.TakeDamage(Damage, this);
        }

        protected override void OnDamaged(float amount, UnitBase source) => _runtime.ApplyDamage(amount);
    }
}

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
        private float _castleAttackCooldown;
        private CastleArtConfig _art;

        public override bool IsCastle => true;

        internal void Init(BattleController battle, CastleRuntime runtime, CastleConfig config)
        {
            _runtime = runtime;
            _canAttack = config.CanAttack;
            name = runtime.Team == Team.Player ? "Player Castle" : "Enemy Castle";
            var color = runtime.Team == Team.Player ? new Color(0.35f, 0.6f, 1f) : new Color(0.85f, 0.25f, 0.25f);
            InitUnit(battle, runtime.Id, runtime.Team, runtime.MaxHealth, BodyRadius, PlaceholderArt.Square, color,
                withSelectionRing: false);
            _art = config.Art;
            if (_art != null)
            {
                Visual.UseArt(_art.IntactSprite);
                // 3D: the command post model stands there instead of its picture.
                if (MapProp3D.TryFind(battle, _art.IntactSprite, out var prop, out var yaw))
                {
                    if (_art.FaceOpponent)
                        yaw = MapProp3D.YawFacing(prop, MapProp3D.TowardOpponent(battle, config.Position));
                    Visual.UsePropModel(prop, yaw, battle.View.PropShadowAlpha);
                }
            }
            SetCombat(config.Damage, config.AttackRange, config.AttackInterval, 0f, 0f, config.AttackRange);
            SetPosition(config.Position);
        }

        public override void Simulate(float deltaTime)
        {
            if (!_canAttack)
                return;
            _castleAttackCooldown -= deltaTime;
            if (_castleAttackCooldown > 0f)
                return;
            var target = Battle.Registry.Get(Battle.Registry.FindNearestHostile(this, AttackRange));
            if (target == null)
                return;
            _castleAttackCooldown = AttackInterval;
            target.TakeDamage(Damage, this);
        }

        protected override void OnDamaged(float amount, UnitBase source) => _runtime.ApplyDamage(amount);

        // The castle object goes away; its fall is played by a separate ruin that stays on the field.
        protected override void OnDied()
        {
            if (_art != null && _art.IntactSprite != null)
                Battle.ShowCastleFall(_art, Position);
        }
    }
}

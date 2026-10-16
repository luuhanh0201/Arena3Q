using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>An enemy soldier: castle defender (Attack stage) or wave attacker (Defense stage).</summary>
    public sealed class EnemyUnit : UnitBase, IEnemySensor
    {
        public const float BodyRadius = 0.32f;

        private EnemyBrain _brain;
        private Vector2 _post;
        private float _guardRadius;

        public EnemyRole Role { get; private set; }

        /// <summary>Defense-stage wave this enemy belongs to, or -1 for defenders.</summary>
        public int WaveIndex { get; private set; } = -1;

        internal void Init(BattleController battle, UnitId id, TroopDefinition definition, EnemyRole role, Vector2 position,
            float guardRadius, int waveIndex)
        {
            Role = role;
            WaveIndex = waveIndex;
            _post = position;
            _guardRadius = guardRadius;
            _brain = new EnemyBrain(new EnemyBrainSettings { Role = role, Post = position, GuardRadius = guardRadius });
            name = $"Enemy {definition.DisplayName} ({id})";
            InitUnit(battle, id, Team.Enemy, definition.Stats.MaxHealth, BodyRadius, definition.Sprite,
                definition.PlaceholderColor, withSelectionRing: false);
            SetCombat(definition.Stats);
            SetPosition(position);
        }

        public override void Simulate(float deltaTime)
        {
            var intent = _brain.Tick(this);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
        }

        // ---- IEnemySensor ----

        UnitId IEnemySensor.OpponentInAttackRange => Battle.Registry.FindNearestHostile(this, AttackRange);
        UnitId IEnemySensor.OpponentInAggroRange => Battle.Registry.FindNearestHostile(this, AggroRange);

        UnitId IEnemySensor.IntruderNearPost
        {
            get
            {
                var intruder = Battle.Registry.FindNearest(_post, 0f, _guardRadius, u => u.Team == Team.Player && !u.IsCastle);
                return intruder != null ? intruder.Id : UnitId.None;
            }
        }

        UnitId IEnemySensor.CloseAttacker => FindCloseAttacker();
        UnitId IEnemySensor.PlayerCastle => Battle.PlayerCastleId;
        bool IEnemySensor.IsAlive(UnitId unit) => Battle.Registry.IsAlive(unit);
        Vector2 IEnemySensor.GetPosition(UnitId unit) => Battle.Registry.GetPosition(unit);
    }
}

using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A squad soldier: always follows its own hero's orders through <see cref="TroopBrain"/>.</summary>
    public sealed class TroopUnit : UnitBase, ITroopSensor
    {
        public const float BodyRadius = 0.3f;

        private TroopBrain _brain;
        private TroopBrainSettings _settings;

        public Squad Squad { get; private set; }

        /// <summary>Substitute target picked because the hero's ordered target was unreachable, or None.</summary>
        public UnitId FallbackTarget => _brain.FallbackTarget;

        /// <summary>Seconds without progress towards the hero's ordered target (diagnostics).</summary>
        public float StuckSeconds => _brain.StuckSeconds;
        public int SlotIndex { get; private set; }
        public TroopDefinition Definition { get; private set; }

        internal void Init(BattleController battle, UnitId id, TroopDefinition definition, CombatNumbers numbers, Squad squad,
            int slotIndex, TroopBrainSettings settings, Color tint)
        {
            Definition = definition;
            Squad = squad;
            SlotIndex = slotIndex;
            _settings = settings;
            _brain = new TroopBrain(settings);
            name = $"Troop {definition.DisplayName} ({id}) of {squad.HeroId}";
            InitUnit(battle, id, Team.Player, numbers.MaxHealth, BodyRadius, definition.Sprite, tint,
                withSelectionRing: false);
            SetCombat(numbers);
            SetPosition(squad.SlotPosition(slotIndex));
            squad.Add(this);
        }

        public override void Simulate(float deltaTime)
        {
            var intent = _brain.Tick(Squad.Mode, this, deltaTime);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
        }

        protected override void OnDied() => Squad.Remove(this);

        // ---- ITroopSensor ----

        Vector2 ITroopSensor.SlotPosition => Squad.SlotPosition(SlotIndex);
        Vector2 ITroopSensor.HeroPosition => Squad.LastHeroPosition;
        UnitId ITroopSensor.HeroTarget => Squad.HeroTarget;
        UnitId ITroopSensor.EnemyInAttackRange => Battle.Registry.FindNearestHostilePreferUnits(this, AttackRange);
        UnitId ITroopSensor.EnemyInAggroRange => Battle.Registry.FindNearestHostile(this, AggroRange);
        UnitId ITroopSensor.NearestEnemy => Battle.Registry.FindNearestHostile(this, float.PositiveInfinity);
        UnitId ITroopSensor.EnemyCastle => Battle.EnemyCastleId;
        UnitId ITroopSensor.CloseAttacker => FindCloseAttacker();
        Vector2 ITroopSensor.RetreatPoint => Battle.PlayerCastleRetreatPoint;

        bool ITroopSensor.HeroTargetInAttackRange
        {
            get
            {
                var target = Battle.Registry.Get(Squad.HeroTarget);
                return target != null && IsInAttackRange(target);
            }
        }

        float ITroopSensor.DistanceToHeroTarget
        {
            get
            {
                var target = Battle.Registry.Get(Squad.HeroTarget);
                return target != null ? EdgeDistance(target) : float.PositiveInfinity;
            }
        }

        UnitId ITroopSensor.EnemyNearestToHeroTarget
        {
            get
            {
                var target = Battle.Registry.Get(Squad.HeroTarget);
                if (target == null)
                    return UnitId.None;
                var team = Team;
                var substitute = Battle.Registry.FindNearest(target.Position, target.Radius, _settings.AssistRadius,
                    u => u.Team != team && !u.IsCastle && u != target);
                return substitute != null ? substitute.Id : UnitId.None;
            }
        }

        bool ITroopSensor.IsAlive(UnitId unit) => Battle.Registry.IsAlive(unit);
        Vector2 ITroopSensor.GetPosition(UnitId unit) => Battle.Registry.GetPosition(unit);
    }
}

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

        public Squad Squad { get; private set; }
        public int SlotIndex { get; private set; }
        public TroopDefinition Definition { get; private set; }

        internal void Init(BattleController battle, UnitId id, TroopDefinition definition, Squad squad, int slotIndex,
            TroopBrainSettings settings, Color tint)
        {
            Definition = definition;
            Squad = squad;
            SlotIndex = slotIndex;
            _brain = new TroopBrain(settings);
            name = $"Troop {definition.DisplayName} ({id}) of {squad.HeroId}";
            InitUnit(battle, id, Team.Player, definition.Stats.MaxHealth, BodyRadius, definition.Sprite, tint,
                withSelectionRing: false);
            SetCombat(definition.Stats);
            SetPosition(squad.SlotPosition(slotIndex));
            squad.Add(this);
        }

        public override void Simulate(float deltaTime)
        {
            var intent = _brain.Tick(Squad.Mode, this);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
        }

        protected override void OnDied() => Squad.Remove(this);

        // ---- ITroopSensor ----

        Vector2 ITroopSensor.SlotPosition => Squad.SlotPosition(SlotIndex);
        Vector2 ITroopSensor.HeroPosition => Squad.LastHeroPosition;
        UnitId ITroopSensor.HeroTarget => Squad.HeroTarget;
        UnitId ITroopSensor.EnemyInAttackRange => Battle.Registry.FindNearestHostile(this, AttackRange);
        UnitId ITroopSensor.EnemyInAggroRange => Battle.Registry.FindNearestHostile(this, AggroRange);
        UnitId ITroopSensor.NearestEnemy => Battle.Registry.FindNearestHostile(this, float.PositiveInfinity);
        UnitId ITroopSensor.EnemyCastle => Battle.EnemyCastleId;
        UnitId ITroopSensor.CloseAttacker => FindCloseAttacker();
        Vector2 ITroopSensor.RetreatPoint => Battle.PlayerCastleRetreatPoint;
        bool ITroopSensor.IsAlive(UnitId unit) => Battle.Registry.IsAlive(unit);
        Vector2 ITroopSensor.GetPosition(UnitId unit) => Battle.Registry.GetPosition(unit);
    }
}

using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A hero on the field: feeds <see cref="HeroBrain"/> from the live battle and executes its intent.</summary>
    public sealed class HeroUnit : UnitBase, IHeroSensor
    {
        public const float BodyRadius = 0.45f;

        public HeroDefinition Definition { get; private set; }
        public HeroBrain Brain { get; private set; }
        public HeroRuntime Runtime { get; private set; }
        public Squad Squad { get; internal set; }
        public int CardIndex => Runtime.CardIndex;
        public HeroState State => Brain.State;

        internal void Init(BattleController battle, HeroRuntime runtime, HeroDefinition definition,
            HeroBrainSettings settings, Vector2 position)
        {
            Definition = definition;
            Runtime = runtime;
            Brain = new HeroBrain(settings);
            name = $"Hero {definition.DisplayName} ({runtime.Id})";
            InitUnit(battle, runtime.Id, Team.Player, definition.Stats.MaxHealth, BodyRadius, definition.Sprite,
                definition.PlaceholderColor, withSelectionRing: true);
            SetCombat(definition.Stats);
            SetPosition(position);
        }

        public override void Simulate(float deltaTime)
        {
            var intent = Brain.Tick(deltaTime, this);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
            Runtime.SyncFrom(Brain);
        }

        public void SetSelected(bool selected) => Visual.SetSelected(selected);

        protected override void OnDamaged(float amount, UnitBase source) => Runtime.Health = Health;

        protected override void OnDied()
        {
            Brain.Kill();
            Battle.NotifyHeroDied(this);
        }

        // ---- IHeroSensor ----

        UnitId IHeroSensor.EnemyInAttackRange => Battle.Registry.FindNearestHostilePreferUnits(this, AttackRange);
        UnitId IHeroSensor.EnemyInAggroRange => Battle.Registry.FindNearestHostile(this, AggroRange);
        UnitId IHeroSensor.NearestEnemy => Battle.Registry.FindNearestHostile(this, float.PositiveInfinity);
        UnitId IHeroSensor.EnemyCastle => Battle.EnemyCastleId;
        UnitId IHeroSensor.CloseAttacker => FindCloseAttacker();
        bool IHeroSensor.IsAlive(UnitId unit) => Battle.Registry.IsAlive(unit);
    }
}

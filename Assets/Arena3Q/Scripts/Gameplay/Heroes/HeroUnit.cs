using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A hero on the field: feeds <see cref="HeroBrain"/> from the live battle, executes its intent and its skills.</summary>
    public sealed class HeroUnit : UnitBase, IHeroSensor
    {
        public const float BodyRadius = 0.45f;

        public HeroDefinition Definition { get; private set; }
        public HeroBrain Brain { get; private set; }
        public HeroRuntime Runtime { get; private set; }
        public Squad Squad { get; internal set; }
        public int CardIndex => Runtime.CardIndex;
        public HeroState State => Brain.State;

        /// <summary>Level and stars of the card this hero was summoned from.</summary>
        public int Level { get; private set; } = 1;
        public int Stars { get; private set; } = 1;

        /// <summary>Combat numbers after level, stars, breakthroughs and faction synergy.</summary>
        public CombatNumbers Numbers { get; private set; }

        /// <summary>Passive, skills 2 and 3, ultimate and rage (spec §3).</summary>
        public HeroSkills Skills { get; private set; }
        private HeroAnimationView _animation;
        public MacAnhCombatPerformance Performance { get; private set; }

        internal void Init(BattleController battle, HeroRuntime runtime, LineupEntry card, CombatNumbers numbers,
            HeroBrainSettings settings, Vector2 position)
        {
            var definition = card.Hero;
            Definition = definition;
            Level = card.Level;
            Stars = card.Stars;
            Runtime = runtime;
            Brain = new HeroBrain(settings);
            name = $"Hero {definition.DisplayName} Lv{Level} ({runtime.Id})";
            InitUnit(battle, runtime.Id, Team.Player, numbers.MaxHealth, BodyRadius, definition.Sprite,
                definition.PlaceholderColor, withSelectionRing: true);
            SetPosition(position);
            if (definition.Animations != null && definition.MacAnhPerformance == null)
            {
                Visual.UseArt(definition.Sprite);
                _animation = Visual.BodyRenderer.gameObject.AddComponent<HeroAnimationView>();
                _animation.Init(this, Visual.BodyRenderer);
            }
            Skills = new HeroSkills(this, card, battle.Rules);
            if (definition.MacAnhPerformance != null)
            {
                Visual.UseArt(definition.Sprite);
                Performance = new MacAnhCombatPerformance(this, Visual.BodyRenderer);
            }
            // 3D: heroes are drawn larger than the troops around them (the picture and what hangs on it).
            if (battle.Presentation3D)
                Visual.SetBodySize(battle.View.HeroModelScale);

            // Passive range bonus (e.g. Ưng Nhãn +15 %): the reach the hero fights and aims with.
            var rangeBonus = Skills.AttackRangeBonus;
            if (rangeBonus > 0f)
            {
                numbers.AttackRange *= 1f + rangeBonus;
                numbers.AggroRange = Mathf.Max(numbers.AggroRange, numbers.AttackRange);
            }
            Numbers = numbers;
            SetCombat(numbers);
        }

        // Seconds stunned without a break (anti-lock, spec §8 🟡).
        private float _stunnedSeconds;

        public override void Simulate(float deltaTime)
        {
            ResetLocomotion();
            Skills.Tick(deltaTime);
            if (Skills.FrenzyMaxStacks > 0)
                Visual.SetStacks(Skills.FrenzyStacks, Skills.FrenzyMaxStacks);
            Performance?.Tick(deltaTime);
            var settings = Battle.Rules.Settings;
            _stunnedSeconds = EnemyRules.TrackCrowdControl(_stunnedSeconds, IsStunned, deltaTime,
                settings.CrowdControlLockSeconds, out var breakFree);
            if (breakFree)
                ApplyStatus(StatusKind.CrowdControlImmunity, 1f, settings.CrowdControlBreakSeconds);
            // Stunned or winding up a skill: the hero stands still but keeps its order (spec §3, §8).
            if (IsStunned || Skills.IsCasting || (Performance != null && Performance.IsAttacking))
            {
                if (Performance != null && Performance.IsAttacking) Execute(HeroIntent.Hold, deltaTime);
                ConsumeDamageFlags();
                return;
            }
            var intent = Brain.Tick(deltaTime, this);
            // Taunt (spec §8 ✅): only while the hero is not carrying out a player order (Moving / Attacking ignore it).
            var taunter = TauntSource;
            if (taunter != null && EnemyRules.HeroFollowsTaunt(Brain.State))
                intent = HeroIntent.Engage(taunter.Id, mayChase: true);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
            Runtime.SyncFrom(Brain);
        }

        public void SetSelected(bool selected) => Visual.SetSelected(selected);

        /// <summary>
        /// How much larger than its own size the hero is drawn (BattleViewConfig.HeroModelScale): its steps on screen are
        /// that much longer, so the walk and run clips must play that much slower to keep the feet planted.
        /// </summary>
        public float DrawnSize => Visual.BodySize;

        public override float CurrentAttackInterval =>
            base.CurrentAttackInterval / Skills.AttackSpeedMultiplier(Health / MaxHealth);

        protected override float ExtraDamageReduction => Skills.PassiveDamageReduction(Health / MaxHealth);

        protected override float ComputeAttackDamage(UnitBase target) =>
            Skills.ModifyBasicAttack(target, base.ComputeAttackDamage(target));

        protected override void BeginBasicAttack(UnitBase target)
        {
            if (Performance != null) Performance.BeginAttack(target);
            else base.BeginBasicAttack(target);
        }

        internal System.Action<UnitBase> CreateMacAnhBasicHit(UnitBase target)
        {
            var damage = ComputeAttackDamage(target);
            return hit =>
            {
                var source = this != null && IsAlive ? this : null;
                hit.TakeDamage(damage, source);
                if (source != null) OnAttackLanded(hit, damage);
            };
        }

        protected override void OnAttackLanded(UnitBase target, float damage)
        {
            Skills.OnBasicAttackLanded(target, damage);
            _animation?.Attack(target);
        }

        protected override void OnDealtDamage(UnitBase target, float amount) => Skills.OnDealtDamage(amount);

        protected override void OnDamaged(float amount, UnitBase source) => Runtime.Health = Health;

        protected override void OnHealthChanged() => Runtime.Health = Health;

        protected override void OnDied()
        {
            _animation?.Die();
            Performance?.Die();
            Skills.OnDeath();
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

        UnitId IHeroSensor.NearestEnemyIn(Rect area)
        {
            var team = Team;
            var found = Battle.Registry.FindNearest(Position, Radius, float.PositiveInfinity,
                u => u.Team != team && !u.IsCastle && area.Contains(u.Position));
            return found != null ? found.Id : UnitId.None;
        }

        bool IHeroSensor.IsInArea(UnitId unit, Rect area)
        {
            var found = Battle.Registry.Get(unit);
            return found != null && area.Contains(found.Position);
        }
    }
}

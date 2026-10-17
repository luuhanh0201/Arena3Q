using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// An enemy soldier: castle defender (Attack stage) or wave attacker (Defense stage). Elites and bosses
    /// (<see cref="EnemyDefinition"/>, spec §8) also use skills, bosses change phase once below half health, and traits
    /// apply (flying, standing up once). Summoned soldiers and boss copies are enemies like any other.
    /// </summary>
    public sealed class EnemyUnit : UnitBase, IEnemySensor
    {
        public const float BodyRadius = 0.32f;

        private EnemyBrain _brain;
        private Vector2 _post;
        private float _guardRadius;
        private bool _revived;
        private float _moveSpeedFactor = 1f;
        private float _attackSpeedFactor = 1f;

        public EnemyRole Role { get; private set; }
        public TroopDefinition Definition { get; private set; }
        /// <summary>The rank and skills, or null for a plain troop (a normal enemy).</summary>
        public EnemyDefinition Enemy { get; private set; }
        public EnemyTier Tier => Enemy != null && !IsClone ? Enemy.Tier : EnemyTier.Normal;
        public bool IsBoss => Tier == EnemyTier.Boss;
        /// <summary>A copy made by a boss skill (Huyễn Ảnh): no skills, no phase, no boss bar.</summary>
        public bool IsClone { get; private set; }
        public bool InPhaseTwo { get; private set; }
        public EnemySkills Skills { get; private set; }
        public float GuardRadius => _guardRadius;
        public string DisplayName => Definition != null ? Definition.DisplayName : name;

        /// <summary>Defense-stage wave this enemy belongs to, or -1 for defenders.</summary>
        public int WaveIndex { get; private set; } = -1;

        public override bool IsFlying => Enemy != null && Enemy.Flying;
        protected override float MoveSpeedFactor => _moveSpeedFactor;
        public override float CurrentAttackInterval => base.CurrentAttackInterval / _attackSpeedFactor;

        internal void Init(BattleController battle, UnitId id, TroopDefinition definition, CombatNumbers numbers,
            EnemyRole role, Vector2 position, float guardRadius, int waveIndex, bool isClone = false)
        {
            Definition = definition;
            Enemy = definition as EnemyDefinition;
            IsClone = isClone;
            Role = role;
            WaveIndex = waveIndex;
            _post = position;
            _guardRadius = guardRadius;
            _brain = new EnemyBrain(new EnemyBrainSettings { Role = role, Post = position, GuardRadius = guardRadius });
            var tierName = Tier == EnemyTier.Normal ? "" : $"{Tier} ";
            name = $"Enemy {tierName}{definition.DisplayName}{(isClone ? " (copy)" : "")} ({id})";
            var radius = Enemy != null ? Enemy.BodyRadius : BodyRadius;
            InitUnit(battle, id, Team.Enemy, numbers.MaxHealth, radius, definition.Sprite, definition.PlaceholderColor,
                withSelectionRing: false);
            // Elites wear a golden outline, bosses a larger red one (spec §8); copies look like their boss.
            if (Tier != EnemyTier.Normal || isClone)
                Visual.SetRankOutline(Tier == EnemyTier.Boss || isClone
                    ? new Color(1f, 0.35f, 0.25f, isClone ? 0.45f : 0.95f)
                    : new Color(1f, 0.85f, 0.3f, 0.95f));
            SetCombat(numbers);
            SetPosition(position);
            Skills = new EnemySkills(this, isClone || Enemy == null ? null : Enemy.Skills);
        }

        public override void Simulate(float deltaTime)
        {
            Skills.Tick(deltaTime);
            // Stunned, warning or channelling a skill: no moving, no attacking (spec §8 ✅).
            if (IsStunned || Skills.IsBusy)
            {
                ConsumeDamageFlags();
                return;
            }
            var intent = _brain.Tick(this);
            ConsumeDamageFlags();
            Execute(intent, deltaTime);
        }

        protected override void OnDamaged(float amount, UnitBase source) => CheckPhaseTwo();

        protected override void OnHealthChanged() => CheckPhaseTwo();

        // Spec §8 ✅: once, the first time health drops below the threshold (🟡 50 %).
        private void CheckPhaseTwo()
        {
            var phase = IsClone || Enemy == null ? null : Enemy.Phase;
            if (phase == null || !EnemyRules.ShouldEnterPhaseTwo(InPhaseTwo, Health, MaxHealth, phase.healthThreshold))
                return;
            InPhaseTwo = true;
            _attackSpeedFactor *= 1f + phase.attackSpeedBonus;
            _moveSpeedFactor *= 1f + phase.moveSpeedBonus;
            if (phase.damageBonus != 0f)
                SetCombat(Damage * (1f + phase.damageBonus), AttackRange, AttackInterval, MoveSpeed, ReactionRange,
                    AggroRange, AttackerMemorySeconds);
            if (phase.skills != null && phase.skills.Length > 0)
                Skills.SetSkills(phase.skills);
            Visual.SetTint(Enemy.PhaseTwoColor);
            if (phase.onEnter != null)
            {
                foreach (var skill in phase.onEnter)
                    Skills.UseNow(skill);
            }
            Battle.NotifyBossPhase(this);
        }

        // Trait (spec §8 🟡): a chance to stand up once with part of its health (Cương Thi).
        protected override bool TryCheatDeath()
        {
            if (_revived || IsClone || Enemy == null || Enemy.ReviveChance <= 0f)
                return false;
            _revived = true;
            if (Battle.Random01() >= Enemy.ReviveChance)
                return false;
            Revive(Enemy.ReviveHealth);
            Skills.Cancel();
            return true;
        }

        /// <summary>Whether this enemy already used its one chance to stand up (tests).</summary>
        public bool HasRevived => _revived && IsAlive;

        protected override void OnDied() => Skills.Cancel();

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

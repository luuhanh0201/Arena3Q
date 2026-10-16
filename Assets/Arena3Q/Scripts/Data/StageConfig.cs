using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Everything that defines one stage (spec §10). Turned into Core objects at battle start.</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Stage Config", fileName = "Stage")]
    public sealed class StageConfig : ScriptableObject
    {
        public const float DefaultTimeLimit = 180f;

        [Header("Identity")]
        [SerializeField] private string stageId = "stage";
        [SerializeField] private StageType stageType = StageType.Attack;

        [Header("Map (spec §9)")]
        [SerializeField] private Rect mapBounds = new Rect(-20f, -12f, 40f, 24f);
        [SerializeField] private Rect[] forbiddenZones = Array.Empty<Rect>();

        [Header("Castles")]
        [Tooltip("Provisional: in Defense stages the player castle shoots by default.")]
        [SerializeField] private CastleConfig playerCastle = new CastleConfig(new Vector2(-16f, 0f), canAttack: true);
        [SerializeField] private CastleConfig enemyCastle = new CastleConfig(new Vector2(16f, 0f), canAttack: false);

        [Header("Summon (spec §4)")]
        [Tooltip("Free-summon drop area in front of the player castle.")]
        [SerializeField] private Rect summonZone = new Rect(-14f, -8f, 8f, 16f);
        [SerializeField] private Vector2[] quickSummonPoints = { new Vector2(-12f, 0f) };
        [Tooltip("Provisional: allow summoning a dead hero's card again.")]
        [SerializeField] private bool allowResummonDeadHero;

        [Header("Enemies")]
        [Tooltip("Attack stages: defenders around the enemy castle.")]
        [SerializeField] private DefenderPlacement[] defenders = Array.Empty<DefenderPlacement>();
        [Tooltip("Defense stages: attacking waves.")]
        [SerializeField] private WaveConfig[] waves = Array.Empty<WaveConfig>();

        [Header("Rules (spec §8)")]
        [Tooltip("Seconds. Attack: lose when it runs out. Defense: win when it runs out (0 = end on waves only).")]
        [SerializeField, Min(0f)] private float timeLimit = DefaultTimeLimit;
        [SerializeField] private LossRules lossRules = new LossRules();
        [SerializeField] private StarThresholds starThresholds = new StarThresholds();
        [Tooltip("Provisional: what a squad does after its hero dies.")]
        [SerializeField] private OnHeroDeathTroopBehavior onHeroDeathTroopBehavior = OnHeroDeathTroopBehavior.KeepFighting;

        [Header("Hero overrides")]
        [Tooltip("Provisional: override every hero's idleBeforeAdvanceSeconds in this stage.")]
        [SerializeField] private bool overrideIdleBeforeAdvance;
        [SerializeField, Min(0f)] private float idleBeforeAdvanceSeconds = HeroDefinition.DefaultIdleBeforeAdvanceSeconds;

        [Header("Prototype")]
        [SerializeField] private TestLoadout testLoadout;

        public string StageId => stageId;
        public StageType StageType => stageType;
        public Rect MapBounds => mapBounds;
        public IReadOnlyList<Rect> ForbiddenZones => forbiddenZones;
        public CastleConfig PlayerCastle => playerCastle;
        public CastleConfig EnemyCastle => enemyCastle;
        public Rect SummonZone => summonZone;
        public IReadOnlyList<Vector2> QuickSummonPoints => quickSummonPoints;
        public bool AllowResummonDeadHero => allowResummonDeadHero;
        public IReadOnlyList<DefenderPlacement> Defenders => defenders;
        public IReadOnlyList<WaveConfig> Waves => waves;
        public float TimeLimit => timeLimit;
        public LossRules LossRules => lossRules;
        public StarThresholds StarThresholds => starThresholds;
        public OnHeroDeathTroopBehavior OnHeroDeathTroopBehavior => onHeroDeathTroopBehavior;
        public TestLoadout TestLoadout => testLoadout;

        public float GetIdleBeforeAdvanceSeconds(HeroDefinition hero) =>
            overrideIdleBeforeAdvance ? idleBeforeAdvanceSeconds : hero.IdleBeforeAdvanceSeconds;

        public HeroBrainSettings CreateHeroBrainSettings(HeroDefinition hero) => new HeroBrainSettings
        {
            StageType = stageType,
            IdleBeforeAdvanceSeconds = GetIdleBeforeAdvanceSeconds(hero),
            IdleTimerResetsOnOutOfRangeDamage = hero.IdleTimerResetsOnOutOfRangeDamage,
            SelfDefendWhileAttacking = hero.SelfDefendWhileAttacking
        };

        public BattlefieldLayout CreateLayout() => new BattlefieldLayout(mapBounds, summonZone, forbiddenZones);

        public SummonRules CreateSummonRules() => new SummonRules(CreateLayout(), quickSummonPoints, playerCastle.Position);

        /// <summary>Builds the Core battle settings. Card order follows <see cref="Data.TestLoadout.GetCards"/>.</summary>
        public BattleSetup CreateBattleSetup()
        {
            if (testLoadout == null)
                throw new InvalidOperationException($"Stage '{name}' has no test loadout.");

            var cards = testLoadout.GetCards();
            var heroIds = new string[cards.Count];
            for (var i = 0; i < cards.Count; i++)
                heroIds[i] = cards[i].HeroId;

            return new BattleSetup
            {
                StageType = stageType,
                TimeLimit = timeLimit,
                WaveCount = waves?.Length ?? 0,
                CardHeroIds = heroIds,
                AllowResummonDeadHero = allowResummonDeadHero,
                PlayerCastleMaxHealth = playerCastle.MaxHealth,
                EnemyCastleMaxHealth = enemyCastle.MaxHealth,
                LossRules = lossRules,
                StarThresholds = starThresholds
            };
        }

        /// <summary>Collects configuration errors. An empty list means the stage is playable.</summary>
        public void Validate(List<string> errors)
        {
            var owner = $"Stage '{name}'";
            var layout = CreateLayout();

            if (string.IsNullOrWhiteSpace(stageId))
                errors.Add($"{owner}: stageId is empty.");
            if (mapBounds.width <= 0f || mapBounds.height <= 0f)
                errors.Add($"{owner}: mapBounds must have a positive size.");

            if (summonZone.width <= 0f || summonZone.height <= 0f)
                errors.Add($"{owner}: summonZone must have a positive size.");
            else if (!ContainsRect(mapBounds, summonZone))
                errors.Add($"{owner}: summonZone must lie inside mapBounds.");

            if (quickSummonPoints == null || quickSummonPoints.Length == 0)
                errors.Add($"{owner}: needs at least one quick summon point.");
            else
            {
                for (var i = 0; i < quickSummonPoints.Length; i++)
                {
                    if (!layout.IsWalkable(quickSummonPoints[i]))
                        errors.Add($"{owner}: quickSummonPoints[{i}] is outside the map or in a forbidden zone.");
                }
            }

            ValidateCastle(owner, "playerCastle", playerCastle, errors);
            ValidateCastle(owner, "enemyCastle", enemyCastle, errors);

            if (stageType == StageType.Attack)
            {
                if (timeLimit <= 0f)
                    errors.Add($"{owner}: Attack stages need a timeLimit > 0.");
                for (var i = 0; i < (defenders?.Length ?? 0); i++)
                {
                    if (defenders[i]?.Unit == null)
                        errors.Add($"{owner}: defenders[{i}] has no unit.");
                    else if (!mapBounds.Contains(defenders[i].Position))
                        errors.Add($"{owner}: defenders[{i}] is outside mapBounds.");
                }
            }
            else
            {
                if (timeLimit <= 0f && (waves == null || waves.Length == 0))
                    errors.Add($"{owner}: Defense stages need a timeLimit > 0 or at least one wave.");
                ValidateWaves(owner, errors);
            }

            if (starThresholds == null)
                errors.Add($"{owner}: starThresholds are missing.");
            else if (starThresholds.maxHeroesLostForTwoStars < starThresholds.maxHeroesLostForThreeStars)
                errors.Add($"{owner}: the 2-star threshold must be >= the 3-star threshold.");
            if (lossRules == null)
                errors.Add($"{owner}: lossRules are missing.");

            if (testLoadout == null)
                errors.Add($"{owner}: testLoadout is not set.");
            else
                testLoadout.Validate(errors);
        }

        private void ValidateCastle(string owner, string label, CastleConfig castle, List<string> errors)
        {
            if (castle == null)
            {
                errors.Add($"{owner}: {label} is missing.");
                return;
            }
            if (castle.MaxHealth <= 0f)
                errors.Add($"{owner}: {label}.maxHealth must be > 0.");
            if (!mapBounds.Contains(castle.Position))
                errors.Add($"{owner}: {label} is outside mapBounds.");
        }

        private void ValidateWaves(string owner, List<string> errors)
        {
            if (waves == null)
                return;
            for (var w = 0; w < waves.Length; w++)
            {
                var spawns = waves[w]?.Spawns;
                if (spawns == null || spawns.Length == 0)
                {
                    errors.Add($"{owner}: waves[{w}] has no spawns.");
                    continue;
                }
                for (var s = 0; s < spawns.Length; s++)
                {
                    if (spawns[s]?.Unit == null)
                        errors.Add($"{owner}: waves[{w}].spawns[{s}] has no unit.");
                    else if (!mapBounds.Contains(spawns[s].SpawnPoint))
                        errors.Add($"{owner}: waves[{w}].spawns[{s}] spawns outside mapBounds.");
                }
            }
        }

        private static bool ContainsRect(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
    }
}

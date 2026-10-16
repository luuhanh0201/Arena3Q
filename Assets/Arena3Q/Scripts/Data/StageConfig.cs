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
        [Tooltip("Rectangular forbidden zones.")]
        [SerializeField] private Rect[] forbiddenZones = Array.Empty<Rect>();
        [Tooltip("River forbidden zones: crossable only at their crossings (bridges/fords).")]
        [SerializeField] private RiverConfig[] rivers = Array.Empty<RiverConfig>();
        [Tooltip("Provisional: one painted image covering the whole map (concept art used as a temporary background). " +
                 "Final art uses Tilemap + SpriteShape (map spec §8.4). Leave empty for placeholder visuals.")]
        [SerializeField] private Sprite backgroundArt;
        [Tooltip("Vertical scale of the 3/4 view the background art is painted in (map spec §8.3). The camera tilt is " +
                 "set to match it when the stage starts.")]
        [SerializeField, Range(0.5f, 1f)] private float backgroundArtVerticalScale = 0.75f;

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
        public IReadOnlyList<RiverConfig> Rivers => rivers;
        public Sprite BackgroundArt => backgroundArt;
        public float BackgroundArtVerticalScale => backgroundArtVerticalScale;

        /// <summary>The view tilt (degrees) that makes the flat logic map line up with the painted background.</summary>
        public float BackgroundArtTiltDegrees => Mathf.Acos(Mathf.Clamp01(backgroundArtVerticalScale)) * Mathf.Rad2Deg;
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

        public BattlefieldLayout CreateLayout() =>
            new BattlefieldLayout(mapBounds, summonZone, forbiddenZones, CreateRiverObstacles());

        /// <summary>Core shapes of the valid rivers (invalid ones are skipped here and reported by Validate).</summary>
        public List<IObstacle> CreateRiverObstacles()
        {
            var result = new List<IObstacle>();
            if (rivers == null)
                return result;
            foreach (var river in rivers)
            {
                if (river?.Path == null || river.Path.Length < 2 || river.Width <= 0f)
                    continue;
                var crossings = new List<(Vector2, float)>();
                if (river.Crossings != null)
                {
                    foreach (var crossing in river.Crossings)
                    {
                        if (crossing != null)
                            crossings.Add((crossing.Position, crossing.Width));
                    }
                }
                result.Add(new RiverObstacle(river.Path, river.Width, crossings, river.Name));
            }
            return result;
        }

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

            ValidateRivers(owner, errors);
            ValidateReachability(owner, layout, errors);

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

        private void ValidateRivers(string owner, List<string> errors)
        {
            if (rivers == null)
                return;
            for (var r = 0; r < rivers.Length; r++)
            {
                var river = rivers[r];
                var label = $"{owner}: rivers[{r}]";
                if (river?.Path == null || river.Path.Length < 2)
                {
                    errors.Add($"{label} needs at least two centre-line points.");
                    continue;
                }
                if (river.Width < 1f)
                    errors.Add($"{label}.width must be >= 1.");
                if (river.Crossings == null)
                    continue;
                var shape = new RiverObstacle(river.Path, Mathf.Max(0.5f, river.Width), Array.Empty<(Vector2, float)>());
                for (var c = 0; c < river.Crossings.Length; c++)
                {
                    var crossing = river.Crossings[c];
                    if (crossing == null)
                        continue;
                    // A hero (diameter 0.9) plus some room must fit through.
                    if (crossing.Width < 2f)
                        errors.Add($"{label}.crossings[{c}].width must be >= 2.");
                    if (!shape.Contains(crossing.Position, 0f))
                        errors.Add($"{label}.crossings[{c}] is not on the river.");
                }
            }
        }

        // Forbidden zones must not cut the player off from the fight (spec §9: the map stays an open field).
        private void ValidateReachability(string owner, BattlefieldLayout layout, List<string> errors)
        {
            const float heroRadius = BattlefieldLayout.DefaultMaxUnitRadius;
            if (playerCastle != null && layout.IsInForbiddenZone(playerCastle.Position))
                errors.Add($"{owner}: playerCastle is inside a forbidden zone.");
            if (enemyCastle != null && layout.IsInForbiddenZone(enemyCastle.Position))
                errors.Add($"{owner}: enemyCastle is inside a forbidden zone.");
            if (quickSummonPoints == null || playerCastle == null || enemyCastle == null)
                return;

            if (stageType == StageType.Attack)
            {
                for (var i = 0; i < quickSummonPoints.Length; i++)
                {
                    if (layout.IsWalkable(quickSummonPoints[i])
                        && !layout.HasPath(quickSummonPoints[i], enemyCastle.Position, heroRadius))
                        errors.Add($"{owner}: no walking route from quickSummonPoints[{i}] to the enemy castle.");
                }
            }
            else if (waves != null)
            {
                for (var w = 0; w < waves.Length; w++)
                {
                    var spawns = waves[w]?.Spawns;
                    if (spawns == null)
                        continue;
                    for (var s = 0; s < spawns.Length; s++)
                    {
                        if (spawns[s] != null && !layout.HasPath(spawns[s].SpawnPoint, playerCastle.Position, heroRadius))
                            errors.Add($"{owner}: no walking route from waves[{w}].spawns[{s}] to the player castle.");
                    }
                }
            }
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

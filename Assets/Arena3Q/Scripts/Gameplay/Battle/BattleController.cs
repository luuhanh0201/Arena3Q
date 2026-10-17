using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Composition root of one battle. Owns the Core state and rules, spawns units, runs the simulation and is the
    /// single entry point for player actions (summon, select, move, attack) coming from input and UI.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        private const float HeroPickRadius = 0.6f;
        private const float EnemyPickRadius = 0.5f;
        private const float QuickPointOccupiedRadius = 0.9f;

        [Tooltip("Stages this scene can play. The result screen can switch between them.")]
        [SerializeField] private StageConfig[] stages = Array.Empty<StageConfig>();
        [Tooltip("Material for generated sprites (URP 2D Sprite-Unlit-Default).")]
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private bool startOnAwake = true;
        [Tooltip("Off for tests that step the battle with Simulate().")]
        [SerializeField] private bool autoSimulate = true;
        [Tooltip("2.5D view tilt: 0 = straight top-down, 45 = Clash of Clans-like. Only the display changes; the battle " +
                 "runs on the flat logic plane. Also adjustable in game from the HUD slider.")]
        [SerializeField, Range(0f, MaxViewTiltDegrees)] private float viewTiltDegrees;
        [Tooltip("Levels, stars, breakthroughs, crits and damage reduction (docs/KeHoach/HeroProgression.md). " +
                 "Empty = the design defaults.")]
        [SerializeField] private ProgressionConfig progression;
        [Tooltip("Seed of the battle's random numbers (crits): the same seed replays the same battle.")]
        [SerializeField] private int randomSeed = 1;

        public const float MaxViewTiltDegrees = 45f;

        private readonly List<UnitBase> _simulationScratch = new List<UnitBase>();
        private IReadOnlyList<LineupEntry> _lineupOverride;
        private System.Random _random = new System.Random(1);
        private readonly List<Squad> _squads = new List<Squad>();
        private readonly Dictionary<UnitId, HeroUnit> _heroes = new Dictionary<UnitId, HeroUnit>();
        private SummonRules _summonRules;
        private StageRunner _stageRunner;
        private Transform _worldRoot;
        private Transform _groundRoot;
        private SpriteRenderer _summonZoneOverlay;
        private CastleUnit _playerCastle;
        private CastleUnit _enemyCastle;

        /// <summary>Battle (re)started: UI should rebuild cards and bindings.</summary>
        public event Action BattleStarted;
        /// <summary>Cards, selection or outcome changed.</summary>
        public event Action Changed;
        public event Action<UnitId> SelectionChanged;
        public event Action<HeroUnit> HeroSummoned;
        public event Action<RejectReason> ActionRejected;
        public event Action<OutcomeDecision> Finished;
        /// <summary>The 2.5D view tilt changed.</summary>
        public event Action ViewChanged;

        /// <summary>View tilt in degrees (0–45). Setting it re-projects every unit and the map at once.</summary>
        public float ViewTiltDegrees
        {
            get => viewTiltDegrees;
            set
            {
                var clamped = Mathf.Clamp(value, 0f, MaxViewTiltDegrees);
                if (Mathf.Approximately(clamped, viewTiltDegrees))
                    return;
                viewTiltDegrees = clamped;
                RefreshView();
                ViewChanged?.Invoke();
            }
        }

        /// <summary>How much the ground's Y axis is squashed on screen: cos(tilt), 1 when looking straight down.</summary>
        public float ViewYScale => Mathf.Cos(viewTiltDegrees * Mathf.Deg2Rad);

        /// <summary>Logic-plane point → world position shown on screen.</summary>
        public Vector3 ToView(Vector2 logic) => new Vector3(logic.x, logic.y * ViewYScale, 0f);

        /// <summary>World position on screen (e.g. under a pointer) → logic-plane point.</summary>
        public Vector2 ToLogic(Vector2 view) => new Vector2(view.x, view.y / ViewYScale);

        /// <summary>Map bounds as shown on screen, for camera limits.</summary>
        public Rect ViewBounds
        {
            get
            {
                var b = Stage != null ? Stage.MapBounds : default;
                return new Rect(b.xMin, b.yMin * ViewYScale, b.width, b.height * ViewYScale);
            }
        }

        public IReadOnlyList<StageConfig> Stages => stages;
        public int StageIndex { get; private set; }
        public StageConfig Stage { get; private set; }
        public BattleRuntimeState State { get; private set; }
        public BattlefieldLayout Layout { get; private set; }
        public UnitRegistry Registry { get; } = new UnitRegistry();

        /// <summary>The cards of this battle with their level and stars; card index = position in this list.</summary>
        public IReadOnlyList<LineupEntry> Lineup { get; private set; } = Array.Empty<LineupEntry>();

        /// <summary>Hero definitions of <see cref="Lineup"/>, same indices.</summary>
        public IReadOnlyList<HeroDefinition> Cards { get; private set; } = Array.Empty<HeroDefinition>();

        public ProgressionConfig Progression => progression;

        /// <summary>Progression rules of the running battle (from <see cref="Progression"/>, or the design defaults).</summary>
        public ProgressionRules Rules { get; private set; } = new ProgressionRules(new HeroProgressionSettings());

        /// <summary>Faction synergy of the lineup, applied to every hero and soldier of the player.</summary>
        public CombatModifiers PlayerModifiers { get; private set; }
        public OutcomeDecision LastOutcome { get; private set; }
        public bool IsOver => State == null || State.IsOver;
        /// <summary>Seconds since the battle started (simulation time, not wall clock).</summary>
        public float BattleTime => State?.ElapsedTime ?? 0f;
        public bool AutoSimulate { get => autoSimulate; set => autoSimulate = value; }
        public int WaveCount => _stageRunner?.WaveCount ?? 0;

        public UnitId PlayerCastleId => _playerCastle != null && _playerCastle.IsAlive ? _playerCastle.Id : UnitId.None;
        public UnitId EnemyCastleId => _enemyCastle != null && _enemyCastle.IsAlive ? _enemyCastle.Id : UnitId.None;
        public Vector2 PlayerCastleRetreatPoint =>
            Stage != null ? Stage.PlayerCastle.Position + Vector2.right * (CastleUnit.BodyRadius + 1f) : Vector2.zero;

        public IEnumerable<HeroUnit> LivingHeroes
        {
            get
            {
                foreach (var hero in _heroes.Values)
                {
                    if (hero != null && hero.IsAlive)
                        yield return hero;
                }
            }
        }

        public HeroUnit SelectedHero => State != null ? GetHero(State.SelectedHero) : null;

        /// <summary>Sets up a controller created from code (editor tooling and tests).</summary>
        public void Configure(StageConfig[] stageList, Material material, bool startAutomatically, bool simulateAutomatically)
        {
            stages = stageList ?? Array.Empty<StageConfig>();
            spriteMaterial = material;
            startOnAwake = startAutomatically;
            autoSimulate = simulateAutomatically;
            if (spriteMaterial != null)
                PlaceholderArt.SpriteMaterial = spriteMaterial;
        }

        /// <summary>Replaces the progression config (tools and tests); takes effect at the next battle start.</summary>
        public void UseProgression(ProgressionConfig config) => progression = config;

        private void Awake()
        {
            if (spriteMaterial != null)
                PlaceholderArt.SpriteMaterial = spriteMaterial;
        }

        private void Start()
        {
            // A battle requested by the game flow wins over the scene's own stages.
            var launch = BattleLaunch.Take();
            if (launch?.Stage != null)
                Launch(launch);
            else if (startOnAwake && stages.Length > 0)
                StartBattle(Mathf.Clamp(_lastStageIndex, 0, stages.Length - 1));
        }

        private BattleLaunchRequest _launch;

        /// <summary>Scene the result screen can go back to (set by the game flow), or empty.</summary>
        public string ReturnSceneName => _launch?.ReturnScene ?? "";

        /// <summary>Note of the last result (rewards from the game flow), or empty.</summary>
        public string LastResultNote { get; private set; } = "";

        /// <summary>Starts a battle requested by another scene; its lineup is kept for replays.</summary>
        public void Launch(BattleLaunchRequest request)
        {
            _launch = request ?? throw new ArgumentNullException(nameof(request));
            StartBattle(request.Stage, request.Lineup);
        }

        /// <summary>Leaves the battle for the scene that requested it (no-op when played directly).</summary>
        public void ReturnToMenu()
        {
            if (string.IsNullOrEmpty(ReturnSceneName))
                return;
            _launch.OnReturn?.Invoke();
            UnityEngine.SceneManagement.SceneManager.LoadScene(ReturnSceneName);
        }

        // Last chosen stage, kept for this app session only (no saved progress in the prototype, spec §11).
        private static int _lastStageIndex;

        private void Update()
        {
            if (autoSimulate)
                Simulate(Time.deltaTime);
        }

        // ---- Lifecycle ----

        public void StartBattle(int stageIndex)
        {
            if (stages.Length == 0)
                throw new InvalidOperationException("BattleController has no stages.");
            StageIndex = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
            _lastStageIndex = StageIndex;
            StartBattle(stages[StageIndex], _lineupOverride);
        }

        /// <summary>Starts a fresh battle with the stage's test loadout. Nothing from a previous battle is kept (spec §12).</summary>
        public void StartBattle(StageConfig stage) => StartBattle(stage, null);

        /// <summary>
        /// Starts a fresh battle with the given lineup (lord first), e.g. the player's own heroes from the game flow.
        /// Null = the stage's test loadout. The lineup is kept for replays and stage switches.
        /// </summary>
        public void StartBattle(StageConfig stage, IReadOnlyList<LineupEntry> lineup)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            var errors = new List<string>();
            stage.Validate(errors);
            if (lineup != null)
                LineupValidation.Validate(lineup, $"Lineup for '{stage.name}'", errors);
            if (progression != null)
                progression.Validate(errors);
            foreach (var error in errors)
                Debug.LogError($"[Arena3Q] {error}", stage);

            if (spriteMaterial != null)
                PlaceholderArt.SpriteMaterial = spriteMaterial;

            ClearWorld();
            _lineupOverride = lineup;
            Lineup = lineup ?? stage.TestLoadout.GetLineup();
            var cards = new List<HeroDefinition>(Lineup.Count);
            foreach (var card in Lineup)
                cards.Add(card.Hero);
            Cards = cards;
            Rules = progression != null ? progression.CreateRules() : new ProgressionRules(new HeroProgressionSettings());
            PlayerModifiers = CombatNumbersFactory.SynergyOf(Lineup);
            _random = new System.Random(randomSeed);

            Stage = stage;
            State = new BattleRuntimeState(stage.CreateBattleSetup(Lineup));
            Layout = stage.CreateLayout();
            _summonRules = stage.CreateSummonRules();
            LastOutcome = OutcomeDecision.InProgress;
            LastResultNote = "";

            // Painted art is drawn at one fixed 3/4 angle: start at that angle so logic and picture line up
            // (the HUD slider can still change it for debugging).
            if (stage.BackgroundArt != null)
            {
                viewTiltDegrees = Mathf.Clamp(stage.BackgroundArtTiltDegrees, 0f, MaxViewTiltDegrees);
                ViewChanged?.Invoke();
            }

            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, false);
            BuildMapVisuals();

            _playerCastle = SpawnCastle(State.PlayerCastle, stage.PlayerCastle);
            // Defense stages have no enemy castle to attack (spec §8: enemies come in waves).
            if (stage.StageType == StageType.Attack)
                _enemyCastle = SpawnCastle(State.EnemyCastle, stage.EnemyCastle);

            _stageRunner = new StageRunner(this, stage);
            _stageRunner.Begin();

            BattleStarted?.Invoke();
            Changed?.Invoke();
        }

        public void Restart() => StartBattle(Stage, _lineupOverride);

        private void ClearWorld()
        {
            Registry.Clear();
            _squads.Clear();
            _heroes.Clear();
            _zones.Clear();
            _armyHealthFactor = 1f;
            _playerCastle = null;
            _enemyCastle = null;
            if (_worldRoot != null)
            {
                // Detach first so queries during this frame cannot see the old units.
                _worldRoot.SetParent(null);
                if (Application.isPlaying)
                    Destroy(_worldRoot.gameObject);
                else
                    DestroyImmediate(_worldRoot.gameObject);
            }
            _worldRoot = null;
            _groundRoot = null;
        }

        private void OnDestroy() => ClearWorld();

        // ---- Simulation ----

        /// <summary>Advances the battle by one step.</summary>
        public void Simulate(float deltaTime)
        {
            if (State == null || State.IsOver || deltaTime <= 0f)
                return;

            State.Tick(deltaTime);
            _stageRunner.Tick(State);

            foreach (var squad in _squads)
                squad.Refresh();

            _simulationScratch.Clear();
            _simulationScratch.AddRange(Registry.Units);
            foreach (var unit in _simulationScratch)
            {
                if (unit != null && unit.IsAlive)
                    unit.TickStatuses(deltaTime);
            }
            foreach (var unit in _simulationScratch)
            {
                if (unit != null && unit.IsAlive)
                    unit.Simulate(deltaTime);
            }

            // Skill areas that stay on the ground (fire field, rain of arrows…).
            for (var i = _zones.Count - 1; i >= 0; i--)
            {
                _zones[i].Tick(deltaTime);
                if (_zones[i].IsOver)
                    _zones.RemoveAt(i);
            }

            SeparateUnits();

            var decision = BattleOutcomeRules.EvaluateAndApply(State);
            if (decision.IsFinal)
            {
                LastOutcome = decision;
                LastResultNote = _launch?.OnFinished?.Invoke(Stage, decision) ?? "";
                SetSelectionVisual(UnitId.None);
                Finished?.Invoke(decision);
                Changed?.Invoke();
            }
        }

        // Keeps units from stacking on one point. Castles do not move.
        private void SeparateUnits()
        {
            var units = Registry.Units;
            for (var i = 0; i < units.Count; i++)
            {
                var a = units[i];
                if (a == null || !a.IsAlive)
                    continue;
                for (var j = i + 1; j < units.Count; j++)
                {
                    var b = units[j];
                    if (b == null || !b.IsAlive || (a.IsCastle && b.IsCastle))
                        continue;
                    var delta = b.Position - a.Position;
                    var minDistance = (a.Radius + b.Radius) * 0.9f;
                    var sqr = delta.sqrMagnitude;
                    if (sqr >= minDistance * minDistance)
                        continue;
                    var distance = Mathf.Sqrt(sqr);
                    var normal = distance > 1e-4f ? delta / distance : new Vector2(((i + j) & 1) == 0 ? 1f : -1f, 0.3f).normalized;
                    var push = (minDistance - distance);
                    if (a.IsCastle)
                        b.SetPosition(b.Position + normal * push);
                    else if (b.IsCastle)
                        a.SetPosition(a.Position - normal * push);
                    else
                    {
                        a.SetPosition(a.Position - normal * push * 0.5f);
                        b.SetPosition(b.Position + normal * push * 0.5f);
                    }
                }
            }
        }

        // ---- Player actions ----

        public SummonDecision TrySummon(SummonRequest request)
        {
            if (State == null)
                return SummonDecision.Reject(RejectReason.BattleOver);

            var decision = _summonRules.Evaluate(request, State, IsQuickPointOccupied);
            if (!decision.Accepted)
            {
                ActionRejected?.Invoke(decision.Result.Reason);
                return decision;
            }

            var card = Lineup[request.CardIndex];
            var definition = card.Hero;
            var numbers = CombatNumbersFactory.ForHero(card, Rules, PlayerModifiers);
            numbers.MaxHealth *= _armyHealthFactor;
            var runtime = State.RegisterSummon(request.CardIndex, numbers.MaxHealth);
            var hero = CreateUnit<HeroUnit>("Hero");
            hero.Init(this, runtime, card, numbers, Stage.CreateHeroBrainSettings(definition), decision.Position);
            Register(hero);
            _heroes[hero.Id] = hero;
            RaiseArmyHealth(1f + hero.Skills.ArmyMaxHealthBonus);

            // Passive extra soldiers (Quân Lệnh Như Sơn), never above maxSquadSize (spec §7).
            var squadSize = Mathf.Min(definition.MaxSquadSize, definition.BaseSquadSize + hero.Skills.ExtraSoldiers);
            var squad = new Squad(hero, Stage.OnHeroDeathTroopBehavior, squadSize);
            hero.Squad = squad;
            _squads.Add(squad);
            for (var i = 0; i < squadSize; i++)
                SpawnSoldier(hero, card, i);

            // Provisional: a freshly summoned hero becomes the selected one.
            TrySelectHero(hero.Id);
            HeroSummoned?.Invoke(hero);
            Changed?.Invoke();
            return decision;
        }

        /// <summary>Selects a living hero (spec §5: one selected hero at a time).</summary>
        public bool TrySelectHero(UnitId heroId)
        {
            if (State == null || !State.TrySelectHero(heroId))
                return false;
            SetSelectionVisual(heroId);
            SelectionChanged?.Invoke(heroId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Tapping the card of a deployed hero selects that hero (spec §3).</summary>
        public bool TrySelectCard(int cardIndex)
        {
            if (State == null || cardIndex < 0 || cardIndex >= State.Cards.Count)
                return false;
            var card = State.Cards[cardIndex];
            return card.State == CardState.Deployed && TrySelectHero(card.Hero);
        }

        public enum TapResult
        {
            Nothing,
            Selected,
            Attack,
            Move,
            Rejected
        }

        /// <summary>
        /// A tap on the battlefield (spec §5): on a hero = select it; with a hero selected, on an enemy unit or castle =
        /// AttackCommand (provisional), on ground = MoveCommand.
        /// </summary>
        public TapResult TapAt(Vector2 worldPoint)
        {
            var hero = FindHeroAt(worldPoint);
            if (hero != null)
                return TrySelectHero(hero.Id) ? TapResult.Selected : TapResult.Rejected;

            var selected = SelectedHero;
            if (selected == null)
                return TapResult.Nothing;

            var enemy = FindEnemyAt(worldPoint);
            if (enemy != null)
                return AttackWithHero(selected.Id, enemy).Accepted ? TapResult.Attack : TapResult.Rejected;
            return MoveSelectedHero(worldPoint).Accepted ? TapResult.Move : TapResult.Rejected;
        }

        public CommandResult MoveSelectedHero(Vector2 destination)
        {
            var hero = SelectedHero;
            if (hero == null)
                return Reject(RejectReason.HeroNotAvailable);
            var command = new MoveCommand(hero.Id, destination);
            var result = CommandRules.ValidateMove(command, State, Layout);
            if (!result.Accepted)
                return Reject(result.Reason);
            hero.Brain.Issue(command);
            hero.Runtime.SyncFrom(hero.Brain);
            SpawnMarker(destination, new Color(1f, 1f, 1f, 0.8f));
            return result;
        }

        public CommandResult AttackWithHero(UnitId heroId, UnitBase target)
        {
            if (State == null)
                return Reject(RejectReason.BattleOver);
            if (target == null)
                return Reject(RejectReason.InvalidTarget);
            var command = new AttackCommand(heroId, target.Id);
            var result = CommandRules.ValidateAttack(command, State, target.Team, target.IsAlive);
            if (!result.Accepted)
                return Reject(result.Reason);
            var hero = GetHero(heroId);
            hero.Brain.Issue(command);
            hero.Runtime.SyncFrom(hero.Brain);
            TrySelectHero(heroId);
            SpawnMarker(target.Position, new Color(1f, 0.3f, 0.25f, 0.9f));
            return result;
        }

        private CommandResult Reject(RejectReason reason)
        {
            ActionRejected?.Invoke(reason);
            return CommandResult.Reject(reason);
        }

        public HeroUnit GetHero(UnitId id) =>
            id.IsValid && _heroes.TryGetValue(id, out var hero) && hero != null && hero.IsAlive ? hero : null;

        public HeroUnit FindHeroAt(Vector2 worldPoint) =>
            Registry.FindNearest(worldPoint, 0f, HeroPickRadius, u => u is HeroUnit) as HeroUnit;

        /// <summary>Enemy unit or castle under a world point, for attack swipes.</summary>
        public UnitBase FindEnemyAt(Vector2 worldPoint) =>
            Registry.FindNearest(worldPoint, 0f, EnemyPickRadius, u => u.Team == Team.Enemy);

        public void ShowSummonZone(bool visible)
        {
            if (_summonZoneOverlay != null)
                _summonZoneOverlay.enabled = visible;
        }

        // ---- Spawning ----

        internal void NotifyHeroDied(HeroUnit hero)
        {
            State.MarkHeroDead(hero.Id);
            SelectionChanged?.Invoke(State.SelectedHero);
            Changed?.Invoke();
        }

        internal EnemyUnit SpawnEnemy(TroopDefinition definition, EnemyRole role, Vector2 position, float guardRadius,
            int waveIndex)
        {
            var enemy = CreateUnit<EnemyUnit>("Enemy");
            var numbers = CombatNumbersFactory.ForEnemy(definition, Stage.EnemyLevel, Rules);
            enemy.Init(this, State.AllocateUnitId(), definition, numbers, role, position, guardRadius, waveIndex);
            enemy.Record = State.RegisterUnit(enemy.Id, Team.Enemy, UnitKind.Enemy, UnitId.None, enemy.MaxHealth);
            Register(enemy);
            return enemy;
        }

        private CastleUnit SpawnCastle(CastleRuntime runtime, CastleConfig config)
        {
            var castle = CreateUnit<CastleUnit>("Castle");
            castle.Init(this, runtime, config);
            Register(castle);
            return castle;
        }

        private T CreateUnit<T>(string objectName) where T : UnitBase
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(_worldRoot, false);
            return go.AddComponent<T>();
        }

        private void Register(UnitBase unit)
        {
            Registry.Add(unit);
            unit.Died += OnUnitDied;
        }

        private void OnUnitDied(UnitBase unit)
        {
            Registry.Remove(unit);
            if (unit.Record != null)
                State.MarkUnitDead(unit.Id);
        }

        // ---- Skills ----

        private readonly List<SkillZone> _zones = new List<SkillZone>();
        private float _armyHealthFactor = 1f;

        /// <summary>
        /// The card of a deployed hero was tapped (auto-aim) or dragged to <paramref name="point"/>: use its ultimate
        /// (spec §3). Rejected with "kỹ năng chưa thể sử dụng" when not learned, not full or the hero is stunned.
        /// </summary>
        public CommandResult TryUseUltimate(int cardIndex, Vector2? point = null)
        {
            if (State == null || State.IsOver)
                return Reject(RejectReason.BattleOver);
            if (cardIndex < 0 || cardIndex >= State.Cards.Count || State.Cards[cardIndex].State != CardState.Deployed)
                return Reject(RejectReason.HeroNotAvailable);
            var hero = GetHero(State.Cards[cardIndex].Hero);
            if (hero == null)
                return Reject(RejectReason.HeroNotAvailable);
            var reason = hero.Skills.TryUltimate(point);
            if (reason != RejectReason.None)
                return Reject(reason);
            Changed?.Invoke();
            return CommandResult.Ok;
        }

        internal void AddZone(SkillZone zone) => _zones.Add(zone);

        public int ActiveZoneCount => _zones.Count;

        /// <summary>The nearest living hero of the same team (the caster included) whose squad lost soldiers, in range.</summary>
        internal HeroUnit FindHeroNeedingSoldiers(HeroUnit caster, float range)
        {
            HeroUnit best = null;
            var bestDistance = float.MaxValue;
            foreach (var hero in LivingHeroes)
            {
                if (hero.Team != caster.Team || hero.Squad == null || hero.Squad.Members.Count >= hero.Squad.Size)
                    continue;
                var distance = hero == caster ? 0f : caster.EdgeDistance(hero);
                if (distance <= range && distance < bestDistance)
                {
                    best = hero;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Brings back up to <paramref name="count"/> fallen soldiers of a hero's squad (Hiệu Triệu).</summary>
        internal int RefillSquad(HeroUnit hero, int count)
        {
            if (hero == null || !hero.IsAlive || hero.Squad == null)
                return 0;
            var card = Lineup[hero.CardIndex];
            var added = 0;
            for (var slot = 0; slot < hero.Squad.Size && added < count; slot++)
            {
                if (hero.Squad.HasSlot(slot))
                    continue;
                SpawnSoldier(hero, card, slot);
                added++;
            }
            return added;
        }

        private void SpawnSoldier(HeroUnit hero, LineupEntry card, int slot)
        {
            var definition = card.Hero;
            if (definition.Troop == null)
                return;
            var settings = new TroopBrainSettings
            {
                StageType = Stage.StageType,
                LeashRadius = definition.LeashRadius,
                UnreachableSeconds = definition.TroopUnreachableSeconds,
                AssistRadius = definition.TroopAssistRadius
            };
            var tint = Color.Lerp(definition.PlaceholderColor, Color.white, 0.35f);
            var numbers = CombatNumbersFactory.ForTroop(definition.Troop, card.Level, definition.Leadership, Rules, PlayerModifiers);
            numbers.MaxHealth *= _armyHealthFactor;
            var troop = CreateUnit<TroopUnit>("Troop");
            troop.Init(this, State.AllocateUnitId(), definition.Troop, numbers, hero.Squad, slot, settings, tint);
            troop.Record = State.RegisterUnit(troop.Id, Team.Player, UnitKind.Troop, hero.Id, troop.MaxHealth);
            Register(troop);
        }

        // Army-wide max health passive (Quân Lệnh Như Sơn): every player unit, present and future. 🟡 The bonus stays
        // after its hero dies (no stat drop mid-fight).
        private void RaiseArmyHealth(float factor)
        {
            if (factor <= _armyHealthFactor)
                return;
            var ratio = factor / _armyHealthFactor;
            _armyHealthFactor = factor;
            foreach (var unit in Registry.Units)
            {
                if (unit != null && unit.IsAlive && unit.Team == Team.Player && !unit.IsCastle)
                    unit.ScaleMaxHealth(ratio);
            }
        }

        private static readonly Color PlayerSkillColor = new Color(0.35f, 0.75f, 1f, 0.35f);
        private static readonly Color EnemySkillColor = new Color(1f, 0.3f, 0.25f, 0.35f);

        /// <summary>Placeholder look of a skill until the VFX exist: the area on the ground, fading out.</summary>
        internal void ShowSkill(SkillDefinition skill, SkillAim aim, Team team, float seconds, UnitBase follow)
        {
            if (_groundRoot == null)
                return;
            var color = skill.IconColor;
            color.a = 0.4f;
            if (color.r + color.g + color.b < 0.1f)
                color = team == Team.Player ? PlayerSkillColor : EnemySkillColor;
            SpriteRenderer renderer;
            switch (skill.Shape)
            {
                case SkillShape.Circle:
                    renderer = PlaceholderArt.CreateRenderer($"Skill {skill.SkillId}", _groundRoot, PlaceholderArt.Circle, color,
                        Vector2.one * skill.Radius * 2f, 29000);
                    renderer.transform.localPosition = follow != null ? follow.Position : aim.Point;
                    break;
                case SkillShape.Line:
                case SkillShape.Cone:
                {
                    var length = skill.CasterMove == CasterMove.ChargeLine ? skill.MoveDistance : skill.Length;
                    var width = skill.Shape == SkillShape.Line ? skill.Width : 2f * length * Mathf.Tan(skill.Angle * 0.5f * Mathf.Deg2Rad);
                    renderer = PlaceholderArt.CreateRenderer($"Skill {skill.SkillId}", _groundRoot, PlaceholderArt.Square, color,
                        new Vector2(length, Mathf.Max(0.3f, width)), 29000);
                    renderer.transform.localPosition = aim.Origin + aim.Direction * length * 0.5f;
                    renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.Direction.y, aim.Direction.x) * Mathf.Rad2Deg);
                    break;
                }
                case SkillShape.Global:
                    renderer = PlaceholderArt.CreateRenderer($"Skill {skill.SkillId}", _groundRoot, PlaceholderArt.Square, color,
                        Stage.MapBounds.size, 29000);
                    renderer.transform.localPosition = Stage.MapBounds.center;
                    break;
                default:
                    renderer = PlaceholderArt.CreateRenderer($"Skill {skill.SkillId}", _groundRoot, PlaceholderArt.Star,
                        new Color(color.r, color.g, color.b, 0.9f), new Vector2(0.9f, 0.9f), 29001);
                    renderer.transform.localPosition = aim.Point;
                    break;
            }
            var effect = renderer.gameObject.AddComponent<SkillAreaVisual>();
            effect.Setup(seconds, follow);
        }

        /// <summary>One crit roll from the battle's seeded random numbers (deterministic for a given seed).</summary>
        internal bool RollCritical(float chance) => chance >= 1f || (chance > 0f && _random.NextDouble() < chance);

        private bool IsQuickPointOccupied(Vector2 point) =>
            Registry.FindNearest(point, 0f, QuickPointOccupiedRadius - HeroUnit.BodyRadius, u => u is HeroUnit) != null;

        private void SetSelectionVisual(UnitId selected)
        {
            foreach (var hero in _heroes.Values)
            {
                if (hero != null)
                    hero.SetSelected(hero.Id == selected);
            }
        }

        // ---- Map visuals ----

        private static readonly Color GroundColor = new Color(0.36f, 0.55f, 0.3f);
        private static readonly Color ZoneColor = new Color(0.25f, 0.35f, 0.55f);
        private static readonly Color WaterColor = new Color(0.22f, 0.48f, 0.82f);
        private static readonly Color BridgeColor = new Color(0.58f, 0.4f, 0.22f);

        private void BuildMapVisuals()
        {
            // Everything lying flat on the ground lives under one root in logic coordinates; squashing that root's Y
            // gives the 2.5D tilt, including for rotated pieces such as river segments.
            _groundRoot = new GameObject("Ground").transform;
            _groundRoot.SetParent(_worldRoot, false);
            ApplyGroundTilt();

            if (Stage.BackgroundArt != null)
            {
                // The painting already shows grass, river and bridges; it is stretched over the map bounds in logic
                // space, and the ground root's tilt (set to the art's angle) brings it back to its painted proportions.
                var art = PlaceholderArt.CreateRenderer("MapArt", _groundRoot, Stage.BackgroundArt, Color.white,
                    Vector2.one, -32000);
                var artSize = Stage.BackgroundArt.bounds.size;
                var bounds = Stage.MapBounds;
                art.transform.localPosition = bounds.center;
                art.transform.localScale = new Vector3(bounds.width / artSize.x, bounds.height / artSize.y, 1f);
            }
            else
            {
                AddGroundVisual("Ground", PlaceholderArt.Square, GroundColor, Stage.MapBounds, -32000);

                foreach (var zone in Stage.ForbiddenZones)
                    AddGroundVisual("ForbiddenZone", PlaceholderArt.Square, ZoneColor, zone, -31990);

                foreach (var obstacle in Layout.Obstacles)
                {
                    if (obstacle is RiverObstacle river)
                        BuildRiverVisual(river);
                }
            }

            _summonZoneOverlay = AddGroundVisual("SummonZone", PlaceholderArt.Square, new Color(0.4f, 0.75f, 1f, 0.28f),
                Stage.SummonZone, -31980);
            _summonZoneOverlay.enabled = false;

            foreach (var point in Stage.QuickSummonPoints)
            {
                AddGroundVisual("QuickSummonPoint", PlaceholderArt.Ring, new Color(1f, 1f, 1f, 0.35f),
                    new Rect(point - new Vector2(0.55f, 0.55f), new Vector2(1.1f, 1.1f)), -31970);
            }
        }

        // The water is drawn along the whole centre line (under the bridges too); bridges are drawn on top.
        private void BuildRiverVisual(RiverObstacle river)
        {
            var root = new GameObject($"River {river.Name}").transform;
            root.SetParent(_groundRoot, false);
            var path = river.Path;
            for (var i = 0; i < path.Count; i++)
            {
                var joint = PlaceholderArt.CreateRenderer("Bend", root, PlaceholderArt.Circle, WaterColor,
                    new Vector2(river.Width, river.Width), -31995);
                joint.transform.localPosition = path[i];
                if (i == path.Count - 1)
                    break;
                var delta = path[i + 1] - path[i];
                var segment = PlaceholderArt.CreateRenderer("Water", root, PlaceholderArt.Square, WaterColor,
                    new Vector2(delta.magnitude, river.Width), -31995);
                segment.transform.localPosition = (path[i] + path[i + 1]) * 0.5f;
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            foreach (var crossing in river.Crossings)
            {
                var bridge = PlaceholderArt.CreateRenderer("Bridge", root, PlaceholderArt.Square, BridgeColor,
                    new Vector2(crossing.Width, river.Width + 1f), -31993);
                bridge.transform.localPosition = crossing.Center;
                bridge.transform.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(crossing.Along.y, crossing.Along.x) * Mathf.Rad2Deg);
            }
        }

        // A flat decoration lying on the ground, placed in logic coordinates under the ground root.
        private SpriteRenderer AddGroundVisual(string objectName, Sprite sprite, Color color, Rect area, int order)
        {
            var renderer = PlaceholderArt.CreateRenderer(objectName, _groundRoot, sprite, color, area.size, order);
            renderer.transform.localPosition = area.center;
            return renderer;
        }

        private void ApplyGroundTilt()
        {
            if (_groundRoot != null)
                _groundRoot.localScale = new Vector3(1f, ViewYScale, 1f);
        }

        private void RefreshView()
        {
            ApplyGroundTilt();
            foreach (var unit in Registry.Units)
            {
                if (unit != null)
                    unit.RefreshView();
            }
        }

        private void SpawnMarker(Vector2 position, Color color)
        {
            if (_groundRoot == null)
                return;
            var marker = PlaceholderArt.CreateRenderer("CommandMarker", _groundRoot, PlaceholderArt.Ring, color,
                new Vector2(0.8f, 0.8f), 30000);
            marker.transform.localPosition = position;
            marker.gameObject.AddComponent<FadeAndDestroy>();
        }
    }
}

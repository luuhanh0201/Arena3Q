using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>
    /// All mutable state of one battle (spec §10). Create a new instance per battle; nothing carries over on replay.
    /// Rule checks live in <see cref="SummonRules"/>, <see cref="CommandRules"/> and <see cref="BattleOutcomeRules"/>.
    /// </summary>
    public sealed class BattleRuntimeState
    {
        private readonly List<HeroCard> _cards = new List<HeroCard>();
        private readonly List<HeroRuntime> _heroes = new List<HeroRuntime>();
        private readonly Dictionary<UnitId, HeroRuntime> _heroesById = new Dictionary<UnitId, HeroRuntime>();
        private int _lastUnitId;

        public BattleRuntimeState(BattleSetup setup)
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));

            for (var i = 0; i < setup.CardHeroIds.Count; i++)
                _cards.Add(new HeroCard(i, setup.CardHeroIds[i]));

            PlayerCastle = new CastleRuntime(AllocateUnitId(), Team.Player, setup.PlayerCastleMaxHealth);
            EnemyCastle = new CastleRuntime(AllocateUnitId(), Team.Enemy, setup.EnemyCastleMaxHealth);
        }

        public BattleSetup Setup { get; }
        public StageType StageType => Setup.StageType;

        public float ElapsedTime { get; private set; }
        public bool HasTimeLimit => Setup.TimeLimit > 0f;
        public float RemainingTime => HasTimeLimit ? Math.Max(0f, Setup.TimeLimit - ElapsedTime) : float.PositiveInfinity;
        public bool IsTimeUp => HasTimeLimit && ElapsedTime >= Setup.TimeLimit;

        /// <summary>Index of the wave in progress (Defense stages). Written by the wave spawner.</summary>
        public int CurrentWave { get; set; }

        /// <summary>True once every wave has spawned and all of its enemies are dead. Written by the Defense stage runner.</summary>
        public bool AllWavesCleared { get; set; }

        public IReadOnlyList<HeroCard> Cards => _cards;
        public IReadOnlyList<HeroRuntime> Heroes => _heroes;
        public int SummonedCount => _heroes.Count;
        public int HeroesLost { get; private set; }
        public int AliveHeroCount => SummonedCount - HeroesLost;

        public UnitId SelectedHero { get; private set; }

        public CastleRuntime PlayerCastle { get; }
        public CastleRuntime EnemyCastle { get; }

        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public int Stars { get; private set; }
        public bool IsOver => Outcome != BattleOutcome.InProgress;

        public UnitId AllocateUnitId() => new UnitId(++_lastUnitId);

        public void Tick(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
                return;
            ElapsedTime += deltaTime;
        }

        public bool TryGetHero(UnitId id, out HeroRuntime hero) => _heroesById.TryGetValue(id, out hero);

        /// <summary>
        /// Records a summon that <see cref="SummonRules"/> has already accepted. The new hero starts in Idle (spec §4).
        /// Throws if the card cannot be summoned, since that means the caller skipped validation.
        /// </summary>
        public HeroRuntime RegisterSummon(int cardIndex, float heroMaxHealth)
        {
            if (IsOver)
                throw new InvalidOperationException("Battle is over.");
            if (cardIndex < 0 || cardIndex >= _cards.Count)
                throw new ArgumentOutOfRangeException(nameof(cardIndex));
            if (SummonedCount >= SummonRules.MaxHeroesPerBattle)
                throw new InvalidOperationException("Hero limit reached.");

            var card = _cards[cardIndex];
            if (!SummonRules.IsCardSummonable(card, Setup.AllowResummonDeadHero))
                throw new InvalidOperationException($"Card {cardIndex} cannot be summoned in state {card.State}.");

            var hero = new HeroRuntime(AllocateUnitId(), cardIndex, heroMaxHealth);
            _heroes.Add(hero);
            _heroesById.Add(hero.Id, hero);
            card.State = CardState.Deployed;
            card.Hero = hero.Id;
            return hero;
        }

        /// <summary>Kills a hero: cancels its command, greys out its card and counts it as lost. Idempotent.</summary>
        public void MarkHeroDead(UnitId id)
        {
            if (!_heroesById.TryGetValue(id, out var hero) || !hero.IsAlive)
                return;

            hero.State = HeroState.Dead;
            hero.Health = 0f;
            hero.ClearCommand();
            HeroesLost++;

            var card = _cards[hero.CardIndex];
            if (card.Hero == id)
                card.State = CardState.Dead;

            if (SelectedHero == id)
                SelectedHero = UnitId.None;
        }

        /// <summary>Only one living hero can be selected at a time (spec §5).</summary>
        public bool TrySelectHero(UnitId id)
        {
            if (IsOver || !_heroesById.TryGetValue(id, out var hero) || !hero.IsAlive)
                return false;
            SelectedHero = id;
            return true;
        }

        public void ClearSelection() => SelectedHero = UnitId.None;

        /// <summary>Ends the battle once. Stars are computed from heroes lost (spec §8).</summary>
        public void Finish(BattleOutcome outcome)
        {
            if (IsOver || outcome == BattleOutcome.InProgress)
                return;
            Outcome = outcome;
            Stars = StarRating.Calculate(outcome == BattleOutcome.Victory, HeroesLost, Setup.StarThresholds);
            SelectedHero = UnitId.None;
        }
    }
}

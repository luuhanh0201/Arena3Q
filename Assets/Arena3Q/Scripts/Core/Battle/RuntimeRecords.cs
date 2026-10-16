using System;
using UnityEngine;

namespace Arena3Q.Core
{
    public enum CardState
    {
        /// <summary>Not summoned yet: tap or drag to summon.</summary>
        Available,
        /// <summary>Hero is on the field: tapping the card selects the hero.</summary>
        Deployed,
        /// <summary>Hero died: card is greyed out (spec §3).</summary>
        Dead
    }

    public enum BattleOutcome
    {
        InProgress,
        Victory,
        Defeat
    }

    public sealed class HeroCard
    {
        internal HeroCard(int index, string heroId)
        {
            Index = index;
            HeroId = heroId;
        }

        public int Index { get; }
        public string HeroId { get; }
        public CardState State { get; internal set; } = CardState.Available;

        /// <summary>The most recent hero summoned from this card, or <see cref="UnitId.None"/>.</summary>
        public UnitId Hero { get; internal set; }
    }

    /// <summary>Per-hero battle data (spec §10). Behaviour fields are written by the gameplay layer.</summary>
    public sealed class HeroRuntime
    {
        internal HeroRuntime(UnitId id, int cardIndex, float maxHealth)
        {
            Id = id;
            CardIndex = cardIndex;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public UnitId Id { get; }
        public int CardIndex { get; }
        public float MaxHealth { get; }
        public float Health { get; set; }

        public HeroState State { get; internal set; } = HeroState.Idle;
        public bool IsAlive => State != HeroState.Dead;

        public HeroCommandKind CurrentCommand { get; set; }
        /// <summary>Saved MoveCommand destination; Combat resumes towards it (spec §6).</summary>
        public Vector2 Destination { get; set; }
        public UnitId AttackTarget { get; set; }

        /// <summary>Sets a non-Dead behaviour state. Death goes through <see cref="BattleRuntimeState.MarkHeroDead"/>.</summary>
        public void SetState(HeroState state)
        {
            if (state == HeroState.Dead)
                throw new InvalidOperationException("Use BattleRuntimeState.MarkHeroDead to kill a hero.");
            if (!IsAlive)
                return;
            State = state;
        }

        public void ClearCommand()
        {
            CurrentCommand = HeroCommandKind.None;
            AttackTarget = UnitId.None;
        }

        /// <summary>Copies behaviour state from the hero's brain. Death is not copied; it goes through MarkHeroDead.</summary>
        public void SyncFrom(HeroBrain brain)
        {
            if (!IsAlive || brain.IsDead)
                return;
            State = brain.State;
            CurrentCommand = brain.CurrentCommand;
            Destination = brain.Destination;
            AttackTarget = brain.AttackTarget;
        }
    }

    public sealed class CastleRuntime
    {
        internal CastleRuntime(UnitId id, Team team, float maxHealth)
        {
            Id = id;
            Team = team;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public UnitId Id { get; }
        public Team Team { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public bool IsDestroyed => Health <= 0f;

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDestroyed)
                return;
            Health = Mathf.Max(0f, Health - amount);
        }
    }
}

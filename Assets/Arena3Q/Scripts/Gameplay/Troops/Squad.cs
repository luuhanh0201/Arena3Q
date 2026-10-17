using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>A hero's own soldiers (spec §7). Soldiers belong to exactly one squad for their whole life.</summary>
    public sealed class Squad
    {
        public const float SlotSpacing = 1.05f;

        private readonly List<TroopUnit> _members = new List<TroopUnit>();
        private readonly OnHeroDeathTroopBehavior _onHeroDeath;

        public Squad(HeroUnit hero, OnHeroDeathTroopBehavior onHeroDeath, int size = 0)
        {
            Hero = hero;
            Size = size;
            HeroId = hero.Id;
            _onHeroDeath = onHeroDeath;
            LastHeroPosition = hero.Position;
            Mode = SquadMode.HoldFormation;
        }

        /// <summary>The leading hero; null once it has died and been destroyed.</summary>
        public HeroUnit Hero { get; private set; }

        /// <summary>Soldiers the squad should have (base size + passive extras, ≤ maxSquadSize).</summary>
        public int Size { get; }
        public UnitId HeroId { get; }
        public IReadOnlyList<TroopUnit> Members => _members;
        public SquadMode Mode { get; private set; }
        public Vector2 LastHeroPosition { get; private set; }
        public bool HeroAlive => Hero != null && Hero.IsAlive;

        internal void Add(TroopUnit troop) => _members.Add(troop);

        /// <summary>Is a living soldier standing in this formation slot?</summary>
        public bool HasSlot(int slotIndex) => _members.Exists(m => m != null && m.IsAlive && m.SlotIndex == slotIndex);
        internal void Remove(TroopUnit troop) => _members.Remove(troop);

        public Vector2 SlotPosition(int slotIndex) => LastHeroPosition + FormationMath.SlotOffset(slotIndex, SlotSpacing);

        /// <summary>FocusTarget: the hero's ordered target; otherwise what the hero is fighting right now.</summary>
        public UnitId HeroTarget
        {
            get
            {
                if (!HeroAlive)
                    return UnitId.None;
                return Mode == SquadMode.FocusTarget ? Hero.Brain.AttackTarget : Hero.EngagedTarget;
            }
        }

        /// <summary>Called once per simulation step before the soldiers think.</summary>
        internal void Refresh()
        {
            if (HeroAlive)
            {
                LastHeroPosition = Hero.Position;
                Mode = SquadModes.FromHero(Hero.State, _onHeroDeath);
                return;
            }

            Hero = null;
            Mode = SquadModes.FromHero(HeroState.Dead, _onHeroDeath);
            if (Mode == SquadMode.Disband)
            {
                // Copy: Die() removes the member from the list.
                foreach (var troop in _members.ToArray())
                    troop.Die();
            }
        }
    }
}

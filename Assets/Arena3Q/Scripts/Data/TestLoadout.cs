using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Prototype card set handed out when the Battle scene is played directly (spec §3, provisional): the lord card first,
    /// then the other heroes, each with a level and stars. Defaults to 1 lord + 4 heroes; add a sixth card to exercise
    /// the 5-hero limit. A game flow launch (lineup from the player's profile) replaces it.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Test Loadout", fileName = "TestLoadout")]
    public sealed class TestLoadout : ScriptableObject
    {
        [Tooltip("The lord. Always dealt, and counts toward the 5-hero limit.")]
        [SerializeField] private HeroDefinition lordHero;
        [SerializeField, Min(1)] private int lordLevel = 1;
        [SerializeField] private LineupEntry[] heroes = new LineupEntry[4];

        public HeroDefinition LordHero => lordHero;
        public int LordLevel => Mathf.Max(1, lordLevel);

        /// <summary>Cards in deal order: lord first. Card index in battle = index in this list.</summary>
        public IReadOnlyList<LineupEntry> GetLineup()
        {
            var lineup = new List<LineupEntry>(1 + (heroes?.Length ?? 0));
            if (lordHero != null)
                lineup.Add(new LineupEntry(lordHero, LordLevel));
            if (heroes != null)
            {
                foreach (var entry in heroes)
                {
                    if (entry?.Hero != null)
                        lineup.Add(entry);
                }
            }
            return lineup;
        }

        /// <summary>Hero definitions in deal order (same indices as <see cref="GetLineup"/>).</summary>
        public IReadOnlyList<HeroDefinition> GetCards()
        {
            var lineup = GetLineup();
            var cards = new List<HeroDefinition>(lineup.Count);
            foreach (var entry in lineup)
                cards.Add(entry.Hero);
            return cards;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Loadout '{name}'";
            if (lordHero == null)
                errors.Add($"{owner}: lordHero is not set.");
            if (heroes != null)
            {
                for (var i = 0; i < heroes.Length; i++)
                {
                    if (heroes[i]?.Hero == null)
                        errors.Add($"{owner}: heroes[{i}] is empty.");
                }
            }
            if (lordHero != null)
                LineupValidation.Validate(GetLineup(), owner, errors);
        }
    }
}

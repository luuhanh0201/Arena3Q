using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Prototype card set handed out at battle start (spec §3, provisional): the faction lord card first, then the other
    /// heroes. Defaults to 1 lord + 4 heroes; add a sixth card to exercise the 5-hero limit.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Test Loadout", fileName = "TestLoadout")]
    public sealed class TestLoadout : ScriptableObject
    {
        [Tooltip("The faction lord. Always dealt, and counts toward the 5-hero limit.")]
        [SerializeField] private HeroDefinition lordHero;
        [SerializeField] private HeroDefinition[] heroes = new HeroDefinition[4];

        public HeroDefinition LordHero => lordHero;

        /// <summary>Cards in deal order: lord first. Card index in battle = index in this list.</summary>
        public IReadOnlyList<HeroDefinition> GetCards()
        {
            var cards = new List<HeroDefinition>(1 + (heroes?.Length ?? 0));
            if (lordHero != null)
                cards.Add(lordHero);
            if (heroes != null)
            {
                foreach (var hero in heroes)
                {
                    if (hero != null)
                        cards.Add(hero);
                }
            }
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
                    if (heroes[i] == null)
                        errors.Add($"{owner}: heroes[{i}] is empty.");
                }
            }

            var seen = new HashSet<HeroDefinition>();
            foreach (var hero in GetCards())
            {
                if (!seen.Add(hero))
                    errors.Add($"{owner}: hero '{hero.name}' appears more than once.");
                else
                    hero.Validate(errors);
            }
        }
    }
}

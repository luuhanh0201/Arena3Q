using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>The four lords on the lord select screen, with what the screen shows about each (spec §2).</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Lord Roster", fileName = "LordRoster")]
    public sealed class LordRoster : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public HeroDefinition lord;
            [TextArea] public string role = "";
            [Tooltip("1 = easy, 3 = hard.")]
            [Range(1, 3)] public int difficulty = 1;
            [Tooltip("Passive, skill 2, skill 3, ultimate (docs/Tuong/LordHeroes.md §2).")]
            public string[] skillNames = new string[4];
            [TextArea] public string[] skillSummaries = new string[4];
            [Tooltip("Card portrait (portrait_<lord>.png); empty = placeholder.")]
            public Sprite portrait;
            [Tooltip("Splash art for the confirm dialog (splash_<lord>.png); empty = placeholder.")]
            public Sprite splash;

            public string SkillName(SkillSlot slot) =>
                skillNames != null && (int)slot < skillNames.Length ? skillNames[(int)slot] ?? "" : "";

            public string SkillSummary(SkillSlot slot) =>
                skillSummaries != null && (int)slot < skillSummaries.Length ? skillSummaries[(int)slot] ?? "" : "";
        }

        [SerializeField] private Entry[] lords = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Lords => lords;

        public Entry Find(string heroId)
        {
            foreach (var entry in lords)
            {
                if (entry?.lord != null && entry.lord.HeroId == heroId)
                    return entry;
            }
            return null;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Lord roster '{name}'";
            if (lords == null || lords.Length == 0)
            {
                errors.Add($"{owner}: no lords.");
                return;
            }
            var ids = new HashSet<string>();
            foreach (var entry in lords)
            {
                if (entry?.lord == null)
                {
                    errors.Add($"{owner}: an entry has no lord.");
                    continue;
                }
                if (entry.lord.Kind != HeroKind.Lord)
                    errors.Add($"{owner}: '{entry.lord.name}' is not a lord.");
                if (!ids.Add(entry.lord.HeroId))
                    errors.Add($"{owner}: '{entry.lord.HeroId}' is listed twice.");
                if (entry.skillNames == null || entry.skillNames.Length != 4)
                    errors.Add($"{owner}: '{entry.lord.name}' needs 4 skill names (passive, skill 2, skill 3, ultimate).");
            }
        }
    }
}

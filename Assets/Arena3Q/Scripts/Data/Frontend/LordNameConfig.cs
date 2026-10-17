using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Lord name rules and the random-name lists (the dice button).</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Lord Name Config", fileName = "LordNameConfig")]
    public sealed class LordNameConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int minLength = 2;
        [SerializeField, Min(1)] private int maxLength = 12;
        [Tooltip("Rejected anywhere in a name, compared without case and diacritics.")]
        [SerializeField] private string[] bannedWords = Array.Empty<string>();
        [SerializeField] private string[] familyNames = Array.Empty<string>();
        [SerializeField] private string[] givenNames = Array.Empty<string>();
        [Tooltip("The dice never produces these (famous Three Kingdoms generals).")]
        [SerializeField] private string[] reservedNames = Array.Empty<string>();

        public LordNameRules CreateRules() => new LordNameRules
        {
            MinLength = minLength,
            MaxLength = maxLength,
            BannedWords = bannedWords ?? Array.Empty<string>()
        };

        public RandomNameGenerator CreateGenerator() => new RandomNameGenerator(familyNames, givenNames, reservedNames);

        public void Validate(List<string> errors)
        {
            var owner = $"Name config '{name}'";
            if (maxLength < minLength) errors.Add($"{owner}: maxLength must be >= minLength.");
            if (familyNames == null || familyNames.Length == 0 || givenNames == null || givenNames.Length == 0)
                errors.Add($"{owner}: the dice needs family names and given names.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Arena3Q.Core
{
    /// <summary>Ways to sign in (spec §2 "Luồng vào game", docs/KeHoach/VaoGame.md §6).</summary>
    public enum AccountKind
    {
        None,
        /// <summary>"Chơi ngay": an account bound to this device, no sign-in.</summary>
        Guest,
        Password,
        Google,
        Apple
    }

    public enum CredentialProblem
    {
        None,
        UsernameLength,
        UsernameCharacters,
        PasswordLength,
        PasswordMismatch
    }

    /// <summary>Username / password rules of the game account (lengths come from data).</summary>
    public sealed class AccountRules
    {
        public int MinUsernameLength { get; set; } = 6;
        public int MaxUsernameLength { get; set; } = 20;
        public int MinPasswordLength { get; set; } = 8;

        /// <summary>Letters without diacritics, digits and '_' only.</summary>
        public CredentialProblem ValidateUsername(string username)
        {
            username = username ?? "";
            if (username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
                return CredentialProblem.UsernameLength;
            foreach (var c in username)
            {
                var ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                    return CredentialProblem.UsernameCharacters;
            }
            return CredentialProblem.None;
        }

        /// <param name="confirmation">Null when signing in (no confirmation field).</param>
        public CredentialProblem ValidatePassword(string password, string confirmation = null)
        {
            password = password ?? "";
            if (password.Length < MinPasswordLength)
                return CredentialProblem.PasswordLength;
            if (confirmation != null && confirmation != password)
                return CredentialProblem.PasswordMismatch;
            return CredentialProblem.None;
        }
    }

    public enum NameProblem
    {
        None,
        TooShort,
        TooLong,
        InvalidCharacters,
        Banned,
        Taken
    }

    /// <summary>
    /// Rules for the lord's name (spec §2: the lord carries the player's name; one name per server):
    /// Vietnamese letters with diacritics, digits and single spaces; no special characters; banned words filtered.
    /// </summary>
    public sealed class LordNameRules
    {
        public int MinLength { get; set; } = 2;
        public int MaxLength { get; set; } = 12;

        /// <summary>Compared without case and without diacritics, as substrings.</summary>
        public IReadOnlyCollection<string> BannedWords { get; set; } = Array.Empty<string>();

        /// <summary>Trims, collapses spaces and composes diacritics (NFC) so the same name is always stored the same way.</summary>
        public static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            var parts = name.Normalize(NormalizationForm.FormC).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        /// <param name="isTaken">Uniqueness check of the server (null = not checked).</param>
        public NameProblem Validate(string name, Func<string, bool> isTaken = null)
        {
            var normalized = Normalize(name);
            if (normalized.Length < MinLength)
                return NameProblem.TooShort;
            if (normalized.Length > MaxLength)
                return NameProblem.TooLong;
            foreach (var c in normalized)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    return NameProblem.InvalidCharacters;
            }

            var plain = Fold(normalized);
            foreach (var word in BannedWords)
            {
                var banned = Fold(word);
                if (banned.Length > 0 && plain.Contains(banned))
                    return NameProblem.Banned;
            }
            return isTaken != null && isTaken(normalized) ? NameProblem.Taken : NameProblem.None;
        }

        /// <summary>Lower case without diacritics and spaces ("Đặng Văn" → "dangvan"), for filtering.</summary>
        public static string Fold(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == ' ')
                    continue;
                builder.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }
    }

    /// <summary>Random period-style names ("Lâm Tử Yên") from data lists, avoiding reserved names (famous generals).</summary>
    public sealed class RandomNameGenerator
    {
        private readonly IReadOnlyList<string> _familyNames;
        private readonly IReadOnlyList<string> _givenNames;
        private readonly HashSet<string> _reserved;

        public RandomNameGenerator(IReadOnlyList<string> familyNames, IReadOnlyList<string> givenNames,
            IEnumerable<string> reservedNames)
        {
            _familyNames = familyNames ?? Array.Empty<string>();
            _givenNames = givenNames ?? Array.Empty<string>();
            _reserved = new HashSet<string>();
            if (reservedNames != null)
            {
                foreach (var name in reservedNames)
                    _reserved.Add(LordNameRules.Fold(name));
            }
        }

        /// <summary>A name that passes <paramref name="rules"/>, or "" if the lists cannot make one.</summary>
        public string Next(Random random, LordNameRules rules, Func<string, bool> isTaken = null)
        {
            if (_familyNames.Count == 0 || _givenNames.Count == 0)
                return "";
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var name = LordNameRules.Normalize(
                    $"{_familyNames[random.Next(_familyNames.Count)]} {_givenNames[random.Next(_givenNames.Count)]}");
                var folded = LordNameRules.Fold(name);
                var reserved = false;
                foreach (var entry in _reserved)
                {
                    if (folded.Contains(entry))
                    {
                        reserved = true;
                        break;
                    }
                }
                if (!reserved && (rules == null || rules.Validate(name, isTaken) == NameProblem.None))
                    return name;
            }
            return "";
        }
    }
}

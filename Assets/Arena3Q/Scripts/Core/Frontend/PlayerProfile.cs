using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    /// <summary>
    /// Everything the game remembers about one player on this device (prototype: local; later the server's copy).
    /// Public fields so it serializes with Unity's JsonUtility; bump <see cref="version"/> when the shape changes.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string accountId = "";
        public AccountKind accountKind = AccountKind.None;
        /// <summary>Shown on the "tap to enter" screen: username, e-mail, or "Khách".</summary>
        public string accountLabel = "";
        /// <summary>The 3-second intro has been watched once; afterwards a tap skips it.</summary>
        public bool introSeen;
        public string lastServerId = "";
        public List<CharacterRecord> characters = new List<CharacterRecord>();

        public bool IsSignedIn => !string.IsNullOrEmpty(accountId) && accountKind != AccountKind.None;

        /// <summary>This account's character on a server (one per server, spec §2), or null.</summary>
        public CharacterRecord FindCharacter(string serverId)
        {
            foreach (var character in characters)
            {
                if (character != null && character.serverId == serverId)
                    return character;
            }
            return null;
        }

        public CharacterRecord GetOrCreateCharacter(string serverId)
        {
            var character = FindCharacter(serverId);
            if (character != null)
                return character;
            character = new CharacterRecord { serverId = serverId };
            characters.Add(character);
            return character;
        }

        /// <summary>Signs out and forgets the account (the device keeps nothing of it).</summary>
        public void Clear()
        {
            accountId = "";
            accountKind = AccountKind.None;
            accountLabel = "";
            lastServerId = "";
            characters.Clear();
        }
    }

    /// <summary>The player's character on one server: the chosen lord, its name and progress, owned heroes.</summary>
    [Serializable]
    public sealed class CharacterRecord
    {
        public string serverId = "";
        /// <summary>heroId of the chosen lord; empty until chosen. Fixed for the whole game (spec §2).</summary>
        public string lordHeroId = "";
        /// <summary>The lord's name = the player's name on this server (spec §2).</summary>
        public string name = "";
        public int lordLevel = 1;
        public int lordStars = 1;
        public int lordExperience;
        public int vip;
        public long combatPower;
        public List<ResourceBalance> wallet = new List<ResourceBalance>();
        /// <summary>Points bought in each lord skill (passive, skill 2, skill 3, ultimate); 0 = not upgraded.</summary>
        public int[] lordSkillLevels = new int[4];
        public bool storySeen;
        public bool tutorialCleared;
        public List<OwnedHero> heroes = new List<OwnedHero>();
        public List<ChapterProgress> chapters = new List<ChapterProgress>();

        public ChapterProgress GetChapter(string chapterId)
        {
            chapters ??= new List<ChapterProgress>();
            var chapter = chapters.Find(c => c != null && c.chapterId == chapterId);
            if (chapter == null) { chapter = new ChapterProgress { chapterId = chapterId }; chapters.Add(chapter); }
            return chapter;
        }

        public bool HasLord => !string.IsNullOrEmpty(lordHeroId);
        public bool HasName => !string.IsNullOrEmpty(name);

        public OwnedHero FindHero(string heroId)
        {
            foreach (var hero in heroes)
            {
                if (hero != null && hero.heroId == heroId)
                    return hero;
            }
            return null;
        }

        /// <summary>Adds a hero, or raises the stars of one already owned.</summary>
        public OwnedHero AddHero(string heroId, int level, int stars)
        {
            var hero = FindHero(heroId);
            if (hero == null)
            {
                hero = new OwnedHero { heroId = heroId, level = Math.Max(1, level), stars = Math.Max(1, stars) };
                heroes.Add(hero);
            }
            else
            {
                hero.stars = Math.Max(hero.stars, stars);
                hero.level = Math.Max(hero.level, level);
            }
            return hero;
        }
    }

    [Serializable]
    public sealed class ResourceBalance
    {
        public string id = "";
        public long amount, capacity;
    }

    [Serializable]
    public sealed class OwnedHero
    {
        public string heroId = "";
        public int level = 1;
        public int stars = 1;
        public int shards;
    }

    /// <summary>Where the profile lives. Prototype: a JSON file on the device; tests: memory; later: the game server.</summary>
    public interface IPlayerProfileStore
    {
        /// <summary>The saved profile, or a new empty one.</summary>
        PlayerProfile Load();
        void Save(PlayerProfile profile);
        void Delete();
    }

    /// <summary>Keeps the profile in memory only (tests must never write files, docs/QuyTac/NhatKySuaLoi.md).</summary>
    public sealed class InMemoryProfileStore : IPlayerProfileStore
    {
        private PlayerProfile _profile;

        public InMemoryProfileStore(PlayerProfile initial = null)
        {
            _profile = initial;
        }

        public int SaveCount { get; private set; }

        public PlayerProfile Load() => _profile ?? (_profile = new PlayerProfile());

        public void Save(PlayerProfile profile)
        {
            _profile = profile;
            SaveCount++;
        }

        public void Delete() => _profile = null;
    }
}

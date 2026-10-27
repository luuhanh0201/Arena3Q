using Arena3Q.Core;
using Arena3Q.Data;
using System;
using System.Collections.Generic;

namespace Arena3Q.Frontend
{
    public interface ILobbyProfileSource
    {
        LobbyProfile Read(CharacterRecord character, LobbyConfig config, ProgressionConfig progression = null);
    }
    public sealed class LobbyProfile
    {
        public string name;
        public int level, stars, vip, experience;
        public long power;
        public IReadOnlyList<ResourceBalance> wallet = Array.Empty<ResourceBalance>();
        public ResourceBalance Balance(string id)
        {
            foreach (var balance in wallet)
                if (balance != null && balance.id == id) return balance;
            return new ResourceBalance { id = id };
        }
        public long experienceLimit;
        public bool maxLevel;
        public float ExperienceFraction => LobbyResourceRules.ExperienceFraction(experience, experienceLimit, maxLevel);
    }
    // Economy remains prototype data until the account service supplies it.
    public sealed class LocalLobbyProfileSource : ILobbyProfileSource
    {
        public LobbyProfile Read(CharacterRecord character, LobbyConfig config, ProgressionConfig progression = null) => new LobbyProfile
        {
            name = character?.name ?? "", level = character?.lordLevel ?? 1,
            stars = character?.lordStars ?? 1, experience = character?.lordExperience ?? 0,
            experienceLimit = progression != null ? progression.ExperienceRequiredForLevel(character?.lordLevel ?? 1) : 0,
            maxLevel = progression != null && (character?.lordLevel ?? 1) >= progression.MaxLevel,
            vip = character?.vip ?? 0, power = character?.combatPower ?? 0,
            wallet = character?.wallet ?? (IReadOnlyList<ResourceBalance>)Array.Empty<ResourceBalance>()
        };
    }
}

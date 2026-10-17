using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>Enemy rank (spec §8 ✅): normal soldiers have no skill, elites one, bosses 2–3 and a second phase.</summary>
    public enum EnemyTier
    {
        Normal,
        Elite,
        Boss
    }

    /// <summary>Where an enemy skill lands; the place is fixed when its red warning appears (spec §8 ✅).</summary>
    public enum EnemySkillAim
    {
        /// <summary>Around the caster (shape faces the unit it fights).</summary>
        Self,
        /// <summary>The unit the caster fights, else the nearest player unit in range (cones and lines face it).</summary>
        CurrentTarget,
        /// <summary>The nearest player hero in range.</summary>
        NearestHero,
        /// <summary>A random player hero in range.</summary>
        RandomHero,
        /// <summary><c>zoneCount</c> areas on random player units in range (rain of lightning, fireballs).</summary>
        AroundPlayerUnits
    }

    /// <summary>Pure helpers for enemy tiers and skills.</summary>
    public static class EnemyRules
    {
        /// <summary>Skill count allowed per tier (spec §8 ✅): normal 0, elite 1, boss 2–3.</summary>
        public static bool IsSkillCountValid(EnemyTier tier, int skills)
        {
            switch (tier)
            {
                case EnemyTier.Normal: return skills == 0;
                case EnemyTier.Elite: return skills == 1;
                default: return skills >= 2 && skills <= 3;
            }
        }

        /// <summary>The boss enters its second phase once, the first time health drops below the threshold.</summary>
        public static bool ShouldEnterPhaseTwo(bool alreadyEntered, float health, float maxHealth, float threshold) =>
            !alreadyEntered && maxHealth > 0f && health > 0f && health / maxHealth < threshold;

        /// <summary>
        /// Picks <paramref name="count"/> aim points among <paramref name="candidates"/> (each at most once while there are
        /// enough; extra zones land near a random candidate, offset by up to <paramref name="spread"/>).
        /// <paramref name="random01"/> returns values in [0, 1).
        /// </summary>
        public static List<Vector2> PickZonePoints(IReadOnlyList<Vector2> candidates, int count, float spread,
            System.Func<float> random01)
        {
            var result = new List<Vector2>();
            if (candidates == null || candidates.Count == 0 || count <= 0)
                return result;
            var pool = new List<Vector2>(candidates);
            while (result.Count < count && pool.Count > 0)
            {
                var index = Mathf.Min(pool.Count - 1, (int)(random01() * pool.Count));
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            while (result.Count < count)
            {
                var anchor = candidates[Mathf.Min(candidates.Count - 1, (int)(random01() * candidates.Count))];
                var angle = random01() * Mathf.PI * 2f;
                result.Add(anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread * Mathf.Max(0.3f, random01()));
            }
            return result;
        }

        /// <summary>
        /// Anti-lock (spec §8 🟡): a hero stunned without a break for longer than <paramref name="lockSeconds"/> gets
        /// crowd-control immunity. Returns the new stunned time, and whether immunity must be granted now.
        /// </summary>
        public static float TrackCrowdControl(float stunnedSeconds, bool stunned, float deltaTime, float lockSeconds,
            out bool grantImmunity)
        {
            grantImmunity = false;
            if (!stunned || lockSeconds <= 0f)
                return 0f;
            stunnedSeconds += deltaTime;
            if (stunnedSeconds <= lockSeconds)
                return stunnedSeconds;
            grantImmunity = true;
            return 0f;
        }

        /// <summary>Taunt (spec §8 ✅) only moves heroes that are not carrying out a player order.</summary>
        public static bool HeroFollowsTaunt(HeroState state) =>
            state == HeroState.Idle || state == HeroState.Advancing || state == HeroState.Combat;

        /// <summary>Soldiers follow a taunt only while fighting freely (not marching or focusing their hero's target).</summary>
        public static bool SquadFollowsTaunt(SquadMode mode) =>
            mode == SquadMode.HoldFormation || mode == SquadMode.Skirmish || mode == SquadMode.Leaderless;
    }
}

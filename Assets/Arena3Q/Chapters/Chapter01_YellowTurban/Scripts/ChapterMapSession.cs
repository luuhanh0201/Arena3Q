using System;
using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using Arena3Q.Frontend;
using Arena3Q.Gameplay;
using UnityEngine;

namespace Arena3Q.Exploration
{
    /// <summary>Profile-backed map session. Battle callbacks retain data only, never scene components.</summary>
    public sealed class ChapterMapSession
    {
        public ChapterMapConfig Config { get; }
        public PlayerProfile Profile { get; }
        public CharacterRecord Character { get; }
        public ChapterProgress Progress { get; }
        public ChapterProgressRules Rules { get; }
        private readonly IPlayerProfileStore _store;
        public ChapterMapSession(ChapterMapConfig config, IPlayerProfileStore store)
        {
            Config = config; _store = store; Profile = store.Load();
            Character = Profile.FindCharacter(Profile.lastServerId);
            if (Character == null || !Character.HasLord) throw new InvalidOperationException("A selected character is required to explore.");
            Progress = Character.GetChapter(config.chapterId); Rules = config.CreateRules();
            // No live battle survives a process restart. An interrupted attempt grants nothing.
            if (!string.IsNullOrEmpty(Progress.pendingBattleId) || !string.IsNullOrEmpty(Progress.pendingStageId))
            {
                Rules.CancelBattle(Progress);
                Save();
            }
        }
        public void Save() => _store.Save(Profile);
        public bool AcceptQuest(int index)
        {
            if (!Rules.AcceptQuest(Progress, index)) return false; Save(); return true;
        }
        public bool Claim(int rewardIndex)
        {
            if (rewardIndex < 0 || rewardIndex >= Config.rewards.Length) return false;
            var reward = Config.rewards[rewardIndex];
            if (reward.contents == null || reward.contents.Length == 0 || !Rules.CanClaim(Progress, reward.id, reward.stars)) return false;
            var grants = new Dictionary<string, long>();
            try
            {
                foreach (var grant in reward.contents)
                {
                    if (grant == null || string.IsNullOrWhiteSpace(grant.id) || grant.amount < 0) return false;
                    grants.TryGetValue(grant.id, out var total); grants[grant.id] = checked(total + grant.amount);
                }
                foreach (var grant in grants) { var wallet = Character.wallet?.Find(w => w != null && w.id == grant.Key); checked { _ = (wallet?.amount ?? 0) + grant.Value; } }
            }
            catch (OverflowException) { return false; }
            if (!Rules.Claim(Progress, reward.id, reward.stars)) return false;
            Character.wallet ??= new List<ResourceBalance>();
            foreach (var grant in grants)
            {
                var wallet = Character.wallet.Find(w => w != null && w.id == grant.Key);
                if (wallet == null) { wallet = new ResourceBalance { id = grant.Key }; Character.wallet.Add(wallet); }
                wallet.amount += grant.Value;
            }
            Save(); return true;
        }
        public List<LineupEntry> BuildLineup()
        {
            var lineup = new List<LineupEntry>(); var frontend = Config.frontend;
            var lord = frontend.LordRoster.Find(Character.lordHeroId);
            if (lord == null || lord.lord == null) return lineup;
            lineup.Add(new LineupEntry(lord.lord, Character.lordLevel, Character.lordStars, Character.lordSkillLevels));
            foreach (var owned in Character.heroes)
            {
                if (lineup.Count >= SummonRules.MaxHeroesPerBattle) break;
                var hero = frontend.FindHero(owned.heroId);
                if (hero != null && hero.Kind != HeroKind.Lord) lineup.Add(new LineupEntry(hero, owned.level, owned.stars));
            }
            return lineup;
        }
        public BattleLaunchRequest CreateBattle(int index)
        {
            if (index < 0 || index >= Config.stages.Length) return null;
            var entry = Config.stages[index]; var lineup = BuildLineup();
            if (entry.battle == null || lineup.Count == 0) return null;
            var attempt = Guid.NewGuid().ToString("N");
            if (!Rules.BeginBattle(Progress, index, attempt)) return null;
            Save();
            return new BattleLaunchRequest
            {
                Stage = entry.battle, Lineup = lineup, ReturnScene = Config.sceneName,
                OnStarted = played =>
                {
                    if (played != entry.battle) return;
                    Rules.CancelBattle(Progress);
                    attempt = Guid.NewGuid().ToString("N");
                    Rules.BeginBattle(Progress, index, attempt); Save();
                },
                OnFinishedWithStars = (played, result, stars) =>
                {
                    if (played != entry.battle || !result.IsFinal || !Rules.ApplyResult(Progress, entry.id, attempt, result.Outcome == BattleOutcome.Victory, stars)) return "";
                    Save(); return result.Outcome == BattleOutcome.Victory ? "Đã lưu tiến độ chương." : "";
                },
                OnReturn = () => { Rules.CancelBattle(Progress); Save(); }
            };
        }
    }
}

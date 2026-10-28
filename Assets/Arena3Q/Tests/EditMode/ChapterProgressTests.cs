using NUnit.Framework;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Tests
{
    public sealed class ChapterProgressTests
    {
        private ChapterProgressRules Rules => new ChapterProgressRules(new[] { "one", "two", "three" }, new[] { true, false, true });
        [Test] public void FirstVisitAndQuestMarkersFollowSequentialRules()
        {
            var rules = Rules; var p = new ChapterProgress();
            Assert.AreEqual(1, rules.UnlockedCount(p));
            Assert.True(rules.MarkerVisible(p, 0, MapInteractionKind.Quest));
            Assert.False(rules.MarkerVisible(p, 0, MapInteractionKind.Battle));
            Assert.False(rules.BeginBattle(p, 0, "beforeQuest"));
            Assert.True(rules.AcceptQuest(p, 0));
            Assert.True(rules.MarkerVisible(p, 0, MapInteractionKind.Battle));
            Assert.False(rules.MarkerVisible(p, 0, MapInteractionKind.Quest));
            Assert.False(rules.AcceptQuest(p, 2));
        }
        [Test] public void OnlyMatchingFinalBattleAwardsProgressAndDuplicateResultIsRejected()
        {
            var rules = Rules; var p = new ChapterProgress(); rules.AcceptQuest(p, 0);
            Assert.True(rules.BeginBattle(p, 0, "attempt"));
            Assert.False(rules.ApplyResult(p, "two", "attempt", true, 3));
            Assert.False(rules.ApplyResult(p, "one", "wrong", true, 3));
            Assert.False(rules.ApplyResult(p, "one", "attempt", true, 0));
            Assert.AreEqual(1, rules.UnlockedCount(p));
            Assert.True(rules.ApplyResult(p, "one", "attempt", true, 2));
            Assert.False(rules.ApplyResult(p, "one", "attempt", true, 3));
            Assert.AreEqual(2, rules.TotalStars(p)); Assert.AreEqual(2, rules.UnlockedCount(p));
            Assert.True(rules.MarkerVisible(p, 1, MapInteractionKind.Battle));
            Assert.False(rules.MarkerVisible(p, 0, MapInteractionKind.Battle));
        }
        [Test] public void DefeatCancelAndLowerReplayNeverRemoveStarsOrUnlockNextArea()
        {
            var rules = Rules; var p = new ChapterProgress(); rules.AcceptQuest(p, 0);
            rules.BeginBattle(p, 0, "lose"); Assert.True(rules.ApplyResult(p, "one", "lose", false, 0));
            Assert.AreEqual(1, rules.UnlockedCount(p));
            rules.BeginBattle(p, 0, "cancel"); rules.CancelBattle(p);
            Assert.False(rules.ApplyResult(p, "one", "cancel", true, 3));
            rules.BeginBattle(p, 0, "win"); rules.ApplyResult(p, "one", "win", true, 3);
            rules.BeginBattle(p, 0, "replay"); rules.ApplyResult(p, "one", "replay", true, 1);
            Assert.AreEqual(3, rules.TotalStars(p)); Assert.AreEqual(2, rules.UnlockedCount(p));
        }
        [Test] public void UnknownAndOutOfOrderSaveRecordsCannotInflateRewards()
        {
            var rules = Rules; var p = new ChapterProgress(); p.Get("three").stars = 3; p.Get("unknown").stars = 999;
            Assert.AreEqual(0, rules.TotalStars(p)); Assert.AreEqual(ChapterStageState.Locked, rules.State(p, 2));
            Assert.False(rules.Claim(p, "chest", 1));
            p.Get("one").stars = 3;
            Assert.True(rules.Claim(p, "chest", 3)); Assert.False(rules.Claim(p, "chest", 3));
        }
        [Test] public void ProfileRoundTripKeepsPerServerProgressAndReturnPose()
        {
            var profile = new PlayerProfile(); var a = profile.GetOrCreateCharacter("a");
            var p = a.GetChapter("chapter01"); p.Get("one").stars = 2; p.hasReturnPose = true; p.playerX = 12.5f; p.playerYaw = 73;
            p.claimedRewards.Add("chest");
            var restored = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            var loaded = restored.FindCharacter("a").GetChapter("chapter01");
            Assert.AreEqual(2, loaded.Find("one").stars); Assert.AreEqual(12.5f, loaded.playerX); Assert.AreEqual(73, loaded.playerYaw);
            Assert.True(loaded.hasReturnPose); Assert.Contains("chest", loaded.claimedRewards);
            Assert.AreEqual(0, restored.GetOrCreateCharacter("b").GetChapter("chapter01").stages.Count);
            var legacy = JsonUtility.FromJson<PlayerProfile>("{\"characters\":[{\"serverId\":\"old\"}]}");
            Assert.NotNull(legacy.FindCharacter("old").GetChapter("chapter01"));
        }
    }
}

using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    public enum ChapterStageState { Locked, Available, InProgress, Completed }
    public enum MapInteractionKind { Quest, Battle }

    [Serializable]
    public sealed class ChapterStageProgress
    {
        public string stageId = "";
        public bool questAccepted;
        public int stars;
    }

    [Serializable]
    public sealed class ChapterProgress
    {
        public string chapterId = "";
        public List<ChapterStageProgress> stages = new List<ChapterStageProgress>();
        public List<string> claimedRewards = new List<string>();
        public string pendingBattleId = "", pendingStageId = "";
        public int revealedStages = 1;
        public bool hasReturnPose;
        public float playerX, playerY, playerZ, playerYaw, cameraX, cameraY, cameraZ, cameraSize;

        public ChapterStageProgress Find(string id) => stages?.Find(s => s != null && s.stageId == id);
        public ChapterStageProgress Get(string id)
        {
            stages ??= new List<ChapterStageProgress>();
            var found = Find(id);
            if (found != null) return found;
            found = new ChapterStageProgress { stageId = id }; stages.Add(found); return found;
        }
    }

    /// <summary>Pure sequential progression; no frame clock, Unity objects or storage side effects.</summary>
    public sealed class ChapterProgressRules
    {
        private readonly string[] _ids;
        private readonly bool[] _requiresQuest;
        public ChapterProgressRules(string[] stageIds, bool[] requiresQuest)
        {
            if (stageIds == null || stageIds.Length == 0 || requiresQuest == null || requiresQuest.Length != stageIds.Length)
                throw new ArgumentException("Chapter stages and quest flags must match.");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in stageIds) if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) throw new ArgumentException("Stage ids must be nonempty and unique.");
            _ids = (string[])stageIds.Clone(); _requiresQuest = (bool[])requiresQuest.Clone();
        }
        public int StageCount => _ids.Length;
        public int IndexOf(string id) => Array.IndexOf(_ids, id);
        public ChapterStageState State(ChapterProgress progress, int index)
        {
            if (progress == null || index < 0 || index >= StageCount) return ChapterStageState.Locked;
            // A malformed save cannot unlock later regions by injecting an isolated star record.
            for (int i = 0; i < index; i++) if ((progress.Find(_ids[i])?.stars ?? 0) <= 0) return ChapterStageState.Locked;
            var record = progress.Find(_ids[index]);
            if ((record?.stars ?? 0) > 0) return ChapterStageState.Completed;
            return record != null && record.questAccepted ? ChapterStageState.InProgress : ChapterStageState.Available;
        }
        public int UnlockedCount(ChapterProgress progress)
        {
            int count = 0; for (int i = 0; i < StageCount; i++) if (State(progress, i) != ChapterStageState.Locked) count++;
            return count;
        }
        public bool MarkerVisible(ChapterProgress progress, int index, MapInteractionKind kind)
        {
            var state = State(progress, index);
            if (state == ChapterStageState.Locked || state == ChapterStageState.Completed) return false;
            return kind == MapInteractionKind.Quest ? _requiresQuest[index] && state == ChapterStageState.Available
                : state == ChapterStageState.InProgress || !_requiresQuest[index];
        }
        public bool AcceptQuest(ChapterProgress progress, int index)
        {
            if (!MarkerVisible(progress, index, MapInteractionKind.Quest)) return false;
            progress.Get(_ids[index]).questAccepted = true; return true;
        }
        public bool BeginBattle(ChapterProgress progress, int index, string attemptId)
        {
            if (string.IsNullOrWhiteSpace(attemptId) || !string.IsNullOrEmpty(progress?.pendingBattleId)) return false;
            var state = State(progress, index);
            if (state == ChapterStageState.Locked || (state == ChapterStageState.Available && _requiresQuest[index])) return false;
            progress.pendingStageId = _ids[index]; progress.pendingBattleId = attemptId; return true;
        }
        public bool ApplyResult(ChapterProgress progress, string stageId, string attemptId, bool victory, int stars)
        {
            int index = IndexOf(stageId);
            if (progress == null || index < 0 || string.IsNullOrEmpty(attemptId) || progress.pendingBattleId != attemptId || progress.pendingStageId != stageId
                || State(progress, index) == ChapterStageState.Locked || (victory ? stars < 1 || stars > StarRating.MaxStars : stars != 0)) return false;
            progress.pendingBattleId = ""; progress.pendingStageId = "";
            if (victory) progress.Get(stageId).stars = Math.Max(progress.Get(stageId).stars, stars);
            return true;
        }
        public void CancelBattle(ChapterProgress progress)
        {
            if (progress == null) return;
            progress.pendingBattleId = ""; progress.pendingStageId = "";
        }
        public int TotalStars(ChapterProgress progress)
        {
            int sum = 0;
            for (int i = 0; i < StageCount; i++) if (State(progress, i) == ChapterStageState.Completed)
                sum += Math.Max(0, Math.Min(StarRating.MaxStars, progress.Find(_ids[i]).stars));
            return sum;
        }
        public bool CanClaim(ChapterProgress progress, string rewardId, int requiredStars) => progress != null
            && !string.IsNullOrWhiteSpace(rewardId) && requiredStars > 0 && TotalStars(progress) >= requiredStars
            && !(progress.claimedRewards?.Contains(rewardId) ?? false);
        public bool Claim(ChapterProgress progress, string rewardId, int requiredStars)
        {
            if (!CanClaim(progress, rewardId, requiredStars)) return false;
            progress.claimedRewards ??= new List<string>(); progress.claimedRewards.Add(rewardId); return true;
        }
    }
}

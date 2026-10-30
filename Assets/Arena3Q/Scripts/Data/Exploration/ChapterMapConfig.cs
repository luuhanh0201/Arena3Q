using System;
using System.Collections.Generic;
using System.Linq;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Exploration/Chapter Map")]
    public sealed class ChapterMapConfig : ScriptableObject
    {
        public string chapterId = "chapter01", title = "Chương 1", subtitle = "Loạn Khăn Vàng";
        [TextArea] public string introduction;
        public string sceneName = "CH01_Exploration";
        public FrontendConfig frontend;
        public MapUiConfig ui;
        public MapMotionConfig motion;
        public ChapterMapStage[] stages = Array.Empty<ChapterMapStage>();
        public ChapterMapReward[] rewards = Array.Empty<ChapterMapReward>();
        public Vector3[] roadRoute = Array.Empty<Vector3>();
        public Rect groundBounds = new Rect(-70, -45, 140, 90);
        public Vector3 initialPosition = new Vector3(-48, 0, -28);
        public float interactionRadius = 1.8f, walkSpeed = 4f, agentRadius = .35f, agentHeight = 1.8f;
        public float stuckSeconds = 4f, minimumProgress = .12f, pathSampleDistance = 3f;
        public float cameraTilt = 50, cameraYaw = 45, cameraMinSize = 12, cameraMaxSize = 38, cameraSize = 22;
        public float cameraFollowSpeed = 4, wheelZoomSpeed = 2, dragThresholdDp = 12;
        public float cameraGroundMargin = 55, navigationCellSize = 4;
        [Tooltip("Map objects the walker may stand on besides the terrain (name contains one of these); every other building blocks movement.")]
        public string[] walkableNames = { "bridge", "plateau", "stairs" };
        public UnityEngine.AI.NavMeshData[] navigationByUnlockedStage = Array.Empty<UnityEngine.AI.NavMeshData>();
        public ChapterProgressRules CreateRules() => new ChapterProgressRules(stages.Select(s => s.id).ToArray(), stages.Select(s => s.targets.Any(t => t.kind == MapInteractionKind.Quest)).ToArray());
        public void Validate(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(chapterId) || string.IsNullOrWhiteSpace(sceneName)) errors.Add("Chapter identifiers are required.");
            if (frontend == null || ui == null || motion == null) errors.Add("Chapter requires frontend, UI and motion configs.");
            if (motion != null) motion.Validate(errors);
            if (stages == null || stages.Length == 0) { errors.Add("Chapter requires stages."); return; }
            if (stages.Length > 16) errors.Add("Chapter fog supports at most 16 regions.");
            var ids = new HashSet<string>();
            foreach (var stage in stages)
            {
                if (stage == null || string.IsNullOrWhiteSpace(stage.id) || !ids.Add(stage.id)) { errors.Add("Invalid or duplicate chapter stage."); continue; }
                if (string.IsNullOrWhiteSpace(stage.areaPath) || string.IsNullOrWhiteSpace(stage.markerPath) || stage.regionRadius <= 0) errors.Add("Stage requires region/marker bindings: " + stage.id);
                if (stage.targets == null || stage.targets.Length == 0) errors.Add("Stage requires interaction targets: " + stage.id);
                else foreach (var target in stage.targets) if (target == null || string.IsNullOrWhiteSpace(target.anchorPath)) errors.Add("Missing NPC anchor: " + stage.id);
                // Missing real battle content is allowed, but runtime must show an unavailable message, never simulate a win.
            }
            if (interactionRadius <= agentRadius || walkSpeed <= 0 || cameraMinSize <= 0 || cameraMaxSize < cameraMinSize) errors.Add("Invalid navigation/camera settings.");
            if (agentRadius <= 0 || agentHeight <= 0 || stuckSeconds <= 0 || minimumProgress <= 0 || pathSampleDistance <= 0 || navigationCellSize <= 0)
                errors.Add("Navigation sizes and timeout settings must be positive.");
            if (walkableNames == null || walkableNames.Any(string.IsNullOrWhiteSpace))
                errors.Add("Walkable names must not contain empty entries (an empty name matches every building).");
            var rewardIds = new HashSet<string>();
            foreach (var reward in rewards) if (reward == null || string.IsNullOrWhiteSpace(reward.id) || !rewardIds.Add(reward.id) || reward.stars <= 0 || reward.stars > stages.Length * StarRating.MaxStars) errors.Add("Invalid chapter reward.");
        }
    }
    [Serializable] public sealed class ChapterMapStage
    {
        public string id, areaName, areaPath, markerPath, bossName, objective;
        public bool boss;
        public bool useAltarBanner;
        [TextArea] public string rewardPreview = "Phần thưởng sẽ được công bố sau.";
        public Vector3 center;
        public float regionRadius = 16;
        public StageConfig battle;
        public ChapterMapTarget[] targets = Array.Empty<ChapterMapTarget>();
    }
    [Serializable] public sealed class ChapterMapTarget
    {
        public string id, name, anchorPath;
        public Vector3 position;
        [TextArea] public string dialogue;
        public MapInteractionKind kind;
    }
    [Serializable] public sealed class ChapterMapReward
    {
        public string id, label;
        public int stars;
        public ResourceBalance[] contents = Array.Empty<ResourceBalance>();
    }
}

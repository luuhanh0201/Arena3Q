using System;
using UnityEngine;

namespace Arena3Q.Data
{
    public enum HeroAnimationState { Idle, Walk, Run, Attack01, Attack02, Victory, Defeat }

    [CreateAssetMenu(menuName = "Arena3Q/Hero Animation Set")]
    public sealed class HeroAnimationSet : ScriptableObject
    {
        [Header("Facing and locomotion")]
        [SerializeField, Range(4, 8)] private int directionCount = 4;
        [SerializeField, Min(1f)] private float turnDegreesPerSecond = 720f;
        [SerializeField, Range(0f, 20f)] private float directionHysteresisDegrees = 8f;
        [SerializeField, Min(0f)] private float motionStopGraceSeconds = 0.09f;
        [SerializeField, Min(0.01f)] private float runStrideLength = 0.9f;
        [SerializeField, Min(0.01f)] private float walkStrideLength = 0.55f;
        [SerializeField, Range(0.1f, 0.9f)] private float walkSpeedRatio = 0.5f;
        [SerializeField, Min(0f)] private float positionSmoothSeconds = 0.045f;
        [SerializeField, Min(0f)] private float maxVisualOffset = 0.15f;
        [SerializeField, Range(0f, 0.2f)] private float transitionSeconds = 0.07f;
        public int DirectionCount => directionCount;
        public float TurnDegreesPerSecond => turnDegreesPerSecond;
        public float DirectionHysteresisDegrees => directionHysteresisDegrees;
        public float MotionStopGraceSeconds => motionStopGraceSeconds;
        public float RunStrideLength => runStrideLength;
        public float WalkStrideLength => walkStrideLength;
        public float WalkSpeedRatio => walkSpeedRatio;
        public float PositionSmoothSeconds => positionSmoothSeconds;
        public float MaxVisualOffset => maxVisualOffset;
        public float TransitionSeconds => transitionSeconds;
        [Serializable]
        public sealed class Clip
        {
            public HeroAnimationState state;
            [Min(1)] public int framesPerDirection = 1;
            [Min(0.01f)] public float duration = 1f;
            public bool loop;
            // Legacy four directions: SE, NE, NW, SW. Eight directions: E, NE, N, NW, W, SW, S, SE.
            public Sprite[] frames;

            public Sprite Frame(int direction, float seconds)
            {
                if (frames == null || frames.Length == 0) return null;
                var count = Mathf.Max(1, framesPerDirection);
                var progress = seconds / Mathf.Max(0.01f, duration);
                progress = loop ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);
                var index = Mathf.Min(count - 1, Mathf.FloorToInt(progress * (loop ? count : count - 1)));
                return frames[Mathf.Clamp(direction * count + index, 0, frames.Length - 1)];
            }
        }

        [SerializeField] private Clip[] clips;
        public Clip Find(HeroAnimationState state) =>
            clips == null ? null : Array.Find(clips, clip => clip != null && clip.state == state);
    }
}

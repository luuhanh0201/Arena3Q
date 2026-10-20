using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// A hero's 3D model for the 3D battle (docs/KeHoach/ChuyenSang3D.md P3): the skinned model, its clip per animation
    /// state and per skill, facing and size. Pictures only; the battle rules never read it.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Hero Model 3D")]
    public sealed class HeroModel3D : ScriptableObject
    {
        [Serializable]
        public sealed class StateClip
        {
            public HeroAnimationState state;
            public AnimationClip clip;
        }

        [Serializable]
        public sealed class SkillClip
        {
            public SkillSlot slot;
            public AnimationClip clip;
            [Tooltip("Seconds into the clip where the skill takes effect (its hit / impact frame); the clip is played " +
                     "at the speed that puts this moment on the skill's cast time.")]
            [Min(0f)] public float effectSeconds;
        }

        [SerializeField] private GameObject prefab;
        [SerializeField] private StateClip[] clips = Array.Empty<StateClip>();
        [SerializeField] private SkillClip[] skillClips = Array.Empty<SkillClip>();
        [Tooltip("Uniform scale of the model in the battle (1 = the model's metres).")]
        [SerializeField, Min(0.01f)] private float scale = 1f;
        [Tooltip("Yaw (degrees about the model's up axis) that makes the model face +Z (north) at facing 90°.")]
        [SerializeField] private float yawOffset;
        [Tooltip("+1 or -1: which way the model turns for growing facing angles.")]
        [SerializeField] private float yawSign = -1f;
        [SerializeField, Min(0f)] private float transitionSeconds = 0.12f;
        [Header("Weapon drop when the hero falls (Defeat)")]
        [Tooltip("Name of the weapon object in the model; empty = it stays in the hand.")]
        [SerializeField] private string weaponObject = "";
        [SerializeField, Min(0f)] private float weaponDropSeconds = 1.2f;
        [Header("Shadow")]
        [SerializeField, Min(0f)] private float shadowDiameter = 1.1f;

        public GameObject Prefab => prefab;
        public float Scale => scale;
        public float YawOffset => yawOffset;
        public float YawSign => yawSign;
        public float TransitionSeconds => transitionSeconds;
        public string WeaponObject => weaponObject;
        public float WeaponDropSeconds => weaponDropSeconds;
        public float ShadowDiameter => shadowDiameter;
        public IReadOnlyList<StateClip> Clips => clips;
        public IReadOnlyList<SkillClip> SkillClips => skillClips;

        public AnimationClip Find(HeroAnimationState state)
        {
            foreach (var entry in clips)
                if (entry != null && entry.state == state && entry.clip != null)
                    return entry.clip;
            return null;
        }

        public SkillClip Find(SkillSlot slot)
        {
            foreach (var entry in skillClips)
                if (entry != null && entry.slot == slot && entry.clip != null)
                    return entry;
            return null;
        }

        public void Validate(List<string> errors)
        {
            if (prefab == null) errors.Add($"Hero model '{name}': no prefab.");
            foreach (HeroAnimationState state in Enum.GetValues(typeof(HeroAnimationState)))
                if (Find(state) == null) errors.Add($"Hero model '{name}': no clip for {state}.");
        }
    }
}

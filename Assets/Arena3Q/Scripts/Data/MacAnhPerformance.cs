using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    public enum MacAnhAction { Idle, Walk, Run, Dodge, Attack01, Attack02, Skill01, Skill02, Skill03, Ultimate, DodgeAttack }
    public enum MacAnhSocket { FanTip, LeftHand, Cast }

    [CreateAssetMenu(menuName = "Arena3Q/Mac Anh Performance")]
    public sealed class MacAnhPerformance : ScriptableObject
    {
        [Serializable] public sealed class EventMark
        {
            public string key;
            public int frame;
            public int comboIndex;
            public float Seconds => frame / 30f;
        }
        [Serializable] public struct SocketFrame
        {
            public Vector2 fanTip, leftHand, cast;
            public Vector2 Get(MacAnhSocket socket) => socket == MacAnhSocket.FanTip ? fanTip :
                socket == MacAnhSocket.LeftHand ? leftHand : cast;
        }
        [Serializable] public sealed class Clip
        {
            public MacAnhAction action;
            public float duration;
            public bool loop;
            public int framesPerDirection;
            public Sprite[] frames;
            public SocketFrame[] sockets;
            public EventMark[] events = Array.Empty<EventMark>();
            public int Index(int direction, float seconds)
            {
                var phase = seconds / Mathf.Max(.01f, duration);
                phase = loop ? Mathf.Repeat(phase, 1f) : Mathf.Clamp01(phase);
                return direction * framesPerDirection + Mathf.Min(framesPerDirection - 1,
                    Mathf.FloorToInt(phase * (loop ? framesPerDirection : framesPerDirection - 1)));
            }
        }
        [Serializable] public sealed class SkillBinding
        {
            public SkillDefinition skill;
            public MacAnhAction action;
            public GameObject vfx;
        }
        [Serializable] public sealed class EffectPrefab
        {
            public string key;
            public GameObject prefab;
        }
        [SerializeField] private int directionCount = 8;
        [SerializeField] private Clip[] clips = Array.Empty<Clip>();
        [SerializeField] private string spriteLibraryResource;
        private MacAnhSpriteLibrary _spriteLibrary;
        [SerializeField] private SkillBinding[] skills = Array.Empty<SkillBinding>();
        [SerializeField] private EffectPrefab[] effects = Array.Empty<EffectPrefab>();
        [Tooltip("Store effects played instead of the effect with the same key (Hovl Studio's Magic circle 2 for " +
                 "MagicArray, ...). They live in Assets/ThirdParty, which is not in the public repository: where they " +
                 "are missing, the key's own effect plays.")]
        [SerializeField] private EffectPrefab[] thirdPartyEffects = Array.Empty<EffectPrefab>();
        [SerializeField] private Material spriteMaterial;
        [Tooltip("Experiment approved 2026-10-20; keep off until the user chooses the rendering route.")]
        [SerializeField] private bool enableBattle3DTrial;
        [SerializeField] private GameObject battle3DViewPrefab;
        [SerializeField] private string battle3DViewResource;
        private GameObject _loadedBattle3DView;
        [SerializeField, Range(0, 2)] private int effectQuality = 1;
        [SerializeField, Min(.1f)] private float projectileSpeed = 8f;
        [SerializeField, Min(.01f)] private float projectileRadius = .12f;
        [SerializeField, Min(.1f)] private float projectileLifetime = 2f;
        [SerializeField, Min(.1f)] private float projectileVisualScale = 1.15f;
        [SerializeField, Min(1f)] private float finalFeatherVisualScale = 1.18f;
        [Tooltip("Size of the basic attack's wind blade and its hit flash next to the skill projectiles (user 2026-10-21: half).")]
        [SerializeField, Range(.1f, 2f)] private float basicAttackVisualScale = .5f;
        [Tooltip("Size of the hit flash where a projectile lands.")]
        [SerializeField, Min(.05f)] private float impactVisualScale = .5f;
        [SerializeField, Min(0f)] private float dodgeBackstepDistance = .6f;
        [SerializeField, Min(.01f)] private float walkStrideLength = .3387097f;
        [SerializeField, Min(.01f)] private float runStrideLength = .9069767f;
        [SerializeField, Min(0f)] private float visualSmoothSeconds = .045f;
        [SerializeField, Min(0f)] private float maxVisualOffset = .15f;
        [SerializeField, Min(0f)] private float transitionSeconds = .07f;
        [Header("Flipbook extras (spec §12 f31–45, §13 f8–13)")]
        [Tooltip("Flipbook material for a glowing violet copy of the DodgeAttack afterimage; empty = plain tinted ghost.")]
        [SerializeField] private Material afterimageGlowMaterial;
        [SerializeField, Range(0f, 4f)] private float afterimageGlow = .9f;
        [SerializeField] private Color afterimageGlowTint = new Color(.75f, .5f, 1f, 1f);
        [Tooltip("Seconds into the Ultimate cast when the rune circle behind Mặc Ảnh appears (frame 31 = 1.03 s).")]
        [SerializeField, Min(0f)] private float ultimateAuraStart = 31f / 30f;
        [Tooltip("Seconds into DodgeAttack when the dash streak is spawned (frame 8).")]
        [SerializeField, Min(0f)] private float dodgeStreakStart = 8f / 30f;
        [Tooltip("Seconds the Ultimate's rune circle stays on the ground after the release (it spreads, holds, fades).")]
        [SerializeField, Min(.6f)] private float ultimateCircleSeconds = 2.5f;
        [Tooltip("Size of the purple streaks around each hero the Ultimate buffs (effect radius in metres).")]
        [SerializeField, Min(.1f)] private float ultimateBuffAuraRadius = .8f;
        public float UltimateCircleSeconds => ultimateCircleSeconds;
        public float UltimateBuffAuraRadius => ultimateBuffAuraRadius;
        public int DirectionCount => directionCount;
        public Material SpriteMaterial => spriteMaterial;
        public Material AfterimageGlowMaterial => afterimageGlowMaterial;
        public float AfterimageGlow => afterimageGlow;
        public Color AfterimageGlowTint => afterimageGlowTint;
        public float UltimateAuraStart => ultimateAuraStart;
        public float DodgeStreakStart => dodgeStreakStart;
        public bool EnableBattle3DTrial => enableBattle3DTrial;
        /// <summary>Session-only selection; saving the default remains an Editor data decision.</summary>
        public void SetBattle3DTrial(bool enabled) => enableBattle3DTrial=enabled;
        public GameObject Battle3DViewPrefab
        {
            get
            {
                if(battle3DViewPrefab!=null)return battle3DViewPrefab;
                if(_loadedBattle3DView==null&&!string.IsNullOrEmpty(battle3DViewResource))
                    _loadedBattle3DView=Resources.Load<GameObject>(battle3DViewResource);
                return _loadedBattle3DView;
            }
        }
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileRadius => projectileRadius;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileVisualScale => projectileVisualScale;
        public float FinalFeatherVisualScale => finalFeatherVisualScale;
        public float BasicAttackVisualScale => basicAttackVisualScale;
        public float ImpactVisualScale => impactVisualScale;
        public float DodgeBackstepDistance => dodgeBackstepDistance;
        public float WalkStrideLength => walkStrideLength;
        public float RunStrideLength => runStrideLength;
        public float VisualSmoothSeconds => visualSmoothSeconds;
        public float MaxVisualOffset => maxVisualOffset;
        public float TransitionSeconds => transitionSeconds;
        public int EffectQuality => effectQuality;
        public IReadOnlyList<Clip> Clips => enableBattle3DTrial?clips:SpriteClips();
        public IReadOnlyList<Clip> MetadataClips=>clips;
        public IReadOnlyList<SkillBinding> Skills => skills;
        private IReadOnlyList<Clip> SpriteClips()
        {
            if(string.IsNullOrEmpty(spriteLibraryResource))return clips;
            if(_spriteLibrary==null)_spriteLibrary=Resources.Load<MacAnhSpriteLibrary>(spriteLibraryResource);
            return _spriteLibrary!=null?_spriteLibrary.Clips:clips;
        }
        private static Clip FindIn(IReadOnlyList<Clip> values,MacAnhAction action)
        {for(int i=0;i<values.Count;i++)if(values[i]!=null&&values[i].action==action)return values[i];return null;}
        public Clip Find(MacAnhAction action)=>FindIn(Clips,action);
        public Clip FindSprite(MacAnhAction action)=>FindIn(SpriteClips(),action);
        public void ConfigureSpriteStorage(string resource,Clip[] metadata)
        {spriteLibraryResource=resource;clips=metadata;_spriteLibrary=null;}
        public SkillBinding Find(SkillDefinition skill) => Array.Find(skills, b => b != null && b.skill == skill);
        public GameObject Effect(string key)
        {
            var store = Find(thirdPartyEffects, key);
            return store != null ? store : Find(effects, key);
        }

        /// <summary>The key's own effect, ignoring a store effect that stands in for it (checks of the own effects).</summary>
        public GameObject OwnEffect(string key) => Find(effects, key);

        // Unity's null: a reference to a missing asset (a store effect not on this machine) reads as null.
        private static GameObject Find(EffectPrefab[] list, string key)
        {
            var prefab = Array.Find(list, b => b != null && b.key == key && b.prefab != null)?.prefab;
            return prefab != null ? prefab : null;
        }
    }
}

using System;
using UnityEngine;

namespace Arena3Q.Data
{
    public enum MacAnhVfxPlane
    {
        /// <summary>Rendered from the sprite camera (3/4 view), drawn upright; carries its own depth.</summary>
        Upright,
        /// <summary>Rendered top-down; spins in the ground plane and is squashed by the battle's ground projection.</summary>
        Ground,
    }

    public enum MacAnhVfxPhase
    {
        /// <summary>Plays in the instance that delivers the effect (and in plain effects such as projectiles).</summary>
        Release,
        /// <summary>Plays only in the charge instance that runs before the event (MagicArray / Ultimate wind-up).</summary>
        Charge,
    }

    /// <summary>
    /// One Mặc Ảnh battle effect as layers of pre-rendered frames (from Blender, see vfx_macanh_flipbooks.py).
    /// All timing is in seconds from the start of the instance; a negative end counts back from the instance's
    /// duration, so the same recipe fits any zone length the skill data asks for.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Mac Anh VFX Recipe")]
    public sealed class MacAnhVfxRecipe : ScriptableObject
    {
        [Serializable]
        public sealed class Layer
        {
            public string name = "Layer";
            public Sprite[] frames = Array.Empty<Sprite>();
            [Min(1f)] public float framesPerSecond = 24f;
            [Tooltip("Repeat the frames while the layer lives; otherwise hold the last frame.")]
            public bool loop;
            public MacAnhVfxPlane plane = MacAnhVfxPlane.Upright;
            public MacAnhVfxPhase phase = MacAnhVfxPhase.Release;
            [Tooltip("Seconds after the start.")] public float start;
            [Tooltip("Seconds after the start; 0 or less = that many seconds before the end of the instance.")]
            public float end;
            [Min(0f)] public float fadeIn = .08f;
            [Min(0f)] public float fadeOut = .2f;
            public float scaleFrom = 1f, scaleTo = 1f;
            [Tooltip("Ground layers only: spin in the ground plane, degrees per second (sign = direction).")]
            public float spinDegreesPerSecond;
            [Tooltip("Upright layers laid out on the ground (pillars around a ring): stretch vertically so their 3/4-view " +
                     "footprint matches the battle's ground projection.")]
            public bool stretchToGround;
            [Tooltip("Height above the anchor in metres (before the effect scale).")]
            public float height;
            [Range(0f, 4f)] public float glow = 1f;
            [Range(0f, 1f)] public float underlay = .6f;
            public Color underlayColour = new Color(.2f, .08f, .38f, 1f);
            public Color tint = Color.white;
            public int sortingOffset;
            [Tooltip("Skipped at the Low quality level.")] public bool highQualityOnly;
        }

        [Tooltip("Ground radius the frames were authored for; the battle radius divided by this scales the effect.")]
        [Min(.01f)] public float referenceRadius = 1f;
        [Tooltip("Ground projection of the camera the upright frames were rendered with (the hero sprites use 0.75).")]
        [Range(.05f, 1f)] public float renderProjection = .75f;
        public Layer[] layers = Array.Empty<Layer>();

        [Header("Screen dim (cinematic)")]
        [Tooltip("Darkens the whole battlefield behind the effect while it plays, so the light reads like the concept " +
                 "art on a dark ground; units, decorations and effects stay above it (only the ground darkens). 0 = off.")]
        [Range(0f, 1f)] public float screenDim;
        public Color dimColour = new Color(.06f, .02f, .12f, 1f);
        [Min(0f)] public float dimFadeIn = .25f;
        [Min(0f)] public float dimFadeOut = .4f;

        public float LayerEnd(Layer layer, float duration) => layer.end > 0f ? Mathf.Min(layer.end, duration) : duration + layer.end;
    }
}

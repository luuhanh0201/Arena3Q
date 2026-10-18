using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>
    /// Settings for rendering 3D prop models (e.g. Meshy FBX) into 2D sprites in the fixed 3/4 art view
    /// (docs/BanDo/MapSpecification.md §8.3). Used by <see cref="PropSpriteRenderer"/> (Arena3Q > Render Prop Sprites).
    /// Editor-only: the models never go into a build, only the sprites do.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Prop Sprite Render Config", fileName = "PropSprites")]
    public sealed class PropSpriteRenderConfig : ScriptableObject
    {
        public enum SizeAxis
        {
            [Tooltip("The larger of the model's width and depth at yaw 0.")]
            Footprint,
            [Tooltip("The model's height (for tall, thin props such as flagpoles).")]
            Height,
        }

        [Serializable]
        public sealed class Prop
        {
            [Tooltip("Output file name (without extension); a yaw suffix is added when there are several yaws.")]
            public string name;
            public GameObject model;
            public Texture2D baseColor;
            [Tooltip("Tangent-space normal map imported as a plain linear texture (not the NormalMap type).")]
            public Texture2D normalMap;
            [Tooltip("Which model dimension 'size' sets.")]
            public SizeAxis sizeAxis = SizeAxis.Footprint;
            [Tooltip("Logic units (map spec §8.5).")]
            [Min(0.05f)] public float size = 1f;
            [Tooltip("Fixes the model's own orientation (Euler degrees) before sizing, e.g. a model that lies on its side.")]
            public Vector3 rotation;
            [Tooltip("Rotations around the vertical axis (degrees, Unity Y / layout rotY); one sprite per value.")]
            public List<float> yaws = new() { 0f };
        }

        [Tooltip("Folder (under Assets/) the sprites are written to.")]
        public string outputFolder = "Assets/Arena3Q/Art/Props";

        [Header("View (map spec §8.2, §8.3)")]
        [Tooltip("On-screen vertical scale of the ground in the 3/4 view (0.75 = 41.4° tilt).")]
        [Range(0.3f, 1f)] public float verticalScale = 0.75f;
        [Min(16)] public int pixelsPerUnit = 128;
        [Tooltip("Render this many times larger, then average down (anti-aliasing).")]
        [Range(1, 4)] public int supersampling = 3;
        [Tooltip("Empty border around each sprite, logic units.")]
        [Min(0f)] public float padding = 0.06f;
        [Min(256)] public int maxTextureSize = 2048;

        [Header("Light")]
        [Tooltip("Direction the light comes from, around the vertical axis (0 = from the back, -90 = from the left, -180 = from the camera).")]
        public float lightAzimuth = -50f;
        [Range(5f, 90f)] public float lightElevation = 55f;
        [Range(0f, 2f)] public float ambient = 0.62f;
        [Range(0f, 2f)] public float diffuse = 0.5f;
        [Tooltip("Wraps the light around the shape for a softer, painted look (0 = plain Lambert).")]
        [Range(0f, 1f)] public float lightWrap = 0.35f;

        [Header("Ground shadow")]
        public bool castShadow = true;
        public Color shadowColor = new(0.10f, 0.07f, 0.04f, 0.38f);
        [Tooltip("Blur radius of the shadow edge, logic units.")]
        [Min(0f)] public float shadowBlur = 0.08f;

        public List<Prop> props = new();
    }
}

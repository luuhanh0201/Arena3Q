using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Exploration/Map Motion")]
    public sealed class MapMotionConfig : ScriptableObject
    {
        public float markerBobPixels = 6, markerBobSeconds = 2.4f, markerPulseSeconds = 2, markerFadeIn = .3f, markerFadeOut = .25f;
        public float cameraRevealSeconds = 1.2f, fogRevealSeconds = 2.8f, environmentFadeSeconds = .6f;
        public float fogHeight = 4, fogOpacity = .9f, fogDrift = .25f, fogEdge = .28f;
        public Texture2D fogTexture;
        public Material fogMaterial;
        public Color fogColor = new Color(.75f, .79f, .78f, 1);
        public float fogTextureScale = .025f, fogBoundaryWidth = 6, fogDarkening = .2f;
        public float fogRegionRadius = 40, fogRevealEdge = .08f;
        public int fogNoiseSize = 128, fogNoiseCells = 4, fogNoiseOctaves = 4, fogNoiseSeed = 731;
        public float windStrength = .35f, gustSeconds = 8, waterSpeed = .06f, waterSecondSpeed = .03f;
        public bool environmentMotion;
        public BattlefieldConfig battlefieldWind;
        public float lowWindMultiplier = .45f, lowBambooMultiplier = .5f, waterSecondBlend = .18f;
        public Vector2 waterSecondDirection = new Vector2(.3f,-1);
        public Material waterSource, waterMotionMaterial, bambooSource, bambooMotionMaterial;
        public float leafLifetime = 5, dustInterval = 8, dustAlpha = .15f, birdInterval = 35, cloudShadowAlpha = .07f;
        public float flagAmplitude = .04f, flagSeconds = 2, bambooAmplitude = .1f, bambooSeconds = 4.2f;
        public int[] frameRates = { 30, 60, 120 };
        public int[] leavesPerArea = { 1, 3, 3 };
        public int[] fogLayers = { 2, 3, 3 };
        public int maxAudioSources = 4;
        public float routeDashesPerSecond = .5f;
        public void Validate(System.Collections.Generic.List<string> errors)
        {
            if (cameraRevealSeconds <= 0 || fogRevealSeconds <= 0 || environmentFadeSeconds <= 0
                || fogBoundaryWidth <= 0 || fogTextureScale <= 0 || markerFadeIn <= 0 || markerFadeOut <= 0
                || fogRegionRadius <= 0 || fogRevealEdge <= 0 || fogNoiseSize < 16 || fogNoiseCells < 1 || fogNoiseOctaves < 1)
                errors.Add("Map presentation durations and fog dimensions must be positive.");
            if (frameRates == null || frameRates.Length != 3 || fogLayers == null || fogLayers.Length != 3
                || leavesPerArea == null || leavesPerArea.Length != 3)
                errors.Add("Map requires three display tiers.");
            if(environmentMotion&&(battlefieldWind==null||waterSource==null||waterMotionMaterial==null||bambooSource==null||bambooMotionMaterial==null))
                errors.Add("Environment motion requires wind configuration and source/replacement materials.");
            if(gustSeconds<=0||bambooSeconds<=0||bambooAmplitude<0||lowWindMultiplier<0||lowBambooMultiplier<0||waterSecondBlend<0||waterSecondBlend>1)
                errors.Add("Environment motion periods and strengths are invalid.");
        }
    }
}

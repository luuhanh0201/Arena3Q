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
        public float windStrength = .35f, gustSeconds = 8, waterSpeed = .06f, waterSecondSpeed = .03f;
        public float leafLifetime = 5, dustInterval = 8, dustAlpha = .15f, birdInterval = 35, cloudShadowAlpha = .07f;
        public float flagAmplitude = .04f, flagSeconds = 2, bambooAmplitude = .1f, bambooSeconds = 4.2f;
        public int[] frameRates = { 30, 60, 120 };
        public int[] leavesPerArea = { 1, 3, 3 };
        public int[] fogLayers = { 2, 3, 3 };
        public int maxAudioSources = 4;
        public float routeDashesPerSecond = .5f;
    }
}

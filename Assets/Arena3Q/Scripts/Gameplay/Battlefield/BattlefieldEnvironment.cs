using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Gameplay
{
    public enum BattlefieldGraphics { Low, Medium, High }

    [ExecuteAlways]
    public sealed class BattlefieldEnvironment : MonoBehaviour
    {
        public BattlefieldConfig config;
        public BattlefieldAssetLibrary assets;
        public Camera battleCamera;
        public Light sun;
        public Volume volume;
        public BattlefieldGraphics quality = BattlefieldGraphics.High;
        public bool animateInEditor = true;
        public bool autoFitCamera = true;
        public Transform grassRoot;
        public Transform detailRoot;
        public ParticleSystem[] particles;
        private float _clock;
        private bool _playing;
        private static readonly int WindId = Shader.PropertyToID("_ArenaWind");
        private static readonly int TimeId = Shader.PropertyToID("_ArenaWindTime");
        private static readonly int NoiseId = Shader.PropertyToID("_ArenaWindNoise");

        private void OnEnable() { ApplyQuality(); ApplyLighting(); if (autoFitCamera) FitCamera(); }
        private void OnValidate() { if (config != null) { ApplyLighting(); ApplyQuality(); if (autoFitCamera) FitCamera(); } }
        private void Update()
        {
            if (config == null) return;
            if (Application.isPlaying || animateInEditor)
                _clock = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            Vector2 direction = config.windDirection.sqrMagnitude > .001f ? config.windDirection.normalized : Vector2.right;
            float strength = config.windStrength * (quality == BattlefieldGraphics.Low ? .45f : 1f);
            Shader.SetGlobalVector(WindId, new Vector4(direction.x, direction.y, strength, config.windSpeed));
            Shader.SetGlobalFloat(TimeId, _clock);
            Shader.SetGlobalVector(NoiseId, new Vector4(config.gustFrequency, config.windNoiseScale, config.branchBend, config.grassBend));
        }
        public void ApplyLighting()
        {
            if (config == null) return;
            if (sun != null) { sun.color = config.sunColor; sun.intensity = config.sunIntensity; sun.transform.rotation = Quaternion.Euler(config.sunEuler); }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.5f, .59f, .7f);
            RenderSettings.ambientEquatorColor = new Color(.5f, .46f, .36f);
            RenderSettings.ambientGroundColor = new Color(.24f, .25f, .23f);
            RenderSettings.fog = false;
        }
        public void FitCamera()
        {
            if (battleCamera == null || config == null) return;
            battleCamera.orthographic = true;
            float elevation = config.cameraElevation * Mathf.Deg2Rad;
            float halfWidth = (config.size.x + config.rimWidth * 2f) * .5f;
            float halfDepth = (config.size.y + config.rimWidth * 2f) * .5f;
            battleCamera.orthographicSize = Mathf.Max(halfWidth / Mathf.Max(.5f, battleCamera.aspect), halfDepth * Mathf.Sin(elevation) + config.rimHeight) * config.cameraPadding;
            Vector3 focus = new Vector3(config.cameraOffset.x, 0f, config.cameraOffset.y);
            battleCamera.transform.position = focus + new Vector3(0f, Mathf.Sin(elevation), -Mathf.Cos(elevation)) * 100f;
            battleCamera.transform.rotation = Quaternion.LookRotation(focus - battleCamera.transform.position, Vector3.up);
            battleCamera.nearClipPlane = .3f; battleCamera.farClipPlane = 240f;
            battleCamera.GetUniversalAdditionalCameraData().SetRenderer(1);
        }
        [ContextMenu("Apply Graphics Preset")]
        public void ApplyQuality()
        {
            if (sun != null) sun.shadows = quality == BattlefieldGraphics.Low ? LightShadows.None : LightShadows.Soft;
            if (volume != null) volume.enabled = quality != BattlefieldGraphics.Low;
            if (grassRoot != null) for (int i = 0; i < grassRoot.childCount; i++) grassRoot.GetChild(i).gameObject.SetActive(quality == BattlefieldGraphics.High || (i/2) % (quality == BattlefieldGraphics.Low ? 4 : 2) == 0);
            if (detailRoot != null) for (int i = 0; i < detailRoot.childCount; i++) detailRoot.GetChild(i).gameObject.SetActive(quality != BattlefieldGraphics.Low || (i/2) % 3 == 0);
            if (particles != null) foreach (var particle in particles)
            {
                if (particle == null) continue;
                var emission = particle.emission; emission.enabled = quality != BattlefieldGraphics.Low;
                var lights = particle.lights; lights.enabled = false;
            }
            if (Application.isPlaying && config != null && !_playing) { Application.targetFrameRate = config.targetFrameRate; _playing = true; }
        }
    }
}

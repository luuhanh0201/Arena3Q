using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Scene set-up of the 3D presentation (docs/KeHoach/ChuyenSang3D.md §12): the battle camera on the Universal
    /// renderer, tilted about X and orthographic; one directional light and a flat ambient, no realtime shadows.
    /// </summary>
    public static class BattleView3D
    {
        /// <summary>Puts the camera on the renderer of the chosen presentation (2D renderer = the URP default).</summary>
        public static void ConfigureCamera(Camera camera, BattleController battle)
        {
            camera.orthographic = true;
            var data = camera.GetUniversalAdditionalCameraData();
            if (battle.Presentation3D)
            {
                data.SetRenderer(battle.View.RendererIndex);
                data.renderShadows = false;
                data.renderPostProcessing = BloomOn(battle.View);
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = battle.View.CameraDistance * 2f + 50f;
            }
            else
            {
                data.SetRenderer(-1);
                data.renderPostProcessing = false;
                camera.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>Set by the device benchmark to compare with / without bloom on one build (null = the data).</summary>
        public static bool? BloomOverride;

        public static bool BloomOn(BattleViewConfig view) => BloomOverride ?? view.Bloom;

        /// <summary>How far the effect pictures of the running battle push their hottest light (0 = as painted).</summary>
        public static float EffectBloomBoost { get; private set; }
        private static VolumeProfile _bloomProfile;

        /// <summary>Effect pictures as painted again (no bloom push) once the battle is gone.</summary>
        public static void ClearBloom() => EffectBloomBoost = 0f;

        /// <summary>
        /// The battle's bloom (BattleViewConfig): a global volume, and the push that lifts the hottest light of the effect
        /// pictures past the threshold (<see cref="EffectBloomBoost"/>, read by the Mặc Ảnh flipbook player; 0 when off).
        /// </summary>
        public static void CreateBloom(Transform parent, BattleViewConfig view)
        {
            var on = BloomOn(view);
            EffectBloomBoost = on ? view.EffectBloomBoost : 0f;
            if (!on)
                return;
            if (_bloomProfile == null)
            {
                _bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _bloomProfile.name = "Battle bloom";
                _bloomProfile.hideFlags = HideFlags.DontSave;
            }
            if (!_bloomProfile.TryGet<Bloom>(out var bloom))
                bloom = _bloomProfile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(view.BloomThreshold);
            bloom.intensity.Override(view.BloomIntensity);
            bloom.scatter.Override(view.BloomScatter);
            bloom.tint.Override(view.BloomTint);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(view.BloomQuarterResolution ? BloomDownscaleMode.Quarter : BloomDownscaleMode.Half);
            var go = new GameObject("Bloom");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = _bloomProfile;
        }

        /// <summary>Camera transform that looks at a ground point (logic plane) with the battle's tilt and diagonal turn.</summary>
        public static void PlaceCamera(Transform camera, BattleController battle, Vector2 ground)
        {
            var rotation = battle.CameraRotation;
            camera.rotation = rotation;
            camera.position = TiltedViewMath.CameraPosition(ground, rotation, battle.View.CameraDistance);
        }

        public static Light CreateLight(Transform parent, BattleViewConfig view)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(view.LightEulerAngles);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = view.LightColour;
            light.intensity = view.LightIntensity;
            light.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = view.AmbientColour;
            RenderSettings.sun = light;
            return light;
        }
    }
}

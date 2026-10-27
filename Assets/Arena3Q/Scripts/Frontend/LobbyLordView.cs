using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    // One independent portrait stage. No battle components, events or global render settings.
    public sealed class LobbyLordView : MonoBehaviour
    {
        public GameObject Model { get; private set; }
        public Camera CaptureCamera { get; private set; }
        public AnimationClip Idle { get; private set; }
        public RenderTexture Texture { get; private set; }
        private GameObject _stage;
        private Light _key, _fill;
        private Renderer[] _renderers;
        private Color _ambient, _lightColor;
        private Cubemap _reflection;
        private float _seconds;
        private RawImage _image;
        private LobbyConfig _config;
        private readonly Vector3[] _displayCorners = new Vector3[4];
        public bool Initialize(RawImage image, LobbyConfig config, LobbyConfig.LordModel source)
        {
            return Initialize(image, config, source, source?.hero?.PortraitModelPrefab, source?.hero?.PortraitIdle);
        }
        // Allows an Editor comparison to render the battle model with the same capture stage.
        public bool Initialize(RawImage image, LobbyConfig config, LobbyConfig.LordModel source,
            GameObject modelPrefab, AnimationClip idle)
        {
            if (source == null || modelPrefab == null || idle == null) return false;
            _image = image; _config = config;
            Idle = idle;
            _stage = new GameObject("LobbyLordStage");
            _stage.transform.position = config.stagePosition;
            Model = Instantiate(modelPrefab, _stage.transform);
            Model.transform.localPosition = Vector3.zero;
            Model.transform.localRotation = Quaternion.Euler(0, source.yaw, 0);
            foreach (var animator in Model.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var animation in Model.GetComponentsInChildren<Animation>()) animation.enabled = false;
            foreach (var child in Model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = config.modelLayer;
            foreach (var skin in Model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.quality = SkinQuality.Bone4; skin.updateWhenOffscreen = true;
                skin.shadowCastingMode = ShadowCastingMode.Off; skin.receiveShadows = false;
            }
            // The capture has its own ambient probe, so it does not depend on the Frontend scene's skybox.
            _renderers = Model.GetComponentsInChildren<Renderer>();
            _ambient = config.ambientColor; _lightColor = config.lightColor;
            _reflection = config.reflectionEnvironment;
            foreach (var renderer in _renderers) renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
            Sample(0);
            var cameraObject = new GameObject("LobbyLordCamera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            cameraObject.transform.SetParent(_stage.transform, false);
            CaptureCamera = cameraObject.GetComponent<Camera>();
            CaptureCamera.orthographic = true; CaptureCamera.orthographicSize = source.cameraSize;
            CaptureCamera.clearFlags = CameraClearFlags.SolidColor; CaptureCamera.backgroundColor = Color.clear;
            CaptureCamera.cullingMask = 1 << config.modelLayer;
            CaptureCamera.nearClipPlane = .1f; CaptureCamera.farClipPlane = 10;
            CaptureCamera.allowHDR = false; CaptureCamera.allowMSAA = config.captureMsaa > 1;
            cameraObject.transform.localPosition = new Vector3(0, source.cameraHeight, -4);
            var data = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(config.rendererIndex); data.renderPostProcessing = false;
            data.requiresColorTexture = data.requiresDepthTexture = false;
            Texture = new RenderTexture(config.renderResolution, config.renderResolution, 24, RenderTextureFormat.ARGB32)
                { name = "LobbyLordTexture", antiAliasing = config.captureMsaa };
            Texture.Create(); CaptureCamera.targetTexture = Texture;
            image.texture = Texture; image.raycastTarget = false;
            if (config.captureComposite != null) image.material = config.captureComposite;
            if (config.reflectionEnvironment != null)
            {
                var probeObject = new GameObject("Lobby portrait reflection", typeof(ReflectionProbe));
                probeObject.transform.SetParent(_stage.transform, false); probeObject.transform.localPosition = Vector3.up;
                var probe = probeObject.GetComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Custom;
                probe.customBakedTexture = config.reflectionEnvironment; probe.size = Vector3.one * 8;
                probe.boxProjection = false; probe.intensity = 1; probe.importance = 10;
                foreach (var renderer in _renderers) renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            }
            _key = AddLight("Key", new Vector3(-3, 4, -3), config.directionalIntensity, config.modelLayer);
            _key.type = LightType.Directional; _key.transform.localRotation = Quaternion.Euler(config.lightRotation);
            _lightColor *= config.portraitWarmth;
            _fill = AddLight("Fill", new Vector3(2, 2, -1), config.fillIntensity, config.modelLayer);
            SetTint(Color.white);
            return true;
        }
        private Light AddLight(string label, Vector3 position, float intensity, int layer)
        {
            var item = new GameObject(label, typeof(Light)); item.transform.SetParent(_stage.transform, false);
            item.transform.localPosition = position;
            var light = item.GetComponent<Light>(); light.type = LightType.Point;
            light.intensity = intensity; light.range = 10; light.cullingMask = 1 << layer;
            light.shadows = LightShadows.None; return light;
        }
        public void SetTint(Color tint)
        {
            if (_key != null) _key.color = _lightColor * tint;
            if (_fill != null) _fill.color = tint;
            if (_renderers == null) return;
            var ambient = new SphericalHarmonicsL2(); ambient.AddAmbientLight((_ambient * tint).linear);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.CopySHCoefficientArraysFrom(new[] { ambient });
                if (_reflection != null)
                {
                    // The source custom PBR shader otherwise samples URP's fallback sky cube.
                    // This capture uses an unencoded linear RGBAHalf environment, so its HDR multiplier is one.
                    properties.SetTexture("unity_SpecCube0", _reflection);
                    properties.SetVector("unity_SpecCube0_HDR", new Vector4(1, 1, 0, 0));
                }
                renderer.SetPropertyBlock(properties);
            }
        }
        public void Sample(float seconds)
        {
            if (Model != null && Idle != null) Idle.SampleAnimation(Model, Mathf.Repeat(seconds, Mathf.Max(.01f, Idle.length)));
        }
        public void UpdateDisplayResolution()
        {
            if (_image == null || _image.canvas == null || !_config.adaptiveRenderResolution || Texture == null) return;
            _image.rectTransform.GetWorldCorners(_displayCorners);
            var canvas = _image.canvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var bottom = RectTransformUtility.WorldToScreenPoint(camera, _displayCorners[0]);
            var top = RectTransformUtility.WorldToScreenPoint(camera, _displayCorners[2]);
            var step = Mathf.Max(1, _config.renderResolutionStep);
            var required = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(top.x - bottom.x), Mathf.Abs(top.y - bottom.y)) / step) * step;
            var resolution = Mathf.Clamp(required, _config.renderResolution, Mathf.Max(_config.renderResolution, _config.renderResolutionMaximum));
            if (Texture.width == resolution) return;
            var previous = Texture;
            Texture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
                { name = "LobbyLordTexture", antiAliasing = _config.captureMsaa };
            Texture.Create(); CaptureCamera.targetTexture = Texture; _image.texture = Texture;
            previous.Release(); Release(previous);
        }
        private void LateUpdate() { _seconds += Time.unscaledDeltaTime; Sample(_seconds); UpdateDisplayResolution(); }
        private void OnDestroy()
        {
            if (CaptureCamera != null) CaptureCamera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Release(Texture); }
            if (_stage != null) Release(_stage);
        }
        private static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}

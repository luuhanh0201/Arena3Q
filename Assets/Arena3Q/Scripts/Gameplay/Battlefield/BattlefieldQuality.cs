using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Gameplay
{
    [ExecuteAlways]
    public sealed class BattlefieldQuality : MonoBehaviour
    {
        public BattlefieldEnvironment environment;
        public RenderPipelineAsset low;
        public RenderPipelineAsset medium;
        public RenderPipelineAsset high;
        public int lowFrameRate = 30;
        public int mediumFrameRate = 30;
        public int highFrameRate = 60;
        private RenderPipelineAsset _previous, _active;
        private float _previousLod;
        private int _previousFps;
        private bool _changed;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            _previous = QualitySettings.renderPipeline; _previousLod = QualitySettings.lodBias; _previousFps = Application.targetFrameRate; _changed = true;
            Apply();
        }
        private void Start() { if (Application.isPlaying) Apply(); }
        private void OnDisable()
        {
            if (!_changed) return;
            if (QualitySettings.renderPipeline == _active) QualitySettings.renderPipeline = _previous;
            QualitySettings.lodBias = _previousLod; Application.targetFrameRate = _previousFps; _changed = false;
        }
        public RenderPipelineAsset Selected => environment == null || environment.quality == BattlefieldGraphics.High ? high : environment.quality == BattlefieldGraphics.Medium ? medium : low;
        public void Apply()
        {
            if (environment == null) return; environment.ApplyQuality();
            if (!Application.isPlaying) return;
            _active = Selected; if (_active != null) QualitySettings.renderPipeline = _active;
            QualitySettings.lodBias = environment.quality == BattlefieldGraphics.High ? 1.8f : environment.quality == BattlefieldGraphics.Medium ? 1f : .6f;
            Application.targetFrameRate = environment.quality == BattlefieldGraphics.High ? highFrameRate : environment.quality == BattlefieldGraphics.Medium ? mediumFrameRate : lowFrameRate;
        }
    }
}

using Arena3Q.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Arena3Q.Frontend
{
    public sealed class IntroScreen : StartupScreen
    {
        private VideoPlayer _video;
        private RenderTexture _texture;
        private AudioSource _audio;
        private RawImage _view;
        private float _elapsed;
        private bool _finished;
        public bool CanSkip => Flow.Profile.introSeen;
        public bool IsFinished => _finished;

        protected override void BuildScreen()
        {
            var still = UiFactory.Stretch("SplashStill", Root); still.SetAsFirstSibling();
            UiFactory.AddImage(still, Color.white, Art.splashStill, false);
            var catcher = UiFactory.Stretch("SkipSplash", Root); catcher.SetSiblingIndex(1);
            var hit = UiFactory.AddImage(catcher, Color.clear); hit.raycastTarget = true;
            hit.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (CanSkip) Finish(); });
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(Flow.Config.WebIntroFile)) return;
            _texture = new RenderTexture((int)Art.referenceSize.x, (int)Art.referenceSize.y, 0);
#else
            if (Flow.Config.IntroVideo == null) return;
            var clip = Flow.Config.IntroVideo;
            _texture = new RenderTexture((int)clip.width, (int)clip.height, 0);
#endif
            _texture.Create();
            var view = UiFactory.Stretch("SplashVideo", Root); view.SetSiblingIndex(1);
            _view = view.gameObject.AddComponent<RawImage>(); _view.texture = _texture; _view.raycastTarget = false; _view.enabled = false;
            _audio = Root.gameObject.AddComponent<AudioSource>(); _audio.playOnAwake = false; _audio.spatialBlend = 0;
            _audio.mute = Kit.Audio.Muted; _audio.volume = Art.introVolume;
            _video = Root.gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            _video.source = VideoSource.Url;
            _video.url = Application.streamingAssetsPath.TrimEnd('/') + "/" + Flow.Config.WebIntroFile;
#else
            _video.clip = clip;
#endif
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _texture; _video.isLooping = false; _video.waitForFirstFrame = true;
            _video.audioOutputMode = VideoAudioOutputMode.AudioSource;
#if UNITY_WEBGL && !UNITY_EDITOR
            _video.EnableAudioTrack(0, true); _video.SetTargetAudioSource(0, _audio);
#else
            if (clip.audioTrackCount > 0) { _video.EnableAudioTrack(0, true); _video.SetTargetAudioSource(0, _audio); }
#endif
            _video.loopPointReached += OnEnd;
            _video.sendFrameReadyEvents = true; _video.frameReady += OnFrame;
            _video.errorReceived += OnError;
            _video.Play();
        }
        private void OnEnd(VideoPlayer _) => Finish();
        private void OnFrame(VideoPlayer _, long frame) { if (_view != null) _view.enabled = true; }
        private void OnError(VideoPlayer _, string message)
        {
            Debug.LogWarning("[Arena3Q UI] Splash video unavailable, using supplied still: " + message);
            if (_video != null) _video.Stop();
            if (_view != null) _view.enabled = false;
        }
        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= Flow.Config.IntroSeconds + (_video != null ? 2 : 0)) Finish();
        }
        public void Finish()
        {
            if (_finished) return;
            _finished = true;
            Flow.Profile.introSeen = true; Flow.Save(); Flow.Complete();
        }
        protected override void OnHide()
        {
            if (_video != null) { _video.loopPointReached -= OnEnd; _video.frameReady -= OnFrame; _video.errorReceived -= OnError; _video.Pause(); _video.targetTexture = null; }
            if (_audio != null) _audio.Stop();
            if (_texture != null) { Object.Destroy(_texture, Art.screenSeconds); _texture = null; }
        }
    }
}

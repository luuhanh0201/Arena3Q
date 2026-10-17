using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// The ~3-second intro (docs/KeHoach/VaoGame.md §5): the video when one is assigned, otherwise the logo appears and
    /// the banner fades in. The first time it plays to the end; afterwards a tap skips it.
    /// </summary>
    public sealed class IntroScreen : FrontendScreen
    {
        private Text _title;
        private Image _logo;
        private Image _banner;
        private VideoPlayer _video;
        private RenderTexture _videoTexture;
        private float _elapsed;
        private bool _finished;

        public bool CanSkip => Flow.Profile.introSeen;
        public bool IsFinished => _finished;

        protected override void Build()
        {
            Ui.Fill(Root, "Black", Color.black);
            var config = Flow.Config;
            AddTapCatcher(() =>
            {
                if (CanSkip)
                    Finish();
            });

            if (config.IntroVideo != null)
            {
                BuildVideo(config.IntroVideo);
                return;
            }

            _banner = Ui.Fill(Root, "Banner", config.Banner != null ? Color.white : Ui.Theme.panel, config.Banner);
            if (config.Logo != null)
                _logo = Ui.Portrait(Root, "Logo", config.Logo, Color.white, FrontendUi.Center, Vector2.zero, new Vector2(900f, 400f));
            else
                _title = Ui.Label(Root, "Title", config.GameTitle, 140, TextAnchor.MiddleCenter, FrontendUi.Center, Vector2.zero,
                    new Vector2(1400f, 240f), Ui.Theme.accent);
            Apply(0f);
        }

        private void BuildVideo(VideoClip clip)
        {
            _videoTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            var raw = Root.gameObject.AddComponent<RawImage>();
            raw.texture = _videoTexture;
            raw.raycastTarget = false;
            _video = Root.gameObject.AddComponent<VideoPlayer>();
            _video.clip = clip;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoTexture;
            _video.isLooping = false;
            _video.loopPointReached += _ => Finish();
            _video.Play();
        }

        public override void Tick(float deltaTime)
        {
            if (_finished)
                return;
            _elapsed += deltaTime;
            if (_video == null)
                Apply(_elapsed / Mathf.Max(0.5f, Flow.Config.IntroSeconds));
            // The video ends through loopPointReached; the timer is a safety net for both.
            if (_elapsed >= Flow.Config.IntroSeconds + (_video != null ? 1f : 0f))
                Finish();
        }

        // Storyboard (§5): logo appears at ~1/3, banner fades in during the middle third, logo settles at the top.
        private void Apply(float t)
        {
            var logoAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.45f, t));
            var bannerAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.75f, t));
            var rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.8f, t));
            if (_banner != null)
                _banner.color = new Color(_banner.color.r, _banner.color.g, _banner.color.b, bannerAlpha);
            Graphic logo = _logo != null ? (Graphic)_logo : _title;
            if (logo != null)
            {
                logo.color = new Color(logo.color.r, logo.color.g, logo.color.b, logoAlpha);
                logo.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 380f, rise));
                logo.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.7f, rise);
            }
        }

        /// <summary>Ends the intro (end of the timer or video, or a tap once it has been seen).</summary>
        public void Finish()
        {
            if (_finished)
                return;
            _finished = true;
            if (_video != null)
                _video.Stop();
            if (_videoTexture != null)
                _videoTexture.Release();
            Flow.Profile.introSeen = true;
            Flow.Save();
            Flow.Complete();
        }
    }
}

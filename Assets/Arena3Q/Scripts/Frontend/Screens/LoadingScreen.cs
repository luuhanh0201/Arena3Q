using System.Globalization;
using Arena3Q.Core;
using Arena3Q.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    public sealed class LoadingScreen : StartupScreen
    {
        private IStartupPreparation _preparation;
        private Image _fill, _download; private RectTransform _cart; private RectTransform[] _wheels;
        private RectTransform _flag; private Image[] _dust;
        private TMP_Text _stage, _speed, _tip, _percent;
        private RectTransform _notice; private StartupNetState? _noticeState;
        private float _time, _doneTime; private int _tipIndex = -1;
        private CanvasGroup _tipGroup;
        public StartupProgress Progress => _preparation.Current;

        protected override void BuildScreen()
        {
            Backdrop();
            Kit.BottomShade(Design, 330);
            Kit.Logo(Design, 400, 16, 480);
            Kit.Footer(Design);
            var bar = Kit.Image(Design, "ProgressTrack", Art.rounded, new Rect(206, 570, 868, 16), StartupUiConfig.Hex("B8893A"), true);
            Kit.Image(bar.transform, "TrackInner", Art.rounded, new Rect(2, 2, 864, 12), StartupUiConfig.Hex("160707"), true);
            _fill = Kit.Image(bar.transform, "ProgressFill", null, new Rect(3, 3, 0, 10));
            _fill.gameObject.AddComponent<UiGradient>().Configure(StartupUiConfig.Hex("8A0F2E"), StartupUiConfig.Hex("FF4F7A"));
            Kit.Image(Design, "SpearLeft", Art.spear, new Rect(150, 560, 72, 36));
            var spearRight = Kit.Image(Design, "SpearRight", Art.spear, new Rect(1130, 560, 72, 36));
            spearRight.rectTransform.localScale = new Vector3(-1, 1, 1);
            _cart = Kit.Rect(Design, "SupplyCart", new Rect(206, 480, 132, 84));
            Kit.Image(_cart, "Body", Art.cart, new Rect(0, 0, 132, 84));
            _wheels = new RectTransform[2];
            for (var i = 0; i < 2; i++)
            {
                var wheel = Kit.Image(_cart, "Wheel" + i, Art.wheel, new Rect(i == 0 ? 24 : 86, 58, 24, 24)).rectTransform;
                wheel.pivot = new Vector2(.5f, .5f); wheel.anchoredPosition += new Vector2(12, -12); _wheels[i] = wheel;
            }
            _flag = Kit.Image(_cart, "Flag", Art.flag, new Rect(22, 2, 26, 20)).rectTransform;
            _dust = new Image[3];
            for (var i = 0; i < 3; i++) _dust[i] = Kit.Image(_cart, "Dust" + i, Art.rounded, new Rect(-10, 70, 18, 8), new Color(1, .8f, .5f, .5f), true);
            _stage = Kit.Text(Design, "LoadingStage", "", new Rect(350, 516, 360, 32), 21, weight: 800);
            _percent = Kit.Text(Design, "Percent", "0%", new Rect(350, 543, 100, 28), 21, Art.gold, weight: 800);
            _download = Kit.Image(Design, "DownloadIcon", Art.download, new Rect(540, 608, 14, 14));
            _speed = Kit.Text(Design, "DownloadSpeed", "", new Rect(560, 602, 280, 26), 16, align: TextAlignmentOptions.MidlineLeft, weight: 800);
            _tip = Kit.Text(Design, "Tip", "", new Rect(190, 642, 900, 40), 18, Art.gold, TextAlignmentOptions.Center, 700);
            _tipGroup = _tip.gameObject.AddComponent<CanvasGroup>();
            Kit.Image(Design, "CloudLeft", Art.cloud, new Rect(88, 646, 96, 32));
            var cloud = Kit.Image(Design, "CloudRight", Art.cloud, new Rect(1192, 646, 96, 32)); cloud.rectTransform.localScale = new Vector3(-1, 1, 1);
            _preparation = FrontendServiceLocator.Prepare(Art);
            Apply();
        }

        public override void Tick(float deltaTime)
        {
            _time += deltaTime; _preparation.Tick(deltaTime);
#if UNITY_EDITOR || DEBUG
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
            {
                var next = ((int)Progress.State + 1) % 6;
                Preview(new StartupProgress(.63f, (StartupNetState)next, next == 0 ? 2400000 : next == 1 ? 86000 : 0, 2));
            }
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
            { _preparation.Dispose(); _preparation = FrontendServiceLocator.Prepare(Art); }
#endif
            Apply();
            if (Progress.Complete && !Progress.IsPaused)
            {
                _doneTime += deltaTime;
                if (_doneTime >= Art.screenSeconds) Flow.Complete();
            }
            else _doneTime = 0;
        }

        private void Apply()
        {
            var progress = Progress; var pct = Mathf.RoundToInt(progress.Progress * 100);
            if (Flow.StatusBar != null)
            {
                Flow.StatusBar.ManualState = !progress.IsLocal;
                if (!progress.IsLocal) Flow.StatusBar.SetState(progress.State);
            }
            _fill.rectTransform.sizeDelta = new Vector2(862 * progress.Progress, 10);
            _fill.color = progress.IsPaused ? Art.off : Color.white;
            var cartX = 206 + Mathf.Clamp(868 * progress.Progress - 118, -6, 868 - 124);
            _cart.anchoredPosition = new Vector2(cartX, -480 + (progress.IsPaused ? 0 : Mathf.Sin(_time * 16) * 1.2f));
            foreach (var wheel in _wheels) if (!progress.IsPaused) wheel.localRotation = Quaternion.Euler(0, 0, -_time * 655);
            _flag.localRotation = Quaternion.Euler(0, 0, progress.IsPaused ? 0 : Mathf.Sin(_time * 7) * 5);
            for (var i = 0; i < _dust.Length; i++)
            {
                var t = (_time * 2 + i / 3f) % 1;
                _dust[i].color = new Color(1, .8f, .5f, progress.IsPaused ? 0 : (1 - t) * .55f);
                _dust[i].rectTransform.anchoredPosition = new Vector2(-10 - 34 * t, -70 + 8 * t);
            }
            _stage.text = Kit.C(progress.State == StartupNetState.Weak ? "loadingWeak" : progress.IsLocal ? "loadingLocal"
                : pct < 20 ? "loadingVersion" : pct < 90 ? "loadingResources" : pct < 100 ? "loadingConnect" : "loadingDone");
            if (pct == 100) _stage.text = Kit.C("loadingDone");
            _percent.text = pct + "%";
            var stageWidth = _stage.preferredWidth;
            var pairWidth = stageWidth + _percent.preferredWidth + 12;
            var textX = cartX - pairWidth - 14;
            if (textX < 206) textX = cartX + 146;
            _stage.rectTransform.sizeDelta = new Vector2(stageWidth + 2, 32);
            _stage.rectTransform.anchoredPosition = new Vector2(textX, -520);
            _percent.rectTransform.anchoredPosition = new Vector2(textX + stageWidth + 12, -520);
            _speed.text = progress.IsLocal ? Kit.C("localResources") : FormatSpeed(progress.BytesPerSecond);
            var speedLeft = 640 - (_speed.preferredWidth + 20) / 2;
            _download.rectTransform.anchoredPosition = new Vector2(speedLeft, -608);
            _speed.rectTransform.anchoredPosition = new Vector2(speedLeft + 20, -602);
            _speed.color = progress.IsPaused ? Art.bad : progress.State == StartupNetState.Weak ? Art.weak : Art.text;
            if (progress.IsPaused) _speed.alpha = .55f + .45f * Mathf.Abs(Mathf.Sin(_time * 3)); else _speed.alpha = 1;
            if (Art.tips.Length > 0)
            {
                var index = Mathf.FloorToInt(_time / Mathf.Max(.1f, Art.tipSeconds)) % Art.tips.Length;
                if (index != _tipIndex) { _tipIndex = index; _tip.text = Art.tips[index]; }
                var phase = (_time % Art.tipSeconds) / .45f;
                _tipGroup.alpha = Mathf.Clamp01(phase);
                _tip.rectTransform.anchoredPosition = new Vector2(190, -642 - 8 * (1 - Mathf.Clamp01(phase)));
            }
            if (_noticeState != progress.State) RebuildNotice(progress);
        }

        public static string FormatSpeed(double bytes) => bytes <= 0 ? "0 KB/s" : bytes >= 1024 * 1024
            ? (bytes / (1024 * 1024)).ToString("0.0", CultureInfo.GetCultureInfo("vi-VN")) + " MB/s"
            : (bytes / 1024).ToString("0", CultureInfo.InvariantCulture) + " KB/s";

        private void RebuildNotice(StartupProgress progress)
        {
            _noticeState = progress.State;
            if (_notice != null) { _notice.gameObject.SetActive(false); Object.Destroy(_notice.gameObject); }
            _notice = null;
            if (progress.State == StartupNetState.Good) return;
            if (progress.State == StartupNetState.Weak)
            {
                _notice = Kit.Rect(Design, "WeakNetworkToast", new Rect(365, 204, 550, 44));
                Kit.Image(_notice, "ToastBackground", Art.rounded, new Rect(0, 0, 550, 44), new Color(.16f, .1f, .02f, .96f), true);
                Kit.Image(_notice, "Warning", Art.warning, new Rect(12, 12, 20, 20), Art.weak);
                Kit.Text(_notice, "Message", Kit.C("weakToast"), new Rect(42, 0, 495, 44), 13, Art.gold);
                return;
            }
            _notice = Kit.Dim(Design, "NetworkOverlay", progress.State == StartupNetState.Stalled || progress.State == StartupNetState.Retrying ? .45f : .6f);
            if (progress.State == StartupNetState.Stalled || progress.State == StartupNetState.Retrying)
            {
                LoadingSpinner.Create(Kit, _notice, new Vector2(640, 292));
                Kit.Text(_notice, "SpinnerTitle", Kit.C(progress.State == StartupNetState.Retrying ? "retrying" : "stalled"), new Rect(290, 382, 700, 34), 20, Art.text, TextAlignmentOptions.Center, 800);
                Kit.Text(_notice, "RetryAttempt", string.Format(Kit.C("retryAttempt"), Mathf.Max(1, progress.Attempt)), new Rect(290, 420, 700, 28), 14, Art.dim, TextAlignmentOptions.Center);
                return;
            }
            var timeout = progress.State == StartupNetState.ServerTimeout;
            var panel = Kit.Panel(_notice, "ConnectionProblem", Art.networkPanel, tassels: false, animate: true);
            Kit.Image(panel, "ErrorIcon", timeout ? Art.serverOff : Art.wifiOff, new Rect(248, 40, 64, 64), Art.bad);
            Kit.Text(panel, "Title", Kit.C(timeout ? "timeoutTitle" : "lostTitle"), new Rect(48, 112, 464, 34), 22, Art.gold, TextAlignmentOptions.Center, 800);
            Kit.Text(panel, "Explanation", Kit.C(timeout ? "timeoutMessage" : "lostMessage"), new Rect(50, 152, 460, 72), 14, Art.dim, TextAlignmentOptions.Center);
            Kit.Text(panel, "ResumeProgress", string.Format(Kit.C("resumeProgress"), Mathf.RoundToInt(progress.Progress * 100)), new Rect(50, 230, 460, 28), 13, Art.good, TextAlignmentOptions.Center);
            Kit.Text(panel, "ErrorCode", Kit.C(timeout ? "timeoutCode" : "lostCode"), new Rect(50, 262, 460, 24), 12, Art.muted, TextAlignmentOptions.Center);
            Kit.Button(panel, "Quit", Kit.C("quit"), new Rect(50, 300, 222, 50), Quit, sound: UiSound.Back);
            Kit.Button(panel, "Retry", Kit.C("retry"), new Rect(286, 300, 224, 50), Retry, GameButtonStyle.Gold, UiSound.Confirm, 18);
        }

        public void Retry() { _preparation.Retry(); _noticeState = null; Apply(); }
        private void Quit()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.isEditor)
            { _notice.gameObject.SetActive(false); return; }
            Application.Quit();
        }
        /// <summary>Replaces the service for a development visual review; not compiled into release players.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEBUG")]
        public void Preview(StartupProgress progress)
        {
            _preparation.Dispose(); _preparation = new PreviewPreparation(progress); _noticeState = null; Apply();
        }
        private sealed class PreviewPreparation : IStartupPreparation
        {
            public PreviewPreparation(StartupProgress progress) { Current = progress; }
            public StartupProgress Current { get; private set; }
            public void Tick(float deltaTime) { }
            public void Retry() => Current = new StartupProgress(Current.Progress, StartupNetState.Retrying, attempt: 1);
            public void Dispose() { }
        }
        protected override void OnHide()
        {
            _preparation?.Dispose();
            if (Flow.StatusBar != null) { Flow.StatusBar.ManualState = false; Flow.StatusBar.SetState(StartupNetState.Good); }
        }
    }
}

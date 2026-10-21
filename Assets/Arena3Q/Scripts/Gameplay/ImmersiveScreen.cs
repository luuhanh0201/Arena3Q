using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Keeps Android full screen (<see cref="DisplayModeConfig"/>): hides the status and navigation bars at start and
    /// whenever the game gets the focus back (Home, recent apps, notification shade), lets a swipe show them as
    /// transient bars, and hides them again when they have stayed longer than the configured time. No-op elsewhere.
    /// </summary>
    public sealed class ImmersiveScreen : MonoBehaviour
    {
        private DisplayModeConfig _config;
        private float _nextCheck;
        private float _visibleSeconds;
        private volatile bool _barsVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var config = Resources.Load<DisplayModeConfig>(DisplayModeConfig.ResourceName);
            if (config == null || !config.Immersive)
                return;
            var go = new GameObject("Immersive screen");
            DontDestroyOnLoad(go);
            go.AddComponent<ImmersiveScreen>()._config = config;
#endif
        }

        private void Start() => Hide();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                return;
            // Coming back from Home / recent apps / the shade: full screen again at once.
            _visibleSeconds = 0f;
            Hide();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + _config.CheckIntervalSeconds;
            Check();
            _visibleSeconds = _barsVisible ? _visibleSeconds + _config.CheckIntervalSeconds : 0f;
            if (_visibleSeconds >= _config.HideBarsAfterSeconds)
            {
                _visibleSeconds = 0f;
                Hide();
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // View flags for Android below 11: LAYOUT_STABLE | LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN | HIDE_NAVIGATION
        // | FULLSCREEN | IMMERSIVE_STICKY.
        private const int LegacyImmersiveSticky = 0x100 | 0x200 | 0x400 | 0x2 | 0x4 | 0x1000;
        private const int BehaviorShowTransientBarsBySwipe = 2;

        private static int Sdk
        {
            get
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
        }

        private static void OnUiThread(System.Action<AndroidJavaObject> action)
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try { action(activity); }
                catch (System.Exception e) { Debug.LogWarning("[Arena3Q] Immersive screen: " + e.Message); }
                finally { activity.Dispose(); }
            }));
        }

        private static void Hide()
        {
            OnUiThread(activity =>
            {
                using var window = activity.Call<AndroidJavaObject>("getWindow");
                if (Sdk >= 30)
                {
                    using var controller = window.Call<AndroidJavaObject>("getInsetsController");
                    using var types = new AndroidJavaClass("android.view.WindowInsets$Type");
                    controller.Call("setSystemBarsBehavior", BehaviorShowTransientBarsBySwipe);
                    controller.Call("hide", types.CallStatic<int>("systemBars"));
                }
                else
                {
                    using var decor = window.Call<AndroidJavaObject>("getDecorView");
                    decor.Call("setSystemUiVisibility", LegacyImmersiveSticky);
                }
            });
        }

        private void Check()
        {
            OnUiThread(activity =>
            {
                using var window = activity.Call<AndroidJavaObject>("getWindow");
                using var decor = window.Call<AndroidJavaObject>("getDecorView");
                if (Sdk >= 30)
                {
                    using var insets = decor.Call<AndroidJavaObject>("getRootWindowInsets");
                    using var types = new AndroidJavaClass("android.view.WindowInsets$Type");
                    _barsVisible = insets != null && insets.Call<bool>("isVisible", types.CallStatic<int>("navigationBars"));
                }
                else
                    _barsVisible = (decor.Call<int>("getSystemUiVisibility") & 0x2) == 0; // HIDE_NAVIGATION cleared
            });
        }
#else
        private static void Hide() { }
        private void Check() => _barsVisible = false;
#endif
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arena3Q.UI
{
    /// <summary>A persistent cover keeps scene activation out of view, including the first layout frame.</summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        private static SceneTransition _active;
        private static float _seconds;
        private CanvasGroup _cover;

        public static void Configure(float seconds) => _seconds = Mathf.Max(0, seconds);

        public static void Load(string scene) => Run(() => SceneManager.LoadScene(scene));

        public static void Run(Action change)
        {
            if (_active != null) return;
            if (_seconds <= 0) { change(); return; }
            var root = new GameObject("SceneTransition");
            DontDestroyOnLoad(root);
            _active = root.AddComponent<SceneTransition>();
            var canvas = UiFactory.CreateCanvas(root.transform, "TransitionCanvas", short.MaxValue);
            UiFactory.AddImage(UiFactory.Stretch("Cover", canvas.transform), Color.black);
            _active._cover = canvas.gameObject.AddComponent<CanvasGroup>();
            _active._cover.alpha = 0;
            _active.StartCoroutine(_active.Change(change, _seconds));
        }

        private IEnumerator Change(Action change, float seconds)
        {
            yield return Fade(0, 1, seconds);
            try { change(); }
            catch { Destroy(gameObject); throw; }
            // Let Awake/Start and layout finish while the cover is opaque.
            yield return null; yield return null;
            yield return Fade(1, 0, seconds);
            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds); t = t * t * (3 - 2 * t);
                _cover.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
        }

        private void OnDestroy() { if (_active == this) _active = null; }
    }
}

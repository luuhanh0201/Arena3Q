using UnityEngine;

namespace Arena3Q.Frontend
{
    public sealed class UiReveal : MonoBehaviour
    {
        private CanvasGroup _group; private float _duration, _from, _time, _alpha;
        private bool _exit, _fade;
        public void Configure(float duration, float fromScale = 1)
        {
            _duration = Mathf.Max(.01f, duration); _from = fromScale; _time = 0;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0; _group.interactable = false; _group.blocksRaycasts = true;
            if (_from != 1) transform.localScale = Vector3.one * _from;
        }
        public static void Exit(GameObject target, float seconds, bool fade = true)
        {
            var motion = target.GetComponent<UiReveal>();
            if (motion == null) motion = target.AddComponent<UiReveal>();
            motion._group = target.GetComponent<CanvasGroup>();
            if (motion._group == null) motion._group = target.AddComponent<CanvasGroup>();
            motion._exit = true; motion._fade = fade; motion._time = 0;
            motion._duration = Mathf.Max(.01f, seconds); motion._alpha = motion._group.alpha;
            motion._group.interactable = false; motion._group.blocksRaycasts = false;
        }
        private void Update()
        {
            if (_group == null) return;
            _time += Time.unscaledDeltaTime; var t = Mathf.Clamp01(_time / _duration);
            var eased = t * t * (3 - 2 * t);
            if (_exit)
            {
                if (_fade) _group.alpha = _alpha * (1 - eased);
                if (t >= 1) { gameObject.SetActive(false); Destroy(gameObject); }
                return;
            }
            _group.alpha = eased;
            if (_from != 1) transform.localScale = Vector3.one * Mathf.Lerp(_from, 1, 1 - Mathf.Pow(1 - t, 3));
            if (t >= 1) { _group.interactable = true; Destroy(this); }
        }
    }
}

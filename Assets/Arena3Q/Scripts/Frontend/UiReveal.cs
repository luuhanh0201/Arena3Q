using UnityEngine;

namespace Arena3Q.Frontend
{
    public sealed class UiReveal : MonoBehaviour
    {
        private CanvasGroup _group; private float _duration, _from, _time;
        public void Configure(float duration, float fromScale = 1)
        {
            _duration = Mathf.Max(.01f, duration); _from = fromScale;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0; _group.interactable = false; _group.blocksRaycasts = true;
            transform.localScale = Vector3.one * _from;
        }
        private void Update()
        {
            if (_group == null) return;
            _time += Time.unscaledDeltaTime; var t = Mathf.Clamp01(_time / _duration);
            _group.alpha = t; transform.localScale = Vector3.one * Mathf.Lerp(_from, 1, 1 - Mathf.Pow(1 - t, 3));
            if (t >= 1) { _group.interactable = true; Destroy(this); }
        }
    }
}

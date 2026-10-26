using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D
{
    // Reuses the project's UI gradient / independent border layer; no particle systems or material instances.
    public sealed class LobbyArtLight : MonoBehaviour
    {
        public LobbyArtCatalog catalog;
        public LobbyArtCatalog.Node settings;
        private Image _image;
        private RectTransform _rect;
        private Vector2 _origin;
        private float _clock, _alpha;
        private void Awake()
        {
            _image = GetComponent<Image>(); _rect = (RectTransform)transform; _origin = _rect.anchoredPosition;
            var color = _image.color; color.a = 0; _image.color = color;
        }
        private void Update() { Sample(_clock + Time.unscaledDeltaTime, Time.unscaledDeltaTime); }
        public void Sample(float clock, float delta)
        {
            if (_image == null) Awake();
            _clock = clock;
            var enabledEffect = settings.effect == "vipSheen" ? catalog.vipAnimation && catalog.vip >= settings.minVip : settings.effect=="recruitSheen"?catalog.recruitAnimation:catalog.ctaAnimation;
            var phase = Mathf.Repeat(clock, Mathf.Max(.01f, settings.period));
            var progress = Mathf.Clamp01(phase / Mathf.Max(.01f, settings.activeSeconds));
            var target = enabledEffect && phase < settings.activeSeconds ? Mathf.Sin(progress * Mathf.PI) * settings.amplitude : 0;
            _alpha = Mathf.MoveTowards(_alpha, target, delta / Mathf.Max(.01f, settings.fadeSeconds));
            var color = _image.color; color.a = _alpha; _image.color = color;
            _rect.anchoredPosition = _origin + Vector2.right * Mathf.Lerp(-settings.travel * .5f, settings.travel * .5f, progress);
        }
    }
}

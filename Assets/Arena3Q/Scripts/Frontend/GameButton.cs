using Arena3Q.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    [RequireComponent(typeof(Button))]
    public sealed class GameButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Button _button; private RectTransform _rect; private Graphic _image;
        private UIAudio _audio; private UiSound _sound; private Vector2 _position;
        private CanvasGroup _alpha; private Outline _glow; private bool _transparent;
        private bool _down, _hover, _selected; private float _pressScale = .94f, _time;
        public void Configure(Button button, UIAudio audio, UiSound sound, StartupUiConfig config, bool round = false)
        {
            _button = button; _audio = audio; _sound = sound; _rect = (RectTransform)transform; _image = button.targetGraphic;
            _position = _rect.anchoredPosition; _pressScale = round ? .9f : .94f;
            _transparent = _image.color.a == 0;
            _alpha = gameObject.AddComponent<CanvasGroup>();
            _glow = gameObject.AddComponent<Outline>(); _glow.effectColor = config.goldLine * new Color(1, 1, 1, .5f);
            _glow.effectDistance = new Vector2(2, 2); _glow.enabled = false;
            // The sound listener is installed first, before callbacks can destroy this button.
            button.onClick.AddListener(() => { if (button.IsInteractable()) _audio.Play(_sound); });
        }
        public void SetSelected(bool selected) => _selected = selected;
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left && _button.IsInteractable()) { _down = true; _time = 0; } }
        public void OnPointerUp(PointerEventData data) { _down = false; _time = 0; }
        public void OnPointerEnter(PointerEventData data) => _hover = true;
        public void OnPointerExit(PointerEventData data) { _hover = false; _down = false; }
        public void OnSelect(BaseEventData data) => _hover = true;
        public void OnDeselect(BaseEventData data) { _hover = false; _down = false; }
        private void Update()
        {
            if (_button == null) return;
            _time += Time.unscaledDeltaTime;
            var enabled = _button.IsInteractable();
            var scale = _down && enabled ? _pressScale : 1;
            var t = Mathf.Clamp01(_time / (_down ? .05f : .12f));
            if (!_down) { var u = t - 1; t = 1 + 2.70158f * u * u * u + 1.70158f * u * u; }
            _rect.localScale = Vector3.one * Mathf.LerpUnclamped(_rect.localScale.x, scale, t);
            _rect.anchoredPosition = _position + (_down && enabled ? Vector2.down * 2 : Vector2.zero);
            var v = !enabled ? .7f : _down ? .8f : _hover || _selected ? 1.15f : 1;
            _alpha.alpha = enabled ? 1 : .55f;
            _glow.enabled = enabled && !_down && (_hover || _selected);
            _image.color = new Color(v, v, v, _transparent ? 0 : 1);
        }
        private void OnDisable()
        {
            _down = _hover = false;
            if (_rect != null) { _rect.localScale = Vector3.one; _rect.anchoredPosition = _position; }
        }
    }
}

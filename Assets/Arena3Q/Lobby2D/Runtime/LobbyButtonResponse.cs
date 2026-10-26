using UnityEngine;
using UnityEngine.EventSystems;

namespace Arena3Q.Lobby2D
{
    public sealed class LobbyButtonResponse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public LobbyArtCatalog settings;
        private bool _down;
        private float _scale = 1;
        public void OnPointerDown(PointerEventData data) { _down = true; }
        public void OnPointerUp(PointerEventData data) { _down = false; }
        public void OnPointerExit(PointerEventData data) { _down = false; }
        private void OnDisable() { _down = false; _scale = 1; transform.localScale = Vector3.one; }
        private void Update()
        {
            var target = settings != null && settings.buttonResponse && _down ? settings.pressedScale : 1;
            _scale = Mathf.MoveTowards(_scale, target, Time.unscaledDeltaTime / Mathf.Max(.01f, settings != null ? settings.responseSeconds : .1f));
            transform.localScale = Vector3.one * _scale;
        }
    }
}

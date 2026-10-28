using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Arena3Q.Data;

namespace Arena3Q.Exploration
{
    public sealed class ChapterMapInput : MonoBehaviour
    {
        private ChapterMapController _map;
        private Vector2 _start, _last;
        private bool _held, _blocked, _dragged, _pinching;
        private float _pinchDistance;
        public void Configure(ChapterMapController map) => _map = map;
        private bool OverUi(Vector2 p)
        {
            if (EventSystem.current == null) return false;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = p }, hits); return hits.Count > 0;
        }
        private void Update()
        {
            if (_map == null || !_map.Ready || _map.Busy) return;
            var screen = Touchscreen.current; var touches = new List<UnityEngine.InputSystem.Controls.TouchControl>();
            if (screen != null) foreach (var touch in screen.touches) if (touch.press.isPressed) touches.Add(touch);
            if (touches.Count > 1)
            {
                var a = touches[0].position.ReadValue(); var b = touches[1].position.ReadValue(); float d = Vector2.Distance(a, b);
                if (!_pinching) _blocked = OverUi(a) || OverUi(b);
                if (_pinching && !_blocked && d > 1) _map.View.Zoom(_pinchDistance / d, (a + b) * .5f);
                _pinchDistance = d; _pinching = true; _held = false; return;
            }
            if (_pinching) { if (touches.Count == 0) _pinching = false; return; }
            bool down, pressed, up; Vector2 point;
            if (touches.Count == 1)
            {
                var touch = touches[0]; point = touch.position.ReadValue(); down = touch.press.wasPressedThisFrame; pressed = true; up = false;
            }
            else
            {
                var mouse = Mouse.current; point = mouse != null ? mouse.position.ReadValue() : _last;
                down = mouse != null && mouse.leftButton.wasPressedThisFrame; pressed = mouse != null && mouse.leftButton.isPressed;
                up = _held && !pressed;
                if (mouse != null && !OverUi(point)) { float scroll = mouse.scroll.ReadValue().y; if (Mathf.Abs(scroll) > .01f) _map.View.Zoom(Mathf.Exp(-scroll * _map.Config.wheelZoomSpeed / 1000f), point); }
            }
            if (down) { _held = true; _start = _last = point; _blocked = OverUi(point); _dragged = false; }
            if (_held && pressed && !_blocked)
            {
                float threshold = _map.Config.dragThresholdDp * DisplayMetrics.Dpi / 160;
                _dragged |= Vector2.Distance(_start, point) > threshold;
                if (_dragged) _map.View.Pan(_last, point); _last = point;
            }
            if (up)
            {
                if (!_blocked && !_dragged && !OverUi(_last)) _map.GroundTap(_last);
                _held = false;
            }
        }
    }
}

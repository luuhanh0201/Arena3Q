using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// World gestures (spec §5), for both mouse and touch through the Input System:
    /// tap hero = select, tap ground = move the selected hero, swipe starting on a hero = attack the enemy under the
    /// release point, drag starting on empty ground = pan, pinch / scroll = zoom. Pointers that start over UI are ignored.
    /// </summary>
    public sealed class BattleInputController : MonoBehaviour
    {
        private enum Gesture
        {
            None,
            Pending,
            Pan,
            AttackSwipe,
            Blocked
        }

        [SerializeField] private BattleController battle;
        [SerializeField] private BattleCameraController cameraController;
        [Tooltip("Max pointer travel for a tap, in inches (converted with Screen.dpi).")]
        [SerializeField, Min(0.01f)] private float tapMaxInches = 0.12f;
        [SerializeField, Min(0.05f)] private float tapMaxSeconds = 0.4f;
        [SerializeField, Range(0.01f, 0.5f)] private float scrollZoomStep = 0.1f;

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private Gesture _gesture;
        private Vector2 _startScreen;
        private Vector2 _lastScreen;
        private float _startTime;
        private HeroUnit _swipeHero;
        private float _pinchDistance = -1f;
        private SpriteRenderer _swipeLine;

        public void Bind(BattleController controller, BattleCameraController cameraRig)
        {
            battle = controller;
            cameraController = cameraRig;
        }

        private float TapMaxPixels => Mathf.Max(10f, (Screen.dpi > 0f ? Screen.dpi : 160f) * tapMaxInches);

        private void OnEnable() => EnhancedTouchSupport.Enable();

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            Cancel();
        }

        private void Update()
        {
            if (battle == null || cameraController == null || battle.State == null)
                return;

            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
            {
                Pinch(touches[0].screenPosition, touches[1].screenPosition);
                return;
            }
            _pinchDistance = -1f;

            if (touches.Count == 1)
            {
                var touch = touches[0];
                switch (touch.phase)
                {
                    case TouchPhase.Began: Begin(touch.screenPosition); break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary: Move(touch.screenPosition); break;
                    case TouchPhase.Ended: End(touch.screenPosition); break;
                    case TouchPhase.Canceled: Cancel(); break;
                }
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
                return;
            var position = mouse.position.ReadValue();

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUi(position))
                cameraController.Zoom(scroll > 0f ? 1f - scrollZoomStep : 1f + scrollZoomStep, position);

            if (mouse.leftButton.wasPressedThisFrame)
                Begin(position);
            else if (mouse.leftButton.isPressed)
                Move(position);
            if (mouse.leftButton.wasReleasedThisFrame)
                End(position);
        }

        private void Begin(Vector2 screen)
        {
            _startScreen = screen;
            _lastScreen = screen;
            _startTime = Time.unscaledTime;
            _swipeHero = null;

            if (IsOverUi(screen) || battle.IsOver)
            {
                _gesture = Gesture.Blocked;
                return;
            }
            _gesture = Gesture.Pending;
            _swipeHero = battle.FindHeroAt(cameraController.ScreenToWorld(screen));
        }

        private void Move(Vector2 screen)
        {
            switch (_gesture)
            {
                case Gesture.Pending:
                    if (GestureRules.HasMovedBeyond(_startScreen, screen, TapMaxPixels))
                    {
                        _gesture = _swipeHero != null ? Gesture.AttackSwipe : Gesture.Pan;
                        if (_gesture == Gesture.Pan)
                            cameraController.Pan(screen - _startScreen);
                    }
                    break;
                case Gesture.Pan:
                    cameraController.Pan(screen - _lastScreen);
                    break;
                case Gesture.AttackSwipe:
                    UpdateSwipeLine(screen);
                    break;
            }
            _lastScreen = screen;
        }

        private void End(Vector2 screen)
        {
            var world = cameraController.ScreenToWorld(screen);
            switch (_gesture)
            {
                case Gesture.Pending:
                    if (GestureRules.IsTap(_startScreen, screen, Time.unscaledTime - _startTime, TapMaxPixels, tapMaxSeconds))
                        Tap(world);
                    break;
                case Gesture.AttackSwipe:
                    if (_swipeHero != null && _swipeHero.IsAlive)
                    {
                        var target = battle.FindEnemyAt(world);
                        if (target != null)
                            battle.AttackWithHero(_swipeHero.Id, target);
                    }
                    break;
            }
            Cancel();
        }

        private void Tap(Vector2 world)
        {
            var hero = battle.FindHeroAt(world);
            if (hero != null)
            {
                battle.TrySelectHero(hero.Id);
                return;
            }
            if (battle.SelectedHero != null)
                battle.MoveSelectedHero(world);
        }

        private void Pinch(Vector2 a, Vector2 b)
        {
            Cancel();
            _gesture = Gesture.Blocked;
            var distance = Vector2.Distance(a, b);
            if (_pinchDistance > 0f && distance > 1f)
                cameraController.Zoom(_pinchDistance / distance, (a + b) * 0.5f);
            _pinchDistance = distance;
        }

        private void Cancel()
        {
            _gesture = Gesture.None;
            _swipeHero = null;
            if (_swipeLine != null)
                _swipeLine.enabled = false;
        }

        private void UpdateSwipeLine(Vector2 screen)
        {
            if (_swipeHero == null || !_swipeHero.IsAlive)
                return;
            if (_swipeLine == null)
            {
                _swipeLine = PlaceholderArt.CreateRenderer("AttackSwipe", transform, PlaceholderArt.Square, Color.white,
                    Vector2.one, 31000);
            }
            var from = _swipeHero.Position;
            var to = cameraController.ScreenToWorld(screen);
            var delta = to - from;
            _swipeLine.enabled = true;
            _swipeLine.color = battle.FindEnemyAt(to) != null ? new Color(1f, 0.3f, 0.25f, 0.9f) : new Color(1f, 1f, 1f, 0.6f);
            var t = _swipeLine.transform;
            t.position = (from + to) * 0.5f;
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(delta.magnitude, 0.12f, 1f);
        }

        /// <summary>Whether a screen point is over any uGUI element (spec §5: no commands through UI).</summary>
        public bool IsOverUi(Vector2 screen)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            _uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screen }, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}

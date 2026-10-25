using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Battle camera (spec §9): drag to pan, pinch/scroll to zoom, always inside the map bounds, and follows the
    /// selected hero while it moves. Manual panning pauses following until a hero is selected again.
    /// Panning, zoom and limits work in the screen plane (x, y · cos(tilt)); in 3D the camera itself is tilted and
    /// orthographic and looks at the ground point under that plane point, and picking casts a ray down to the ground.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattleCameraController : MonoBehaviour
    {
        [SerializeField] private BattleController battle;
        [SerializeField, Min(1f)] private float minOrthographicSize = 3.5f;
        [SerializeField, Min(1f)] private float maxOrthographicSize = 11f;
        [SerializeField, Min(0.1f)] private float startOrthographicSize = 7f;
        [Tooltip("How quickly the camera catches up with the followed hero.")]
        [SerializeField, Min(0.1f)] private float followSharpness = 4f;

        private Camera _camera;
        private bool _followPaused;
        private Vector2? _focusTarget; // in the screen plane
        private Vector2 _center;       // in the screen plane
        private float _lastYScale = 1f;

        public Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();
        public bool IsFollowPaused => _followPaused;

        public void Bind(BattleController controller)
        {
            if (battle != null)
            {
                battle.SelectionChanged -= OnSelectionChanged;
                battle.BattleStarted -= OnBattleStarted;
            }
            battle = controller;
            if (battle != null && isActiveAndEnabled)
            {
                battle.SelectionChanged += OnSelectionChanged;
                battle.BattleStarted += OnBattleStarted;
            }
        }

        private void OnEnable()
        {
            Camera.orthographic = true;
            _center = transform.position;
            if (battle != null)
            {
                battle.SelectionChanged += OnSelectionChanged;
                battle.BattleStarted += OnBattleStarted;
                if (battle.Stage != null)
                    OnBattleStarted();
            }
        }

        private void OnDisable()
        {
            if (battle == null)
                return;
            battle.SelectionChanged -= OnSelectionChanged;
            battle.BattleStarted -= OnBattleStarted;
        }

        private void OnBattleStarted()
        {
            _followPaused = false;
            _focusTarget = null;
            BattleView3D.ConfigureCamera(Camera, battle);
            Camera.orthographicSize = startOrthographicSize;
            // Start looking at the player's side of the map.
            var stage = battle.Stage;
            var start = Vector2.Lerp(stage.PlayerCastle.Position, stage.MapBounds.center, 0.35f);
            _lastYScale = battle.GroundProjection;
            SetCenter(battle.ToScreenPlane(start));
        }

        private void OnSelectionChanged(UnitId heroId)
        {
            // Selecting a hero (again) resumes following.
            if (heroId.IsValid)
                _followPaused = false;
        }

        /// <summary>Drag the view by a screen-space delta in pixels.</summary>
        public void Pan(Vector2 screenDelta)
        {
            var worldPerPixel = Camera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            SetCenter(_center - screenDelta * worldPerPixel);
            _followPaused = true;
            _focusTarget = null;
        }

        /// <summary>Zoom by a factor (&gt;1 zooms out) keeping the world point under <paramref name="screenFocus"/> fixed.</summary>
        public void Zoom(float factor, Vector2 screenFocus)
        {
            if (factor <= 0f || battle == null || battle.Stage == null)
                return;
            var before = ScreenToPlane(screenFocus);
            var maxSize = Mathf.Min(maxOrthographicSize, CameraMath.MaxOrthographicSize(battle.ViewBounds, Camera.aspect));
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize * factor, Mathf.Min(minOrthographicSize, maxSize), maxSize);
            var after = ScreenToPlane(screenFocus);
            SetCenter(_center + (before - after));
        }

        /// <summary>Smoothly bring a logic-plane point into the centre of the view (edge icon tap).</summary>
        public void FocusOn(Vector2 worldPoint)
        {
            _focusTarget = battle != null ? battle.ToScreenPlane(worldPoint) : worldPoint;
            _followPaused = false;
        }

        /// <summary>Screen pixel → logic-plane point (undoes the tilt). Use this for all gameplay picking.</summary>
        public Vector2 ScreenToWorld(Vector2 screenPoint)
        {
            if (battle != null && battle.Presentation3D)
            {
                var ray = Camera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
                if (TiltedViewMath.RayToGround(ray.origin, ray.direction, out var ground))
                    return ground;
            }
            var view = (Vector2)Camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            return battle != null ? battle.ToLogic(view) : view;
        }

        /// <summary>Logic-plane point → viewport (0..1 on screen).</summary>
        public Vector2 WorldToViewport(Vector2 worldPoint) =>
            Camera.WorldToViewportPoint(battle != null ? battle.ToView(worldPoint) : (Vector3)worldPoint);

        /// <summary>Logic-plane point → screen pixel.</summary>
        public Vector2 WorldToScreen(Vector2 worldPoint) =>
            Camera.WorldToScreenPoint(battle != null ? battle.ToView(worldPoint) : (Vector3)worldPoint);

        private Vector2 ScreenToPlane(Vector2 screenPoint)
        {
            var logic = ScreenToWorld(screenPoint);
            return battle != null ? battle.ToScreenPlane(logic) : logic;
        }

        private void LateUpdate()
        {
            if (battle == null || battle.Stage == null)
                return;

            // Tilt changed: keep looking at the same logic point.
            var yScale = battle.GroundProjection;
            if (!Mathf.Approximately(yScale, _lastYScale))
            {
                _center = new Vector2(_center.x, _center.y / _lastYScale * yScale);
                if (_focusTarget.HasValue)
                    _focusTarget = new Vector2(_focusTarget.Value.x, _focusTarget.Value.y / _lastYScale * yScale);
                _lastYScale = yScale;
            }

            Vector2? goal = null;
            if (_focusTarget.HasValue)
            {
                goal = _focusTarget.Value;
                if ((_center - goal.Value).sqrMagnitude < 0.01f)
                    _focusTarget = null;
            }
            else if (!_followPaused)
            {
                var hero = battle.SelectedHero;
                if (hero != null && hero.State != HeroState.Idle)
                    goal = battle.ToScreenPlane(hero.Position);
            }

            if (goal.HasValue)
            {
                var t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                SetCenter(Vector2.Lerp(_center, goal.Value, t));
            }
            else
            {
                // Re-clamp in case the screen aspect or the tilt changed.
                SetCenter(_center);
            }
        }

        private void SetCenter(Vector2 center)
        {
            if (battle != null && battle.Stage != null)
            {
                // Clamp the size too: startup, aspect/tilt changes and external FitCamera calls can all
                // leave a viewport larger than the ground even without a pinch/scroll event.
                float maxSize = Mathf.Min(maxOrthographicSize, CameraMath.MaxOrthographicSize(battle.ViewBounds, Camera.aspect));
                Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize, Mathf.Min(minOrthographicSize, maxSize), maxSize);
                center = CameraMath.ClampCenter(battle.ViewBounds, center, Camera.orthographicSize, Camera.aspect);
            }
            _center = center;
            if (battle != null && battle.Presentation3D)
                BattleView3D.PlaceCamera(transform, battle, TiltedViewMath.PlaneToGround(center, battle.GroundProjection));
            else
                transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}

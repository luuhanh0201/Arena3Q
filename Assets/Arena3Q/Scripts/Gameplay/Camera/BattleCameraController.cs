using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Battle camera (spec §9): drag to pan, pinch/scroll to zoom, always inside the map bounds, and follows the
    /// selected hero while it moves. Manual panning pauses following until a hero is selected again.
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
        private Vector2? _focusTarget;

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
            Camera.orthographicSize = startOrthographicSize;
            // Start looking at the player's side of the map.
            var stage = battle.Stage;
            var start = Vector2.Lerp(stage.PlayerCastle.Position, stage.MapBounds.center, 0.35f);
            SetCenter(start);
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
            SetCenter((Vector2)transform.position - screenDelta * worldPerPixel);
            _followPaused = true;
            _focusTarget = null;
        }

        /// <summary>Zoom by a factor (&gt;1 zooms out) keeping the world point under <paramref name="screenFocus"/> fixed.</summary>
        public void Zoom(float factor, Vector2 screenFocus)
        {
            if (factor <= 0f || battle == null || battle.Stage == null)
                return;
            var before = ScreenToWorld(screenFocus);
            var maxSize = Mathf.Min(maxOrthographicSize, CameraMath.MaxOrthographicSize(battle.Stage.MapBounds, Camera.aspect));
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize * factor, Mathf.Min(minOrthographicSize, maxSize), maxSize);
            var after = ScreenToWorld(screenFocus);
            SetCenter((Vector2)transform.position + (before - after));
        }

        /// <summary>Smoothly bring a world point into the centre of the view (edge icon tap).</summary>
        public void FocusOn(Vector2 worldPoint)
        {
            _focusTarget = worldPoint;
            _followPaused = false;
        }

        public Vector2 ScreenToWorld(Vector2 screenPoint) => Camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));

        public Vector2 WorldToViewport(Vector2 worldPoint) => Camera.WorldToViewportPoint(worldPoint);

        private void LateUpdate()
        {
            if (battle == null || battle.Stage == null)
                return;

            Vector2? goal = null;
            if (_focusTarget.HasValue)
            {
                goal = _focusTarget.Value;
                if (((Vector2)transform.position - goal.Value).sqrMagnitude < 0.01f)
                    _focusTarget = null;
            }
            else if (!_followPaused)
            {
                var hero = battle.SelectedHero;
                if (hero != null && hero.State != HeroState.Idle)
                    goal = hero.Position;
            }

            if (goal.HasValue)
            {
                var t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                SetCenter(Vector2.Lerp(transform.position, goal.Value, t));
            }
            else
            {
                // Re-clamp in case the screen aspect changed.
                SetCenter(transform.position);
            }
        }

        private void SetCenter(Vector2 center)
        {
            if (battle != null && battle.Stage != null)
                center = CameraMath.ClampCenter(battle.Stage.MapBounds, center, Camera.orthographicSize, Camera.aspect);
            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}

using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Battle camera (spec §9, user 2026-10-28): a fixed diagonal orthographic view (tilt and turn from
    /// <see cref="Arena3Q.Data.BattleViewConfig"/>), drag to pan, pinch/scroll to zoom. The whole view always stays on the
    /// ground the map builds (map bounds plus <c>CameraGroundMargin</c> of outer ground), so the empty background never
    /// shows; zooming in stops at <c>ZoomInLimit</c> of the default view so asset flaws do not show. It no longer follows
    /// a hero (no per-hero control); edge icons bring a hero into view with <see cref="FocusOn"/>.
    /// The camera looks at a ground point of the logic plane; picking casts a ray down to the ground.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattleCameraController : MonoBehaviour
    {
        [SerializeField] private BattleController battle;
        [Tooltip("How quickly the camera glides to a point brought into view (edge icon tap).")]
        [SerializeField, Min(0.1f)] private float followSharpness = 4f;

        private Camera _camera;
        private Vector2 _focus;          // ground point under the view centre (logic plane)
        private Vector2? _focusTarget;   // ground point the view glides to
        private float _defaultSize = 1f;

        public Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();

        /// <summary>Ground point under the centre of the view (logic plane).</summary>
        public Vector2 Focus => _focus;

        /// <summary>Orthographic size of the default view (the zoom the battle starts at).</summary>
        public float DefaultOrthographicSize => _defaultSize;

        public float MinOrthographicSize => _defaultSize * (battle != null ? battle.View.ZoomInLimit : 1f);

        /// <summary>Largest size whose view stays inside the built ground (map plus outer ground).</summary>
        public float MaxOrthographicSize =>
            battle != null && battle.Stage != null
                ? TiltedViewMath.MaxOrthographicSize(GroundBounds, battle.CameraRotation, Camera.aspect)
                : Camera.orthographicSize;

        /// <summary>The ground the view may show: the map bounds plus the outer ground around them.</summary>
        public Rect GroundBounds
        {
            get
            {
                var map = battle.Stage.MapBounds;
                var margin = battle.Presentation3D ? battle.View.CameraGroundMargin : 0f;
                return new Rect(map.xMin - margin, map.yMin - margin, map.width + margin * 2f, map.height + margin * 2f);
            }
        }

        public void Bind(BattleController controller)
        {
            if (battle != null)
                battle.BattleStarted -= OnBattleStarted;
            battle = controller;
            if (battle != null && isActiveAndEnabled)
                battle.BattleStarted += OnBattleStarted;
        }

        private void OnEnable()
        {
            Camera.orthographic = true;
            if (battle != null)
            {
                battle.BattleStarted += OnBattleStarted;
                if (battle.Stage != null)
                    OnBattleStarted();
            }
        }

        private void OnDisable()
        {
            if (battle != null)
                battle.BattleStarted -= OnBattleStarted;
        }

        private void OnBattleStarted()
        {
            _focusTarget = null;
            BattleView3D.ConfigureCamera(Camera, battle);
            _focus = battle.Stage.MapBounds.center;
            _defaultSize = ComputeDefaultSize();
            Camera.orthographicSize = _defaultSize;
            Apply();
        }

        // Default zoom: the view spans DefaultViewMapWidths map widths along the map's x axis, never more than fits.
        private float ComputeDefaultSize()
        {
            var box = TiltedViewMath.FootprintBox(battle.CameraRotation, 1f, Camera.aspect);
            var wanted = battle.Stage.MapBounds.width * battle.View.DefaultViewMapWidths / Mathf.Max(1e-4f, box.width);
            return Mathf.Min(wanted, MaxOrthographicSize);
        }

        /// <summary>Drag the view so the ground point under <paramref name="fromScreen"/> moves under <paramref name="toScreen"/>.</summary>
        public void Pan(Vector2 fromScreen, Vector2 toScreen)
        {
            _focus += ScreenToWorld(fromScreen) - ScreenToWorld(toScreen);
            _focusTarget = null;
            Apply();
        }

        /// <summary>Drag the view by a screen-space delta in pixels (from the screen centre).</summary>
        public void Pan(Vector2 screenDelta)
        {
            var centre = new Vector2(Camera.pixelWidth, Camera.pixelHeight) * 0.5f;
            Pan(centre, centre + screenDelta);
        }

        /// <summary>Zoom by a factor (&gt;1 zooms out) keeping the ground point under <paramref name="screenFocus"/> fixed.</summary>
        public void Zoom(float factor, Vector2 screenFocus)
        {
            if (factor <= 0f || battle == null || battle.Stage == null)
                return;
            var before = ScreenToWorld(screenFocus);
            Camera.orthographicSize = ClampSize(Camera.orthographicSize * factor);
            Apply();
            var after = ScreenToWorld(screenFocus);
            _focus += before - after;
            Apply();
        }

        /// <summary>Smoothly bring a logic-plane point into the centre of the view (edge icon tap).</summary>
        public void FocusOn(Vector2 worldPoint) => _focusTarget = worldPoint;

        /// <summary>Screen pixel → logic-plane point (a ray down to the ground). Use this for all gameplay picking.</summary>
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

        private void LateUpdate()
        {
            if (battle == null || battle.Stage == null)
                return;
            if (_focusTarget.HasValue)
            {
                var t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                _focus = Vector2.Lerp(_focus, _focusTarget.Value, t);
                if ((_focus - _focusTarget.Value).sqrMagnitude < 0.01f)
                    _focusTarget = null;
            }
            // Re-clamp every frame: the screen aspect can change (rotation, web window resize).
            Apply();
        }

        private float ClampSize(float size)
        {
            var max = MaxOrthographicSize;
            return Mathf.Clamp(size, Mathf.Min(MinOrthographicSize, max), max);
        }

        private void Apply()
        {
            if (battle == null || battle.Stage == null)
                return;
            if (!battle.Presentation3D)
            {
                transform.position = new Vector3(_focus.x, _focus.y * battle.ViewYScale, transform.position.z);
                return;
            }
            var rotation = battle.CameraRotation;
            Camera.orthographicSize = ClampSize(Camera.orthographicSize);
            _focus = TiltedViewMath.ClampFocus(GroundBounds, _focus, rotation, Camera.orthographicSize, Camera.aspect);
            BattleView3D.PlaceCamera(transform, battle, _focus);
        }
    }
}

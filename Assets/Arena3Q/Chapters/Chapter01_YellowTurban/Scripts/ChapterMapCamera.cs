using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Exploration
{
    public sealed class ChapterMapCamera : MonoBehaviour
    {
        private ChapterMapConfig _config;
        public Camera Camera { get; private set; }
        public Vector3 Focus { get; private set; }
        private Transform _follow;
        public void Configure(Camera camera, ChapterMapConfig config, Vector3 focus, float size)
        {
            Camera = camera; _config = config; Focus = focus;
            Camera.orthographic = true; Camera.transform.rotation = Quaternion.Euler(config.cameraTilt, config.cameraYaw, 0);
            Camera.orthographicSize = Mathf.Clamp(size, config.cameraMinSize, config.cameraMaxSize); Apply();
        }
        public void Follow(Transform target) => _follow = target;
        public void FocusOn(Vector3 point) { _follow = null; Focus = point; Apply(); }
        public Vector3 Ground(Vector2 point)
        {
            var ray = Camera.ScreenPointToRay(point);
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var distance) ? ray.GetPoint(distance) : Focus;
        }
        public void Pan(Vector2 before, Vector2 after) { _follow = null; Focus += Ground(before) - Ground(after); Apply(); }
        public void Zoom(float multiplier, Vector2 point)
        {
            var before = Ground(point); Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize * multiplier, _config.cameraMinSize, _config.cameraMaxSize);
            Focus += before - Ground(point); Apply();
        }
        private void LateUpdate()
        {
            if (Camera == null) return;
            if (_follow != null) Focus = Vector3.Lerp(Focus, _follow.position, 1 - Mathf.Exp(-_config.cameraFollowSpeed * Time.unscaledDeltaTime));
            Apply();
        }
        private void Apply()
        {
            // Project all corners to the XZ ground; clamp the entire footprint, including on window resize.
            Camera.transform.position = Focus - Camera.transform.forward * Camera.farClipPlane * .25f;
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var p in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
            {
                var ray = Camera.ViewportPointToRay(p);
                if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out var d)) continue;
                var v = ray.GetPoint(d) - Focus; min = Vector2.Min(min, new Vector2(v.x, v.z)); max = Vector2.Max(max, new Vector2(v.x, v.z));
            }
            var b = _config.groundBounds; var margin = _config.cameraGroundMargin;
            var low = new Vector2(b.xMin - margin, b.yMin - margin) - min; var high = new Vector2(b.xMax + margin, b.yMax + margin) - max;
            Focus = new Vector3(low.x <= high.x ? Mathf.Clamp(Focus.x, low.x, high.x) : b.center.x, 0,
                low.y <= high.y ? Mathf.Clamp(Focus.z, low.y, high.y) : b.center.y);
            Camera.transform.position = Focus - Camera.transform.forward * Camera.farClipPlane * .25f;
        }
    }
}

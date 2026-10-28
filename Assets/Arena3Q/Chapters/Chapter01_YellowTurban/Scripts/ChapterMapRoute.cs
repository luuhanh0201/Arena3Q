using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Exploration
{
    /// <summary>One ground ribbon following the authored road, with distance-based dashes.</summary>
    public sealed class ChapterMapRoute : MonoBehaviour
    {
        private ChapterMapController _map;
        private Mesh _mesh;
        private Material _material;
        private float[] _stageDistances;
        public Mesh RouteMesh => _mesh;
        public void Configure(ChapterMapController map)
        {
            _map = map;
            var config = map.Config; var points = config.roadRoute; var ui = config.ui;
            if (points == null || points.Length < 2 || ui.routeMaterial == null) return;
            var vertices = new Vector3[points.Length * 2]; var uv = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6]; var distances = new float[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
                var tangent = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(0, i - 1)];
                var side = Vector3.Cross(Vector3.up, tangent).normalized * ui.routeWidth * .5f;
                vertices[i * 2] = points[i] - side + Vector3.up * ui.routeLift;
                vertices[i * 2 + 1] = points[i] + side + Vector3.up * ui.routeLift;
                uv[i * 2] = new Vector2(distances[i], -1); uv[i * 2 + 1] = new Vector2(distances[i], 1);
                if (i == 0) continue;
                int t = (i - 1) * 6, v = i * 2;
                triangles[t] = v - 2; triangles[t + 1] = v; triangles[t + 2] = v - 1;
                triangles[t + 3] = v - 1; triangles[t + 4] = v; triangles[t + 5] = v + 1;
            }
            _stageDistances = new float[config.stages.Length];
            for (int stage = 0; stage < config.stages.Length; stage++)
            {
                float best = float.MaxValue;
                for (int i = 0; i < points.Length; i++)
                {
                    var delta = points[i] - config.stages[stage].center; delta.y = 0;
                    if (delta.sqrMagnitude >= best) continue;
                    best = delta.sqrMagnitude; _stageDistances[stage] = distances[i];
                }
            }
            _mesh = new Mesh { name = "Chapter road guide", vertices = vertices, uv = uv, triangles = triangles };
            _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            _material = new Material(ui.routeMaterial);
            _material.SetColor("_RouteColor", ui.routeColor); _material.SetColor("_OutlineColor", ui.routeOutline);
            _material.SetFloat("_DashLength", ui.routeDashLength); _material.SetFloat("_DashFill", ui.routeDashFill);
            _material.SetFloat("_StartDistance", _stageDistances[0]);
            var root = new GameObject("Road guidance", typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(transform, false);
            root.GetComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = root.GetComponent<MeshRenderer>(); renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        private void LateUpdate()
        {
            if (_material == null || _map.Session == null) return;
            int current = Mathf.Max(0, _map.Session.Rules.UnlockedCount(_map.Session.Progress) - 1);
            _material.SetFloat("_ActiveStart", _stageDistances[current]);
            _material.SetFloat("_EndDistance", _stageDistances[Mathf.Min(current + 1, _stageDistances.Length - 1)]);
            _material.SetFloat("_Clock", Time.unscaledTime * _map.Config.motion.routeDashesPerSecond);
        }
        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
        }
    }
}

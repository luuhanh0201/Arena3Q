using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// The painted ground as a 3D mesh (docs/KeHoach/ChuyenSang3D.md §12 decision 2: mesh + texture). Inside the map
    /// bounds it is exactly the flat logic plane (z = 0) the units stand on; outside them, where no unit goes, low hills
    /// rise and a skirt continues the painting's edge so the field does not float in empty space. Pictures only.
    /// </summary>
    internal static class Ground3DView
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        public static MeshRenderer Create(Transform parent, Sprite art, Rect area, Rect mapBounds, BattleViewConfig view)
        {
            var mesh = BuildMesh(art, area, mapBounds, view);
            var go = new GameObject("Ground3D", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = view.GroundMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            var block = new MaterialPropertyBlock();
            block.SetTexture(BaseMapId, art.texture);
            renderer.SetPropertyBlock(block);
            return renderer;
        }

        /// <summary>Height above the ground plane (metres) at a logic point: 0 inside the map bounds.</summary>
        public static float Height(Vector2 point, Rect mapBounds, BattleViewConfig view)
        {
            var outside = Mathf.Max(Mathf.Max(mapBounds.xMin - point.x, point.x - mapBounds.xMax),
                Mathf.Max(mapBounds.yMin - point.y, point.y - mapBounds.yMax));
            if (outside <= 0f || view.GroundRimHeight <= 0f)
                return 0f;
            var rise = Mathf.SmoothStep(0f, 1f, outside / Mathf.Max(0.01f, view.GroundRimRamp));
            // Irregular low hills, not a regular wall: two octaves of value noise.
            var hills = 0.6f * Noise(point * 0.23f) + 0.4f * Noise(point * 0.51f + new Vector2(7.1f, 3.3f));
            return view.GroundRimHeight * rise * Mathf.Lerp(0.35f, 1f, hills);
        }

        private static Mesh BuildMesh(Sprite art, Rect area, Rect mapBounds, BattleViewConfig view)
        {
            var skirt = view.GroundSkirt;
            var outer = new Rect(area.xMin - skirt, area.yMin - skirt, area.width + 2f * skirt, area.height + 2f * skirt);
            var cell = Mathf.Max(0.1f, view.GroundCell);
            // Grid lines fall exactly on the painting's edges, where the skirt's mirrored copy folds.
            var xs = Axis(outer.xMin, area.xMin, area.xMax, outer.xMax, cell);
            var ys = Axis(outer.yMin, area.yMin, area.yMax, outer.yMax, cell);
            var nx = xs.Count - 1;
            var ny = ys.Count - 1;
            var vertices = new Vector3[(nx + 1) * (ny + 1)];
            var uvs = new Vector2[vertices.Length];
            var texture = art.texture;
            var rect = art.textureRect;
            for (var j = 0; j <= ny; j++)
            {
                for (var i = 0; i <= nx; i++)
                {
                    var p = new Vector2(xs[i], ys[j]);
                    var index = j * (nx + 1) + i;
                    vertices[index] = new Vector3(p.x, p.y, -Height(p, mapBounds, view));
                    // The skirt continues the painting mirrored (grass carries on without stretched edge streaks).
                    var u = Mirror((p.x - area.xMin) / area.width);
                    var v = Mirror((p.y - area.yMin) / area.height);
                    uvs[index] = new Vector2((rect.x + u * rect.width) / texture.width, (rect.y + v * rect.height) / texture.height);
                }
            }
            var triangles = new int[nx * ny * 6];
            var t = 0;
            for (var j = 0; j < ny; j++)
            {
                for (var i = 0; i < nx; i++)
                {
                    var a = j * (nx + 1) + i;
                    var b = a + 1;
                    var c = a + nx + 1;
                    var d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }
            var mesh = new Mesh { name = "Ground3D", hideFlags = HideFlags.DontSave };
            if (vertices.Length > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            // Normals of the battle space point up = -Z; flip the ones the winding made point down.
            var normals = mesh.normals;
            for (var k = 0; k < normals.Length; k++)
                if (normals[k].z > 0f)
                    normals[k] = -normals[k];
            mesh.normals = normals;
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        // 0..1 inside the painting, mirrored back beyond its edges (…, 1 → 0, 0 → 1, …).
        private static float Mirror(float t)
        {
            var m = Mathf.Repeat(t, 2f);
            return m <= 1f ? m : 2f - m;
        }

        // Positions along one axis: the skirt, the painting and the skirt again, each split into cells of about
        // `cell`, so the painting's edges are grid lines.
        private static System.Collections.Generic.List<float> Axis(float min, float areaMin, float areaMax, float max, float cell)
        {
            var points = new System.Collections.Generic.List<float> { min };
            foreach (var (from, to) in new[] { (min, areaMin), (areaMin, areaMax), (areaMax, max) })
            {
                if (to - from <= 1e-5f)
                    continue;
                var steps = Mathf.Max(1, Mathf.CeilToInt((to - from) / cell - 1e-4f));
                for (var k = 1; k <= steps; k++)
                    points.Add(from + (to - from) * k / steps);
            }
            return points;
        }

        private static float Noise(Vector2 p)
        {
            var i = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
            var f = p - i;
            var u = f * f * (new Vector2(3f, 3f) - 2f * f);
            return Mathf.Lerp(Mathf.Lerp(Hash(i), Hash(i + Vector2.right), u.x),
                Mathf.Lerp(Hash(i + Vector2.up), Hash(i + Vector2.one), u.x), u.y);
        }

        private static float Hash(Vector2 p)
        {
            var h = Mathf.Sin(p.x * 127.1f + p.y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}

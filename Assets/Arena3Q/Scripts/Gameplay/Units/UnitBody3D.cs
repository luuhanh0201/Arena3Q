using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// 3D placeholder body of a unit without a model or a picture (docs/KeHoach/ChuyenSang3D.md P1): a lit capsule in
    /// the unit's colour with a small "nose" showing where it faces, standing on the ground (up = -Z). Lives under the
    /// unit root, which faces the camera, so its local rotation undoes that first. Pictures only.
    /// </summary>
    internal sealed class UnitBody3D
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Mesh _capsule;
        private static Mesh _cube;

        // Model space: Y up and +Z forward; this turns it so up = -Z and forward = +Y (north) on the logic ground.
        private static readonly Quaternion StandUp = Quaternion.Euler(-90f, 0f, 0f);

        private readonly Transform _transform;
        private readonly MeshRenderer[] _renderers;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Quaternion _groundFromRoot = Quaternion.identity;
        private float _facingDegrees = 90f;
        private Color _color;

        public float Height { get; }

        public UnitBody3D(Transform root, Material material, Color color, float diameter, float height)
        {
            Height = height;
            var go = new GameObject("Body3D");
            go.transform.SetParent(root, false);
            _transform = go.transform;

            var width = diameter * 0.8f;
            var body = Part("Capsule", Capsule, material, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height * 0.5f, width));
            var nose = Part("Nose", Cube, material, new Vector3(0f, height * 0.72f, width * 0.45f),
                new Vector3(width * 0.3f, width * 0.22f, width * 0.35f));
            _renderers = new[] { body, nose };
            SetColor(color);
            Apply();
        }

        private MeshRenderer Part(string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
        }

        /// <summary>The unit root's rotation is undone by this (inverse of the billboard rotation).</summary>
        public void SetGroundFromRoot(Quaternion groundFromRoot)
        {
            _groundFromRoot = groundFromRoot;
            Apply();
        }

        /// <summary>Turns the body towards a direction on the logic ground (x east, y north).</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return;
            _facingDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Apply();
        }

        public float FacingDegrees => _facingDegrees;

        public void SetColor(Color color)
        {
            if (color == _color)
                return;
            _color = color;
            _block.SetColor(BaseColorId, color);
            foreach (var renderer in _renderers)
                renderer.SetPropertyBlock(_block);
        }

        public void Destroy()
        {
            if (_transform == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(_transform.gameObject);
            else
                Object.DestroyImmediate(_transform.gameObject);
        }

        // Forward (+Y on the ground) is 90°, so the yaw about the ground normal is the facing minus 90°.
        private void Apply() =>
            _transform.localRotation = _groundFromRoot * Quaternion.AngleAxis(_facingDegrees - 90f, Vector3.forward) * StandUp;

        // Built in code: GameObject.CreatePrimitive also adds a physics collider, and player builds strip the physics
        // module ("CapsuleCollider doesn't exist"), so the primitives never appeared on devices.
        private static Mesh Capsule => _capsule != null ? _capsule : _capsule = BuildCapsule(16, 6);
        private static Mesh Cube => _cube != null ? _cube : _cube = BuildCube();

        /// <summary>Capsule like Unity's primitive: radius 0.5, height 2, along Y, centred.</summary>
        private static Mesh BuildCapsule(int segments, int ringsPerCap)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            // Rings from the bottom pole to the top pole: bottom cap, then top cap (the cylinder joins them).
            var rings = new System.Collections.Generic.List<(float y, float radius, float normalY)>();
            for (var i = 0; i <= ringsPerCap; i++)
            {
                var a = -Mathf.PI * 0.5f + Mathf.PI * 0.5f * i / ringsPerCap;
                rings.Add((-0.5f + Mathf.Sin(a) * 0.5f, Mathf.Cos(a) * 0.5f, Mathf.Sin(a)));
            }
            for (var i = 0; i <= ringsPerCap; i++)
            {
                var a = Mathf.PI * 0.5f * i / ringsPerCap;
                rings.Add((0.5f + Mathf.Sin(a) * 0.5f, Mathf.Cos(a) * 0.5f, Mathf.Sin(a)));
            }
            foreach (var (y, radius, normalY) in rings)
            {
                var horizontal = Mathf.Sqrt(Mathf.Max(0f, 1f - normalY * normalY));
                for (var s = 0; s <= segments; s++)
                {
                    var angle = 2f * Mathf.PI * s / segments;
                    var x = Mathf.Cos(angle);
                    var z = Mathf.Sin(angle);
                    vertices.Add(new Vector3(x * radius, y, z * radius));
                    normals.Add(new Vector3(x * horizontal, normalY, z * horizontal).normalized);
                }
            }
            var triangles = new System.Collections.Generic.List<int>();
            var row = segments + 1;
            for (var r = 0; r < rings.Count - 1; r++)
            {
                for (var s = 0; s < segments; s++)
                {
                    var a = r * row + s;
                    var b = a + row;
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            }
            var mesh = new Mesh { name = "Placeholder capsule", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Unit cube centred on the origin, flat-shaded faces.</summary>
        private static Mesh BuildCube()
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = normal.y != 0f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(normal, u);
                var start = vertices.Count;
                foreach (var (du, dv) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
                {
                    vertices.Add((normal + u * du + v * dv) * 0.5f);
                    normals.Add(normal);
                }
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "Placeholder box", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

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

        private static Mesh Capsule => _capsule != null ? _capsule : _capsule = BuiltIn(PrimitiveType.Capsule);
        private static Mesh Cube => _cube != null ? _cube : _cube = BuiltIn(PrimitiveType.Cube);

        private static Mesh BuiltIn(PrimitiveType type)
        {
            var primitive = GameObject.CreatePrimitive(type);
            var mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(primitive);
            return mesh;
        }
    }
}

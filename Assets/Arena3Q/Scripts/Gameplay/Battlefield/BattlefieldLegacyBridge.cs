using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Scene-local rendering adapter. Existing combat/picking keep their tested XY coordinates;
    /// only their render hierarchy is rigidly rotated into the Y-up environment during camera rendering.
    /// Restores both transforms after rendering, including disable/exception recovery.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public sealed class BattlefieldLegacyBridge : MonoBehaviour
    {
        public BattleController battle;
        public Camera battleCamera;
        public BattlefieldEnvironment environment;
        public Collider altarCollider;
        private static readonly Quaternion Basis = Quaternion.Euler(90f, 0f, 0f);
        private Quaternion _rootRotation, _cameraRotation;
        private Vector3 _cameraPosition;
        private bool _rotated, _cameraMoved;
        public bool RenderingYUp => _rotated;
        private readonly List<(Transform target, Vector3 position)> _lifted = new List<(Transform, Vector3)>(128);

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (battle != null) battle.BattleStarted += Refresh;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }
        private void OnDisable()
        {
            Restore();
            if (battle != null) battle.BattleStarted -= Refresh;
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
        }
        public void Refresh()
        {
            if (battle == null) return;
            var world = battle.transform.Find("World");
            if (world == null) return;
            // The new scene owns static terrain/camps/altar/pillars; keep the live gates,
            // runes, summon overlays, health bars and battle units from the existing game.
            foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                bool ground = renderer.name == "MapArt" || renderer.name == "Ground3D" || renderer.name == "MapGround" || renderer.name == "Ground" || renderer.name == "ForbiddenZone" || renderer.name.StartsWith("Rune ");
                bool formationStatic = false;
                for (var parent = renderer.transform; parent != null && parent != world; parent = parent.parent)
                    if (parent.name.StartsWith("Pillar ") || parent.name == "Altar") formationStatic = true;
                if (ground || formationStatic) renderer.enabled = false;
            }
            foreach (var light in world.GetComponentsInChildren<Light>()) light.enabled = false;
            foreach (var volume in world.GetComponentsInChildren<Volume>()) volume.enabled = false;
            foreach (var unit in battle.Registry.Units)
            {
                if (!unit.IsCastle) continue;
                foreach (var renderer in unit.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = false;
                foreach (var sprite in unit.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.name == "Body" || sprite.name == "Prop shadow") sprite.enabled = false;
            }
            if (environment != null) environment.ApplyLighting();
        }
        private void Begin(ScriptableRenderContext context, Camera camera)
        {
            if (!Application.isPlaying || battle == null || _rotated || (camera != battleCamera && camera.cameraType != CameraType.SceneView)) return;
            _rootRotation = battle.transform.rotation;
            if (altarCollider != null)
            {
                var bounds = altarCollider.bounds;
                foreach (var unit in battle.Registry.Units)
                {
                    if (unit.IsCastle || Mathf.Abs(unit.Position.x-bounds.center.x)>bounds.extents.x || Mathf.Abs(unit.Position.y-bounds.center.z)>bounds.extents.z) continue;
                    if (altarCollider.Raycast(new Ray(ToEnvironment(unit.Position, bounds.max.y + 2f), Vector3.down), out var hit, bounds.size.y+4f))
                    {
                        _lifted.Add((unit.transform, unit.transform.localPosition));
                        unit.transform.localPosition += Vector3.back * Mathf.Max(0f,hit.point.y);
                    }
                }
            }
            battle.transform.rotation = Basis * _rootRotation;
            _rotated = true;
            if (camera == battleCamera)
            {
                _cameraPosition = camera.transform.position; _cameraRotation = camera.transform.rotation;
                camera.transform.SetPositionAndRotation(Basis * _cameraPosition, Basis * _cameraRotation);
                _cameraMoved = true;
            }
        }
        private void End(ScriptableRenderContext context, Camera camera) { if (_rotated && (camera == battleCamera || camera.cameraType == CameraType.SceneView)) Restore(); }
        private void Restore()
        {
            if (_cameraMoved && battleCamera != null) battleCamera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation);
            if (_rotated && battle != null) battle.transform.rotation = _rootRotation;
            foreach (var entry in _lifted) if (entry.target != null) entry.target.localPosition = entry.position;
            _lifted.Clear();
            _rotated = false; _cameraMoved = false;
        }
        public static Vector3 ToEnvironment(Vector2 logic, float height = 0f) => new Vector3(logic.x, height, logic.y);
    }
}

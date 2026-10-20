using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Stands the 3D model of a map prop where its picture would stand (docs/KeHoach/ChuyenSang3D.md P2): same size,
    /// ground pivot and yaw as the sprite PropSpriteRenderer made from it, with a soft shadow on the ground.
    /// Pictures only; the stage data and the forbidden zones do not change.
    /// </summary>
    internal static class MapProp3D
    {
        // Above the painted ground, the rune discs and the altar, below unit shadows.
        public const int ShadowOrder = -30995;

        // Model space (Y up, +Z north) → battle space (ground XY, up = -Z, north = +Y).
        private static readonly Quaternion StandUp = Quaternion.Euler(-90f, 0f, 0f);

        /// <summary>The model for a prop picture, in the 3D presentation only.</summary>
        public static bool TryFind(BattleController battle, Sprite sprite, out MapProps3DLibrary.Prop prop, out float yaw)
        {
            prop = null;
            yaw = 0f;
            return battle != null && battle.Presentation3D && battle.View.MapProps != null &&
                   battle.View.MapProps.TryFind(sprite, out prop, out yaw);
        }

        /// <summary>World rotation of a prop turned by <paramref name="yaw"/> (degrees about the vertical, as rendered).</summary>
        public static Quaternion Rotation(float yaw) => StandUp * Quaternion.Euler(0f, yaw, 0f);

        /// <summary>The model under <paramref name="parent"/>; its own transform is the ground pivot (place and turn it).</summary>
        public static Transform Create(string name, Transform parent, MapProps3DLibrary.Prop prop)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * prop.scale;
            var go = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.transform.localPosition = prop.meshPosition - prop.groundCentre;
            go.transform.localRotation = prop.meshRotation;
            go.transform.localScale = prop.meshScale;
            go.GetComponent<MeshFilter>().sharedMesh = prop.mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = prop.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return root;
        }

        /// <summary>Soft round shadow lying on the ground under a prop (no realtime shadows).</summary>
        public static SpriteRenderer CreateShadow(Transform parent, MapProps3DLibrary.Prop prop, float alpha)
        {
            var size = prop.footprint * 1.15f;
            var shadow = PlaceholderArt.CreateRenderer("Prop shadow", parent, PlaceholderArt.Blob, new Color(0f, 0f, 0f, alpha),
                new Vector2(size, size), ShadowOrder);
            return shadow;
        }

        /// <summary>Height of the placed model above the ground (world units).</summary>
        public static float Height(MapProps3DLibrary.Prop prop)
        {
            var bounds = prop.mesh.bounds;
            var matrix = Matrix4x4.TRS(prop.meshPosition, prop.meshRotation, prop.meshScale);
            var top = float.MinValue;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                top = Mathf.Max(top, matrix.MultiplyPoint3x4(corner).y);
            }
            return (top - prop.groundCentre.y) * prop.scale;
        }
    }
}

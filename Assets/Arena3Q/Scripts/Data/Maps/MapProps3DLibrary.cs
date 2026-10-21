using System;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// 3D versions of the map's prop pictures (docs/KeHoach/ChuyenSang3D.md P2): for every prop rendered into sprites
    /// by PropSpriteRenderer, its battle-budget mesh and material with the same size, ground pivot and yaw, so the 3D
    /// battle can stand the model wherever the stage data places the picture. Built by Arena3Q > Build 3D Map Props.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Map Props 3D")]
    public sealed class MapProps3DLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Prop
        {
            [Tooltip("Same name as the prop's sprites (\"<name>\" or \"<name>_<yaw>\").")]
            public string name;
            public Mesh mesh;
            public Material material;
            [Tooltip("Mesh → model space (Y up, +Z north) of the imported FBX, before sizing.")]
            public Vector3 meshPosition;
            public Quaternion meshRotation = Quaternion.identity;
            public Vector3 meshScale = Vector3.one;
            [Tooltip("Uniform scale that gives the prop its size on the map (as for the sprites).")]
            public float scale = 1f;
            [Tooltip("Centre of the model's footprint on the ground, in model space before scaling: the pivot.")]
            public Vector3 groundCentre;
            [Tooltip("Width of the footprint after scaling (soft shadow size).")]
            public float footprint = 1f;
            [Tooltip("Yaw at which the model's front (door, drum) faces the camera side (south); used to turn it towards a " +
                     "direction. The Meshy props face south at 0.")]
            public float frontYaw;
        }

        [SerializeField] private Prop[] props = Array.Empty<Prop>();

        public Prop[] Props => props;

        /// <summary>The model behind a prop sprite, and the yaw its picture was rendered at.</summary>
        public bool TryFind(Sprite sprite, out Prop prop, out float yaw)
        {
            prop = null;
            yaw = 0f;
            if (sprite == null)
                return false;
            var name = sprite.name;
            foreach (var candidate in props)
            {
                if (candidate?.mesh == null || !name.StartsWith(candidate.name, StringComparison.Ordinal))
                    continue;
                var rest = name.Substring(candidate.name.Length);
                if (rest.Length == 0)
                {
                    prop = candidate;
                    return true;
                }
                if (rest[0] == '_' && float.TryParse(rest.Substring(1), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out yaw))
                {
                    prop = candidate;
                    return true;
                }
            }
            return false;
        }

        public void SetProps(Prop[] value) => props = value ?? Array.Empty<Prop>();
    }
}

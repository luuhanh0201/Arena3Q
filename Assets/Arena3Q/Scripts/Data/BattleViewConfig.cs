using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// How the battle is presented (docs/KeHoach/ChuyenSang3D.md §12): a 3D scene seen by a tilted orthographic camera
    /// on the Universal (Forward) renderer, or the old flat 2.5D projection. Only the display changes; the battle
    /// always runs on the flat logic plane. The tilt itself comes from the stage's painted ground
    /// (<see cref="StageConfig.BackgroundArtTiltDegrees"/>), or <see cref="DefaultTiltDegrees"/> without one.
    /// Loaded from Resources by the battle.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Battle View")]
    public sealed class BattleViewConfig : ScriptableObject
    {
        public const string ResourceName = "BattleView";

        [Tooltip("3D presentation (tilted orthographic camera, models, billboards). Off = the old flat 2.5D squash.")]
        [SerializeField] private bool presentation3D = true;
        [Tooltip("Camera tilt when the stage has no painted ground. 41.4° squashes the ground to 0.75 like the art.")]
        [SerializeField, Range(0f, 80f)] private float defaultTiltDegrees = 41.41f;
        [Tooltip("Index in the URP asset's renderer list used by the battle camera in 3D (the Universal renderer).")]
        [SerializeField, Min(0)] private int rendererIndex = 1;
        [Tooltip("How far the camera sits from the ground point it looks at (orthographic: only clipping depends on it).")]
        [SerializeField, Min(5f)] private float cameraDistance = 60f;

        [Header("Light (no realtime shadows: blob shadows only, spec budget)")]
        [Tooltip("Direction the sunlight travels in battle space (ground = XY, up = -Z): Euler angles of the light.")]
        [SerializeField] private Vector3 lightEulerAngles = new Vector3(-40f, 25f, 0f);
        [SerializeField] private Color lightColour = new Color(1f, 0.96f, 0.9f, 1f);
        [SerializeField, Min(0f)] private float lightIntensity = 0.85f;
        [SerializeField] private Color ambientColour = new Color(0.5f, 0.49f, 0.52f, 1f);

        [Header("Placeholder bodies (units without a model or a picture)")]
        [Tooltip("Lit material of the placeholder capsules (tinted per unit). Empty = the pipeline's default material.")]
        [SerializeField] private Material unitMaterial;
        [Tooltip("Body height as a multiple of the unit's diameter.")]
        [SerializeField, Min(0.5f)] private float placeholderHeight = 1.8f;
        [Tooltip("Darkness of the soft round shadow under every unit (no realtime shadows).")]
        [SerializeField, Range(0f, 1f)] private float blobShadowAlpha = 0.35f;

        [Header("Ground (3D mesh + painted texture)")]
        [Tooltip("Material of the ground mesh (Arena3Q/Ground 3D); empty = the painting stays a flat sprite.")]
        [SerializeField] private Material groundMaterial;
        [Tooltip("Grid cell of the ground mesh (metres).")]
        [SerializeField, Min(0.1f)] private float groundCell = 1f;
        [Tooltip("Height of the low hills outside the map bounds (where no unit goes); inside, the ground stays flat.")]
        [SerializeField, Min(0f)] private float groundRimHeight = 0.8f;
        [Tooltip("Distance outside the map bounds over which the hills rise to their height.")]
        [SerializeField, Min(0.01f)] private float groundRimRamp = 4f;
        [Tooltip("How far the ground continues past the painting's edge (it repeats the edge).")]
        [SerializeField, Min(0f)] private float groundSkirt = 8f;

        public Material GroundMaterial => groundMaterial;
        public float GroundCell => groundCell;
        public float GroundRimHeight => groundRimHeight;
        public float GroundRimRamp => groundRimRamp;
        public float GroundSkirt => groundSkirt;

        [Header("Map")]
        [Tooltip("3D models standing in for the map's prop pictures (pillars, slabs, altar, castles, decorations).")]
        [SerializeField] private MapProps3DLibrary mapProps;
        [Tooltip("Darkness of the soft shadow under 3D map props.")]
        [SerializeField, Range(0f, 1f)] private float propShadowAlpha = 0.3f;
        [Tooltip("Extra size of the decoration and command post models (tents, watchtowers, flags, drum) on top of " +
                 "their sprite size. Not the formation (pillars, slabs, altar): those match their obstacles.")]
        [SerializeField, Min(0.1f)] private float decorModelScale = 1.5f;
        [Tooltip("Drawn size of the heroes (picture, 3D model or placeholder body) over their gameplay size; troops and " +
                 "enemies keep theirs. Pictures only: body radius and reach do not change.")]
        [SerializeField, Min(0.1f)] private float heroModelScale = 1.3f;
        [Tooltip("Upright pictures of area effects at a target (Mặc Ảnh flipbooks) are drawn this many effect radii nearer the camera along " +
                 "the view, so a 3D model at the target (a command post) does not cut them off with a straight line. " +
                 "The camera is orthographic, so they stay in the same place on screen.")]
        [SerializeField, Min(0f)] private float effectDepthBias = 1.25f;

        [Header("Bloom (light of the effects spreading into the scene; post-processing, measure FPS on the device)")]
        [Tooltip("Soft glow around the brightest light of the effects (URP bloom). Off = no post-processing at all.")]
        [SerializeField] private bool bloom = true;
        [Tooltip("Brightness (gamma) above which light spreads. Above 1 only light pushed past 1 by effectBloomBoost " +
                 "blooms (the effects), not the sand or the lit models.")]
        [SerializeField, Min(0f)] private float bloomThreshold = 1.1f;
        [SerializeField, Range(0f, 5f)] private float bloomIntensity = 1.2f;
        [Tooltip("How far the glow spreads (0..1).")]
        [SerializeField, Range(0f, 1f)] private float bloomScatter = 0.65f;
        [Tooltip("How far the hottest light of the effect pictures is pushed past 1 (light below about half stays as " +
                 "painted), so that the bloom picks up the effects only. 0 = effects do not bloom.")]
        [SerializeField, Min(0f)] private float effectBloomBoost = 0.5f;
        [Tooltip("Colour of the glow (the qi of the effects is violet).")]
        [SerializeField] private Color bloomTint = new Color(0.85f, 0.75f, 1f, 1f);
        [Tooltip("Bloom works on a quarter-size picture (cheaper on phones) instead of a half-size one.")]
        [SerializeField] private bool bloomQuarterResolution = true;

        public bool Bloom => bloom;
        public float BloomThreshold => bloomThreshold;
        public float BloomIntensity => bloomIntensity;
        public float BloomScatter => bloomScatter;
        public float EffectBloomBoost => effectBloomBoost;
        public Color BloomTint => bloomTint;
        public bool BloomQuarterResolution => bloomQuarterResolution;

        [Header("Motion")]
        [Tooltip("How high a knocked-up unit rises at the top of its arc (metres).")]
        [SerializeField, Min(0f)] private float knockUpHeight = 0.9f;

        public float KnockUpHeight => knockUpHeight;
        public MapProps3DLibrary MapProps => mapProps;
        public float PropShadowAlpha => propShadowAlpha;
        public float DecorModelScale => decorModelScale;
        public float HeroModelScale => heroModelScale;
        public float EffectDepthBias => effectDepthBias;
        public bool Presentation3D => presentation3D;
        public Material UnitMaterial => unitMaterial;
        public float PlaceholderHeight => placeholderHeight;
        public float BlobShadowAlpha => blobShadowAlpha;
        public float DefaultTiltDegrees => defaultTiltDegrees;
        public int RendererIndex => rendererIndex;
        public float CameraDistance => cameraDistance;
        public Vector3 LightEulerAngles => lightEulerAngles;
        public Color LightColour => lightColour;
        public float LightIntensity => lightIntensity;
        public Color AmbientColour => ambientColour;

        /// <summary>Default presentation for tools and tests when the asset is missing.</summary>
        public static BattleViewConfig CreateDefault()
        {
            var config = CreateInstance<BattleViewConfig>();
            config.name = ResourceName;
            config.hideFlags = HideFlags.DontSave;
            return config;
        }

        /// <summary>Switches the presentation (tests and tools; changing the asset is the normal way).</summary>
        public void SetPresentation3D(bool value) => presentation3D = value;
    }
}

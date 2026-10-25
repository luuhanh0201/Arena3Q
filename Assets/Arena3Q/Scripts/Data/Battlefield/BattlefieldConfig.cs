using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Battlefield/Config")]
    public sealed class BattlefieldConfig : ScriptableObject
    {
        public Vector2 size = new Vector2(80f, 44f);
        [Min(.3f)] public float cellSize = .8f;
        public int seed = 314159;
        [Min(2f)] public float roadWidth = 5.5f;
        [Min(5f)] public float ringRadius = 11.8f;
        [Min(1f)] public float altarDiameter = 6.8f;
        public float campX = 31f;
        public float rimHeight = 3.5f;
        public float rimWidth = 5f;
        [Range(35f, 70f)] public float cameraElevation = 50f;
        public Vector2 cameraOffset = new Vector2(0f, -1f);
        public float cameraPadding = 1.1f;
        public Color blue = new Color(.2f, .46f, .82f);
        public Color red = new Color(.8f, .19f, .1f);
        public Color sunColor = new Color(1f, .85f, .66f);
        public float sunIntensity = 1.4f;
        public Vector3 sunEuler = new Vector3(48f, -32f, 0f);
        public Vector2 windDirection = new Vector2(.85f, .3f);
        [Range(0f, 1f)] public float windStrength = .16f;
        public float windSpeed = 1.1f;
        public float gustFrequency = .3f;
        public float windNoiseScale = .25f;
        [Range(0f, 1f)] public float branchBend = .12f;
        [Range(0f, 1f)] public float grassBend = .5f;
        [Min(0)] public int treePairs = 38;
        [Min(0)] public int bushPairs = 45;
        [Min(0)] public int grassPatchPairs = 45;
        [Min(0)] public int grassPerPatch = 8;
        public int targetFrameRate = 60;
        public Rect Bounds => new Rect(-size * .5f, size);
        public Vector3 Point(Vector2 ground, float height = 0f) => new Vector3(ground.x, height, ground.y);
        public float TerrainHeight(Vector2 p)
        {
            float outside = Mathf.Max(Mathf.Abs(p.x) - size.x * .5f, Mathf.Abs(p.y) - size.y * .5f);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, outside) / Mathf.Max(.1f, rimWidth));
            float noise = .6f + .4f * Mathf.PerlinNoise(Mathf.Abs(p.x) * .16f, Mathf.Abs(p.y) * .16f);
            return rise * rimHeight * noise;
        }
        public float RoadDistance(Vector2 p)
        {
            float t = Mathf.Clamp01((p.x + campX) / (2f * campX));
            float arc = Mathf.Sin(t * Mathf.PI) * (ringRadius + 5f);
            return Mathf.Min(Mathf.Abs(p.y - arc), Mathf.Abs(p.y + arc));
        }
    }
}

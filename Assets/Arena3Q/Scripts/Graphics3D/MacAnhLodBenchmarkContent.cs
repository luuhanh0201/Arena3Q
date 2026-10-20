using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Graphics3D
{
    /// <summary>Resource bundles loaded only by their measured route.</summary>
    public sealed class MacAnhLodBenchmarkContent:ScriptableObject
    {
        public MacAnhPerformance performance;
        public HeroDefinition[] heroes;
        public GameObject portraitPrefab;
        public AnimationClip[] clips;
        public int bodyTriangles,fanTriangles;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Loaded only by the sprite route; realtime 3D retains lightweight timings/sockets.</summary>
    public sealed class MacAnhSpriteLibrary:ScriptableObject
    {
        [SerializeField]private MacAnhPerformance.Clip[] clips=Array.Empty<MacAnhPerformance.Clip>();
        public IReadOnlyList<MacAnhPerformance.Clip> Clips=>clips;
        public void Configure(MacAnhPerformance.Clip[] values){clips=values;}
    }
}

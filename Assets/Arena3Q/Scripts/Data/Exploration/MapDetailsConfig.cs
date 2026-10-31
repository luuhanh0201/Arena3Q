using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName="Arena3Q/Exploration/Map Details")]
    public sealed class MapDetailsConfig : ScriptableObject
    {
        public bool enabled;
        public Material groundSource, groundReplacement, detailsMaterial;
        public Mesh[] stageMeshes=Array.Empty<Mesh>();
        public int[] patchCounts=Array.Empty<int>();
        public float groundOffset=.025f;
        public int groundGrid=3;
        public void Validate(List<string> errors,int stages)
        {
            if(!enabled)return;
            if(groundSource==null||groundReplacement==null||detailsMaterial==null||stageMeshes==null||stageMeshes.Length!=stages||patchCounts==null||patchCounts.Length!=stages)
                errors.Add("Map details require ground materials and one mesh/count entry per stage.");
            if(groundOffset<=0||groundOffset>.06f||groundGrid<2)errors.Add("Ground details must stay close to the terrain with a subdivided mesh.");
        }
    }
}

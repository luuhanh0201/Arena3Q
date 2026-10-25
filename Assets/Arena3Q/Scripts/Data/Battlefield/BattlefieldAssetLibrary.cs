using System;
using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Battlefield/Asset Library")]
    public sealed class BattlefieldAssetLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string id;
            public string category;
            public GameObject prefab;
            public bool missing;
            public string note;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public Texture2D[] groundAlbedo = new Texture2D[6];
        public Material groundMaterial;
        public Material runeMaterial;
        public Material fireMaterial;
        public Material smokeMaterial;
        public Material dustMaterial;
        public Mesh leafMesh;
        public Material leafMaterial;
        public GameObject Find(string id)
        {
            foreach (var entry in entries) if (entry.id == id) return entry.prefab;
            return null;
        }
    }
}

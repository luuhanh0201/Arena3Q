using UnityEngine;

namespace Arena3Q.Gameplay
{
    // Ownership marker: tools may rebuild only this subtree, never unrelated scene objects.
    public sealed class BattlefieldGenerated : MonoBehaviour
    {
        public int schemaVersion = 1;
        public int seed;
    }
}

using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Minimal faction data (spec §2, deferred). Faction choice, lord skills and faction UI are not in the prototype;
    /// this exists so hero data does not need restructuring later.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Faction Definition", fileName = "Faction")]
    public sealed class FactionDefinition : ScriptableObject
    {
        [SerializeField] private string factionId = "placeholder";
        [SerializeField] private string displayName = "Placeholder Faction";
        [SerializeField] private Color color = new Color(0.25f, 0.55f, 1f);

        public string FactionId => factionId;
        public string DisplayName => displayName;
        public Color Color => color;
    }
}

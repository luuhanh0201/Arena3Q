using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Editor replacement anchor, deliberately contains no substitute primitive geometry.</summary>
    public sealed class BattlefieldAssetSlot : MonoBehaviour
    {
        public string assetId;
        [TextArea] public string replacementNote;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(1f,.7f,.2f,.8f);Gizmos.DrawWireCube(transform.position+Vector3.up,new Vector3(2f,2f,2f));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position+Vector3.up*2.2f,assetId);
#endif
        }
    }
}

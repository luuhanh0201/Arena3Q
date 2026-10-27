using UnityEngine;

namespace Arena3Q.Frontend
{
    public sealed class LobbyEdgeGroup : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (!(transform.parent is RectTransform parent)) return;
            transform.localScale = Vector3.one * Mathf.Min(parent.rect.height / 720f, parent.rect.width / 1280f);
        }
    }
}

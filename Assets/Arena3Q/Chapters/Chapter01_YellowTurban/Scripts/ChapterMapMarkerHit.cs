using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arena3Q.Exploration
{
    /// <summary>The closest visible marker wins when phone touch areas overlap.</summary>
    public sealed class ChapterMapMarkerHit : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> Click;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Click?.Invoke(data.position);
        }
    }
}

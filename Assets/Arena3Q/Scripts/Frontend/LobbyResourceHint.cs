using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arena3Q.Frontend
{
    /// <summary>The same resource hit area handles mouse hover and a touch/mouse click.</summary>
    public sealed class LobbyResourceHint : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private Action _show;
        public void Configure(Action show) => _show = show;
        public void OnPointerEnter(PointerEventData data) => _show?.Invoke();
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) _show?.Invoke();
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    /// <summary>Inline legal links, shared by login and registration; the rest of the checkbox sentence still toggles.</summary>
    public sealed class UiLegalLinks : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _text; private FrontendController _flow; private Toggle _toggle;
        public void Configure(FrontendController flow, TMP_Text text, Toggle toggle = null)
        { _flow = flow; _text = text; _toggle = toggle; }
        public void OnPointerClick(PointerEventData data)
        {
            var index = TMP_TextUtilities.FindIntersectingLink(_text, data.position, data.pressEventCamera);
            if (index < 0) { if (_toggle != null) _toggle.isOn = !_toggle.isOn; return; }
            Open(_text.textInfo.linkInfo[index].GetLinkID());
        }
        public void Open(string id)
        {
            var url = id == "terms" ? _flow.Config.TermsUrl : _flow.Config.PrivacyUrl;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")) Application.OpenURL(url);
            else _flow.ShowMessage(_flow.StartupUi.C("legalUnavailable"));
        }
    }
}

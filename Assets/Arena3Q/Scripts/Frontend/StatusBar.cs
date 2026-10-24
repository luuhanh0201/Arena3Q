using System;
using Arena3Q.Core;
using Arena3Q.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.Frontend
{
    public sealed class StatusBar : MonoBehaviour
    {
        private StartupUi _ui; private TextMeshProUGUI _time; private Image _wifi, _slash; private float _elapsed = 100;
        public StartupNetState State { get; private set; }
        public bool ManualState { get; set; }
        public void Configure(StartupUi ui)
        {
            _ui = ui;
            var rect = UiFactory.CreateRect("StatusPill", transform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-12, -6), new Vector2(120, 26));
            var pill = UiFactory.AddImage(rect, new Color(0, 0, 0, .5f), ui.Config.rounded, false); pill.type = Image.Type.Sliced;
            _wifi = ui.Image(rect, "Connection", ui.Config.wifi, new Rect(12, 5, 18, 16));
            _slash = ui.Image(rect, "OfflineSlash", ui.Config.slash, new Rect(12, 5, 18, 16), ui.Config.bad);
            ui.Image(rect, "Divider", null, new Rect(40, 6, 1, 14), new Color(1, 1, 1, .35f));
            _time = ui.Text(rect, "DeviceTime", "", new Rect(49, 0, 64, 26), 13, align: TextAlignmentOptions.Center);
            SetState(StartupNetState.Good); Tick(0);
        }
        public void SetState(StartupNetState state)
        {
            State = state;
            var lost = state == StartupNetState.Lost || state == StartupNetState.ServerTimeout;
            var weak = state == StartupNetState.Weak || state == StartupNetState.Stalled || state == StartupNetState.Retrying;
            _wifi.sprite = weak ? _ui.Config.wifiLow : _ui.Config.wifi;
            _slash.enabled = lost;
            _wifi.color = lost ? _ui.Config.off : weak ? _ui.Config.weak : Color.white;
        }
        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
            if (!ManualState)
            {
                var state = Application.internetReachability == NetworkReachability.NotReachable ? StartupNetState.Lost : StartupNetState.Good;
                if (state != State) SetState(state);
            }
        }
        private void Tick(float dt)
        {
            if (_ui == null) return;
            _elapsed += dt;
            if (_elapsed >= _ui.Config.statusSeconds) { _elapsed = 0; _time.text = DateTime.Now.ToString("HH:mm"); }
        }
    }
}

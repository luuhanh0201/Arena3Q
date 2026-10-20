using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>An isolated visual inspection scene. No combat units or damage are created here.</summary>
    public sealed class MacAnhShowcase : MonoBehaviour
    {
        [SerializeField] private MacAnhPerformance performance;
        [SerializeField] private Material spriteMaterial;
        private SpriteRenderer _body;
        private MacAnhAction _action;
        private float _time, _speed = 1;
        private int _direction = 6, _quality = 1;
        private bool _paused;
        private readonly HashSet<string> _events = new HashSet<string>();
        private readonly List<MacAnhFx> _effects = new List<MacAnhFx>();
        private readonly Dictionary<string, List<MacAnhFx>> _pool = new Dictionary<string, List<MacAnhFx>>();
        private sealed class PreviewFlight { public Transform transform; public Vector3 direction; }
        private readonly List<PreviewFlight> _flying = new List<PreviewFlight>();
        private MacAnhFx _charge;
        private string _lastEvent = "";
        private Vector2 _scroll;
        public MacAnhPerformance Performance => performance;
        public void Configure(MacAnhPerformance profile, Material material) { performance = profile; spriteMaterial = material; }
        private void Start()
        {
            var go = new GameObject("MacAnh — actual rendered rig"); go.transform.SetParent(transform, false);
            _body = go.AddComponent<SpriteRenderer>(); _body.sharedMaterial = spriteMaterial; _body.sortingOrder = 10;
            Play(MacAnhAction.Idle);
        }
        public void Play(MacAnhAction action)
        {
            _action = action; _time = 0; _events.Clear(); _lastEvent = "";
            foreach (var fx in _effects.ToArray()) if (fx != null) fx.Stop();
            _effects.Clear(); _flying.Clear(); _charge = null; Draw();
        }
        private void Draw()
        {
            var clip = performance.Find(_action); if (clip == null || _body == null) return;
            _body.sprite = clip.frames[clip.Index(_direction, _time)];
        }
        private MacAnhFx Spawn(string key, Vector3 position, Vector3 scale, float duration)
        {
            var prefab = performance.Effect(key); if (prefab == null) return null;
            if (!_pool.TryGetValue(key, out var instances)) _pool[key] = instances = new List<MacAnhFx>();
            var fx = instances.Find(f => f != null && !f.gameObject.activeSelf);
            if (fx == null) { fx = Instantiate(prefab, transform).GetComponent<MacAnhFx>(); instances.Add(fx); }
            fx.transform.SetPositionAndRotation(position, Quaternion.identity); fx.transform.localScale = scale; fx.gameObject.SetActive(true);
            fx.ManualClock = true; fx.Begin((MacAnhFxQuality)_quality, duration, 20, item => { _effects.Remove(item); item.gameObject.SetActive(false); });
            _effects.Add(fx); return fx;
        }
        private Vector3 Socket(MacAnhSocket socket)
        {
            var clip = performance.Find(_action); var point = clip.sockets[clip.Index(_direction, _time)].Get(socket);
            return _body.transform.position + new Vector3(point.x, point.y, 0);
        }
        private void Update()
        {
            if (performance == null || _paused) return;
            var delta = Time.deltaTime * _speed; var clip = performance.Find(_action); if (clip == null) return;
            _time = Mathf.Min(_time + delta, clip.loop ? float.MaxValue : clip.duration); Draw();
            foreach (var effect in _effects.ToArray()) if (effect != null) effect.Advance(delta);
            foreach (var flight in _flying)
                if (flight.transform != null && flight.transform.gameObject.activeSelf) flight.transform.position += flight.direction * delta * 3;
            if ((_action == MacAnhAction.Skill01 && _time >= .5f || _action == MacAnhAction.Ultimate && _time >= 1f) &&
                clip.events.Length > 0 && _time < clip.events[0].Seconds && _events.Add("Charge"))
            {
                _charge = Spawn(_action == MacAnhAction.Skill01 ? "MagicArray" : "Ultimate", new Vector3(1.8f, -.2f, 0), Vector3.one,
                    clip.events[0].Seconds - _time);
                if (_charge != null) _charge.SetCharge();
            }
            if (_action == MacAnhAction.DodgeAttack && _time >= 8f / 30 && _time <= 13f / 30 && _events.Add("Ghost" + Mathf.FloorToInt(_time * 30 / 2)))
            {
                var ghost = Spawn("Afterimage", _body.transform.position - Vector3.right * .12f, Vector3.one, .35f);
                if (ghost != null) ghost.SetAfterimage(_body.sprite, spriteMaterial, 4);
            }
            foreach (var mark in clip.events)
            {
                var id = mark.key + mark.comboIndex;
                if (_time < mark.Seconds || !_events.Add(id)) continue;
                if (_charge != null) { _charge.Stop(); _charge = null; }
                _lastEvent = mark.key + "  f" + mark.frame + (mark.key == "SpawnTripleFeather" ? "  #" + mark.comboIndex : "");
                var projectile = _action == MacAnhAction.Attack01 || _action == MacAnhAction.Attack02 || _action == MacAnhAction.DodgeAttack;
                var key = _action == MacAnhAction.Attack01 ? "WindSlash" : _action == MacAnhAction.Attack02 ? "TripleFeather" :
                    _action == MacAnhAction.DodgeAttack ? "CounterSlash" : _action == MacAnhAction.Skill01 ? "MagicArray" :
                    _action == MacAnhAction.Skill02 ? "Cyclone" : _action == MacAnhAction.Skill03 ? "Shield" : "Ultimate";
                var effect = Spawn(key, projectile ? Socket(MacAnhSocket.FanTip) : _action == MacAnhAction.Skill03 ? Vector3.zero : new Vector3(1.8f, -.2f, 0),
                    projectile ? Vector3.one * .65f : Vector3.one, projectile ? 1 : 2.5f);
                if (projectile && effect != null)
                {
                    var angle = _direction * Mathf.PI / 4;
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .75f, 0).normalized;
                    effect.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                    _flying.Add(new PreviewFlight { transform = effect.transform, direction = direction });
                }
            }
        }
        private void OnGUI()
        {
            if (performance == null) return;
            GUILayout.BeginArea(new Rect(14, 14, Mathf.Min(270, Screen.width * .32f), Screen.height - 28), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("MẶC ẢNH — XEM ANIMATION / VFX");
            foreach (MacAnhAction action in System.Enum.GetValues(typeof(MacAnhAction)))
            {
                GUI.enabled = performance.Find(action) != null;
                if (GUILayout.Button(action.ToString())) Play(action);
            }
            GUI.enabled = true; GUILayout.Space(8);
            GUILayout.Label("Hướng nhìn: " + _direction); _direction = Mathf.RoundToInt(GUILayout.HorizontalSlider(_direction, 0, 7));
            GUILayout.Label("Tốc độ: " + _speed.ToString("0.0") + "×"); _speed = GUILayout.HorizontalSlider(_speed, .1f, 2);
            if (GUILayout.Button(_paused ? "Tiếp tục" : "Tạm dừng")) _paused = !_paused;
            _quality = GUILayout.SelectionGrid(_quality, new[] { "Low", "Medium", "High" }, 3);
            var camera = Camera.main;
            if (camera != null)
            {
                GUILayout.Label("Thu phóng"); camera.orthographicSize = GUILayout.HorizontalSlider(camera.orthographicSize, 1.1f, 3f);
            }
            GUILayout.Label("Frame: " + (_time * 30).ToString("0.0")); GUILayout.Label(_lastEvent);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

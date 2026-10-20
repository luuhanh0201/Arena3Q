using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Rendered metre-scale poses and sampled bone sockets; never writes the simulation position.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class MacAnhPerformanceView : MonoBehaviour
    {
        private HeroUnit _hero;
        private MacAnhPerformance _set;
        private SpriteRenderer _body, _previous;
        private MacAnhPerformance.Clip _clip;
        private Vector3 _display, _velocity;
        private float _seconds, _lastTime, _blend, _angle = -45f, _targetAngle = -45f;
        private int _direction = 7;
        private bool _locked;
        private IMacAnhRealtimeView _realtime;
        public SpriteRenderer Body => _body;
        public MacAnhAction Action => _clip == null ? MacAnhAction.Idle : _clip.action;
        public float Seconds => _seconds;
        public void Init(HeroUnit hero, SpriteRenderer body)
        {
            _hero = hero; _set = hero.Definition.MacAnhPerformance; _body = body;
            // These sprites already contain source lighting. Re-lighting them
            // with the battlefield Sprite-Lit material darkens the painted robe.
            if(_set.SpriteMaterial!=null)_body.sharedMaterial=_set.SpriteMaterial;
            var go = new GameObject("Pose transition"); go.transform.SetParent(transform, false);
            _previous = go.AddComponent<SpriteRenderer>(); _previous.sharedMaterial = body.sharedMaterial; _previous.enabled = false;
            _display = transform.position; _lastTime = hero.Battle.BattleTime;
            if(_set.EnableBattle3DTrial && _set.Battle3DViewPrefab!=null)
            {
                var trial=Instantiate(_set.Battle3DViewPrefab,transform);
                foreach(var component in trial.GetComponents<MonoBehaviour>())
                    if(component is IMacAnhRealtimeView view && view.Initialize(hero,body)){_realtime=view;break;}
                if(_realtime==null)Destroy(trial);
            }
            Play(MacAnhAction.Idle, Vector2.zero); Draw();
        }
        public void Play(MacAnhAction action, Vector2 direction, bool locked = false)
        {
            var next = _realtime==null?_set.FindSprite(action):_set.Find(action);
            if (next == null || (_realtime==null && (next.frames==null||next.frames.Length==0))) return;
            var phase = IsGait(_clip) && IsGait(next) ? Mathf.Repeat(_seconds / _clip.duration, 1f) : 0f;
            if (direction.sqrMagnitude > .0001f) _targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _locked = locked;
            if (_clip == next && !locked) return;
            BeginBlend(); _clip = next; _seconds = phase * next.duration;
        }
        public void SetCastTime(float seconds) { _seconds = seconds; Draw(); }
        public void Cancel() { _locked = false; Play(MacAnhAction.Idle, Vector2.zero); }
        public void ApplyAfterimage(MacAnhFx effect)
        {
            int order=_body.sortingOrder-1;
            if(_realtime==null||!_realtime.CaptureAfterimage(effect,order))effect.SetAfterimage(_body.sprite,_body.sharedMaterial,order);
        }
        private static bool IsGait(MacAnhPerformance.Clip clip) => clip != null && (clip.action == MacAnhAction.Run || clip.action == MacAnhAction.Walk);
        public bool TrySocket(MacAnhSocket socket, out Vector3 position)
        {
            position = default;
            if(_realtime!=null && _realtime.TrySocket(socket,out position))return true;
            if (_clip == null || _clip.sockets == null || _clip.sockets.Length == 0) return false;
            var index = _clip.Index(_direction, _seconds);
            if (index < 0 || index >= _clip.sockets.Length) return false;
            var offset = _clip.sockets[index].Get(socket);
            position = transform.position + new Vector3(offset.x, offset.y, 0);
            return true;
        }
        private void BeginBlend()
        {
            if (_body.sprite == null) return;
            _previous.sprite = _body.sprite; _previous.color = Color.white; _previous.enabled = true;
            _blend = _set.TransitionSeconds;
        }
        private void Draw()
        {
            if (_clip == null) return;
            if(_realtime!=null){_realtime.Sample(_clip.action,_seconds,_angle);return;}
            if(_clip.frames==null)return;
            var index = _clip.Index(_direction, _seconds);
            if (index >= 0 && index < _clip.frames.Length && _clip.frames[index] != null) _body.sprite = _clip.frames[index];
        }
        private void LateUpdate()
        {
            if (_hero == null) return;
            var delta = Mathf.Max(0, _hero.Battle.BattleTime - _lastTime); _lastTime = _hero.Battle.BattleTime;
            if (!_hero.IsAlive)
            {
                _body.color = new Color(1, 1, 1, Mathf.Max(0, _body.color.a - Time.deltaTime * 2));
                _realtime?.SetOpacity(_body.color.a);return;
            }
            if (!_locked)
            {
                var speed = _hero.LocomotionVelocity.magnitude;
                var moving = speed > .05f && !_hero.Battle.IsOver && !_hero.IsStunned;
                var walk = speed < _hero.MoveSpeed * (Action == MacAnhAction.Walk ? .62f : .54f);
                Play(moving ? (walk ? MacAnhAction.Walk : MacAnhAction.Run) : MacAnhAction.Idle,
                    moving ? _hero.LocomotionVelocity : Vector2.zero);
                var stride = walk ? _set.WalkStrideLength : _set.RunStrideLength;
                _seconds += delta * (moving ? speed * _clip.duration / stride : 1f);
            }
            _angle = Mathf.MoveTowardsAngle(_angle, _targetAngle, 720f * delta);
            var centre = _direction * 45f;
            if (Mathf.Abs(Mathf.DeltaAngle(centre, _angle)) > 28f)
            {
                BeginBlend(); _direction = Mathf.RoundToInt(Mathf.Repeat(_angle, 360f) / 45f) % 8;
            }
            var target = _hero.transform.position;
            _display = (target - _display).sqrMagnitude > 1f || _set.VisualSmoothSeconds <= 0 ? target :
                delta>0?Vector3.SmoothDamp(_display, target, ref _velocity, _set.VisualSmoothSeconds, Mathf.Infinity, delta):_display;
            _display = target + Vector3.ClampMagnitude(_display - target, _set.MaxVisualOffset); transform.position = _display;
            Draw();
            _blend = Mathf.Max(0, _blend - delta);
            var fraction = _set.TransitionSeconds > 0 ? _blend / _set.TransitionSeconds : 0;
            _body.color = new Color(1, 1, 1, 1f - fraction);
            _previous.color = new Color(1, 1, 1, fraction); _previous.sortingOrder = _body.sortingOrder + 1;
            _previous.enabled = fraction > 0;
            if(_realtime!=null){_body.enabled=false;_previous.enabled=false;}
        }
    }
}

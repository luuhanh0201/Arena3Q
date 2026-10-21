using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Presentation only: facing, gait and visual easing never change combat positions or damage.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class HeroAnimationView : MonoBehaviour
    {
        private HeroUnit _hero;
        private HeroAnimationSet _set;
        private SpriteRenderer _renderer;
        private SpriteRenderer _transition;
        private HeroAnimationSet.Clip _clip;
        private Vector3 _displayPosition;
        private Vector3 _positionVelocity;
        private float _lastBattleTime;
        private float _seconds;
        private float _angle = -45f;
        private float _desiredAngle = -45f;
        private float _stopGrace;
        private float _gaitRate = 1f;
        private float _attackRate = 1f;
        private int _direction;
        private bool _attack;
        private bool _attackStartedThisFrame;
        private bool _alternate;
        private bool _corpse;
        private bool _casting;
        private bool _transitionPending;
        private bool _hasDrawn;
        private float _transitionTime;

        internal void Init(HeroUnit hero, SpriteRenderer renderer)
        {
            _hero = hero;
            _set = hero.Definition.Animations;
            _renderer = renderer;
            var transitionObject = new GameObject("Pose transition");
            transitionObject.transform.SetParent(transform, false);
            _transition = transitionObject.AddComponent<SpriteRenderer>();
            _transition.sharedMaterial = renderer.sharedMaterial;
            _transition.enabled = false;
            _displayPosition = renderer.transform.position;
            _lastBattleTime = hero.Battle.BattleTime;
            _direction = _set.DirectionCount == 8 ? 7 : 0;
            Play(HeroAnimationState.Idle);
            Draw();
        }

        private void Face(Vector2 velocity)
        {
            if (velocity.sqrMagnitude > 0.0001f)
                _desiredAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        }

        private void Turn(float deltaTime)
        {
            _angle = Mathf.MoveTowardsAngle(_angle, _desiredAngle, _set.TurnDegreesPerSecond * deltaTime);
            var count = _set.DirectionCount == 8 ? 8 : 4;
            var width = 360f / count;
            var centre = count == 8 ? _direction * width : 45f + ((_direction + 3) % 4) * width;
            if (Mathf.Abs(Mathf.DeltaAngle(centre, _angle)) <= width * 0.5f + _set.DirectionHysteresisDegrees)
                return;
            _direction = count == 8
                ? Mathf.RoundToInt(Mathf.Repeat(_angle, 360f) / width) % count
                : (Mathf.FloorToInt(Mathf.Repeat(_angle, 360f) / width) + 1) % count;
            _transitionPending = true;
        }

        internal void Attack(UnitBase target)
        {
            Face(target.Position - _hero.Position);
            _alternate = !_alternate;
            Play(_alternate ? HeroAnimationState.Attack01 : HeroAnimationState.Attack02, true);
            _attackRate = (_clip?.duration ?? 1f) /
                Mathf.Max(0.05f, Mathf.Min(_clip?.duration ?? 1f, _hero.CurrentAttackInterval * 0.9f));
            _attack = true;
            _attackStartedThisFrame = true;
            Draw();
        }

        internal void Die()
        {
            _corpse = true;
            _attack = false;
            transform.SetParent(_hero.transform.parent, true);
            _renderer.color = Color.white;
            _hero = null;
            Play(HeroAnimationState.Defeat, true);
            Draw();
        }

        private static bool IsGait(HeroAnimationSet.Clip clip) => clip != null &&
            (clip.state == HeroAnimationState.Run || clip.state == HeroAnimationState.Walk);

        private void Play(HeroAnimationState state, bool restart = false)
        {
            var clip = _set.Find(state) ?? _set.Find(HeroAnimationState.Idle);
            if (_clip == clip && !restart) return;
            var phase = !restart && IsGait(_clip) && IsGait(clip)
                ? Mathf.Repeat(_seconds / Mathf.Max(0.01f, _clip.duration), 1f) : 0f;
            _clip = clip;
            _seconds = phase * (_clip?.duration ?? 0f);
            _transitionPending = true;
        }

        private void Draw()
        {
            var frame = _clip?.Frame(_direction, _seconds);
            if (frame == null) return;
            if (_hasDrawn && _transitionPending && _renderer.sprite != frame && _set.TransitionSeconds > 0f)
            {
                _transition.sprite = _renderer.sprite;
                _transition.color = _renderer.color;
                _transition.enabled = true;
                _transitionTime = _set.TransitionSeconds;
            }
            _transitionPending = false;
            _hasDrawn = true;
            _renderer.sprite = frame;
        }

        private void BlendPose(float deltaTime)
        {
            if (!_transition.enabled) return;
            _transitionTime = Mathf.Max(0f, _transitionTime - deltaTime);
            var fraction = _transitionTime / Mathf.Max(0.001f, _set.TransitionSeconds);
            var current = _renderer.color;
            _transition.color = new Color(current.r, current.g, current.b, fraction);
            _renderer.color = new Color(current.r, current.g, current.b, 1f - fraction);
            _transition.sortingOrder = _renderer.sortingOrder + 1;
            _transition.enabled = fraction > 0f;
        }

        private void EasePosition(float deltaTime)
        {
            var target = _hero.transform.position;
            if ((target - _displayPosition).sqrMagnitude > 1f || _set.PositionSmoothSeconds <= 0f)
            {
                _displayPosition = target;
                _positionVelocity = Vector3.zero;
            }
            else if (deltaTime > 0f)
            {
                _displayPosition = Vector3.SmoothDamp(_displayPosition, target, ref _positionVelocity,
                    _set.PositionSmoothSeconds, Mathf.Infinity, deltaTime);
                _displayPosition = target + Vector3.ClampMagnitude(_displayPosition - target, _set.MaxVisualOffset);
            }
            transform.position = _displayPosition;
        }

        private void LateUpdate()
        {
            if (_corpse)
            {
                _seconds += Time.unscaledDeltaTime;
                Draw();
                BlendPose(Time.unscaledDeltaTime);
                if (_seconds > (_clip?.duration ?? 1f) + 2f) Destroy(gameObject);
                return;
            }
            if (_hero == null || !_hero.IsAlive) return;
            var battle = _hero.Battle;
            var delta = Mathf.Max(0f, battle.BattleTime - _lastBattleTime);
            _lastBattleTime = battle.BattleTime;
            var motion = _hero.LocomotionVelocity;
            var speed = motion.magnitude;
            var moving = speed > 0.05f;
            _stopGrace = moving ? _set.MotionStopGraceSeconds : Mathf.Max(0f, _stopGrace - delta);
            var rate = 1f;
            if (battle.IsOver)
            {
                _attack = false;
                Play(battle.State.Outcome == BattleOutcome.Victory ? HeroAnimationState.Victory : HeroAnimationState.Defeat);
                delta = Time.unscaledDeltaTime;
            }
            else if (_hero.IsStunned)
            {
                _attack = false;
                _stopGrace = 0f;
                Play(HeroAnimationState.Idle);
            }
            else if (_hero.Skills.IsCasting)
            {
                if (!_casting || _seconds >= (_clip?.duration ?? 0f))
                {
                    var target = battle.Registry.Get(_hero.EngagedTarget);
                    if (target != null) Face(target.Position - _hero.Position);
                    Play(HeroAnimationState.Attack02, true);
                    _attack = true;
                    _attackRate = 1f;
                }
            }
            else if (_attackStartedThisFrame)
            {
                rate = _attackRate;
            }
            else if (moving)
            {
                _attack = false;
                Face(motion);
                var walkLimit = _hero.MoveSpeed * (_set.WalkSpeedRatio +
                    (_clip?.state == HeroAnimationState.Walk ? 0.08f : 0f));
                var walk = speed < walkLimit;
                Play(walk ? HeroAnimationState.Walk : HeroAnimationState.Run);
                // Stride of the drawn picture (heroes are drawn larger than their size): keeps the feet planted.
                var stride = (walk ? _set.WalkStrideLength : _set.RunStrideLength) * _hero.DrawnSize;
                rate = speed * (_clip?.duration ?? 1f) / Mathf.Max(0.01f, stride);
                _gaitRate = rate;
            }
            else if (_attack && _seconds < (_clip?.duration ?? 0f))
            {
                rate = _attackRate;
            }
            else if (_stopGrace > 0f && IsGait(_clip))
            {
                rate = _gaitRate * 0.35f;
            }
            else
            {
                _attack = false;
                Play(HeroAnimationState.Idle);
            }
            _casting = _hero.Skills.IsCasting;
            _attackStartedThisFrame = false;
            Turn(delta);
            _seconds += delta * rate;
            EasePosition(Time.deltaTime);
            Draw();
            BlendPose(Time.deltaTime);
        }
    }
}

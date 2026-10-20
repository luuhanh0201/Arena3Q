using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// A hero drawn as its 3D model in the 3D battle (<see cref="HeroModel3D"/>, docs/KeHoach/ChuyenSang3D.md P3).
    /// <see cref="HeroAnimationView"/> decides the state, clip time and facing; this samples the clip on the skinned
    /// model (cross-fading between clips), turns it continuously, plays a skill's own clip timed so its impact frame
    /// lands on the skill's cast time, carries the model along dashes and leaps instead of snapping, and drops the
    /// weapon when the hero falls. Pictures only: it never writes the hero's logic position.
    /// </summary>
    [DefaultExecutionOrder(55)]
    internal sealed class HeroModel3DView : MonoBehaviour
    {
        // Model space (Y up, +Z forward) → battle space (ground XY, up = -Z).
        private static readonly Quaternion StandUp = Quaternion.Euler(-90f, 0f, 0f);
        private const float DashSnapDistance = 1f;
        private const float DashSeconds = 0.35f;
        private const float LeapHeight = 1.4f;

        private HeroUnit _hero;
        private BattleController _battle;
        private HeroModel3D _model;
        private Transform _placement;
        private GameObject _instance;
        private Transform[] _bones;
        private Vector3[] _fromPosition, _fromScale;
        private Quaternion[] _fromRotation;
        private SpriteRenderer _shadow;
        private Transform _weapon;
        private AnimationClip _clip;
        private float _blendStart = -1f, _clock;
        private bool _blending;

        // Where the model stands on the logic ground (eased through dashes), and the leap of a LeapToPoint cast.
        private Vector2 _ground;
        private Vector2 _dashFrom, _dashTo;
        private float _dashTime = -1f;
        private Vector2 _leapFrom;
        private bool _leaping;

        // A skill clip keeps playing its recovery after the cast is over, until the hero walks or attacks.
        private HeroModel3D.SkillClip _skill;
        private float _skillSeconds, _skillRate = 1f;
        private bool _skillLingering;

        private bool _fallen;
        private float _fallSeconds;
        private bool _weaponDropped;
        private Vector3 _weaponFrom;
        private Quaternion _weaponFromRotation;
        private float _weaponTime;

        public Transform Model => _instance != null ? _instance.transform : null;

        public void Init(HeroUnit hero, HeroModel3D model)
        {
            _hero = hero;
            _battle = hero.Battle;
            _model = model;
            _placement = new GameObject("Hero 3D").transform;
            _placement.SetParent(transform, false);
            _instance = Instantiate(model.Prefab, _placement);
            _instance.transform.localPosition = Vector3.zero;
            foreach (var animator in _instance.GetComponentsInChildren<Animator>())
                animator.enabled = false;
            foreach (var skin in _instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.quality = SkinQuality.Bone4;
                skin.skinnedMotionVectors = false;
                skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                skin.receiveShadows = false;
            }
            foreach (var renderer in _instance.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (!string.IsNullOrEmpty(model.WeaponObject))
                foreach (var child in _instance.GetComponentsInChildren<Transform>())
                    if (child.name == model.WeaponObject)
                        _weapon = child;
            _bones = _instance.GetComponentsInChildren<Transform>();
            _fromPosition = new Vector3[_bones.Length];
            _fromScale = new Vector3[_bones.Length];
            _fromRotation = new Quaternion[_bones.Length];

            _shadow = PlaceholderArt.CreateRenderer("Soft ground shadow", transform, PlaceholderArt.Blob,
                new Color(0f, 0f, 0f, 0.4f), Vector2.one * model.ShadowDiameter, -30990);
            _ground = hero.Position;
        }

        /// <summary>
        /// Shows <paramref name="state"/> at <paramref name="seconds"/>, facing <paramref name="facing"/> (logic degrees);
        /// a skill being cast plays its own clip instead when the model has one.
        /// </summary>
        public void Show(HeroAnimationState state, float seconds, float facing)
        {
            if (_fallen)
                return;
            var clip = _model.Find(state);
            var cast = _hero != null && _hero.IsAlive ? _hero.Skills.CastingSkill : null;
            var skill = cast != null ? _model.Find(cast.Slot) : null;
            if (skill != null)
            {
                // The impact frame of the clip lands on the skill's cast time.
                _skillRate = cast.CastTime > 0f && skill.effectSeconds > 0f ? skill.effectSeconds / cast.CastTime : 1f;
                _skill = skill;
                _skillSeconds = _hero.Skills.CastSeconds * _skillRate;
                _skillLingering = true;
                clip = skill.clip;
                seconds = _skillSeconds;
            }
            else if (_skillLingering)
            {
                // Recovery after the cast, unless the hero has something else to show.
                var busy = state == HeroAnimationState.Walk || state == HeroAnimationState.Run ||
                           state == HeroAnimationState.Attack01 || state == HeroAnimationState.Attack02 ||
                           state == HeroAnimationState.Victory || state == HeroAnimationState.Defeat;
                if (busy || _skillSeconds >= _skill.clip.length)
                    _skillLingering = false;
                else
                {
                    clip = _skill.clip;
                    seconds = _skillSeconds;
                }
            }
            if (state == HeroAnimationState.Defeat && _hero != null && !_hero.IsAlive)
                BeginFall();
            Sample(clip, seconds, facing);
        }

        /// <summary>The hero died: Defeat plays on its own clock and the weapon falls.</summary>
        public void BeginFall()
        {
            if (_fallen)
                return;
            _fallen = true;
            _fallSeconds = 0f;
            _skillLingering = false;
            Sample(_model.Find(HeroAnimationState.Defeat), 0f, _lastFacing);
        }

        private float _lastFacing = 90f;

        private void Sample(AnimationClip clip, float seconds, float facing)
        {
            if (clip == null || _instance == null)
                return;
            _lastFacing = facing;
            if (_clip != null && clip != _clip && _model.TransitionSeconds > 0f)
            {
                for (var i = 0; i < _bones.Length; i++)
                {
                    _fromPosition[i] = _bones[i].localPosition;
                    _fromRotation[i] = _bones[i].localRotation;
                    _fromScale[i] = _bones[i].localScale;
                }
                _blendStart = _clock;
                _blending = true;
            }
            _clip = clip;
            var time = clip.isLooping ? Mathf.Repeat(seconds, clip.length) : Mathf.Clamp(seconds, 0f, clip.length);
            clip.SampleAnimation(_instance, time);
            if (_blending)
            {
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_clock - _blendStart) / _model.TransitionSeconds));
                for (var i = 0; i < _bones.Length; i++)
                {
                    var bone = _bones[i];
                    if (bone == _weapon && _weaponDropped)
                        continue;
                    bone.localPosition = Vector3.Lerp(_fromPosition[i], bone.localPosition, t);
                    bone.localRotation = Quaternion.Slerp(_fromRotation[i], bone.localRotation, t);
                    bone.localScale = Vector3.Lerp(_fromScale[i], bone.localScale, t);
                }
                _blending = t < 1f;
            }
            _placement.rotation = StandUp * Quaternion.Euler(0f, _model.YawOffset + _model.YawSign * facing, 0f);
            _placement.localScale = Vector3.one * _model.Scale;
        }

        private void LateUpdate()
        {
            if (_battle == null)
                return;
            var delta = Time.deltaTime;
            _clock += delta;
            if (_skillLingering && (_hero == null || !_hero.Skills.IsCasting))
                _skillSeconds += delta * _skillRate;

            if (_fallen)
            {
                _fallSeconds += Time.unscaledDeltaTime;
                Sample(_model.Find(HeroAnimationState.Defeat), _fallSeconds, _lastFacing);
                DropWeapon(Time.unscaledDeltaTime);
            }
            else
                Follow(delta);
            if (_shadow != null)
            {
                _shadow.transform.position = _battle.ToView(_ground);
                _shadow.transform.rotation = Quaternion.identity;
            }
        }

        // Where the model stands: the hero's logic position, eased through dashes; a leap flies on an arc to the aim.
        private void Follow(float delta)
        {
            if (_hero == null)
                return;
            var target = _hero.Position;
            var cast = _hero.Skills.CastingSkill;
            var lift = 0f;
            if (cast != null && cast.CasterMove == CasterMove.LeapToPoint && _skill != null && _skill.effectSeconds > 0f)
            {
                if (!_leaping)
                {
                    _leaping = true;
                    _leapFrom = _ground;
                }
                var aim = _hero.Skills.CastAim.Point;
                var offset = aim - _leapFrom;
                if (cast.MoveDistance > 0f)
                    offset = Vector2.ClampMagnitude(offset, cast.MoveDistance);
                // Take-off a third of the way to the impact, landing on it.
                var progress = Mathf.Clamp01((_skillSeconds / _skill.effectSeconds - 0.3f) / 0.7f);
                _ground = _leapFrom + offset * Mathf.SmoothStep(0f, 1f, progress);
                lift = Mathf.Sin(progress * Mathf.PI) * LeapHeight;
                _dashTime = -1f;
            }
            else
            {
                _leaping = false;
                if ((target - _ground).sqrMagnitude > DashSnapDistance * DashSnapDistance && _dashTime < 0f)
                {
                    _dashFrom = _ground;
                    _dashTo = target;
                    _dashTime = 0f;
                }
                if (_dashTime >= 0f)
                {
                    _dashTime += delta;
                    _dashTo = target;
                    var t = Mathf.Clamp01(_dashTime / DashSeconds);
                    _ground = Vector2.Lerp(_dashFrom, _dashTo, 1f - (1f - t) * (1f - t));
                    if (t >= 1f)
                        _dashTime = -1f;
                }
                else
                    _ground = Vector2.Lerp(_ground, target, 1f - Mathf.Exp(-30f * delta));
            }
            _placement.position = _battle.ToView(_ground) + _battle.WorldUp * lift;
        }

        // Released at the drop time of Defeat: the weapon falls to the ground and lies flat.
        private void DropWeapon(float delta)
        {
            if (_weapon == null || _fallSeconds < _model.WeaponDropSeconds)
                return;
            if (!_weaponDropped)
            {
                _weaponDropped = true;
                _weapon.SetParent(transform, true);
                _weaponFrom = _weapon.position;
                _weaponFromRotation = _weapon.rotation;
                _weaponTime = 0f;
            }
            _weaponTime += delta;
            var t = Mathf.Clamp01(_weaponTime / 0.45f);
            var fall = t * t;
            var landed = new Vector3(_weaponFrom.x, _weaponFrom.y, 0f) - _battle.WorldUp * 0.02f;
            _weapon.position = Vector3.Lerp(_weaponFrom, landed, fall);
            // Lies flat on the ground, along the direction it was pointing.
            var along = _weaponFromRotation * Vector3.up;
            along.z = 0f;
            if (along.sqrMagnitude < 1e-4f)
                along = Vector3.right;
            var lying = Quaternion.LookRotation(Vector3.back, along.normalized) * Quaternion.Euler(0f, 0f, 0f);
            _weapon.rotation = Quaternion.Slerp(_weaponFromRotation, Quaternion.LookRotation(along.normalized, Vector3.back) *
                                                                     Quaternion.Euler(-90f, 0f, 0f), fall);
            _ = lying;
        }
    }
}

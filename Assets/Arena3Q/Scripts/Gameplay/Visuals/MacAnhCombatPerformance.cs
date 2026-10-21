using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>One cast clock and one event gate. Animations/VFX cannot apply damage by themselves.</summary>
    public sealed class MacAnhCombatPerformance
    {
        private readonly HeroUnit _hero;
        private readonly MacAnhPerformance _set;
        private readonly MacAnhPerformanceView _view;
        private readonly CastEventGate _gate = new CastEventGate();
        private int _cast;
        private float _timer;
        private UnitBase _target;
        private Vector2 _aim;
        private bool _basic;
        private MacAnhFx _shield;
        private MacAnhFx _charge;
        private MacAnhFx _aura;
        private int _shieldGeneration, _chargeGeneration, _auraGeneration;
        private MacAnhPerformance.SkillBinding _skill;
        private SkillAim _skillAim;
        private float _dodgeTravel;
        public bool IsAttacking => _basic;
        public MacAnhPerformanceView View => _view;
        public MacAnhCombatPerformance(HeroUnit hero, SpriteRenderer renderer)
        {
            _hero = hero; _set = hero.Definition.MacAnhPerformance;
            _view = renderer.gameObject.AddComponent<MacAnhPerformanceView>(); _view.Init(hero, renderer);
        }
        public void BeginAttack(UnitBase target)
        {
            Cancel(); _cast = _gate.Begin(); _timer = 0; _basic = true; _target = target;
            _aim = (target.Position - _hero.Position).normalized; _view.Play(MacAnhAction.Attack01, _aim, true);
        }
        public void Tick(float delta)
        {
            if (_shield != null && (_hero.Statuses.Shield <= 0 || !_hero.IsAlive)) StopOwned(ref _shield, _shieldGeneration);
            if (!_basic) return;
            if (_hero.IsStunned || !_hero.IsAlive || _hero.Skills.IsCasting) { Cancel(); return; }
            _timer += delta; _view.SetCastTime(_timer);
            if (_timer >= 14f / 30f && _gate.TryAccept(_cast, "SpawnWindSlash"))
            {
                if (_target != null && _target.IsAlive) Projectile("WindSlash", _aim, _hero.CreateMacAnhBasicHit(_target), _hero.AttackRange, _target.Radius, basic: true);
            }
            if (_timer >= .8f) { _basic = false; _gate.Cancel(); _view.Cancel(); }
        }
        private MacAnhFx Spawn(string key, Vector3 position, Vector3 scale, float duration, float angle = 0, Transform follow = null,
            int orderOffset = 3)
        {
            return MacAnhFxPool.For(_hero.Battle).Spawn(_set.Effect(key), position, scale, Quaternion.Euler(0, 0, angle),
                (MacAnhFxQuality)_set.EffectQuality, duration, _view.Body.sortingOrder + orderOffset, follow);
        }
        /// <summary>Optional extras (UltimateAura, DodgeStreak): skipped quietly when the profile has no prefab for them.</summary>
        private MacAnhFx SpawnExtra(string key, Vector3 position, float duration, float angle, Transform follow, int orderOffset)
        {
            return _set.Effect(key) == null ? null : Spawn(key, position, Vector3.one, duration, angle, follow, orderOffset);
        }
        private void Projectile(string key, Vector2 direction, System.Action<UnitBase> hit, float range, float targetRadius, int combo=0,
            bool basic=false)
        {
            if (!_view.TrySocket(MacAnhSocket.FanTip, out var socket))
            { Debug.LogError("[MacAnh] Missing sampled FanTip socket; cast cancelled."); return; }
            var projected = _hero.Battle.ToScreenPlane(_hero.Position + direction) - _hero.Battle.ToScreenPlane(_hero.Position);
            var angle = Mathf.Atan2(projected.y, projected.x) * Mathf.Rad2Deg;
            // The basic attack's blade and hit flash are drawn smaller than the skills' (pictures only).
            float size=basic?_set.BasicAttackVisualScale:1;
            float scale=_set.ProjectileVisualScale*size*(key=="TripleFeather"&&combo==2?_set.FinalFeatherVisualScale:1);
            var fx = Spawn(key, socket, Vector3.one * scale, _set.ProjectileLifetime, angle);
            MacAnhProjectiles.For(_hero.Battle).Launch(_hero, direction, socket, fx, hit, range + _hero.Radius + targetRadius, size);
        }
        public void BeginSkill(SkillDefinition skill, SkillAim aim)
        {
            Cancel(); var binding = _set.Find(skill); if (binding == null) return;
            _skill = binding; _skillAim = aim; _dodgeTravel = 0;
            _cast = _gate.Begin(); _view.Play(binding.action, aim.Direction, true);
        }
        public void SkillTime(float time)
        {
            _view.SetCastTime(time);
            if (_skill == null) return;
            var action = _skill.action;
            if (action == MacAnhAction.DodgeAttack)
            {
                var progress = Mathf.Clamp01((time * 30 - 8) / 5);
                _hero.Displace(-_skillAim.Direction * ((progress - _dodgeTravel) * _set.DodgeBackstepDistance));
                _dodgeTravel = progress;
            }
            if ((action == MacAnhAction.Skill01 && time >= .5f || action == MacAnhAction.Ultimate && time >= 1f) &&
                time < _skill.skill.CastTime && _gate.TryAccept(_cast, "Charge"))
            {
                var r = _skill.skill.Radius;
                // Lives a little past the release, which disperses it (FadeOut) while the release gathers.
                _charge = Spawn(action == MacAnhAction.Skill01 ? "MagicArray" : "Ultimate", _hero.Battle.ToView(_skillAim.Point),
                    new Vector3(r, r * _hero.Battle.ViewYScale, 1), _skill.skill.CastTime - time + .3f);
                if (_charge != null) { _chargeGeneration = _charge.Generation; _charge.SetCharge(); }
            }
            // Rune circle behind Mặc Ảnh from frame 31 until the release (spec §12), following the caster.
            if (action == MacAnhAction.Ultimate && time >= _set.UltimateAuraStart && time < _skill.skill.CastTime &&
                _gate.TryAccept(_cast, "UltimateAura"))
            {
                _aura = SpawnExtra("UltimateAura", _view.transform.position, _skill.skill.CastTime - time + .6f, 0, _view.transform, -2);
                if (_aura != null) _auraGeneration = _aura.Generation;
            }
            // Dash streak along the dodge (spec §13 "vệt thân lướt"): points back along the backstep.
            if (action == MacAnhAction.DodgeAttack && time >= _set.DodgeStreakStart && _gate.TryAccept(_cast, "DodgeStreak"))
            {
                var back = _hero.Battle.ToScreenPlane(_hero.Position - _skillAim.Direction) - _hero.Battle.ToScreenPlane(_hero.Position);
                SpawnExtra("DodgeStreak", _view.transform.position, .4f, Mathf.Atan2(back.y, back.x) * Mathf.Rad2Deg, null, -1);
            }
            if (action == MacAnhAction.DodgeAttack && time >= 8f / 30f && time <= 13f / 30f &&
                _gate.TryAccept(_cast, "Afterimage" + Mathf.FloorToInt(time * 15)))
            {
                var ghost = Spawn("Afterimage", _view.transform.position, Vector3.one, .35f);
                if (ghost != null) _view.ApplyAfterimage(ghost);
            }
        }
        public void SkillEffect(SkillDefinition skill, SkillAim aim, int combo)
        {
            var binding = _set.Find(skill); if (binding == null) return;
            var action = binding.action;
            if (!_gate.TryAccept(_cast, action + ":" + combo)) return;
            // The charge disperses into the release instead of vanishing in the same frame.
            if (_charge != null && _charge.Generation == _chargeGeneration) _charge.FadeOut(.25f);
            _charge = null;
            // Released: the rune circle fades out on its own clock; a later cancel/end must not cut it.
            if (action == MacAnhAction.Ultimate) _aura = null;
            if (action == MacAnhAction.Attack02 || action == MacAnhAction.DodgeAttack)
            {
                var context = SkillContext.Of(_hero, _hero.Battle.Rules.SkillValueMultiplier(_hero.Skills.Get(skill.Slot).Level));
                var copy = aim;
                Projectile(action == MacAnhAction.Attack02 ? "TripleFeather" : "CounterSlash", aim.Direction, u =>
                {
                    copy.Target = u.Id; copy.Point = u.Position;
                    SkillExecutor.Hit(skill, context, copy, combo + 1 == skill.RepeatCount ? skill.FinalHitMultiplier : 1, 0);
                }, SkillTargetingResolver.RangeOf(_hero, skill), _hero.Battle.Registry.Get(aim.Target)?.Radius ?? 0,combo);
                return;
            }
            var duration = Mathf.Max(.6f, skill.ZoneDuration);
            var radius = Mathf.Max(.1f, skill.Radius);
            var position = _hero.Battle.ToView(aim.Point);
            if (action == MacAnhAction.Skill03)
            {
                StopOwned(ref _shield, _shieldGeneration);
                var seconds = .6f; foreach (var effect in skill.Effects) if (effect.kind == SkillEffectKind.Shield) seconds = Mathf.Max(seconds, effect.duration);
                _shield = Spawn("Shield", _hero.transform.position, Vector3.one, seconds, 0, _hero.transform);
                if (_shield != null) _shieldGeneration = _shield.Generation;
            }
            else
            {
                // The Ultimate's buff is instant; its rune circle spreads from Mặc Ảnh's feet and stays a while.
                if (action == MacAnhAction.Ultimate) duration = Mathf.Max(duration, _set.UltimateCircleSeconds);
                Spawn(action == MacAnhAction.Skill01 ? "MagicArray" : action == MacAnhAction.Skill02 ? "Cyclone" : "Ultimate",
                    position, new Vector3(radius, radius * (_hero.Battle.ToView(Vector2.up).y), 1), duration);
                if (action == MacAnhAction.Ultimate) BuffAuras(skill, aim, radius);
            }
        }
        // Purple streaks around every allied hero the Ultimate buffed, as long as its buff lasts (soldiers get the buff
        // without them: dozens of particle auras would cost the frame rate).
        private void BuffAuras(SkillDefinition skill, SkillAim aim, float radius)
        {
            var seconds = 0f;
            foreach (var effect in skill.Effects) if (effect != null) seconds = Mathf.Max(seconds, effect.duration);
            if (seconds <= 0f || _set.Effect("UltimateBuff") == null) return;
            var size = Vector3.one * _set.UltimateBuffAuraRadius;
            foreach (var unit in _hero.Battle.Registry.Units)
                if (unit is HeroUnit ally && ally.IsAlive && ally.Team == _hero.Team &&
                    Vector2.Distance(ally.Position, aim.Point) <= radius + ally.Radius)
                    Spawn("UltimateBuff", ally.transform.position, size, seconds, 0, ally.transform);
        }
        public void EndSkill() { Cancel(); }
        public void Cancel()
        {
            _basic = false; _skill = null; _gate.Cancel();
            StopOwned(ref _charge, _chargeGeneration);
            StopOwned(ref _aura, _auraGeneration);
            if (_view != null) _view.Cancel();
        }
        // Never cut an effect off (user 2026-10-22): a cancelled or ended one disperses over a short fade.
        private static void StopOwned(ref MacAnhFx fx, int generation)
        {
            if (fx != null && fx.Generation == generation) fx.FadeOut(.3f);
            fx = null;
        }
        public void Die() { Cancel(); StopOwned(ref _shield, _shieldGeneration); }    }
}

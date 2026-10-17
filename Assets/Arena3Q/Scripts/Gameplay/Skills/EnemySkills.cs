using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// The skills of an elite or a boss in battle (spec §8 ✅). A skill starts on its own once its cooldown is over and a
    /// valid target is in range (never wasted, list order = priority). Its place is fixed at that moment and shown as a
    /// red warning (a mark on the target for single-target skills); the effect lands when the warning ends. Stunned or
    /// killed during the warning = cancelled without cooldown. The caster stands still while warning, casting and
    /// channelling. Enemies have no rage.
    /// </summary>
    public sealed class EnemySkills
    {
        public sealed class Slot
        {
            public EnemySkillDefinition Definition;
            public float CooldownLeft;
        }

        private sealed class Cast
        {
            public EnemySkillDefinition Skill;
            public Slot Slot;
            public List<SkillAim> Aims;
            public SkillContext Context;
            public readonly List<GameObject> Warnings = new List<GameObject>();
            public float Timer;
            public bool Fired;
            public float ChannelLeft;
        }

        private readonly EnemyUnit _caster;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly Dictionary<EnemySkillDefinition, List<EnemyUnit>> _summons =
            new Dictionary<EnemySkillDefinition, List<EnemyUnit>>();
        private Cast _cast;

        public EnemySkills(EnemyUnit caster, IReadOnlyList<EnemySkillDefinition> skills)
        {
            _caster = caster;
            SetSkills(skills);
        }

        public IReadOnlyList<Slot> Slots => _slots;
        public bool IsBusy => _cast != null;
        /// <summary>The skill showing its warning (not fired yet), or null.</summary>
        public EnemySkillDefinition Warning => _cast != null && !_cast.Fired ? _cast.Skill : null;

        /// <summary>A skill took effect (tests, HUD).</summary>
        public event System.Action<EnemySkillDefinition> Used;

        /// <summary>Replaces the skill list (boss phase 2); each new skill waits its first-use cooldown.</summary>
        public void SetSkills(IReadOnlyList<EnemySkillDefinition> skills)
        {
            _slots.Clear();
            if (skills == null)
                return;
            foreach (var skill in skills)
            {
                if (skill != null)
                    _slots.Add(new Slot { Definition = skill, CooldownLeft = skill.InitialCooldown });
            }
        }

        public void Tick(float deltaTime)
        {
            foreach (var slot in _slots)
            {
                if (slot.CooldownLeft > 0f)
                    slot.CooldownLeft = Mathf.Max(0f, slot.CooldownLeft - deltaTime);
            }

            if (_cast != null)
            {
                Advance(deltaTime);
                return;
            }
            if (!_caster.IsAlive || _caster.IsStunned)
                return;
            foreach (var slot in _slots)
            {
                if (slot.CooldownLeft > 0f)
                    continue;
                if (TryBegin(slot))
                    return;
            }
        }

        /// <summary>Uses a skill at once, without warning or cooldown (boss phase start: storm aura, shield).</summary>
        public void UseNow(EnemySkillDefinition skill)
        {
            if (skill == null || !_caster.IsAlive)
                return;
            var aims = ResolveAims(skill);
            if (aims.Count == 0)
                aims.Add(SelfAim());
            Fire(skill, SkillContext.Of(_caster, 1f), aims);
        }

        /// <summary>The caster died or the battle ended: drop the warning.</summary>
        public void Cancel()
        {
            if (_cast == null)
                return;
            foreach (var warning in _cast.Warnings)
            {
                if (warning != null)
                    Object.Destroy(warning);
            }
            _cast = null;
        }

        private bool TryBegin(Slot slot)
        {
            var skill = slot.Definition;
            var aims = ResolveAims(skill);
            if (aims.Count == 0 || !ConditionMet(skill, aims[0]))
                return false;

            _cast = new Cast { Skill = skill, Slot = slot, Aims = aims, Context = SkillContext.Of(_caster, 1f) };
            if (skill.TelegraphSeconds > 0f)
            {
                var battle = _caster.Battle;
                foreach (var aim in aims)
                {
                    // Spec §8: the red area stays where it was put; a single target gets a mark that follows it.
                    var mark = skill.AimsAtUnit && skill.Shape == SkillShape.Single ? battle.Registry.Get(aim.Target) : null;
                    var warning = battle.ShowWarning(skill, aim, skill.TelegraphSeconds + skill.CastTime, mark);
                    if (warning != null)
                        _cast.Warnings.Add(warning);
                }
            }
            if (skill.TelegraphSeconds + skill.CastTime <= 0f)
                Advance(0f);
            return true;
        }

        private void Advance(float deltaTime)
        {
            var cast = _cast;
            if (!cast.Fired)
            {
                // Spec §8 / docs/QuanDich/Animation.md 🟡: stunned or dead before the effect = cancelled, no cooldown.
                if (!_caster.IsAlive || _caster.IsStunned)
                {
                    Cancel();
                    return;
                }
                cast.Timer += deltaTime;
                if (cast.Timer < cast.Skill.TelegraphSeconds + cast.Skill.CastTime)
                    return;
                cast.Fired = true;
                cast.Slot.CooldownLeft = cast.Skill.Cooldown;
                Fire(cast.Skill, cast.Context, cast.Aims);
                cast.ChannelLeft = cast.Skill.ChannelSeconds;
            }
            else
                cast.ChannelLeft -= deltaTime;

            if (cast.ChannelLeft <= 0f || !_caster.IsAlive || _caster.IsStunned)
                _cast = null;
        }

        private void Fire(EnemySkillDefinition skill, SkillContext context, List<SkillAim> aims)
        {
            var registry = _caster.Battle.Registry;
            for (var i = 0; i < aims.Count; i++)
            {
                var aim = aims[i];
                if (skill.AimsAtUnit && skill.Shape == SkillShape.Single)
                {
                    // A single target that died during the warning: nothing to hit.
                    var target = registry.Get(aim.Target);
                    if (target == null)
                        continue;
                    aim.Point = target.Position;
                }
                if (skill.Effects.Count > 0 || skill.CasterMove != CasterMove.None)
                    SkillExecutor.Fire(_caster, skill, context, ref aim);
            }
            if (skill.Summons)
                Summon(skill);
            Used?.Invoke(skill);
        }

        // ---- Aim ----

        private SkillAim SelfAim()
        {
            var aim = new SkillAim { Origin = _caster.Position, Point = _caster.Position, Direction = Vector2.right };
            var target = _caster.Battle.Registry.Get(_caster.EngagedTarget);
            if (target != null && (target.Position - _caster.Position).sqrMagnitude > 1e-6f)
                aim.Direction = (target.Position - _caster.Position).normalized;
            return aim;
        }

        private List<SkillAim> ResolveAims(EnemySkillDefinition skill)
        {
            var aims = new List<SkillAim>();
            var battle = _caster.Battle;
            var range = skill.Range > 0f ? skill.Range : _caster.AttackRange;
            switch (skill.Aim)
            {
                case EnemySkillAim.Self:
                    aims.Add(SelfAim());
                    break;
                case EnemySkillAim.CurrentTarget:
                {
                    var target = battle.Registry.Get(_caster.EngagedTarget);
                    if (target == null || target.Team == _caster.Team || _caster.EdgeDistance(target) > range)
                        target = battle.Registry.Get(battle.Registry.FindNearestHostilePreferUnits(_caster, range));
                    if (target != null)
                        aims.Add(AimAt(target));
                    break;
                }
                case EnemySkillAim.NearestHero:
                case EnemySkillAim.RandomHero:
                {
                    var heroes = new List<HeroUnit>();
                    HeroUnit nearest = null;
                    var best = float.MaxValue;
                    foreach (var hero in battle.LivingHeroes)
                    {
                        if (hero.Team == _caster.Team)
                            continue;
                        var distance = _caster.EdgeDistance(hero);
                        if (distance > range)
                            continue;
                        heroes.Add(hero);
                        if (distance < best)
                        {
                            best = distance;
                            nearest = hero;
                        }
                    }
                    if (heroes.Count == 0)
                        break;
                    var chosen = skill.Aim == EnemySkillAim.NearestHero
                        ? nearest
                        : heroes[Mathf.Min(heroes.Count - 1, (int)(battle.Random01() * heroes.Count))];
                    aims.Add(AimAt(chosen));
                    break;
                }
                case EnemySkillAim.AroundPlayerUnits:
                {
                    var points = new List<Vector2>();
                    foreach (var unit in battle.Registry.Units)
                    {
                        if (unit != null && unit.IsAlive && unit.Team != _caster.Team && !unit.IsCastle &&
                            _caster.EdgeDistance(unit) <= range)
                            points.Add(unit.Position);
                    }
                    foreach (var point in EnemyRules.PickZonePoints(points, skill.ZoneCount, skill.ZoneSpread, battle.Random01))
                    {
                        var aim = SelfAim();
                        aim.Point = point;
                        aims.Add(aim);
                    }
                    break;
                }
            }
            return aims;
        }

        private SkillAim AimAt(UnitBase target)
        {
            var aim = new SkillAim
            {
                Target = target.Id,
                Origin = _caster.Position,
                Point = target.Position,
                Direction = Vector2.right
            };
            var offset = target.Position - _caster.Position;
            if (offset.sqrMagnitude > 1e-6f)
                aim.Direction = offset.normalized;
            return aim;
        }

        private bool ConditionMet(EnemySkillDefinition skill, SkillAim aim)
        {
            switch (skill.Condition)
            {
                case CastCondition.EnemyInArea: return SkillExecutor.CountInArea(_caster, skill, aim) > 0;
                case CastCondition.EnemyInAggroRange:
                    return _caster.Battle.Registry.FindNearestHostile(_caster, _caster.AggroRange).IsValid;
                default: return true;
            }
        }

        // ---- Summons ----

        private void Summon(EnemySkillDefinition skill)
        {
            if (!_summons.TryGetValue(skill, out var alive))
                _summons[skill] = alive = new List<EnemyUnit>();
            alive.RemoveAll(unit => unit == null || !unit.IsAlive);
            // Spec §8 🟡: at most summonLimit of this skill's units alive at once.
            var count = Mathf.Min(skill.SummonCount, Mathf.Max(0, skill.SummonLimit - alive.Count));
            for (var i = 0; i < count; i++)
            {
                var angle = (i + 0.5f) / Mathf.Max(1, count) * Mathf.PI * 2f;
                var position = _caster.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * skill.SummonRadius;
                var unit = skill.SummonClones
                    ? _caster.Battle.SpawnClone(_caster, position, skill.CloneHealthFactor, skill.CloneDamageFactor)
                    : _caster.Battle.SpawnSummon(_caster, skill.SummonUnit, position);
                if (unit != null)
                    alive.Add(unit);
            }
        }
    }
}

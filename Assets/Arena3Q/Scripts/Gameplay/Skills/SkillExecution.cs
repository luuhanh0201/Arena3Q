using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Where a skill lands: a target unit, a point and a direction (from the caster).</summary>
    public struct SkillAim
    {
        public UnitId Target;
        public Vector2 Point;
        public Vector2 Origin;
        public Vector2 Direction;
    }

    /// <summary>
    /// Who cast a hit and with what numbers. Kept as a snapshot so zones keep working after their caster dies
    /// (they then deal damage without a source: no rage, no lifesteal).
    /// </summary>
    public sealed class SkillContext
    {
        public BattleController Battle;
        public UnitBase Caster;
        public Team Team;
        public float Damage;
        public float MaxHealth;
        public float ValueMultiplier = 1f;

        public static SkillContext Of(UnitBase caster, float valueMultiplier) => new SkillContext
        {
            Battle = caster.Battle,
            Caster = caster,
            Team = caster.Team,
            Damage = caster.Damage,
            MaxHealth = caster.MaxHealth,
            ValueMultiplier = valueMultiplier
        };

        public UnitBase Source => Caster != null && Caster.IsAlive ? Caster : null;
    }

    /// <summary>
    /// Auto-aim (spec §3 🟡 order: the target the hero is fighting → the densest enemy group in range → the enemy castle;
    /// nothing in range = wait) and manual aim from a dragged card.
    /// </summary>
    public static class SkillTargetingResolver
    {
        public static float RangeOf(HeroUnit hero, SkillDefinition skill) => skill.Range > 0f ? skill.Range : hero.AttackRange;

        /// <param name="manualPoint">Where the card was dropped (ultimates), or null for auto-aim.</param>
        public static bool TryResolve(HeroUnit hero, SkillDefinition skill, Vector2? manualPoint, out SkillAim aim)
        {
            var battle = hero.Battle;
            var range = RangeOf(hero, skill);
            aim = new SkillAim { Origin = hero.Position, Point = hero.Position, Direction = Vector2.right };
            var found = false;

            switch (skill.Targeting)
            {
                case SkillTargeting.CurrentTarget:
                {
                    var target = manualPoint.HasValue
                        ? battle.Registry.FindNearest(manualPoint.Value, 0f, 2f, u => u.Team != hero.Team)
                        : CurrentOrNearest(hero, range);
                    if (target != null)
                    {
                        aim.Target = target.Id;
                        aim.Point = target.Position;
                        found = true;
                    }
                    break;
                }
                case SkillTargeting.Point:
                case SkillTargeting.Direction:
                {
                    if (manualPoint.HasValue)
                    {
                        var offset = manualPoint.Value - hero.Position;
                        aim.Point = skill.Targeting == SkillTargeting.Point && skill.Range > 0f && offset.magnitude > skill.Range
                            ? hero.Position + offset.normalized * skill.Range
                            : manualPoint.Value;
                        found = true;
                    }
                    else
                        found = TryAutoPoint(hero, skill, range, out aim.Point);
                    break;
                }
                case SkillTargeting.Self:
                    // A dragged card may move the area within the skill's range (the shield wall: 4 around the hero).
                    aim.Point = hero.Position;
                    if (manualPoint.HasValue && skill.Range > 0f && !skill.FollowCaster)
                        aim.Point = hero.Position + Vector2.ClampMagnitude(manualPoint.Value - hero.Position, skill.Range);
                    found = true;
                    break;
                case SkillTargeting.Global:
                    found = AnyEnemy(hero);
                    break;
                case SkillTargeting.AllyNeedingSoldiers:
                {
                    var ally = battle.FindHeroNeedingSoldiers(hero, range);
                    if (ally != null)
                    {
                        aim.Target = ally.Id;
                        aim.Point = ally.Position;
                        found = true;
                    }
                    break;
                }
            }

            var toPoint = aim.Point - hero.Position;
            if (toPoint.sqrMagnitude > 1e-6f)
                aim.Direction = toPoint.normalized;
            if (!found)
                return false;

            switch (skill.Condition)
            {
                case CastCondition.EnemyInArea: return SkillExecutor.CountInArea(hero, skill, aim) > 0;
                case CastCondition.EnemyInAggroRange: return battle.Registry.FindNearestHostile(hero, hero.AggroRange).IsValid;
                default: return true;
            }
        }

        private static UnitBase CurrentOrNearest(HeroUnit hero, float range)
        {
            var registry = hero.Battle.Registry;
            foreach (var id in new[] { hero.EngagedTarget, hero.Brain.AttackTarget })
            {
                var current = registry.Get(id);
                if (current != null && current.Team != hero.Team && hero.EdgeDistance(current) <= range)
                    return current;
            }
            return registry.Get(registry.FindNearestHostilePreferUnits(hero, range));
        }

        private static bool TryAutoPoint(HeroUnit hero, SkillDefinition skill, float range, out Vector2 point)
        {
            var registry = hero.Battle.Registry;
            var current = registry.Get(hero.EngagedTarget);
            if (current != null && current.Team != hero.Team && !current.IsCastle && hero.EdgeDistance(current) <= range)
            {
                point = current.Position;
                return true;
            }

            var enemies = new List<Vector2>();
            foreach (var unit in registry.Units)
            {
                if (unit != null && unit.IsAlive && unit.Team != hero.Team && !unit.IsCastle && hero.EdgeDistance(unit) <= range)
                    enemies.Add(unit.Position);
            }
            var groupRadius = skill.Shape == SkillShape.Circle ? Mathf.Max(0.5f, skill.Radius) : Mathf.Max(1f, skill.Width);
            if (SkillMath.DensestPoint(enemies, groupRadius, out point, out _))
                return true;

            var castle = registry.Get(hero.Battle.EnemyCastleId);
            if (castle != null && hero.Team == Team.Player && hero.EdgeDistance(castle) <= range)
            {
                point = castle.Position;
                return true;
            }
            point = hero.Position;
            return false;
        }

        private static bool AnyEnemy(HeroUnit hero)
        {
            foreach (var unit in hero.Battle.Registry.Units)
            {
                if (unit != null && unit.IsAlive && unit.Team != hero.Team)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Applies a skill: moves the caster, finds who is in the area, applies each effect, draws the placeholder.</summary>
    public static class SkillExecutor
    {
        private static readonly List<UnitBase> Scratch = new List<UnitBase>();

        /// <summary>Once per cast (first repeat): caster movement, then the hit or the zone.</summary>
        public static void Fire(UnitBase caster, SkillAreaDefinition skill, SkillContext context, ref SkillAim aim)
        {
            var battle = caster.Battle;
            switch (skill.CasterMove)
            {
                case CasterMove.DashToTarget:
                {
                    var target = battle.Registry.Get(aim.Target);
                    if (target != null)
                    {
                        var offset = caster.Position - target.Position;
                        var gap = caster.Radius + target.Radius + 0.05f;
                        caster.SetPosition(battle.Layout.ResolveStep(caster.Position,
                            target.Position + (offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector2.left) * gap, caster.Radius));
                        aim.Point = target.Position;
                    }
                    break;
                }
                case CasterMove.ChargeLine:
                {
                    aim.Origin = caster.Position;
                    var end = battle.Layout.ResolveStep(caster.Position, caster.Position + aim.Direction * skill.MoveDistance, caster.Radius);
                    caster.SetPosition(end);
                    break;
                }
            }

            if (skill.ZoneDuration > 0f)
                battle.AddZone(new SkillZone(skill, context, aim));
            else
                Hit(skill, context, aim, 1f, 0f);
            battle.ShowSkill(skill, aim, context.Team, skill.ZoneDuration > 0f ? skill.ZoneDuration : 0.4f,
                skill.FollowCaster ? caster : null);
        }

        /// <summary>
        /// One hit on everything in the area. <paramref name="minStatusSeconds"/> keeps statuses of a zone alive between
        /// its ticks.
        /// </summary>
        public static void Hit(SkillAreaDefinition skill, SkillContext context, SkillAim aim, float multiplier, float minStatusSeconds)
        {
            foreach (var effect in skill.Effects)
            {
                if (effect == null)
                    continue;
                Select(skill, context, aim, effect.affects, Scratch);
                foreach (var unit in Scratch)
                    Apply(effect, skill, context, aim, unit, multiplier, minStatusSeconds);
            }
        }

        public static int CountInArea(UnitBase caster, SkillAreaDefinition skill, SkillAim aim)
        {
            Select(skill, SkillContext.Of(caster, 1f), aim, SkillAffects.Enemies, Scratch);
            return Scratch.Count;
        }

        private static void Select(SkillAreaDefinition skill, SkillContext context, SkillAim aim, SkillAffects affects, List<UnitBase> into)
        {
            into.Clear();
            var battle = context.Battle;
            if (battle == null)
                return;
            if (affects == SkillAffects.Self)
            {
                if (context.Caster != null && context.Caster.IsAlive)
                    into.Add(context.Caster);
                return;
            }

            var center = skill.FollowCaster && context.Caster != null && context.Caster.IsAlive ? context.Caster.Position : aim.Point;
            UnitBase first = null;
            var firstAlong = float.MaxValue;
            foreach (var unit in battle.Registry.Units)
            {
                if (unit == null || !unit.IsAlive)
                    continue;
                var enemy = unit.Team != context.Team;
                if (enemy != (affects == SkillAffects.Enemies))
                    continue;
                if (!enemy && unit.IsCastle)
                    continue;
                switch (skill.Shape)
                {
                    case SkillShape.Single:
                        if (unit.Id == aim.Target)
                            into.Add(unit);
                        break;
                    case SkillShape.Circle:
                        if (Vector2.Distance(unit.Position, center) <= skill.Radius + unit.Radius)
                            into.Add(unit);
                        break;
                    case SkillShape.Cone:
                        if (SkillMath.InCone(aim.Origin, aim.Direction, skill.Length, skill.Angle, unit.Position, unit.Radius))
                            into.Add(unit);
                        break;
                    case SkillShape.Line:
                    {
                        var along = SkillMath.AlongLine(aim.Origin, aim.Direction, LineLength(skill, context, aim), skill.Width,
                            unit.Position, unit.Radius);
                        if (along < 0f)
                            break;
                        if (skill.FirstHitOnly && enemy)
                        {
                            if (along < firstAlong)
                            {
                                firstAlong = along;
                                first = unit;
                            }
                        }
                        else
                            into.Add(unit);
                        break;
                    }
                    case SkillShape.Global:
                        if (!unit.IsCastle)
                            into.Add(unit);
                        break;
                }
            }
            if (first != null)
                into.Add(first);
        }

        // A charge hits what it passes: from where it started to where the caster stopped.
        private static float LineLength(SkillAreaDefinition skill, SkillContext context, SkillAim aim) =>
            skill.CasterMove == CasterMove.ChargeLine && context.Caster != null
                ? Mathf.Max(0.5f, Vector2.Distance(aim.Origin, context.Caster.Position))
                : skill.Length;

        private static void Apply(SkillAreaDefinition.Effect effect, SkillAreaDefinition skill, SkillContext context, SkillAim aim,
            UnitBase unit, float multiplier, float minStatusSeconds)
        {
            var value = effect.value * context.ValueMultiplier * multiplier;
            var seconds = Mathf.Max(effect.duration, minStatusSeconds);
            switch (effect.kind)
            {
                case SkillEffectKind.Damage:
                {
                    var damage = context.Damage * value;
                    if (unit.IsCastle)
                        damage *= skill.CastleMultiplier;
                    if (context.Caster != null)
                    {
                        damage *= context.Caster.Statuses.DamageMultiplier;
                        if (context.Caster is HeroUnit caster)
                            damage = caster.Skills.ModifyOutgoingDamage(unit, damage);
                    }
                    unit.TakeDamage(damage, context.Source);
                    break;
                }
                case SkillEffectKind.Poison:
                    unit.ApplyStatus(StatusKind.Poison, context.Damage * value, seconds, context.Source);
                    break;
                case SkillEffectKind.Shield:
                    unit.ApplyStatus(StatusKind.Shield, context.MaxHealth * value, seconds, context.Source);
                    break;
                case SkillEffectKind.Knockback:
                {
                    var direction = skill.Shape == SkillShape.Line || skill.Shape == SkillShape.Cone
                        ? aim.Direction
                        : unit.Position - (skill.Shape == SkillShape.Global ? aim.Origin : aim.Point);
                    if (direction.sqrMagnitude < 1e-6f)
                        direction = aim.Direction;
                    unit.Displace(direction.normalized * effect.value);
                    break;
                }
                case SkillEffectKind.RefillSquad:
                    if (unit is HeroUnit hero)
                        hero.Battle.RefillSquad(hero, Mathf.RoundToInt(effect.value));
                    break;
                case SkillEffectKind.Heal:
                    unit.Heal(unit.MaxHealth * value);
                    break;
                case SkillEffectKind.Pull:
                {
                    // Towards the caster, stopping next to it (Câu Hồn Tỏa); forbidden zones stop the pull (spec §8).
                    var anchor = context.Caster != null && context.Caster.IsAlive ? context.Caster.Position : aim.Origin;
                    var toAnchor = anchor - unit.Position;
                    var gap = toAnchor.magnitude - unit.Radius - (context.Caster != null ? context.Caster.Radius : 0f) - 0.1f;
                    if (gap > 0f)
                        unit.Displace(toAnchor.normalized * Mathf.Min(gap, effect.value));
                    break;
                }
                case SkillEffectKind.Taunt:
                    if (context.Source != null)
                        unit.ApplyStatus(StatusKind.Taunt, 1f, seconds, context.Source);
                    break;
                // Statuses keep their raw value (a 40 % slow stays 40 % at every skill level).
                case SkillEffectKind.Stun: unit.ApplyStatus(StatusKind.Stun, 1f, seconds, context.Source); break;
                case SkillEffectKind.Slow: unit.ApplyStatus(StatusKind.Slow, effect.value, seconds, context.Source); break;
                case SkillEffectKind.Weaken: unit.ApplyStatus(StatusKind.Weaken, effect.value, seconds, context.Source); break;
                case SkillEffectKind.AttackSpeedDown: unit.ApplyStatus(StatusKind.AttackSpeedDown, effect.value, seconds, context.Source); break;
                case SkillEffectKind.AttackSpeedUp: unit.ApplyStatus(StatusKind.AttackSpeedUp, effect.value, seconds, context.Source); break;
                case SkillEffectKind.MoveSpeedUp: unit.ApplyStatus(StatusKind.MoveSpeedUp, effect.value, seconds, context.Source); break;
                case SkillEffectKind.DamageUp: unit.ApplyStatus(StatusKind.DamageUp, effect.value, seconds, context.Source); break;
                case SkillEffectKind.DamageTakenDown: unit.ApplyStatus(StatusKind.DamageTakenDown, effect.value, seconds, context.Source); break;
                case SkillEffectKind.CrowdControlImmunity: unit.ApplyStatus(StatusKind.CrowdControlImmunity, 1f, seconds, context.Source); break;
            }
        }
    }

    /// <summary>An area that stays on the ground (rain of arrows, fire field, formation, shield wall) and hits every tick.</summary>
    public sealed class SkillZone
    {
        private readonly SkillAreaDefinition _skill;
        private readonly SkillContext _context;
        private SkillAim _aim;
        private float _remaining;
        private float _tick;

        public SkillZone(SkillAreaDefinition skill, SkillContext context, SkillAim aim)
        {
            _skill = skill;
            _context = context;
            _aim = aim;
            _remaining = skill.ZoneDuration;
        }

        public bool IsOver => _remaining <= 0f;

        public void Tick(float deltaTime)
        {
            if (IsOver)
                return;
            if (_skill.FollowCaster && _context.Caster != null && _context.Caster.IsAlive)
                _aim.Point = _context.Caster.Position;
            _tick -= deltaTime;
            if (_tick <= 0f)
            {
                SkillExecutor.Hit(_skill, _context, _aim, 1f, _skill.TickInterval * 1.5f);
                _tick += _skill.TickInterval;
            }
            _remaining -= deltaTime;
        }
    }
}

using System;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// One hero's four skills in battle (spec §3 "Kỹ năng trên thẻ"): the passive's effects, skills 2 and 3 auto-cast
    /// (skill 2 first) with cooldowns, the ultimate on a rage bar that only the hero's own damage fills. A skill takes
    /// effect at the end of its wind-up (Cast); stunned or dead before that, it is not used (no cooldown, rage kept).
    /// </summary>
    public sealed class HeroSkills
    {
        public sealed class Slot
        {
            public SkillDefinition Definition;
            /// <summary>0 = not learned yet (lords by level, others by stars).</summary>
            public int Level;
            public float CooldownLeft;
            public bool Unlocked => Definition != null && Level > 0;
        }

        private sealed class Cast
        {
            public SkillSlot Slot;
            public SkillDefinition Definition;
            public SkillContext Context;
            public SkillAim Aim;
            public float Timer;
            public bool Fired;
            public int RepeatsDone;
            public float NextRepeat;
        }

        private const float AuraRefreshSeconds = 0.25f;

        private readonly HeroUnit _hero;
        private readonly ProgressionRules _rules;
        private readonly Slot[] _slots = new Slot[4];
        private Cast _cast;
        private int _basicAttacks;
        private UnitId _comboTarget;
        private int _comboStacks;
        private float _runSeconds;
        private bool _charged;
        private Vector2 _lastPosition;
        private float _auraTimer;

        public HeroSkills(HeroUnit hero, LineupEntry card, ProgressionRules rules)
        {
            _hero = hero;
            _rules = rules;
            foreach (SkillSlot slot in Enum.GetValues(typeof(SkillSlot)))
                _slots[(int)slot] = new Slot { Definition = card.Hero.GetSkill(slot), Level = card.SkillLevel(slot, rules) };
            Rage = new RageMeter(card.Hero.RageMax);
            _lastPosition = hero.Position;
        }

        public RageMeter Rage { get; }
        public bool IsCasting => _cast != null;
        public SkillSlot? CastingSlot => _cast?.Slot;
        public bool HasUltimate => Get(SkillSlot.Ultimate).Unlocked;
        public bool IsCharged => _charged;
        public int ComboStacks => _comboStacks;

        /// <summary>A skill took effect (for the HUD and tests).</summary>
        public event Action<SkillSlot> Used;

        public Slot Get(SkillSlot slot) => _slots[(int)slot];

        // ---- Passive ----

        private SkillDefinition.Passive Passive(PassiveKind kind)
        {
            var slot = Get(SkillSlot.Passive);
            if (!slot.Unlocked)
                return null;
            foreach (var passive in slot.Definition.Passives)
            {
                if (passive != null && passive.kind == kind)
                    return passive;
            }
            return null;
        }

        public float AttackRangeBonus => Passive(PassiveKind.AttackRangeBonus)?.value ?? 0f;
        public int ExtraSoldiers => Passive(PassiveKind.ExtraSoldiers)?.count ?? 0;
        public float ArmyMaxHealthBonus => Passive(PassiveKind.ArmyMaxHealth)?.value ?? 0f;

        public float PassiveDamageReduction(float healthFraction)
        {
            var passive = Passive(PassiveKind.DamageReduction);
            if (passive == null)
                return 0f;
            return healthFraction < passive.threshold ? passive.secondValue : passive.value;
        }

        public float AttackSpeedMultiplier(float healthFraction)
        {
            var passive = Passive(PassiveKind.LowHealthAttackSpeed);
            return passive != null && healthFraction < passive.threshold ? 1f + passive.value : 1f;
        }

        /// <summary>Damage bonuses that apply to every hit of the hero (basic attacks and skills).</summary>
        public float ModifyOutgoingDamage(UnitBase target, float damage)
        {
            var executer = Passive(PassiveKind.LowHealthTargetBonus);
            if (executer != null && !target.IsCastle && target.Health / target.MaxHealth < executer.threshold)
                damage *= 1f + executer.value;
            return damage;
        }

        /// <summary>Basic-attack-only bonuses: combo stacks and the charged first hit.</summary>
        public float ModifyBasicAttack(UnitBase target, float damage)
        {
            var combo = Passive(PassiveKind.ComboStacks);
            if (combo != null)
                damage *= 1f + combo.value * (target.Id == _comboTarget ? _comboStacks : 0);
            var charge = Passive(PassiveKind.ChargedFirstHit);
            if (charge != null && _charged)
                damage *= charge.value;
            return ModifyOutgoingDamage(target, damage);
        }

        public void OnBasicAttackLanded(UnitBase target, float damage)
        {
            var combo = Passive(PassiveKind.ComboStacks);
            if (combo != null)
            {
                _comboStacks = target.Id == _comboTarget ? Mathf.Min(combo.count, _comboStacks + 1) : 1;
                _comboTarget = target.Id;
            }

            var charge = Passive(PassiveKind.ChargedFirstHit);
            if (charge != null && _charged)
            {
                _charged = false;
                target.ApplyStatus(StatusKind.Stun, 1f, charge.secondValue, _hero);
            }

            var lifesteal = Passive(PassiveKind.Lifesteal);
            if (lifesteal != null)
                _hero.Heal(damage * lifesteal.value);

            var pierce = Passive(PassiveKind.PierceEveryNthAttack);
            if (pierce != null && pierce.count > 0 && ++_basicAttacks % pierce.count == 0)
            {
                // The arrow goes through: the nearest enemy just behind the target is hit too.
                var direction = (target.Position - _hero.Position).normalized;
                var behind = _hero.Battle.Registry.FindNearest(target.Position + direction * 1.2f, 0f, 1.5f,
                    u => u.Team != _hero.Team && u != target && !u.IsCastle);
                if (behind != null)
                    behind.TakeDamage(damage, _hero);
            }
        }

        /// <summary>Rage from the hero's own damage (spec 🟡: proportional to damage, about 18 basic hits to fill).</summary>
        public void OnDealtDamage(float amount)
        {
            if (!HasUltimate || IsCasting && _cast.Slot == SkillSlot.Ultimate)
                return;
            Rage.Add(RageMeter.GainFor(amount, _hero.Damage, _hero.Definition.RageHitsToFill, Rage.Max,
                _hero.Battle.PlayerModifiers.RageGainBonus));
        }

        public void OnDeath()
        {
            _cast = null;
            Rage.Reset();
        }

        // ---- Every step ----

        public void Tick(float deltaTime)
        {
            foreach (var slot in _slots)
            {
                if (slot.CooldownLeft > 0f)
                    slot.CooldownLeft = Mathf.Max(0f, slot.CooldownLeft - deltaTime);
            }
            TickPassives(deltaTime);

            if (_cast != null)
            {
                AdvanceCast(deltaTime);
                return;
            }
            if (_hero.IsStunned)
                return;
            if (!TryAutoCast(SkillSlot.Skill2))
                TryAutoCast(SkillSlot.Skill3);
        }

        private void TickPassives(float deltaTime)
        {
            var moved = (_hero.Position - _lastPosition).sqrMagnitude > 1e-6f;
            _lastPosition = _hero.Position;
            var charge = Passive(PassiveKind.ChargedFirstHit);
            if (charge != null)
            {
                _runSeconds = moved ? _runSeconds + deltaTime : 0f;
                if (_runSeconds >= charge.threshold)
                    _charged = true;
            }

            var aura = Passive(PassiveKind.AllyTroopDamageAura);
            if (aura != null && (_auraTimer -= deltaTime) <= 0f)
            {
                _auraTimer = AuraRefreshSeconds;
                foreach (var unit in _hero.Battle.Registry.Units)
                {
                    if (unit is TroopUnit troop && troop.Team == _hero.Team && troop.IsAlive &&
                        Vector2.Distance(troop.Position, _hero.Position) <= aura.radius + troop.Radius)
                        troop.ApplyStatus(StatusKind.DamageUp, aura.value, AuraRefreshSeconds * 1.6f, _hero);
                }
            }
        }

        private bool TryAutoCast(SkillSlot slot)
        {
            var state = Get(slot);
            if (!state.Unlocked)
                return false;
            var skill = state.Definition;
            var movingOnOrder = _hero.Brain.State == HeroState.Moving;
            if (!SkillMath.CanAutoCast(state.Unlocked, state.CooldownLeft, IsCasting, _hero.IsStunned, movingOnOrder,
                    skill.CasterMove != CasterMove.None, skill.AutoCastWhileMoving))
                return false;
            if (!SkillTargetingResolver.TryResolve(_hero, skill, null, out var aim))
                return false;
            Begin(slot, aim);
            return true;
        }

        /// <summary>Uses the ultimate (tap: auto-aim; drag: <paramref name="point"/>). Rejected when it cannot be used.</summary>
        public RejectReason TryUltimate(Vector2? point)
        {
            if (!_hero.IsAlive)
                return RejectReason.HeroNotAvailable;
            var slot = Get(SkillSlot.Ultimate);
            if (!slot.Unlocked || !Rage.IsFull || _hero.IsStunned || _cast?.Slot == SkillSlot.Ultimate)
                return RejectReason.SkillNotReady;
            if (!SkillTargetingResolver.TryResolve(_hero, slot.Definition, point, out var aim))
                return RejectReason.NoSkillTarget;
            // The player's ultimate goes first: a skill still winding up is dropped without being used.
            if (_cast != null && !_cast.Fired)
                _cast = null;
            Begin(SkillSlot.Ultimate, aim);
            return RejectReason.None;
        }

        private void Begin(SkillSlot slot, SkillAim aim)
        {
            var state = Get(slot);
            _cast = new Cast
            {
                Slot = slot,
                Definition = state.Definition,
                Context = SkillContext.Of(_hero, _rules.SkillValueMultiplier(state.Level)),
                Aim = aim
            };
            if (state.Definition.CastTime <= 0f)
                AdvanceCast(0f);
        }

        private void AdvanceCast(float deltaTime)
        {
            var cast = _cast;
            var skill = cast.Definition;
            if (!_hero.IsAlive || _hero.IsStunned)
            {
                // Spec §3 🟡: interrupted before Cast = not used; once fired, the remaining repeats are lost.
                _cast = null;
                return;
            }

            cast.Timer += deltaTime;
            if (!cast.Fired)
            {
                if (cast.Timer < skill.CastTime)
                    return;
                cast.Fired = true;
                Pay(cast.Slot);
                SkillExecutor.Fire(_hero, skill, cast.Context, ref cast.Aim);
                cast.RepeatsDone = 1;
                cast.NextRepeat = cast.Timer + skill.RepeatInterval;
                Used?.Invoke(cast.Slot);
            }

            while (cast.RepeatsDone < skill.RepeatCount && cast.Timer >= cast.NextRepeat)
            {
                // Repeats keep hitting the same target; if it fell, the next one in reach.
                if (skill.Shape == SkillShape.Single && !_hero.Battle.Registry.IsAlive(cast.Aim.Target) &&
                    SkillTargetingResolver.TryResolve(_hero, skill, null, out var retarget))
                    cast.Aim = retarget;
                cast.RepeatsDone++;
                var last = cast.RepeatsDone == skill.RepeatCount;
                SkillExecutor.Hit(skill, cast.Context, cast.Aim, last ? skill.FinalHitMultiplier : 1f, 0f);
                cast.NextRepeat += skill.RepeatInterval;
            }

            if (cast.RepeatsDone >= skill.RepeatCount && cast.Timer >= skill.CastTime)
                _cast = null;
        }

        private void Pay(SkillSlot slot)
        {
            var state = Get(slot);
            if (slot == SkillSlot.Ultimate)
                Rage.Reset();
            else
                state.CooldownLeft = state.Definition.Cooldown * Mathf.Max(0f, 1f - _hero.Battle.PlayerModifiers.CooldownReduction);
        }
    }
}

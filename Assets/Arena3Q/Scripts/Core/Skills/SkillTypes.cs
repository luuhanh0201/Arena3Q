namespace Arena3Q.Core
{
    /// <summary>How a skill picks where it lands (spec §3 "Kỹ năng trên thẻ", auto-aim order in <c>SkillTargeting</c>).</summary>
    public enum SkillTargeting
    {
        /// <summary>One enemy: the one the hero is fighting, else the nearest in range, else the enemy castle in range.</summary>
        CurrentTarget,
        /// <summary>A point: the current target, else the centre of the densest enemy group in range, else the castle.</summary>
        Point,
        /// <summary>A direction from the hero towards the point picked like <see cref="Point"/> (or where the card is dropped).</summary>
        Direction,
        /// <summary>Centred on the hero.</summary>
        Self,
        /// <summary>The whole battlefield.</summary>
        Global,
        /// <summary>The nearest allied hero (the caster included) whose squad has lost soldiers.</summary>
        AllyNeedingSoldiers
    }

    public enum SkillShape
    {
        Single,
        Circle,
        Cone,
        Line,
        Global
    }

    public enum SkillAffects
    {
        Enemies,
        Allies,
        Self
    }

    /// <summary>When an auto-cast skill may start (spec: "không có mục tiêu trong tầm thì chờ, không phí chiêu").</summary>
    public enum CastCondition
    {
        /// <summary>The targeting found something.</summary>
        HasTarget,
        /// <summary>At least one enemy is inside the skill's area around its target point.</summary>
        EnemyInArea,
        /// <summary>An enemy is within the hero's aggro range (buffs).</summary>
        EnemyInAggroRange
    }

    public enum CasterMove
    {
        None,
        /// <summary>The hero dashes next to its target.</summary>
        DashToTarget,
        /// <summary>The hero charges in a straight line, hitting what it passes.</summary>
        ChargeLine
    }

    /// <summary>
    /// What one effect does. Values: Damage/Poison = fraction of the caster's Công (Poison per second); Shield = fraction
    /// of the caster's max health; Slow/Weaken/…Up/…Down = fraction; Knockback = distance; RefillSquad = soldiers.
    /// </summary>
    public enum SkillEffectKind
    {
        Damage,
        Stun,
        Slow,
        Weaken,
        AttackSpeedDown,
        Poison,
        Knockback,
        Shield,
        AttackSpeedUp,
        MoveSpeedUp,
        DamageUp,
        DamageTakenDown,
        CrowdControlImmunity,
        RefillSquad,
        /// <summary>value = share of the target's max health healed (enemy support skills).</summary>
        Heal,
        /// <summary>Pulls the target towards the caster, at most value units, stopped by forbidden zones.</summary>
        Pull,
        /// <summary>Forces free-fighting units to attack the caster (spec §8: not units carrying out an order).</summary>
        Taunt
    }

    /// <summary>Always-on effects of a passive skill (kỹ năng 1).</summary>
    public enum PassiveKind
    {
        None,
        /// <summary>value = +fraction of attack range.</summary>
        AttackRangeBonus,
        /// <summary>count = every Nth basic attack also hits the enemy behind the target.</summary>
        PierceEveryNthAttack,
        /// <summary>value = fraction of basic-attack damage healed.</summary>
        Lifesteal,
        /// <summary>below threshold health: attack speed +value.</summary>
        LowHealthAttackSpeed,
        /// <summary>allied soldiers within radius: damage +value.</summary>
        AllyTroopDamageAura,
        /// <summary>count = more soldiers in the hero's own squad (still ≤ maxSquadSize).</summary>
        ExtraSoldiers,
        /// <summary>value = +fraction max health for the whole army while the hero is on the field.</summary>
        ArmyMaxHealth,
        /// <summary>targets below threshold health take +value damage (basic attacks and skills).</summary>
        LowHealthTargetBonus,
        /// <summary>value = damage reduction; below threshold health it becomes secondValue.</summary>
        DamageReduction,
        /// <summary>each consecutive hit on the same target: +value damage, up to count stacks.</summary>
        ComboStacks,
        /// <summary>after running threshold seconds: next basic attack × value and stuns secondValue seconds.</summary>
        ChargedFirstHit
    }

    /// <summary>Timed states on a unit (statuses from skills).</summary>
    public enum StatusKind
    {
        Stun,
        Slow,
        Weaken,
        AttackSpeedDown,
        Poison,
        Shield,
        AttackSpeedUp,
        MoveSpeedUp,
        DamageUp,
        DamageTakenDown,
        CrowdControlImmunity,
        /// <summary>Attack the source (spec §8 taunt); crowd control, so immunity blocks it.</summary>
        Taunt
    }
}

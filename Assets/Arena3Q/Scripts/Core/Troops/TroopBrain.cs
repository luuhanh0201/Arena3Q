using System;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>How a squad behaves, derived from its hero's state (spec §7).</summary>
    public enum SquadMode
    {
        /// <summary>Hero Idle: hold formation, hit enemies in attack range without chasing.</summary>
        HoldFormation,
        /// <summary>Hero Moving: march in formation, never chase, only self-defend.</summary>
        MarchFormation,
        /// <summary>Hero Attacking: attack the hero's ordered target.</summary>
        FocusTarget,
        /// <summary>Hero Advancing/Combat: fight freely but stay within the leash radius of the hero.</summary>
        Skirmish,
        /// <summary>Hero dead, KeepFighting: advance like a hero would, without a leash.</summary>
        Leaderless,
        /// <summary>Hero dead, Retreat: walk back to the player castle.</summary>
        Retreat,
        /// <summary>Hero dead, Disband: the squad is removed.</summary>
        Disband
    }

    public static class SquadModes
    {
        public static SquadMode FromHero(HeroState heroState, OnHeroDeathTroopBehavior onHeroDeath)
        {
            switch (heroState)
            {
                case HeroState.Moving: return SquadMode.MarchFormation;
                case HeroState.Attacking: return SquadMode.FocusTarget;
                case HeroState.Advancing:
                case HeroState.Combat: return SquadMode.Skirmish;
                case HeroState.Dead:
                    switch (onHeroDeath)
                    {
                        case OnHeroDeathTroopBehavior.Retreat: return SquadMode.Retreat;
                        case OnHeroDeathTroopBehavior.Disband: return SquadMode.Disband;
                        default: return SquadMode.Leaderless;
                    }
                default: return SquadMode.HoldFormation;
            }
        }
    }

    /// <summary>What a squad soldier perceives. Enemy queries return living enemy units (not castles).</summary>
    public interface ITroopSensor
    {
        Vector2 Position { get; }
        /// <summary>This soldier's formation slot in world space.</summary>
        Vector2 SlotPosition { get; }
        Vector2 HeroPosition { get; }
        /// <summary>
        /// FocusTarget: the hero's AttackCommand target. Other modes: the unit the hero is currently fighting. None if neither.
        /// </summary>
        UnitId HeroTarget { get; }
        UnitId EnemyInAttackRange { get; }
        UnitId EnemyInAggroRange { get; }
        UnitId NearestEnemy { get; }
        UnitId EnemyCastle { get; }
        UnitId CloseAttacker { get; }
        Vector2 RetreatPoint { get; }
        bool IsAlive(UnitId unit);
        Vector2 GetPosition(UnitId unit);
    }

    public sealed class TroopBrainSettings
    {
        public StageType StageType { get; set; } = StageType.Attack;
        /// <summary>Provisional: max distance from the hero while skirmishing (spec §7).</summary>
        public float LeashRadius { get; set; } = 4f;
        public float SlotTolerance { get; set; } = 0.15f;
    }

    /// <summary>Squad soldier behaviour (spec §7). Soldiers always follow their own hero's orders.</summary>
    public sealed class TroopBrain
    {
        private readonly TroopBrainSettings _settings;
        private bool _returningToHero;

        public TroopBrain(TroopBrainSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public HeroIntent Tick(SquadMode mode, ITroopSensor sensor)
        {
            switch (mode)
            {
                case SquadMode.MarchFormation:
                    // Provisional self-defence with the same reactionRange rule as the hero.
                    return sensor.CloseAttacker.IsValid
                        ? HeroIntent.Engage(sensor.CloseAttacker, mayChase: true)
                        : HeroIntent.MoveTo(sensor.SlotPosition);

                case SquadMode.FocusTarget:
                    return sensor.HeroTarget.IsValid && sensor.IsAlive(sensor.HeroTarget)
                        ? HeroIntent.Engage(sensor.HeroTarget, mayChase: true)
                        : HoldFormation(sensor);

                case SquadMode.Skirmish:
                    return Skirmish(sensor);

                case SquadMode.Leaderless:
                    return Leaderless(sensor);

                case SquadMode.Retreat:
                    return HeroIntent.MoveTo(sensor.RetreatPoint);

                case SquadMode.Disband:
                    return HeroIntent.Hold;

                default:
                    return HoldFormation(sensor);
            }
        }

        private HeroIntent HoldFormation(ITroopSensor sensor)
        {
            if (sensor.EnemyInAttackRange.IsValid)
                return HeroIntent.Engage(sensor.EnemyInAttackRange, mayChase: false);
            if (sensor.CloseAttacker.IsValid)
                return HeroIntent.Engage(sensor.CloseAttacker, mayChase: true);
            return ToSlot(sensor);
        }

        private HeroIntent Skirmish(ITroopSensor sensor)
        {
            var leash = _settings.LeashRadius;
            var fromHero = (sensor.Position - sensor.HeroPosition).magnitude;

            // Past the leash: run back to formation and only resume fighting once well inside it again.
            if (fromHero > leash)
                _returningToHero = true;
            else if (fromHero <= leash * 0.5f)
                _returningToHero = false;
            if (_returningToHero)
                return HeroIntent.MoveTo(sensor.SlotPosition);

            var target = sensor.EnemyInAggroRange;
            if (target.IsValid && (sensor.GetPosition(target) - sensor.HeroPosition).magnitude <= leash)
                return HeroIntent.Engage(target, mayChase: true);
            // Help with whatever the hero is fighting (e.g. the enemy castle, which aggro queries exclude).
            if (sensor.HeroTarget.IsValid && sensor.IsAlive(sensor.HeroTarget))
                return HeroIntent.Engage(sensor.HeroTarget, mayChase: true);
            if (sensor.EnemyInAttackRange.IsValid)
                return HeroIntent.Engage(sensor.EnemyInAttackRange, mayChase: false);
            return ToSlot(sensor);
        }

        private HeroIntent Leaderless(ITroopSensor sensor)
        {
            if (sensor.EnemyInAggroRange.IsValid)
                return HeroIntent.Engage(sensor.EnemyInAggroRange, mayChase: true);
            var objective = _settings.StageType == StageType.Attack ? sensor.EnemyCastle : sensor.NearestEnemy;
            return objective.IsValid ? HeroIntent.Engage(objective, mayChase: true) : HeroIntent.Hold;
        }

        private HeroIntent ToSlot(ITroopSensor sensor) =>
            (sensor.Position - sensor.SlotPosition).sqrMagnitude > _settings.SlotTolerance * _settings.SlotTolerance
                ? HeroIntent.MoveTo(sensor.SlotPosition)
                : HeroIntent.Hold;
    }
}

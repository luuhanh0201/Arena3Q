using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Hand-driven <see cref="IHeroSensor"/>: tests set what the hero "sees" before each tick.</summary>
    internal sealed class FakeHeroSensor : IHeroSensor
    {
        private readonly HashSet<UnitId> _dead = new HashSet<UnitId>();

        public Vector2 Position { get; set; }
        public UnitId EnemyInAttackRange { get; set; }
        public UnitId EnemyInAggroRange { get; set; }
        public UnitId NearestEnemy { get; set; }
        public UnitId EnemyCastle { get; set; }
        public UnitId CloseAttacker { get; set; }
        public bool TookDamageFromOutOfRange { get; set; }

        public bool IsAlive(UnitId unit) => unit.IsValid && !_dead.Contains(unit);

        /// <summary>Marks a unit dead and removes it from every query, as the real world would.</summary>
        public void Kill(UnitId unit)
        {
            _dead.Add(unit);
            if (EnemyInAttackRange == unit) EnemyInAttackRange = UnitId.None;
            if (EnemyInAggroRange == unit) EnemyInAggroRange = UnitId.None;
            if (NearestEnemy == unit) NearestEnemy = UnitId.None;
            if (EnemyCastle == unit) EnemyCastle = UnitId.None;
            if (CloseAttacker == unit) CloseAttacker = UnitId.None;
        }
    }
}

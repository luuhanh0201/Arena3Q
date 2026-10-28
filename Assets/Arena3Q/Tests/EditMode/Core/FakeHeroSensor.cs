using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Hand-set perception for <see cref="HeroBrain"/> tests.</summary>
    internal sealed class FakeHeroSensor : IHeroSensor
    {
        private readonly HashSet<UnitId> _dead = new HashSet<UnitId>();
        private readonly Dictionary<UnitId, Vector2> _enemyPositions = new Dictionary<UnitId, Vector2>();

        public Vector2 Position { get; set; }
        public UnitId EnemyInAttackRange { get; set; }
        public UnitId EnemyInAggroRange { get; set; }
        public UnitId NearestEnemy { get; set; }
        public UnitId EnemyCastle { get; set; }
        public UnitId CloseAttacker { get; set; }
        public bool TookDamageFromOutOfRange { get; set; }

        public bool IsAlive(UnitId unit) => unit.IsValid && !_dead.Contains(unit);

        /// <summary>Puts an enemy at a position for the area queries (Phòng thủ).</summary>
        public void PlaceEnemy(UnitId unit, Vector2 position) => _enemyPositions[unit] = position;

        public UnitId NearestEnemyIn(Rect area)
        {
            var best = UnitId.None;
            var bestDistance = float.MaxValue;
            foreach (var pair in _enemyPositions)
            {
                if (!IsAlive(pair.Key) || !area.Contains(pair.Value))
                    continue;
                var distance = (pair.Value - Position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = pair.Key;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public bool IsInArea(UnitId unit, Rect area) =>
            IsAlive(unit) && _enemyPositions.TryGetValue(unit, out var position) && area.Contains(position);

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

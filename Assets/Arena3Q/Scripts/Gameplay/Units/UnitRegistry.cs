using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>All living units of one battle, with the spatial queries the brains need.</summary>
    public sealed class UnitRegistry
    {
        private readonly List<UnitBase> _units = new List<UnitBase>();
        private readonly Dictionary<UnitId, UnitBase> _byId = new Dictionary<UnitId, UnitBase>();

        public IReadOnlyList<UnitBase> Units => _units;

        public void Add(UnitBase unit)
        {
            _units.Add(unit);
            _byId[unit.Id] = unit;
        }

        public void Remove(UnitBase unit)
        {
            _units.Remove(unit);
            _byId.Remove(unit.Id);
        }

        public void Clear()
        {
            _units.Clear();
            _byId.Clear();
        }

        /// <summary>The living unit with this id, or null.</summary>
        public UnitBase Get(UnitId id) =>
            id.IsValid && _byId.TryGetValue(id, out var unit) && unit != null && unit.IsAlive ? unit : null;

        public bool IsAlive(UnitId id) => Get(id) != null;

        public Vector2 GetPosition(UnitId id)
        {
            var unit = Get(id);
            return unit != null ? unit.Position : Vector2.zero;
        }

        /// <summary>
        /// Nearest living unit whose edge is within <paramref name="edgeRange"/> of a circle at <paramref name="point"/>
        /// with radius <paramref name="pointRadius"/>. Pass float.PositiveInfinity for "any distance".
        /// </summary>
        public UnitBase FindNearest(Vector2 point, float pointRadius, float edgeRange, Func<UnitBase, bool> filter)
        {
            UnitBase best = null;
            var bestDistance = float.MaxValue;
            foreach (var unit in _units)
            {
                if (unit == null || !unit.IsAlive || !filter(unit))
                    continue;
                var edge = Vector2.Distance(point, unit.Position) - pointRadius - unit.Radius;
                if (edge > edgeRange || edge >= bestDistance)
                    continue;
                best = unit;
                bestDistance = edge;
            }
            return best;
        }

        /// <summary>Nearest living unit of another team within an edge range of <paramref name="self"/>.</summary>
        public UnitId FindNearestHostile(UnitBase self, float edgeRange, bool includeCastles = false)
        {
            var team = self.Team;
            var found = FindNearest(self.Position, self.Radius, edgeRange,
                u => u.Team != team && (includeCastles || !u.IsCastle));
            return found != null ? found.Id : UnitId.None;
        }

        /// <summary>
        /// Nearest hostile unit within range, or a hostile castle within range if no unit is: soldiers and heroes
        /// come first, the castle is hit only when nothing else is in reach.
        /// </summary>
        public UnitId FindNearestHostilePreferUnits(UnitBase self, float edgeRange)
        {
            var unit = FindNearestHostile(self, edgeRange);
            if (unit.IsValid)
                return unit;
            var team = self.Team;
            var castle = FindNearest(self.Position, self.Radius, edgeRange, u => u.Team != team && u.IsCastle);
            return castle != null ? castle.Id : UnitId.None;
        }

        public int Count(Func<UnitBase, bool> filter)
        {
            var count = 0;
            foreach (var unit in _units)
            {
                if (unit != null && unit.IsAlive && filter(unit))
                    count++;
            }
            return count;
        }
    }
}

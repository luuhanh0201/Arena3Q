using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// Battlefield geometry on the logic plane (spec §9): an open field with bounds, optional
    /// forbidden zones and the summon zone in front of the player's castle. No pathfinding.
    /// </summary>
    public sealed class BattlefieldLayout
    {
        private readonly Rect[] _forbiddenZones;

        public BattlefieldLayout(Rect bounds, Rect summonZone, IReadOnlyList<Rect> forbiddenZones = null)
        {
            Bounds = bounds;
            SummonZone = summonZone;
            _forbiddenZones = forbiddenZones == null ? Array.Empty<Rect>() : new List<Rect>(forbiddenZones).ToArray();
        }

        public Rect Bounds { get; }
        public Rect SummonZone { get; }
        public IReadOnlyList<Rect> ForbiddenZones => _forbiddenZones;

        public bool IsInsideBounds(Vector2 point) => Bounds.Contains(point);

        public bool IsInForbiddenZone(Vector2 point)
        {
            foreach (var zone in _forbiddenZones)
            {
                if (zone.Contains(point))
                    return true;
            }
            return false;
        }

        /// <summary>A valid destination for a <see cref="MoveCommand"/>.</summary>
        public bool IsWalkable(Vector2 point) => IsInsideBounds(point) && !IsInForbiddenZone(point);

        /// <summary>A valid drop position for a free summon.</summary>
        public bool IsValidSummonPosition(Vector2 point) => SummonZone.Contains(point) && IsWalkable(point);
    }
}

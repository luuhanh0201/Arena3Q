using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// The timed statuses on one unit and what they add up to. The same kind does not stack: the strongest value and the
    /// longest time win (poison: one per source). Crowd-control immunity blocks stuns and slows.
    /// </summary>
    public sealed class StatusSet
    {
        private struct Entry
        {
            public StatusKind Kind;
            public float Value;
            public float Remaining;
            public object Source;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public static bool IsCrowdControl(StatusKind kind) =>
            kind == StatusKind.Stun || kind == StatusKind.Slow || kind == StatusKind.Taunt;

        public bool IsStunned => Has(StatusKind.Stun);
        public bool IsImmune => Has(StatusKind.CrowdControlImmunity);
        public int Count => _entries.Count;

        /// <summary>Adds or refreshes a status. Returns false when it was blocked (immunity, zero time or value).</summary>
        public bool Apply(StatusKind kind, float value, float duration, object source = null)
        {
            if (duration <= 0f)
                return false;
            if (IsCrowdControl(kind) && IsImmune)
                return false;
            if (kind == StatusKind.CrowdControlImmunity)
                RemoveAll(IsCrowdControl);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Kind != kind || (kind == StatusKind.Poison && !Equals(entry.Source, source)))
                    continue;
                entry.Value = Math.Max(entry.Value, value);
                // A new taunt pulls towards the newest taunter.
                if (kind == StatusKind.Taunt)
                    entry.Source = source;
                entry.Remaining = Math.Max(entry.Remaining, duration);
                _entries[i] = entry;
                return true;
            }
            _entries.Add(new Entry { Kind = kind, Value = value, Remaining = duration, Source = source });
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.Remaining -= deltaTime;
                if (entry.Remaining <= 0f || (entry.Kind == StatusKind.Shield && entry.Value <= 0f))
                    _entries.RemoveAt(i);
                else
                    _entries[i] = entry;
            }
        }

        public bool Has(StatusKind kind)
        {
            foreach (var entry in _entries)
            {
                if (entry.Kind == kind)
                    return true;
            }
            return false;
        }

        public float Value(StatusKind kind)
        {
            var value = 0f;
            foreach (var entry in _entries)
            {
                if (entry.Kind == kind)
                    value = Math.Max(value, entry.Value);
            }
            return value;
        }

        /// <summary>Who applied the status (taunt: the unit to attack), or null.</summary>
        public object SourceOf(StatusKind kind)
        {
            foreach (var entry in _entries)
            {
                if (entry.Kind == kind)
                    return entry.Source;
            }
            return null;
        }

        /// <summary>Removes every status of this kind (a new player order ends a taunt).</summary>
        public void Remove(StatusKind kind) => RemoveAll(k => k == kind);

        public float Remaining(StatusKind kind)
        {
            var remaining = 0f;
            foreach (var entry in _entries)
            {
                if (entry.Kind == kind)
                    remaining = Math.Max(remaining, entry.Remaining);
            }
            return remaining;
        }

        public float MoveSpeedMultiplier => Math.Max(0f, 1f - Value(StatusKind.Slow)) * (1f + Value(StatusKind.MoveSpeedUp));

        /// <summary>Multiplies attacks per second (divide the attack interval by it).</summary>
        public float AttackSpeedMultiplier => Math.Max(0.2f, 1f + Value(StatusKind.AttackSpeedUp) - Value(StatusKind.AttackSpeedDown));

        public float DamageMultiplier => Math.Max(0f, 1f + Value(StatusKind.DamageUp) - Value(StatusKind.Weaken));

        public float DamageTakenReduction => Value(StatusKind.DamageTakenDown);

        public float Shield => Value(StatusKind.Shield);

        /// <summary>Uses the shield on incoming damage; returns the damage left for health.</summary>
        public float AbsorbWithShield(float damage)
        {
            for (var i = 0; i < _entries.Count && damage > 0f; i++)
            {
                var entry = _entries[i];
                if (entry.Kind != StatusKind.Shield || entry.Value <= 0f)
                    continue;
                var absorbed = Math.Min(entry.Value, damage);
                entry.Value -= absorbed;
                damage -= absorbed;
                _entries[i] = entry;
            }
            return damage;
        }

        /// <summary>Poison damage per second by source, for the owner to apply as damage.</summary>
        public void CollectPoison(List<(object source, float damagePerSecond)> into)
        {
            foreach (var entry in _entries)
            {
                if (entry.Kind == StatusKind.Poison)
                    into.Add((entry.Source, entry.Value));
            }
        }

        public void Clear() => _entries.Clear();

        private void RemoveAll(Func<StatusKind, bool> match) => _entries.RemoveAll(e => match(e.Kind));
    }

    /// <summary>
    /// The ultimate's rage bar (spec §3): fills only from damage the hero deals itself, in proportion to that damage;
    /// never drains; empties on use and on death.
    /// </summary>
    public sealed class RageMeter
    {
        public RageMeter(float max = 100f)
        {
            Max = Math.Max(1f, max);
        }

        public float Max { get; }
        public float Value { get; private set; }
        public float Fraction => Value / Max;
        public bool IsFull => Value >= Max - 1e-3f;

        public void Add(float amount)
        {
            if (amount > 0f)
                Value = Math.Min(Max, Value + amount);
        }

        public void Reset() => Value = 0f;

        /// <summary>
        /// Rage for a hit: a full bar after <paramref name="hitsToFill"/> hits of the hero's own Công
        /// (spec 🟡: about 15–20 basic attacks), times the faction bonus.
        /// </summary>
        public static float GainFor(float damage, float heroDamage, float hitsToFill, float max, float bonus = 0f) =>
            heroDamage <= 0f || hitsToFill <= 0f ? 0f : damage / heroDamage * (max / hitsToFill) * (1f + bonus);
    }

    /// <summary>Small pure helpers of the skill system.</summary>
    public static class SkillMath
    {
        /// <summary>
        /// The point that covers the most points within <paramref name="radius"/>, tried at every point (spec: "cụm quân
        /// địch đông nhất"). Returns false when there are no points.
        /// </summary>
        public static bool DensestPoint(IReadOnlyList<Vector2> points, float radius, out Vector2 best, out int count)
        {
            best = Vector2.zero;
            count = 0;
            if (points == null || points.Count == 0)
                return false;
            var radiusSqr = radius * radius;
            for (var i = 0; i < points.Count; i++)
            {
                var around = 0;
                var sum = Vector2.zero;
                for (var j = 0; j < points.Count; j++)
                {
                    if ((points[j] - points[i]).sqrMagnitude <= radiusSqr)
                    {
                        around++;
                        sum += points[j];
                    }
                }
                if (around > count)
                {
                    count = around;
                    best = sum / around;
                }
            }
            return true;
        }

        /// <summary>Is a circle of <paramref name="radius"/> at <paramref name="point"/> touched by a cone from origin?</summary>
        public static bool InCone(Vector2 origin, Vector2 direction, float length, float angleDegrees, Vector2 point, float radius)
        {
            var offset = point - origin;
            var distance = offset.magnitude;
            if (distance > length + radius)
                return false;
            if (distance <= radius)
                return true;
            var angle = Vector2.Angle(direction, offset);
            var allowance = Mathf.Asin(Mathf.Clamp01(radius / distance)) * Mathf.Rad2Deg;
            return angle <= angleDegrees * 0.5f + allowance;
        }

        /// <summary>Distance along a line from origin (or -1 when the circle is outside the line's rectangle).</summary>
        public static float AlongLine(Vector2 origin, Vector2 direction, float length, float width, Vector2 point, float radius)
        {
            var offset = point - origin;
            var along = Vector2.Dot(offset, direction);
            if (along < -radius || along > length + radius)
                return -1f;
            var across = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
            return across <= width * 0.5f + radius ? Mathf.Max(0f, along) : -1f;
        }

        /// <summary>
        /// May an auto-cast skill start now? Skills that move the hero do not auto-cast while it walks to a move order
        /// (spec §3, settled 2026-10-17; <paramref name="castWhileMoving"/> lets a skill opt out).
        /// </summary>
        public static bool CanAutoCast(bool unlocked, float cooldownLeft, bool busy, bool stunned, bool movingOnOrder,
            bool movesCaster, bool castWhileMoving) =>
            unlocked && cooldownLeft <= 0f && !busy && !stunned && !(movingOnOrder && movesCaster && !castWhileMoving);
    }
}

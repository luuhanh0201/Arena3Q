using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Battle-clock swept projectiles. Visual sockets do not become combat coordinates.</summary>
    public sealed class MacAnhProjectiles : MonoBehaviour
    {
        private sealed class Flight
        {
            public Team Team;
            public Vector2 Position, Velocity;
            public Vector3 VisualStart;
            public Vector2 Start;
            public float Radius, Remaining;
            public float SpawnTime;
            public MacAnhFx Fx;
            public int FxGeneration;
            public bool OwnsFx => Fx != null && Fx.IsRunning && Fx.Generation == FxGeneration;
            public GameObject Impact;
            public MacAnhFxQuality Quality;
            public Action<UnitBase> Hit;
        }
        private readonly List<Flight> _flights = new List<Flight>();
        private BattleController _battle;
        public static MacAnhProjectiles For(BattleController battle)
        {
            var system = battle.GetComponent<MacAnhProjectiles>();
            if (system != null) return system;
            system = battle.gameObject.AddComponent<MacAnhProjectiles>(); system._battle = battle;
            battle.BattleStarted += system.Clear; battle.Finished += system.Finish;
            return system;
        }
        public void Launch(HeroUnit caster, Vector2 direction, Vector3 visualStart, MacAnhFx fx, Action<UnitBase> hit, float maxDistance)
        {
            var config = caster.Definition.MacAnhPerformance;
            _flights.Add(new Flight { Position = caster.Position, Start = caster.Position,
                Team = caster.Team,
                SpawnTime = _battle.BattleTime,
                Velocity = direction.normalized * config.ProjectileSpeed, VisualStart = visualStart,
                Radius = config.ProjectileRadius, Remaining = Mathf.Min(config.ProjectileLifetime, maxDistance / config.ProjectileSpeed), Fx = fx, Hit = hit,
                FxGeneration = fx != null ? fx.Generation : 0,
                Impact = config.Effect("Impact"), Quality = (MacAnhFxQuality)config.EffectQuality });
        }
        public void Simulate(float delta)
        {
            for (var i = _flights.Count - 1; i >= 0; i--)
            {
                var flight = _flights[i]; var step = Mathf.Min(delta, flight.Remaining);
                if (Mathf.Approximately(flight.SpawnTime, _battle.BattleTime)) continue;
                var end = flight.Position + flight.Velocity * step;
                UnitBase first = null; var best = float.PositiveInfinity;
                foreach (var unit in _battle.Registry.Units)
                {
                    if (unit == null || !unit.IsAlive || unit.Team == flight.Team) continue;
                    var t = ProjectileCollision.Entry(flight.Position, end, unit.Position, unit.Radius + flight.Radius);
                    if (t < 0 || t >= best) continue;
                    best = t; first = unit;
                }
                flight.Position = end; flight.Remaining -= step;
                if (flight.OwnsFx)
                {
                    var viewDelta = _battle.ToView(end) - _battle.ToView(flight.Start);
                    flight.Fx.transform.position = flight.VisualStart + viewDelta;
                }
                if (first == null && flight.Remaining > 0) continue;
                if (first != null)
                {
                    flight.Hit(first);
                    if (flight.Impact != null && flight.OwnsFx)
                    {
                        // Mesh (3D / native) and sprite (flipbook) effects are both Renderers.
                        var renderer = flight.Fx.GetComponentInChildren<Renderer>(true);
                        MacAnhFxPool.For(_battle).Spawn(flight.Impact, flight.Fx.transform.position, Vector3.one * .5f,
                            Quaternion.identity, flight.Quality, .3f, renderer != null ? renderer.sortingOrder : 0);
                    }
                }
                // The blade disperses where it is (hit or end of flight) while the impact plays.
                if (flight.OwnsFx) flight.Fx.FadeOut(first != null ? .16f : .25f); _flights.RemoveAt(i);
            }
        }
        private void Finish(Arena3Q.Core.OutcomeDecision _) => Clear();
        private void Clear() { foreach (var flight in _flights) if (flight.OwnsFx) flight.Fx.Stop(); _flights.Clear(); }
        private void OnDestroy()
        {
            if (_battle != null) { _battle.BattleStarted -= Clear; _battle.Finished -= Finish; }
        }
    }
}

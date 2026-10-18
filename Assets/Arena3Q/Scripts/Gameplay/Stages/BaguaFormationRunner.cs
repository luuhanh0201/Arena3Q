using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Runs the stage's Bát Quái Trận in battle (docs/BanDo/BatQuaiTran.md): ticks the Core rules, rebuilds the routes
    /// and pushes caught units out when a slab switches, and applies the gate effects to units entering a trigger.
    /// </summary>
    internal sealed class BaguaFormationRunner
    {
        private readonly BattleController _battle;
        private readonly BaguaGateVisits _visits = new BaguaGateVisits();
        private readonly List<UnitBase> _scratch = new List<UnitBase>();
        private BaguaFormationView _view;

        public BaguaFormationRunner(BattleController battle, BaguaFormationConfig config, BaguaFormation formation)
        {
            _battle = battle;
            Config = config;
            Formation = formation;
        }

        public BaguaFormationConfig Config { get; }
        public BaguaFormation Formation { get; }

        public void BuildView(Transform worldRoot, Transform groundRoot)
        {
            var go = new GameObject("Bát Quái Trận");
            go.transform.SetParent(worldRoot, false);
            _view = go.AddComponent<BaguaFormationView>();
            _view.Init(_battle, Config, Formation, groundRoot);
        }

        public BaguaFormationEvents Tick(float deltaTime)
        {
            bool player = false, enemy = false;
            foreach (var unit in _battle.Registry.Units)
            {
                if (!Counts(unit) || !Formation.IsOnAltar(unit.Position))
                    continue;
                if (unit.Team == Team.Player)
                    player = true;
                else
                    enemy = true;
            }

            var events = Formation.Tick(deltaTime, player, enemy);
            Handle(events);
            ApplyGateEffects();
            _view?.Refresh();
            return events;
        }

        /// <summary>Follows up rule events (also those from the dev tools).</summary>
        public void Handle(BaguaFormationEvents events)
        {
            if ((events & BaguaFormationEvents.ObstaclesChanged) == 0)
                return;
            _battle.Layout.RebuildRoutes();
            // A slab rising in a gap pushes whoever stands there to the nearer side (nobody stays stuck).
            _scratch.Clear();
            _scratch.AddRange(_battle.Registry.Units);
            foreach (var unit in _scratch)
            {
                if (!Counts(unit))
                    continue;
                var layout = _battle.LayoutFor(unit);
                if (layout != null && layout.IsBlocked(unit.Position, unit.Radius))
                    unit.SetPosition(unit.Position);
            }
            _view?.Refresh();
        }

        public void Forget(UnitBase unit) => _visits.Forget(unit.Id);

        private void ApplyGateEffects()
        {
            _scratch.Clear();
            _scratch.AddRange(_battle.Registry.Units);
            foreach (var unit in _scratch)
            {
                if (!Counts(unit))
                    continue;
                var entered = _visits.Enter(unit.Id, Formation.GateTriggerAt(unit.Position));
                if (entered < 0 || Formation.IsGateClosed(entered))
                    continue;
                var kind = Formation.KindOf(entered);
                if (!Formation.TryGetEffect(kind, unit.Team, out var effect)
                    || !_visits.TryUse(unit.Id, kind, _battle.BattleTime, effect.CooldownSeconds))
                    continue;
                Apply(unit, effect);
            }
        }

        private void Apply(UnitBase unit, BaguaGateEffect effect)
        {
            if (effect.HealFraction > 0f)
                unit.Heal(unit.MaxHealth * effect.HealFraction);
            if (effect.MoveSpeedUp > 0f)
                unit.ApplyStatus(StatusKind.MoveSpeedUp, effect.MoveSpeedUp, effect.MoveSpeedUpSeconds, Formation);
            if (effect.Slow > 0f)
                unit.ApplyStatus(StatusKind.Slow, effect.Slow, effect.SlowSeconds, Formation);
            if (effect.DamageFraction > 0f)
                unit.TakeDamage(unit.MaxHealth * effect.DamageFraction, null, ignoreReduction: true);
        }

        private static bool Counts(UnitBase unit) => unit != null && unit.IsAlive && !unit.IsCastle;
    }
}

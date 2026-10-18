using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>The Bát Môn gate types of the Bát Quái Trận map (docs/BanDo/BatQuaiTran.md §2).</summary>
    public enum BaguaGateKind
    {
        /// <summary>Sinh: heals.</summary>
        Life,
        /// <summary>Khai: run faster.</summary>
        Haste,
        /// <summary>Tử: loses health and is slowed.</summary>
        Death,
        /// <summary>Đỗ: the gate is closed by a stone slab.</summary>
        Closed
    }

    /// <summary>What passing one gate type does to a unit (fractions of max health / of move speed).</summary>
    [Serializable]
    public struct BaguaGateEffect
    {
        public float HealFraction;
        public float MoveSpeedUp;
        public float MoveSpeedUpSeconds;
        /// <summary>Health lost as a share of max health (true damage: ignores damage reduction, shields still absorb).</summary>
        public float DamageFraction;
        public float Slow;
        public float SlowSeconds;
        /// <summary>The same unit is affected by this gate type again only after this many seconds.</summary>
        public float CooldownSeconds;

        public BaguaGateEffect Scaled(float factor) => new BaguaGateEffect
        {
            HealFraction = HealFraction * factor,
            MoveSpeedUp = MoveSpeedUp * factor,
            MoveSpeedUpSeconds = MoveSpeedUpSeconds,
            DamageFraction = DamageFraction,
            Slow = Slow,
            SlowSeconds = SlowSeconds,
            CooldownSeconds = CooldownSeconds
        };
    }

    /// <summary>Rules of the formation (every number is a 🟡/✅ config value from the map's data asset).</summary>
    public sealed class BaguaFormationSettings
    {
        public float RotationPeriodSeconds = 20f;
        public float WarningSeconds = 3f;
        /// <summary>Clockwise (✅ default): gate k takes the type of gate k+1 (gates are numbered anticlockwise).</summary>
        public bool Clockwise = true;
        public float SlabRiseSeconds = 0.6f;
        public float SlabLowerSeconds = 0.6f;
        public float CaptureSeconds = 5f;
        /// <summary>Sinh / Khai strength for the side that held the altar at the last rotation.</summary>
        public float BlessingMultiplier = 1.5f;

        public BaguaGateKind[] InitialKinds =
        {
            BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed,
            BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed
        };

        public BaguaGateEffect Life = new BaguaGateEffect { HealFraction = 0.08f, CooldownSeconds = 5f };
        public BaguaGateEffect Haste = new BaguaGateEffect { MoveSpeedUp = 0.3f, MoveSpeedUpSeconds = 4f, CooldownSeconds = 5f };
        public BaguaGateEffect Death = new BaguaGateEffect
            { DamageFraction = 0.1f, Slow = 0.3f, SlowSeconds = 2f, CooldownSeconds = 5f };

        public BaguaGateEffect EffectOf(BaguaGateKind kind) => kind switch
        {
            BaguaGateKind.Life => Life,
            BaguaGateKind.Haste => Haste,
            BaguaGateKind.Death => Death,
            _ => default
        };
    }

    /// <summary>One gate of the ring: its centre and the direction across the gap.</summary>
    public readonly struct BaguaGateSpot
    {
        public readonly Vector2 Position;
        /// <summary>Unit vector along the gap (from one pillar towards the other).</summary>
        public readonly Vector2 Across;

        public BaguaGateSpot(Vector2 position, Vector2 across)
        {
            Position = position;
            Across = across.sqrMagnitude > 1e-8f ? across.normalized : Vector2.right;
        }

        /// <summary>From a layout marker rotation (Unity rotY, degrees): the local X axis on the logic plane.</summary>
        public static Vector2 AcrossFromRotY(float rotYDegrees)
        {
            var r = rotYDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), -Mathf.Sin(r));
        }
    }

    /// <summary>Where the pieces of the formation are (from BatTran_Layout.json via the map's data asset).</summary>
    public sealed class BaguaFormationGeometry
    {
        public Vector2 AltarCenter = Vector2.zero;
        public float AltarRadius = 2f;
        public List<(Vector2 position, float radius)> Pillars = new List<(Vector2, float)>();
        /// <summary>Gate k sits at angle k·45° (0 = east, towards the enemy side); opposite gates are k and k + count/2.</summary>
        public List<BaguaGateSpot> Gates = new List<BaguaGateSpot>();
        public float TriggerWidth = 2.4f;
        public float TriggerDepth = 1f;
        public float SlabLength = 2.5f;
        public float SlabThickness = 0.4f;
    }

    [Flags]
    public enum BaguaFormationEvents
    {
        None = 0,
        WarningStarted = 1,
        Rotated = 2,
        /// <summary>A slab obstacle turned on or off: rebuild the routes and push out units caught in it.</summary>
        ObstaclesChanged = 4,
        AltarOwnerChanged = 8
    }

    /// <summary>
    /// The Bát Quái Trận rules (docs/BanDo/BatQuaiTran.md): 8 pillars that always block, 8 gates whose type rotates
    /// every period, slabs that close Đỗ gates (the obstacle turns on at the start of the rise and off at the end of
    /// the lowering) and the centre altar that a side holds after standing on it alone for a while.
    /// </summary>
    public sealed class BaguaFormation
    {
        private readonly BaguaGateKind[] _kinds;
        private readonly bool?[] _forcedClosed;
        private readonly float[] _slabRaise;
        private readonly WallObstacle[] _slabs;
        private readonly List<IObstacle> _obstacles = new List<IObstacle>();
        private float _untilRotation;
        private bool _warned;

        public BaguaFormation(BaguaFormationGeometry geometry, BaguaFormationSettings settings)
        {
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var count = geometry.Gates.Count;
            if (count == 0 || count % 2 != 0)
                throw new ArgumentException("The formation needs an even number of gates (opposite pairs).", nameof(geometry));
            if (settings.InitialKinds == null || settings.InitialKinds.Length != count)
                throw new ArgumentException($"InitialKinds needs one type per gate ({count}).", nameof(settings));

            foreach (var (position, radius) in geometry.Pillars)
                _obstacles.Add(new CircleObstacle(position, radius, "Cột Bát Quái"));

            _kinds = (BaguaGateKind[])settings.InitialKinds.Clone();
            _forcedClosed = new bool?[count];
            _slabRaise = new float[count];
            _slabs = new WallObstacle[count];
            for (var k = 0; k < count; k++)
            {
                var gate = geometry.Gates[k];
                var half = gate.Across * (geometry.SlabLength * 0.5f);
                var closed = IsGateClosed(k);
                _slabs[k] = new WallObstacle(gate.Position - half, gate.Position + half, geometry.SlabThickness,
                    $"Cửa {k}", closed);
                _slabRaise[k] = closed ? 1f : 0f;
                _obstacles.Add(_slabs[k]);
            }
            _untilRotation = settings.RotationPeriodSeconds;
        }

        public BaguaFormationGeometry Geometry { get; }
        public BaguaFormationSettings Settings { get; }
        public int GateCount => _kinds.Length;

        /// <summary>Pillars and slabs (add them to the battle's <see cref="BattlefieldLayout"/>).</summary>
        public IReadOnlyList<IObstacle> Obstacles => _obstacles;

        public BaguaGateKind KindOf(int gate) => _kinds[gate];
        public int OppositeOf(int gate) => (gate + GateCount / 2) % GateCount;

        /// <summary>Closed by its type (Đỗ) or forced closed / open by the dev tools.</summary>
        public bool IsGateClosed(int gate) => _forcedClosed[gate] ?? _kinds[gate] == BaguaGateKind.Closed;

        public bool? ForcedClosed(int gate) => _forcedClosed[gate];

        /// <summary>0 = slab under the ground, 1 = fully up.</summary>
        public float SlabRaise(int gate) => _slabRaise[gate];

        public bool IsSlabBlocking(int gate) => _slabs[gate].IsActive;
        public WallObstacle Slab(int gate) => _slabs[gate];

        public float SecondsToRotation => _untilRotation;
        public bool IsWarning => _untilRotation <= Settings.WarningSeconds;
        public int RotationCount { get; private set; }

        /// <summary>The side holding the altar ("giữ trận nhãn"), or null.</summary>
        public Team? AltarOwner { get; private set; }

        /// <summary>The side currently counting towards taking the altar, or null.</summary>
        public Team? CapturingTeam { get; private set; }

        /// <summary>0..1 of the capture time for <see cref="CapturingTeam"/>.</summary>
        public float CaptureProgress { get; private set; }

        /// <summary>Both sides stand on the altar: the capture count is paused.</summary>
        public bool IsAltarContested { get; private set; }

        /// <summary>The side that held the altar at the last rotation: blessed until the next one.</summary>
        public Team? BlessedTeam { get; private set; }

        /// <summary>Advances timers; pass whether living units of each side stand on the altar.</summary>
        public BaguaFormationEvents Tick(float deltaTime, bool playerOnAltar, bool enemyOnAltar)
        {
            var events = BaguaFormationEvents.None;
            if (deltaTime <= 0f)
                return events;

            events |= TickAltar(deltaTime, playerOnAltar, enemyOnAltar);

            _untilRotation -= deltaTime;
            if (!_warned && IsWarning)
            {
                _warned = true;
                events |= BaguaFormationEvents.WarningStarted;
            }
            if (_untilRotation <= 0f)
            {
                Rotate();
                _untilRotation += Settings.RotationPeriodSeconds;
                if (_untilRotation <= 0f)
                    _untilRotation = Settings.RotationPeriodSeconds;
                events |= BaguaFormationEvents.Rotated;
            }

            events |= TickSlabs(deltaTime);
            return events;
        }

        /// <summary>Dev tool / tests: rotate now and restart the period. Slabs then move on the following ticks.</summary>
        public BaguaFormationEvents RotateNow()
        {
            Rotate();
            _untilRotation = Settings.RotationPeriodSeconds;
            return BaguaFormationEvents.Rotated | TickSlabs(0f);
        }

        /// <summary>Dev tool: force a gate closed (true), open (false) or back to its type (null).</summary>
        public BaguaFormationEvents SetForcedClosed(int gate, bool? closed)
        {
            _forcedClosed[gate] = closed;
            return TickSlabs(0f);
        }

        /// <summary>Dev tool: set who holds the altar.</summary>
        public void SetAltarOwner(Team? team)
        {
            AltarOwner = team;
            CapturingTeam = null;
            CaptureProgress = 0f;
        }

        /// <summary>
        /// The effect of passing a gate of this type for a unit of this side, with the altar blessing; false when the
        /// gate does nothing to it (Đỗ, or Tử for the blessed side).
        /// </summary>
        public bool TryGetEffect(BaguaGateKind kind, Team team, out BaguaGateEffect effect)
        {
            effect = default;
            if (kind == BaguaGateKind.Closed)
                return false;
            var blessed = BlessedTeam == team;
            if (kind == BaguaGateKind.Death && blessed)
                return false;
            effect = Settings.EffectOf(kind);
            if (blessed)
                effect = effect.Scaled(Settings.BlessingMultiplier);
            return true;
        }

        /// <summary>The gate whose trigger rectangle (width across the gap × depth through it) holds the point, or -1.</summary>
        public int GateTriggerAt(Vector2 point)
        {
            for (var k = 0; k < GateCount; k++)
            {
                if (IsInTrigger(k, point))
                    return k;
            }
            return -1;
        }

        public bool IsInTrigger(int gate, Vector2 point)
        {
            var spot = Geometry.Gates[gate];
            var offset = point - spot.Position;
            var across = Vector2.Dot(offset, spot.Across);
            var through = offset.x * spot.Across.y - offset.y * spot.Across.x;
            return Mathf.Abs(across) <= Geometry.TriggerWidth * 0.5f && Mathf.Abs(through) <= Geometry.TriggerDepth * 0.5f;
        }

        public bool IsOnAltar(Vector2 point) =>
            (point - Geometry.AltarCenter).sqrMagnitude <= Geometry.AltarRadius * Geometry.AltarRadius;

        private void Rotate()
        {
            var count = _kinds.Length;
            var previous = (BaguaGateKind[])_kinds.Clone();
            for (var k = 0; k < count; k++)
                _kinds[k] = previous[Settings.Clockwise ? (k + 1) % count : (k + count - 1) % count];
            BlessedTeam = AltarOwner;
            RotationCount++;
            _warned = false;
        }

        private BaguaFormationEvents TickAltar(float deltaTime, bool player, bool enemy)
        {
            IsAltarContested = player && enemy;
            if (IsAltarContested)
                return BaguaFormationEvents.None; // paused, progress kept
            if (!player && !enemy)
            {
                CapturingTeam = null;
                CaptureProgress = 0f;
                return BaguaFormationEvents.None;
            }

            var team = player ? Team.Player : Team.Enemy;
            if (AltarOwner == team)
            {
                CapturingTeam = null;
                CaptureProgress = 0f;
                return BaguaFormationEvents.None;
            }
            if (CapturingTeam != team)
            {
                CapturingTeam = team;
                CaptureProgress = 0f;
            }
            CaptureProgress += Settings.CaptureSeconds > 0f ? deltaTime / Settings.CaptureSeconds : 1f;
            if (CaptureProgress < 1f)
                return BaguaFormationEvents.None;
            AltarOwner = team;
            CapturingTeam = null;
            CaptureProgress = 0f;
            return BaguaFormationEvents.AltarOwnerChanged;
        }

        private BaguaFormationEvents TickSlabs(float deltaTime)
        {
            var changed = false;
            for (var k = 0; k < _slabs.Length; k++)
            {
                var closing = IsGateClosed(k);
                if (closing)
                    _slabRaise[k] = Settings.SlabRiseSeconds > 0f
                        ? Mathf.Min(1f, _slabRaise[k] + deltaTime / Settings.SlabRiseSeconds)
                        : 1f;
                else
                    _slabRaise[k] = Settings.SlabLowerSeconds > 0f
                        ? Mathf.Max(0f, _slabRaise[k] - deltaTime / Settings.SlabLowerSeconds)
                        : 0f;
                // Blocks from the first moment of the rise until the slab is fully down again.
                var blocking = closing || _slabRaise[k] > 0f;
                if (_slabs[k].IsActive == blocking)
                    continue;
                _slabs[k].IsActive = blocking;
                changed = true;
            }
            return changed ? BaguaFormationEvents.ObstaclesChanged : BaguaFormationEvents.None;
        }
    }

    /// <summary>
    /// Which gate each unit stands in, so an effect fires once per entry, and the per-unit cooldown of each gate type.
    /// </summary>
    public sealed class BaguaGateVisits
    {
        private readonly Dictionary<UnitId, int> _inside = new Dictionary<UnitId, int>();
        private readonly Dictionary<(UnitId, BaguaGateKind), float> _readyAt = new Dictionary<(UnitId, BaguaGateKind), float>();

        /// <summary>Records the gate the unit is in now (-1 = none); returns the gate it just entered, or -1.</summary>
        public int Enter(UnitId unit, int gateNow)
        {
            _inside.TryGetValue(unit, out var before);
            if (!_inside.ContainsKey(unit))
                before = -1;
            if (gateNow < 0)
                _inside.Remove(unit);
            else
                _inside[unit] = gateNow;
            return gateNow >= 0 && gateNow != before ? gateNow : -1;
        }

        /// <summary>True (and starts the cooldown) if the unit may be affected by this gate type at this time.</summary>
        public bool TryUse(UnitId unit, BaguaGateKind kind, float now, float cooldownSeconds)
        {
            if (_readyAt.TryGetValue((unit, kind), out var readyAt) && now < readyAt)
                return false;
            _readyAt[(unit, kind)] = now + Mathf.Max(0f, cooldownSeconds);
            return true;
        }

        public void Forget(UnitId unit)
        {
            _inside.Remove(unit);
            foreach (BaguaGateKind kind in Enum.GetValues(typeof(BaguaGateKind)))
                _readyAt.Remove((unit, kind));
        }
    }
}

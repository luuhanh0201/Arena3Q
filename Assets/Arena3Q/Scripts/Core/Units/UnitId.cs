using System;

namespace Arena3Q.Core
{
    /// <summary>Battle-scoped identifier for a hero, troop or castle. Allocated by <see cref="BattleRuntimeState"/>.</summary>
    public readonly struct UnitId : IEquatable<UnitId>
    {
        public static readonly UnitId None = default;

        public readonly int Value;

        public UnitId(int value)
        {
            Value = value;
        }

        public bool IsValid => Value > 0;

        public bool Equals(UnitId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UnitId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(UnitId a, UnitId b) => a.Value == b.Value;
        public static bool operator !=(UnitId a, UnitId b) => a.Value != b.Value;
        public override string ToString() => IsValid ? $"Unit#{Value}" : "Unit#None";
    }
}

using UnityEngine;

namespace Arena3Q.Core
{
    /// <summary>
    /// What a hero perceives this tick. Implemented by the gameplay layer over the live units; tests use a fake.
    /// "Enemy" queries return living enemy units (not castles) or <see cref="UnitId.None"/>, except
    /// <see cref="EnemyInAttackRange"/>.
    /// </summary>
    public interface IHeroSensor
    {
        Vector2 Position { get; }

        /// <summary>
        /// Nearest enemy unit within attack range; if there is none, an enemy castle within attack range (units come
        /// first, the castle only when nothing else is in reach).
        /// </summary>
        UnitId EnemyInAttackRange { get; }

        /// <summary>Nearest enemy within the hero's aggro range.</summary>
        UnitId EnemyInAggroRange { get; }

        /// <summary>Nearest enemy at any distance (Defense-stage advance direction).</summary>
        UnitId NearestEnemy { get; }

        /// <summary>The enemy castle, or <see cref="UnitId.None"/> if there is none or it is destroyed.</summary>
        UnitId EnemyCastle { get; }

        /// <summary>
        /// Nearest living enemy that has damaged this hero and is within its reaction range (spec §6 rules 2-3).
        /// An attacker stops counting once it dies or leaves the reaction range.
        /// </summary>
        UnitId CloseAttacker { get; }

        /// <summary>True if the hero took damage since the previous tick from an attacker outside its attack range.</summary>
        bool TookDamageFromOutOfRange { get; }

        bool IsAlive(UnitId unit);

        /// <summary>Nearest living enemy unit (not a castle) standing inside <paramref name="area"/> (Phòng thủ).</summary>
        UnitId NearestEnemyIn(Rect area);

        /// <summary>Whether a living unit stands inside <paramref name="area"/>.</summary>
        bool IsInArea(UnitId unit, Rect area);
    }
}

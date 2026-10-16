using UnityEngine;

namespace Arena3Q.Core
{
    public enum HeroCommandKind
    {
        None,
        Move,
        Attack
    }

    /// <summary>Move the hero (and its squad) to a ground position (spec §5).</summary>
    public readonly struct MoveCommand
    {
        public readonly UnitId Hero;
        public readonly Vector2 Destination;

        public MoveCommand(UnitId hero, Vector2 destination)
        {
            Hero = hero;
            Destination = destination;
        }
    }

    /// <summary>Send the hero (and its squad) to attack a specific enemy unit or castle (spec §5).</summary>
    public readonly struct AttackCommand
    {
        public readonly UnitId Hero;
        public readonly UnitId Target;

        public AttackCommand(UnitId hero, UnitId target)
        {
            Hero = hero;
            Target = target;
        }
    }
}

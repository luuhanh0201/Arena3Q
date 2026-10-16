using UnityEngine;

namespace Arena3Q.Core
{
    public enum HeroIntentKind
    {
        /// <summary>Stand still.</summary>
        Hold,
        /// <summary>Walk straight to <see cref="HeroIntent.Destination"/>, ignoring enemies.</summary>
        MoveTo,
        /// <summary>Attack <see cref="HeroIntent.Target"/>; walk towards it first only if <see cref="HeroIntent.MayChase"/>.</summary>
        Engage
    }

    /// <summary>What the hero wants to do this tick. The gameplay layer turns it into movement and attacks.</summary>
    public readonly struct HeroIntent
    {
        public static readonly HeroIntent Hold = default;

        public readonly HeroIntentKind Kind;
        public readonly Vector2 Destination;
        public readonly UnitId Target;
        public readonly bool MayChase;

        private HeroIntent(HeroIntentKind kind, Vector2 destination, UnitId target, bool mayChase)
        {
            Kind = kind;
            Destination = destination;
            Target = target;
            MayChase = mayChase;
        }

        public static HeroIntent MoveTo(Vector2 destination) => new HeroIntent(HeroIntentKind.MoveTo, destination, UnitId.None, false);
        public static HeroIntent Engage(UnitId target, bool mayChase) => new HeroIntent(HeroIntentKind.Engage, default, target, mayChase);

        public override string ToString() => Kind switch
        {
            HeroIntentKind.MoveTo => $"MoveTo{Destination}",
            HeroIntentKind.Engage => $"Engage({Target}, chase={MayChase})",
            _ => "Hold"
        };
    }
}

using UnityEngine;

namespace Arena3Q.Core
{
    public enum SummonMode
    {
        /// <summary>Tap an unused card: spawn at one of the stage's quick summon points.</summary>
        Quick,
        /// <summary>Drag a card onto the battlefield: spawn at the drop position inside the summon zone.</summary>
        Free
    }

    /// <summary>Request to summon the hero on a card (spec §4). <see cref="Position"/> is used only for <see cref="SummonMode.Free"/>.</summary>
    public readonly struct SummonRequest
    {
        public readonly int CardIndex;
        public readonly SummonMode Mode;
        public readonly Vector2 Position;

        private SummonRequest(int cardIndex, SummonMode mode, Vector2 position)
        {
            CardIndex = cardIndex;
            Mode = mode;
            Position = position;
        }

        public static SummonRequest Quick(int cardIndex) => new SummonRequest(cardIndex, SummonMode.Quick, default);
        public static SummonRequest Free(int cardIndex, Vector2 position) => new SummonRequest(cardIndex, SummonMode.Free, position);
    }
}

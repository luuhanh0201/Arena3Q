using System;

namespace Arena3Q.Core
{
    /// <summary>
    /// Stacks of a passive like Lữ Bố's Cuồng Huyết (<see cref="PassiveKind.FrenzyStacks"/>): each attack instance that
    /// hits adds one (an instance counts once however many enemies it hits), skills may add more, at most
    /// <see cref="Max"/>. They last <see cref="Duration"/> seconds, refreshed by any damage dealt, then all fall off.
    /// </summary>
    public sealed class FrenzyMeter
    {
        public FrenzyMeter(int max, float duration)
        {
            Max = Math.Max(1, max);
            Duration = Math.Max(0.01f, duration);
        }

        public int Max { get; }
        public float Duration { get; }
        public int Stacks { get; private set; }
        public float Remaining { get; private set; }
        public bool IsFull => Stacks >= Max;

        public void Gain(int amount)
        {
            if (amount <= 0)
                return;
            Stacks = Math.Min(Max, Stacks + amount);
            Remaining = Duration;
        }

        /// <summary>Any damage dealt keeps the stacks alive (also at full stacks).</summary>
        public void Refresh()
        {
            if (Stacks > 0)
                Remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (Stacks == 0)
                return;
            Remaining -= deltaTime;
            if (Remaining <= 0f)
                Reset();
        }

        public void Reset()
        {
            Stacks = 0;
            Remaining = 0f;
        }

        /// <summary>Damage multiplier for <paramref name="perStack"/> bonus damage per stack.</summary>
        public float DamageMultiplier(float perStack) => 1f + perStack * Stacks;
    }
}

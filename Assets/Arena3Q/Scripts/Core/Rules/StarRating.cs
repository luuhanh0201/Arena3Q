namespace Arena3Q.Core
{
    /// <summary>Up to 3 stars by heroes lost; defeat is always 0 stars (spec §8).</summary>
    public static class StarRating
    {
        public const int MaxStars = 3;

        public static int Calculate(bool victory, int heroesLost, StarThresholds thresholds)
        {
            if (!victory)
                return 0;
            thresholds ??= new StarThresholds();
            if (heroesLost <= thresholds.maxHeroesLostForThreeStars)
                return 3;
            if (heroesLost <= thresholds.maxHeroesLostForTwoStars)
                return 2;
            return 1;
        }
    }
}

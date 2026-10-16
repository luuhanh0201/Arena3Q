using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public class StarRatingTests
    {
        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(5, 1)]
        public void Victory_DefaultThresholds(int heroesLost, int expectedStars)
        {
            Assert.That(StarRating.Calculate(true, heroesLost, new StarThresholds()), Is.EqualTo(expectedStars));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void Defeat_IsAlwaysZeroStars(int heroesLost)
        {
            Assert.That(StarRating.Calculate(false, heroesLost, new StarThresholds()), Is.Zero);
        }

        [Test]
        public void CustomThresholds_AreRespected()
        {
            var thresholds = new StarThresholds { maxHeroesLostForThreeStars = 1, maxHeroesLostForTwoStars = 3 };

            Assert.That(StarRating.Calculate(true, 1, thresholds), Is.EqualTo(3));
            Assert.That(StarRating.Calculate(true, 3, thresholds), Is.EqualTo(2));
            Assert.That(StarRating.Calculate(true, 4, thresholds), Is.EqualTo(1));
        }
    }
}

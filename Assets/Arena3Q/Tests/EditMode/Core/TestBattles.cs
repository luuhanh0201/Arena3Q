using System.Linq;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    /// <summary>Builders for Core objects shared by the EditMode tests.</summary>
    internal static class TestBattles
    {
        public static readonly Rect Bounds = new Rect(-20f, -12f, 40f, 24f);
        public static readonly Rect SummonZone = new Rect(-14f, -8f, 8f, 16f);
        public static readonly Rect ForbiddenZone = new Rect(-2f, -2f, 4f, 4f);
        public static readonly Vector2 PlayerCastle = new Vector2(-16f, 0f);

        public static BattleSetup Setup(int cardCount = 5, StageType stageType = StageType.Attack, bool allowResummon = false,
            float timeLimit = 180f)
        {
            return new BattleSetup
            {
                StageType = stageType,
                TimeLimit = timeLimit,
                CardHeroIds = Enumerable.Range(0, cardCount).Select(i => $"hero{i}").ToArray(),
                AllowResummonDeadHero = allowResummon,
                PlayerCastleMaxHealth = 1000f,
                EnemyCastleMaxHealth = 1000f
            };
        }

        public static BattleRuntimeState State(int cardCount = 5, StageType stageType = StageType.Attack,
            bool allowResummon = false, float timeLimit = 180f) =>
            new BattleRuntimeState(Setup(cardCount, stageType, allowResummon, timeLimit));

        public static BattlefieldLayout Layout() => new BattlefieldLayout(Bounds, SummonZone, new[] { ForbiddenZone });

        public static SummonRules CreateSummonRules(params Vector2[] quickPoints) =>
            new SummonRules(Layout(), quickPoints.Length > 0 ? quickPoints : new[] { new Vector2(-12f, 0f) }, PlayerCastle);

        public static HeroRuntime Summon(BattleRuntimeState state, int cardIndex) => state.RegisterSummon(cardIndex, 500f);
    }
}

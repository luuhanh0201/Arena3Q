using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arena3Q.Core
{
    public readonly struct SummonDecision
    {
        public readonly CommandResult Result;
        public readonly Vector2 Position;

        private SummonDecision(CommandResult result, Vector2 position)
        {
            Result = result;
            Position = position;
        }

        public bool Accepted => Result.Accepted;

        public static SummonDecision Accept(Vector2 position) => new SummonDecision(CommandResult.Ok, position);
        public static SummonDecision Reject(RejectReason reason) => new SummonDecision(CommandResult.Reject(reason), default);
    }

    /// <summary>Validates <see cref="SummonRequest"/>s and resolves the spawn position (spec §4).</summary>
    public sealed class SummonRules
    {
        /// <summary>Settled rule: at most 5 summoned heroes per battle, dead heroes do not give the slot back.</summary>
        public const int MaxHeroesPerBattle = 5;

        private readonly BattlefieldLayout _layout;
        private readonly Vector2[] _quickPointsByCastleDistance;

        public SummonRules(BattlefieldLayout layout, IReadOnlyList<Vector2> quickSummonPoints, Vector2 playerCastlePosition)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            // OrderBy is stable, so equally distant points keep their authored order.
            _quickPointsByCastleDistance = (quickSummonPoints ?? Array.Empty<Vector2>())
                .OrderBy(p => (p - playerCastlePosition).sqrMagnitude)
                .ToArray();
        }

        public static bool IsCardSummonable(HeroCard card, bool allowResummonDeadHero) =>
            card.State == CardState.Available || (card.State == CardState.Dead && allowResummonDeadHero);

        /// <summary>True while at least one card could still be summoned. Used by the Attack-stage defeat rule.</summary>
        public static bool CanSummonAnyCard(BattleRuntimeState state)
        {
            if (state.SummonedCount >= MaxHeroesPerBattle)
                return false;
            foreach (var card in state.Cards)
            {
                if (IsCardSummonable(card, state.Setup.AllowResummonDeadHero))
                    return true;
            }
            return false;
        }

        /// <param name="isPointOccupied">Whether a quick summon point is blocked by a living unit. Null = all points free.</param>
        public SummonDecision Evaluate(in SummonRequest request, BattleRuntimeState state, Func<Vector2, bool> isPointOccupied = null)
        {
            if (state.IsOver)
                return SummonDecision.Reject(RejectReason.BattleOver);
            if (request.CardIndex < 0 || request.CardIndex >= state.Cards.Count)
                return SummonDecision.Reject(RejectReason.UnknownCard);

            var card = state.Cards[request.CardIndex];
            if (card.State == CardState.Deployed)
                return SummonDecision.Reject(RejectReason.CardAlreadyDeployed);
            if (!IsCardSummonable(card, state.Setup.AllowResummonDeadHero))
                return SummonDecision.Reject(RejectReason.HeroDeadCannotResummon);
            if (state.SummonedCount >= MaxHeroesPerBattle)
                return SummonDecision.Reject(RejectReason.HeroLimitReached);

            if (request.Mode == SummonMode.Free)
            {
                return _layout.IsValidSummonPosition(request.Position)
                    ? SummonDecision.Accept(request.Position)
                    : SummonDecision.Reject(RejectReason.OutsideSummonZone);
            }

            return TryPickQuickSummonPoint(isPointOccupied, out var point)
                ? SummonDecision.Accept(point)
                : SummonDecision.Reject(RejectReason.NoQuickSummonPoint);
        }

        /// <summary>
        /// Free point nearest the player's castle (spec §4, provisional). If every point is occupied, falls back to the
        /// nearest point anyway so a quick summon never fails just because heroes are standing on the points.
        /// </summary>
        public bool TryPickQuickSummonPoint(Func<Vector2, bool> isPointOccupied, out Vector2 point)
        {
            point = default;
            if (_quickPointsByCastleDistance.Length == 0)
                return false;

            foreach (var candidate in _quickPointsByCastleDistance)
            {
                if (isPointOccupied == null || !isPointOccupied(candidate))
                {
                    point = candidate;
                    return true;
                }
            }

            point = _quickPointsByCastleDistance[0];
            return true;
        }
    }
}

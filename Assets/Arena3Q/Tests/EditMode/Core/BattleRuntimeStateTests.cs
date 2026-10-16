using System;
using Arena3Q.Core;
using NUnit.Framework;

namespace Arena3Q.Tests.EditMode
{
    public class BattleRuntimeStateTests
    {
        [Test]
        public void NewState_HasAvailableCardsAndFullCastles()
        {
            var state = TestBattles.State(cardCount: 5);

            Assert.That(state.Cards, Has.Count.EqualTo(5));
            Assert.That(state.Cards, Has.All.Matches<HeroCard>(c => c.State == CardState.Available));
            Assert.That(state.PlayerCastle.Health, Is.EqualTo(1000f));
            Assert.That(state.EnemyCastle.Team, Is.EqualTo(Team.Enemy));
            Assert.That(state.Outcome, Is.EqualTo(BattleOutcome.InProgress));
        }

        [Test]
        public void RegisterSummon_DeploysCardAndStartsHeroInIdle()
        {
            var state = TestBattles.State();

            var hero = TestBattles.Summon(state, 2);

            Assert.That(hero.State, Is.EqualTo(HeroState.Idle));
            Assert.That(hero.Health, Is.EqualTo(500f));
            Assert.That(state.Cards[2].State, Is.EqualTo(CardState.Deployed));
            Assert.That(state.Cards[2].Hero, Is.EqualTo(hero.Id));
            Assert.That(state.SummonedCount, Is.EqualTo(1));
            Assert.That(state.TryGetHero(hero.Id, out _), Is.True);
        }

        [Test]
        public void UnitIds_AreUniqueAcrossCastlesAndHeroes()
        {
            var state = TestBattles.State();
            var a = TestBattles.Summon(state, 0).Id;
            var b = TestBattles.Summon(state, 1).Id;

            Assert.That(new[] { state.PlayerCastle.Id, state.EnemyCastle.Id, a, b }, Is.Unique);
        }

        [Test]
        public void RegisterSummon_WithoutValidation_Throws()
        {
            var state = TestBattles.State();
            TestBattles.Summon(state, 0);

            Assert.Throws<InvalidOperationException>(() => TestBattles.Summon(state, 0));
        }

        [Test]
        public void MarkHeroDead_GreysOutCardCountsLossAndClearsCommand()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);
            hero.CurrentCommand = HeroCommandKind.Attack;
            hero.AttackTarget = state.EnemyCastle.Id;

            state.MarkHeroDead(hero.Id);
            state.MarkHeroDead(hero.Id);

            Assert.That(hero.State, Is.EqualTo(HeroState.Dead));
            Assert.That(hero.CurrentCommand, Is.EqualTo(HeroCommandKind.None));
            Assert.That(hero.AttackTarget, Is.EqualTo(UnitId.None));
            Assert.That(state.Cards[0].State, Is.EqualTo(CardState.Dead));
            Assert.That(state.HeroesLost, Is.EqualTo(1), "MarkHeroDead must be idempotent");
            Assert.That(state.AliveHeroCount, Is.Zero);
        }

        [Test]
        public void SetState_CannotReviveOrKillHero()
        {
            var state = TestBattles.State();
            var hero = TestBattles.Summon(state, 0);

            Assert.Throws<InvalidOperationException>(() => hero.SetState(HeroState.Dead));

            state.MarkHeroDead(hero.Id);
            hero.SetState(HeroState.Moving);
            Assert.That(hero.State, Is.EqualTo(HeroState.Dead));
        }

        [Test]
        public void Selection_OnlyOneLivingHero()
        {
            var state = TestBattles.State();
            var a = TestBattles.Summon(state, 0);
            var b = TestBattles.Summon(state, 1);

            Assert.That(state.TrySelectHero(a.Id), Is.True);
            Assert.That(state.TrySelectHero(b.Id), Is.True);
            Assert.That(state.SelectedHero, Is.EqualTo(b.Id));

            state.MarkHeroDead(b.Id);
            Assert.That(state.SelectedHero, Is.EqualTo(UnitId.None));
            Assert.That(state.TrySelectHero(b.Id), Is.False, "dead heroes cannot be selected");
        }

        [Test]
        public void Tick_AdvancesClockUntilBattleIsOver()
        {
            var state = TestBattles.State(timeLimit: 10f);

            state.Tick(4f);
            Assert.That(state.RemainingTime, Is.EqualTo(6f));

            state.Finish(BattleOutcome.Victory);
            state.Tick(4f);
            Assert.That(state.ElapsedTime, Is.EqualTo(4f));
        }

        [Test]
        public void Finish_IsFinalAndComputesStars()
        {
            var state = TestBattles.State();
            state.MarkHeroDead(TestBattles.Summon(state, 0).Id);

            state.Finish(BattleOutcome.Victory);
            state.Finish(BattleOutcome.Defeat);

            Assert.That(state.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(state.Stars, Is.EqualTo(2));
        }

        [Test]
        public void NewBattle_DoesNotKeepPreviousState()
        {
            var setup = TestBattles.Setup();
            var first = new BattleRuntimeState(setup);
            first.MarkHeroDead(TestBattles.Summon(first, 0).Id);
            first.Finish(BattleOutcome.Defeat);

            var second = new BattleRuntimeState(setup);

            Assert.That(second.SummonedCount, Is.Zero);
            Assert.That(second.HeroesLost, Is.Zero);
            Assert.That(second.Cards[0].State, Is.EqualTo(CardState.Available));
            Assert.That(second.IsOver, Is.False);
        }

        [Test]
        public void Castle_DamageClampsAtZero()
        {
            var state = TestBattles.State();

            state.EnemyCastle.ApplyDamage(400f);
            state.EnemyCastle.ApplyDamage(5000f);

            Assert.That(state.EnemyCastle.Health, Is.Zero);
            Assert.That(state.EnemyCastle.IsDestroyed, Is.True);
        }
    }
}

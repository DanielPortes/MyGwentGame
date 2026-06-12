using System.Linq;
using Gwent.Core;
using NUnit.Framework;

namespace Gwent.Tests
{
    public class GwentCoreTests
    {
        [Test]
        public void WeatherSetsAffectedNonHeroRowsToOne()
        {
            var match = TestMatch();
            match.PlayUnit(PlayerId.Player, Card("infantry", "Infantry", CombatRow.Close, 8));
            match.PlayUnit(PlayerId.Opponent, Card("archer", "Archer", CombatRow.Ranged, 6));

            match.ApplyWeather(WeatherEffect.BitingFrost);

            Assert.AreEqual(1, match.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.AreEqual(6, match.GetRowScore(PlayerId.Opponent, CombatRow.Ranged));
        }

        [Test]
        public void HeroesIgnoreWeatherAndScorch()
        {
            var match = TestMatch();
            match.PlayUnit(PlayerId.Player, Card("geralt", "Geralt of Rivia", CombatRow.Close, 15, CardAbility.Hero));
            match.PlayUnit(PlayerId.Opponent, Card("reaver", "Reaver", CombatRow.Close, 10));

            match.ApplyWeather(WeatherEffect.BitingFrost);
            match.PlayScorch(PlayerId.Player);

            Assert.AreEqual(15, match.GetTotalScore(PlayerId.Player));
            Assert.AreEqual(0, match.GetTotalScore(PlayerId.Opponent));
        }

        [Test]
        public void HornTightBondAndMoraleStackLikeWitcher3Gwent()
        {
            var match = TestMatch();
            match.PlayUnit(PlayerId.Player, Card("blue_stripes", "Blue Stripes Commando", CombatRow.Close, 4, CardAbility.TightBond));
            match.PlayUnit(PlayerId.Player, Card("blue_stripes", "Blue Stripes Commando", CombatRow.Close, 4, CardAbility.TightBond));
            match.PlayUnit(PlayerId.Player, Card("dandelion", "Dandelion", CombatRow.Close, 2, CardAbility.MoraleBoost));
            match.ApplyHorn(PlayerId.Player, CombatRow.Close);

            Assert.AreEqual(38, match.GetRowScore(PlayerId.Player, CombatRow.Close));
        }

        [Test]
        public void SpyPlaysOnOpponentBoardAndDrawsTwoCardsForOwner()
        {
            var match = TestMatch();
            match.SetDeck(PlayerId.Player, Card("draw1", "Draw 1", CombatRow.Close, 1), Card("draw2", "Draw 2", CombatRow.Close, 1));

            match.PlaySpy(PlayerId.Player, Card("spy", "Thaler", CombatRow.Close, 1, CardAbility.Spy));

            Assert.AreEqual(1, match.GetTotalScore(PlayerId.Opponent));
            Assert.AreEqual(2, match.GetHand(PlayerId.Player).Count);
        }

        [Test]
        public void MusterPlaysMatchingCardsFromHandAndDeck()
        {
            var match = TestMatch();
            var vampire = Card("vampire", "Vampire", CombatRow.Close, 4, CardAbility.Muster);
            match.SetHand(PlayerId.Player, vampire);
            match.SetDeck(PlayerId.Player, vampire, vampire, Card("other", "Other", CombatRow.Close, 3));

            match.PlayFromHand(PlayerId.Player, 0, CombatRow.Close);

            Assert.AreEqual(12, match.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.AreEqual(1, match.GetDeck(PlayerId.Player).Count);
        }

        [Test]
        public void NorthernRealmsDrawsAfterWinningRound()
        {
            var match = TestMatch(Faction.NorthernRealms, Faction.Monsters);
            match.SetDeck(PlayerId.Player, Card("reward", "Reward", CombatRow.Close, 1));
            match.PlayUnit(PlayerId.Player, Card("winner", "Winner", CombatRow.Close, 9));
            match.Pass(PlayerId.Player);
            match.Pass(PlayerId.Opponent);

            Assert.AreEqual(1, match.GetRoundWins(PlayerId.Player));
            Assert.AreEqual(1, match.GetHand(PlayerId.Player).Count);
        }

        [Test]
        public void NilfgaardWinsDrawnRound()
        {
            var match = TestMatch(Faction.Nilfgaard, Faction.NorthernRealms);
            match.PlayUnit(PlayerId.Player, Card("a", "A", CombatRow.Close, 5));
            match.PlayUnit(PlayerId.Opponent, Card("b", "B", CombatRow.Close, 5));
            match.Pass(PlayerId.Player);
            match.Pass(PlayerId.Opponent);

            Assert.AreEqual(1, match.GetRoundWins(PlayerId.Player));
            Assert.AreEqual(0, match.GetRoundWins(PlayerId.Opponent));
        }

        [Test]
        public void MonstersCarryOneRandomUnitBetweenRounds()
        {
            var match = TestMatch(Faction.Monsters, Faction.NorthernRealms);
            match.PlayUnit(PlayerId.Player, Card("nekker", "Nekker", CombatRow.Close, 2));
            match.PlayUnit(PlayerId.Player, Card("ghoul", "Ghoul", CombatRow.Close, 3));
            match.Pass(PlayerId.Player);
            match.Pass(PlayerId.Opponent);

            Assert.AreEqual(1, match.GetBoardCards(PlayerId.Player).Count());
            Assert.Greater(match.GetTotalScore(PlayerId.Player), 0);
        }

        private static GwentMatch TestMatch(Faction playerFaction = Faction.NorthernRealms, Faction opponentFaction = Faction.Nilfgaard)
        {
            return new GwentMatch(playerFaction, opponentFaction, new DeterministicRandom());
        }

        private static CardDefinition Card(string id, string name, CombatRow row, int strength, params CardAbility[] abilities)
        {
            return new CardDefinition(id, name, Faction.Neutral, CardKind.Unit, row, strength, abilities);
        }
    }
}

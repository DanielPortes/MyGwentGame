using System;
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
            match.PlayUnit(PlayerId.Opponent, Card("reaver", "Reaver", CombatRow.Ranged, 10));

            match.ApplyWeather(WeatherEffect.BitingFrost);
            match.PlayScorch(PlayerId.Player);

            Assert.AreEqual(15, match.GetTotalScore(PlayerId.Player));
            Assert.AreEqual(0, match.GetTotalScore(PlayerId.Opponent));
        }

        [Test]
        public void ScorchOnlyDestroysStrongestUnitsAtTenOrMore()
        {
            var safeMatch = TestMatch();
            safeMatch.PlayUnit(PlayerId.Player, Card("eight", "Eight", CombatRow.Close, 8));

            safeMatch.PlayScorch(PlayerId.Player);

            Assert.AreEqual(8, safeMatch.GetTotalScore(PlayerId.Player));

            var burnMatch = TestMatch();
            burnMatch.PlayUnit(PlayerId.Player, Card("ten", "Ten", CombatRow.Close, 10));
            burnMatch.PlayUnit(PlayerId.Opponent, Card("other_ten", "Other Ten", CombatRow.Ranged, 10));

            burnMatch.PlayScorch(PlayerId.Player);

            Assert.AreEqual(0, burnMatch.GetTotalScore(PlayerId.Player));
            Assert.AreEqual(0, burnMatch.GetTotalScore(PlayerId.Opponent));
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
        public void UnitCommandersHornDoublesRowButNotItselfAndDoesNotStackWithSpecialHorn()
        {
            var match = TestMatch();
            var dandelion = Card("neutral_dandelion", "Dandelion", CombatRow.Close, 2, CardAbility.CommandersHorn);
            var infantry = Card("infantry", "Infantry", CombatRow.Close, 5);

            match.PlayUnit(PlayerId.Player, dandelion);
            match.PlayUnit(PlayerId.Player, infantry);

            Assert.AreEqual(12, match.GetRowScore(PlayerId.Player, CombatRow.Close));

            match.ApplyHorn(PlayerId.Player, CombatRow.Close);

            Assert.AreEqual(14, match.GetRowScore(PlayerId.Player, CombatRow.Close));
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
        public void MusterUsesSharedGroupsForDifferentlyNamedCards()
        {
            var match = TestMatch();
            var brewess = MusterCard("crone_brewess", "Crone: Brewess", "crones", 6);
            var weavess = MusterCard("crone_weavess", "Crone: Weavess", "crones", 6);
            var whispess = MusterCard("crone_whispess", "Crone: Whispess", "crones", 6);
            match.SetHand(PlayerId.Player, brewess, weavess);
            match.SetDeck(PlayerId.Player, whispess);

            match.PlayCardFromHand(PlayerId.Player, 0, CombatRow.Close);

            Assert.AreEqual(18, match.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.IsEmpty(match.GetHand(PlayerId.Player));
            Assert.IsEmpty(match.GetDeck(PlayerId.Player));
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

        [Test]
        public void StartMatchDrawsTenCardsForEachPlayer()
        {
            var match = TestMatch();
            match.SetDeck(PlayerId.Player, Cards("player", 12));
            match.SetDeck(PlayerId.Opponent, Cards("opponent", 11));

            match.StartMatch(PlayerId.Opponent);

            Assert.AreEqual(10, match.GetHand(PlayerId.Player).Count);
            Assert.AreEqual(2, match.GetDeck(PlayerId.Player).Count);
            Assert.AreEqual(10, match.GetHand(PlayerId.Opponent).Count);
            Assert.AreEqual(1, match.GetDeck(PlayerId.Opponent).Count);
            Assert.AreEqual(PlayerId.Opponent, match.CurrentTurn);
        }

        [Test]
        public void MulliganReplacesUpToTwoCards()
        {
            var match = TestMatch();
            match.SetHand(
                PlayerId.Player,
                Card("keep", "Keep", CombatRow.Close, 1),
                Card("replace1", "Replace 1", CombatRow.Close, 1),
                Card("replace2", "Replace 2", CombatRow.Close, 1));
            match.SetDeck(
                PlayerId.Player,
                Card("draw1", "Draw 1", CombatRow.Close, 1),
                Card("draw2", "Draw 2", CombatRow.Close, 1),
                Card("draw3", "Draw 3", CombatRow.Close, 1));

            var firstReplacement = match.Mulligan(PlayerId.Player, 1);
            var secondReplacement = match.Mulligan(PlayerId.Player, 2);

            Assert.AreEqual("draw1", firstReplacement.Id);
            Assert.AreEqual("draw2", secondReplacement.Id);
            Assert.AreEqual(new[] { "keep", "draw1", "draw2" }, match.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.Throws<InvalidOperationException>(() => match.Mulligan(PlayerId.Player, 0));
        }

        [Test]
        public void PlayFromHandAlternatesTurnsAndSkipsPassedPlayer()
        {
            var match = TestMatch();
            match.SetHand(
                PlayerId.Player,
                Card("p1", "Player 1", CombatRow.Close, 5),
                Card("p2", "Player 2", CombatRow.Close, 6));
            match.SetHand(PlayerId.Opponent, Card("o1", "Opponent 1", CombatRow.Close, 1));
            match.StartMatch(PlayerId.Player);

            match.PlayFromHand(PlayerId.Player, 0, CombatRow.Close);

            Assert.AreEqual(PlayerId.Opponent, match.CurrentTurn);
            Assert.Throws<InvalidOperationException>(() => match.PlayFromHand(PlayerId.Player, 0, CombatRow.Close));

            match.Pass(PlayerId.Opponent);
            match.PlayFromHand(PlayerId.Player, 0, CombatRow.Close);

            Assert.AreEqual(PlayerId.Player, match.CurrentTurn);

            match.Pass(PlayerId.Player);

            Assert.AreEqual(1, match.GetRoundWins(PlayerId.Player));
        }

        [Test]
        public void PassRequiresTheCurrentTurn()
        {
            var match = TestMatch();
            match.StartMatch(PlayerId.Player);

            Assert.Throws<InvalidOperationException>(() => match.Pass(PlayerId.Opponent));
        }

        [Test]
        public void AgileCardsCanChooseCloseOrRangedButNotSiege()
        {
            var match = TestMatch();
            var agile = Card("agile", "Agile", CombatRow.Close, 5, CardAbility.Agile);

            match.PlayUnit(PlayerId.Player, agile, CombatRow.Ranged);

            Assert.AreEqual(5, match.GetRowScore(PlayerId.Player, CombatRow.Ranged));
            Assert.AreEqual(new[] { "agile" }, match.GetRowCards(PlayerId.Player, CombatRow.Ranged).Select(card => card.Id).ToArray());
            Assert.Throws<InvalidOperationException>(() => match.PlayUnit(PlayerId.Player, agile, CombatRow.Siege));
        }

        [Test]
        public void MedicRestoresEligibleUnitFromDiscard()
        {
            var match = TestMatch();
            var medic = Card("medic", "Medic", CombatRow.Ranged, 5, CardAbility.Medic);
            var restored = Card("catapult", "Catapult", CombatRow.Siege, 8);
            var hero = Card("geralt", "Geralt of Rivia", CombatRow.Close, 15, CardAbility.Hero);
            match.SetDiscard(PlayerId.Player, hero, restored);

            match.PlayMedic(PlayerId.Player, medic, restored);

            Assert.AreEqual(5, match.GetRowScore(PlayerId.Player, CombatRow.Ranged));
            Assert.AreEqual(8, match.GetRowScore(PlayerId.Player, CombatRow.Siege));
            Assert.AreEqual(new[] { "geralt" }, match.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
        }

        [Test]
        public void DecoyReturnsNonHeroUnitToHandAndCannotTargetHeroes()
        {
            var match = TestMatch();
            var unit = Card("reaver", "Reaver", CombatRow.Ranged, 6);
            var hero = Card("ciri", "Cirilla Fiona Elen Riannon", CombatRow.Close, 15, CardAbility.Hero);
            var decoy = Special("decoy", "Decoy", CardAbility.Decoy);
            match.PlayUnit(PlayerId.Player, unit);
            match.PlayUnit(PlayerId.Player, hero);

            match.PlayDecoy(PlayerId.Player, decoy, unit);

            Assert.AreEqual(0, match.GetRowScore(PlayerId.Player, CombatRow.Ranged));
            Assert.AreEqual(new[] { "reaver" }, match.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.AreEqual(new[] { "decoy" }, match.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.Throws<InvalidOperationException>(() => match.PlayDecoy(PlayerId.Player, decoy, hero));
        }

        [Test]
        public void PlayCardFromHandAppliesWeatherAndDiscardsTheCard()
        {
            var match = TestMatch();
            var frost = new CardDefinition("neutral_biting_frost", "Biting Frost", Faction.Neutral, CardKind.Weather, CombatRow.Close, 0);
            match.SetHand(PlayerId.Player, frost);
            match.PlayUnit(PlayerId.Player, Card("infantry", "Infantry", CombatRow.Close, 8));

            match.PlayCardFromHand(PlayerId.Player, 0);

            Assert.AreEqual(1, match.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.AreEqual(new[] { "neutral_biting_frost" }, match.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.AreEqual(PlayerId.Opponent, match.CurrentTurn);
        }

        [Test]
        public void PlayCardFromHandAppliesHornAndScorchSpecials()
        {
            var hornMatch = TestMatch();
            hornMatch.SetHand(PlayerId.Player, Special("neutral_commanders_horn", "Commander's Horn", CardAbility.CommandersHorn));
            hornMatch.PlayUnit(PlayerId.Player, Card("catapult", "Catapult", CombatRow.Siege, 8));

            hornMatch.PlayCardFromHand(PlayerId.Player, 0, CombatRow.Siege);

            Assert.AreEqual(16, hornMatch.GetRowScore(PlayerId.Player, CombatRow.Siege));
            Assert.AreEqual(new[] { "neutral_commanders_horn" }, hornMatch.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());

            var scorchMatch = TestMatch();
            scorchMatch.SetHand(PlayerId.Player, Special("neutral_scorch", "Scorch", CardAbility.Scorch));
            scorchMatch.PlayUnit(PlayerId.Player, Card("weak", "Weak", CombatRow.Close, 4));
            scorchMatch.PlayUnit(PlayerId.Opponent, Card("strong", "Strong", CombatRow.Ranged, 10));

            scorchMatch.PlayCardFromHand(PlayerId.Player, 0);

            Assert.AreEqual(0, scorchMatch.GetTotalScore(PlayerId.Opponent));
            Assert.AreEqual(new[] { "neutral_scorch" }, scorchMatch.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.AreEqual(new[] { "strong" }, scorchMatch.GetDiscard(PlayerId.Opponent).Select(card => card.Id).ToArray());
        }

        [Test]
        public void PlayCardFromHandHandlesSpyDecoyAndMedicTargets()
        {
            var spyMatch = TestMatch();
            spyMatch.SetDeck(PlayerId.Player, Card("draw1", "Draw 1", CombatRow.Close, 1), Card("draw2", "Draw 2", CombatRow.Close, 1));
            spyMatch.SetHand(PlayerId.Player, Card("thaler", "Thaler", CombatRow.Siege, 1, CardAbility.Spy));

            spyMatch.PlayCardFromHand(PlayerId.Player, 0);

            Assert.AreEqual(1, spyMatch.GetRowScore(PlayerId.Opponent, CombatRow.Siege));
            Assert.AreEqual(new[] { "draw1", "draw2" }, spyMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());

            var decoyMatch = TestMatch();
            var unit = Card("ves", "Ves", CombatRow.Close, 5);
            decoyMatch.PlayUnit(PlayerId.Player, unit);
            decoyMatch.SetHand(PlayerId.Player, Special("neutral_decoy", "Decoy", CardAbility.Decoy));

            decoyMatch.PlayCardFromHand(PlayerId.Player, 0, target: unit);

            Assert.AreEqual(0, decoyMatch.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.AreEqual(new[] { "ves" }, decoyMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.AreEqual(new[] { "neutral_decoy" }, decoyMatch.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());

            var medicMatch = TestMatch();
            var restored = Card("catapult", "Catapult", CombatRow.Siege, 8);
            medicMatch.SetDiscard(PlayerId.Player, restored);
            medicMatch.SetHand(PlayerId.Player, Card("medic", "Medic", CombatRow.Ranged, 5, CardAbility.Medic));

            medicMatch.PlayCardFromHand(PlayerId.Player, 0, target: restored);

            Assert.AreEqual(5, medicMatch.GetRowScore(PlayerId.Player, CombatRow.Ranged));
            Assert.AreEqual(8, medicMatch.GetRowScore(PlayerId.Player, CombatRow.Siege));
            Assert.IsEmpty(medicMatch.GetHand(PlayerId.Player));
        }

        [Test]
        public void UnitRowScorchBurnsStrongestEnemyCardInTargetRow()
        {
            var match = TestMatch();
            var dragon = Card("villentretenmerth", "Villentretenmerth", CombatRow.Close, 7, CardAbility.ScorchClose);
            match.SetHand(PlayerId.Player, dragon);
            match.PlayUnit(PlayerId.Opponent, Card("six", "Six", CombatRow.Close, 6));
            match.PlayUnit(PlayerId.Opponent, Card("five", "Five", CombatRow.Close, 5));

            match.PlayCardFromHand(PlayerId.Player, 0, CombatRow.Close);

            Assert.AreEqual(7, match.GetRowScore(PlayerId.Player, CombatRow.Close));
            Assert.AreEqual(5, match.GetRowScore(PlayerId.Opponent, CombatRow.Close));
            Assert.AreEqual(new[] { "six" }, match.GetDiscard(PlayerId.Opponent).Select(card => card.Id).ToArray());
        }

        [Test]
        public void NonNilfgaardDrawMakesBothPlayersLoseTheRound()
        {
            var match = TestMatch(Faction.NorthernRealms, Faction.Monsters);
            match.PlayUnit(PlayerId.Player, Card("a", "A", CombatRow.Close, 5));
            match.PlayUnit(PlayerId.Opponent, Card("b", "B", CombatRow.Close, 5));

            match.Pass(PlayerId.Player);
            match.Pass(PlayerId.Opponent);

            Assert.AreEqual(0, match.GetRoundWins(PlayerId.Player));
            Assert.AreEqual(0, match.GetRoundWins(PlayerId.Opponent));
            Assert.AreEqual(1, match.GetRoundLosses(PlayerId.Player));
            Assert.AreEqual(1, match.GetRoundLosses(PlayerId.Opponent));
        }

        [Test]
        public void MatchCompletesWhenAPlayerWinsTwoRounds()
        {
            var match = TestMatch();
            match.PlayUnit(PlayerId.Player, Card("round1", "Round 1", CombatRow.Close, 5));
            match.Pass(PlayerId.Player);
            match.Pass(PlayerId.Opponent);
            match.Pass(PlayerId.Opponent);
            match.PlayUnit(PlayerId.Player, Card("round2", "Round 2", CombatRow.Close, 5));

            match.Pass(PlayerId.Player);

            Assert.IsTrue(match.IsMatchComplete);
            Assert.AreEqual(PlayerId.Player, match.MatchWinner);
        }

        [Test]
        public void ScoiataelCanChooseWhoStarts()
        {
            var match = TestMatch(Faction.Scoiatael, Faction.NorthernRealms);

            match.ChooseStartingPlayer(PlayerId.Player, PlayerId.Opponent);

            Assert.AreEqual(PlayerId.Opponent, match.CurrentTurn);
            Assert.Throws<InvalidOperationException>(() => TestMatch(Faction.NorthernRealms, Faction.Monsters)
                .ChooseStartingPlayer(PlayerId.Player, PlayerId.Opponent));
        }

        private static GwentMatch TestMatch(Faction playerFaction = Faction.NorthernRealms, Faction opponentFaction = Faction.Nilfgaard)
        {
            return new GwentMatch(playerFaction, opponentFaction, new DeterministicRandom());
        }

        private static CardDefinition Card(string id, string name, CombatRow row, int strength, params CardAbility[] abilities)
        {
            return new CardDefinition(id, name, Faction.Neutral, CardKind.Unit, row, strength, abilities);
        }

        private static CardDefinition MusterCard(string id, string name, string musterGroup, int strength)
        {
            return new CardDefinition(id, name, Faction.Neutral, CardKind.Unit, CombatRow.Close, strength, musterGroup, CardAbility.Muster);
        }

        private static CardDefinition Special(string id, string name, params CardAbility[] abilities)
        {
            return new CardDefinition(id, name, Faction.Neutral, CardKind.Special, CombatRow.Close, 0, abilities);
        }

        private static CardDefinition[] Cards(string prefix, int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => Card($"{prefix}{index}", $"{prefix} {index}", CombatRow.Close, 1))
                .ToArray();
        }
    }
}

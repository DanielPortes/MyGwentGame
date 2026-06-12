using System.Linq;
using System.Reflection;
using Gwent.Core;
using Gwent.Data;
using NUnit.Framework;
using UnityEngine;

namespace Gwent.Tests
{
    public class GwentCatalogTests
    {
        [TestCase(Faction.NorthernRealms, "Foltest")]
        [TestCase(Faction.Nilfgaard, "Emhyr")]
        [TestCase(Faction.Monsters, "Eredin")]
        [TestCase(Faction.Scoiatael, "Francesca")]
        public void EveryBaseFactionHasLeadersAndAPlayableDeck(Faction faction, string leaderName)
        {
            var deck = GwentCatalog.GetDeck(faction);
            var defaultDeck = GwentCatalog.CreateDefaultDeck(faction);

            Assert.AreEqual(faction, deck.Faction);
            Assert.GreaterOrEqual(deck.Leaders.Count, 4);
            Assert.IsTrue(deck.Leaders.All(leader => leader.Faction == faction));
            Assert.IsTrue(deck.Leaders.Any(leader => leader.Name.Contains(leaderName)));
            Assert.GreaterOrEqual(defaultDeck.Count(card => card.Kind == CardKind.Unit), 22);
            Assert.LessOrEqual(defaultDeck.Count(IsSpecialOrWeather), 10);
            Assert.IsTrue(defaultDeck.Any(card => card.Faction == faction));
        }

        [Test]
        public void NeutralCatalogIncludesCoreWitcher3Cards()
        {
            var neutralCards = GwentCatalog.GetNeutralCards().ToArray();
            var ids = neutralCards.Select(entry => entry.Card.Id).ToArray();

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "neutral_geralt_of_rivia",
                    "neutral_cirilla_fiona_elen_riannon",
                    "neutral_yennefer_of_vengerberg",
                    "neutral_mysterious_elf",
                    "neutral_triss_merigold",
                    "neutral_decoy",
                    "neutral_commanders_horn",
                    "neutral_scorch",
                    "neutral_biting_frost",
                    "neutral_impenetrable_fog",
                    "neutral_torrential_rain",
                    "neutral_clear_weather"
                },
                ids);

            AssertNeutralAbility(neutralCards, "neutral_commanders_horn", CardAbility.CommandersHorn);
            AssertNeutralAbility(neutralCards, "neutral_scorch", CardAbility.Scorch);
        }

        [Test]
        public void CatalogPreservesRowsAbilitiesAndCopyCountsForKnownCards()
        {
            var northernRealms = GwentCatalog.GetDeck(Faction.NorthernRealms);
            var monsters = GwentCatalog.GetDeck(Faction.Monsters);
            var scoiatael = GwentCatalog.GetDeck(Faction.Scoiatael);
            var nilfgaard = GwentCatalog.GetDeck(Faction.Nilfgaard);

            AssertCard(northernRealms, "northern_realms_blue_stripes_commando", CombatRow.Close, 4, CardAbility.TightBond, 3);
            AssertCard(northernRealms, "northern_realms_thaler", CombatRow.Siege, 1, CardAbility.Spy, 1);
            AssertCard(nilfgaard, "nilfgaard_menno_coehoorn", CombatRow.Close, 10, CardAbility.Hero | CardAbility.Medic, 1);
            AssertCard(monsters, "monsters_arachas", CombatRow.Close, 4, CardAbility.Muster, 3);
            AssertCard(monsters, "monsters_kayran", CombatRow.Close, 8, CardAbility.Hero | CardAbility.MoraleBoost | CardAbility.Agile, 1);
            AssertCard(monsters, "monsters_toad", CombatRow.Ranged, 7, CardAbility.ScorchRanged, 1);
            AssertCard(scoiatael, "scoiatael_havekar_healer", CombatRow.Ranged, 0, CardAbility.Medic, 3);
            AssertCard(scoiatael, "scoiatael_schirru", CombatRow.Siege, 8, CardAbility.ScorchSiege, 1);
            AssertCard(scoiatael, "scoiatael_vrihedd_brigade_veteran", CombatRow.Close, 5, CardAbility.Agile, 2);
            AssertNeutralAbility(GwentCatalog.GetNeutralCards().ToArray(), "neutral_villentretenmerth", CardAbility.ScorchClose);
            AssertNeutralAbility(GwentCatalog.GetNeutralCards().ToArray(), "neutral_dandelion", CardAbility.CommandersHorn);
            Assert.IsFalse(GwentCatalog.GetNeutralCards()
                .Single(entry => entry.Card.Id == "neutral_dandelion")
                .Card.HasAbility(CardAbility.MoraleBoost));
        }

        [Test]
        public void AssetCatalogMapsAvailableLocalAssetsAndReportsMissingScoiataelBack()
        {
            Assert.AreEqual("Assets/Assets/border-gold.png", GwentAssetCatalog.CardBorderPath);
            Assert.IsTrue(GwentAssetCatalog.TryGetFactionBackPath(Faction.NorthernRealms, out var northernRealmsBack));
            Assert.IsTrue(GwentAssetCatalog.TryGetFactionBackPath(Faction.Nilfgaard, out var nilfgaardBack));
            Assert.IsTrue(GwentAssetCatalog.TryGetFactionBackPath(Faction.Monsters, out var monstersBack));
            Assert.IsFalse(GwentAssetCatalog.TryGetFactionBackPath(Faction.Scoiatael, out var scoiataelBack));

            Assert.AreEqual("Assets/Assets/default-northern_realms.png", northernRealmsBack);
            Assert.AreEqual("Assets/Assets/default-nilfgaard.png", nilfgaardBack);
            Assert.AreEqual("Assets/Assets/default-monster.png", monstersBack);
            Assert.IsNull(scoiataelBack);
        }

        [Test]
        public void AssetCatalogMapsKnownLocalCardArt()
        {
            Assert.IsTrue(GwentAssetCatalog.TryGetCardArtPath("neutral_geralt_of_rivia", out var geralt));
            Assert.IsTrue(GwentAssetCatalog.TryGetCardArtPath("neutral_cirilla_fiona_elen_riannon", out var ciri));
            Assert.IsTrue(GwentAssetCatalog.TryGetCardArtPath("neutral_yennefer_of_vengerberg", out var yennefer));
            Assert.IsFalse(GwentAssetCatalog.TryGetCardArtPath("missing_card", out var missing));

            Assert.AreEqual("Assets/Assets/1016.jpg", geralt);
            Assert.AreEqual("Assets/Assets/1019.jpg", ciri);
            Assert.AreEqual("Assets/Assets/1683.jpg", yennefer);
            Assert.IsNull(missing);
        }

        [Test]
        public void AssetCatalogProvidesLoadableRuntimeResourcesForEveryDefaultDeck()
        {
            var getCardArtResource = typeof(GwentAssetCatalog).GetMethod(
                "GetCardArtResource",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(CardDefinition) },
                null);
            var getFactionBackResource = typeof(GwentAssetCatalog).GetMethod(
                "GetFactionBackResource",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Faction) },
                null);

            Assert.NotNull(getCardArtResource, "Runtime card art must be exposed as Resources paths for WebGL.");
            Assert.NotNull(getFactionBackResource, "Runtime faction backs must be exposed as Resources paths for WebGL.");

            foreach (var faction in new[] { Faction.NorthernRealms, Faction.Nilfgaard, Faction.Monsters, Faction.Scoiatael })
            {
                var backResource = (string)getFactionBackResource.Invoke(null, new object[] { faction });
                Assert.IsNotEmpty(backResource, faction + " must have a runtime card-back fallback.");
                Assert.NotNull(Resources.Load<Texture2D>(backResource), backResource);

                foreach (var card in GwentCatalog.CreateDefaultDeck(faction))
                {
                    var artResource = (string)getCardArtResource.Invoke(null, new object[] { card });
                    Assert.IsNotEmpty(artResource, card.Id + " must have runtime art or a faction fallback.");
                    Assert.NotNull(Resources.Load<Texture2D>(artResource), card.Id + " -> " + artResource);
                }
            }
        }

        [Test]
        public void LeaderDefinitionsApplyCoreEffects()
        {
            var clearWeatherMatch = TestMatch();
            clearWeatherMatch.PlayUnit(PlayerId.Player, Unit("infantry", CombatRow.Close, 8));
            clearWeatherMatch.ApplyWeather(WeatherEffect.BitingFrost);

            Leader(Faction.NorthernRealms, "northern_realms_foltest_lord_commander").ApplyTo(clearWeatherMatch, PlayerId.Player);

            Assert.AreEqual(8, clearWeatherMatch.GetRowScore(PlayerId.Player, CombatRow.Close));

            var hornMatch = TestMatch(Faction.Monsters, Faction.Nilfgaard);
            hornMatch.PlayUnit(PlayerId.Player, Unit("nekker", CombatRow.Close, 2));

            Leader(Faction.Monsters, "monsters_eredin_commander_of_the_red_riders").ApplyTo(hornMatch, PlayerId.Player);

            Assert.AreEqual(4, hornMatch.GetRowScore(PlayerId.Player, CombatRow.Close));

            var scorchMatch = TestMatch(Faction.Scoiatael, Faction.Nilfgaard);
            scorchMatch.PlayUnit(PlayerId.Opponent, Unit("six", CombatRow.Close, 6));
            scorchMatch.PlayUnit(PlayerId.Opponent, Unit("five", CombatRow.Close, 5));

            Leader(Faction.Scoiatael, "scoiatael_francesca_queen_of_dol_blathanna").ApplyTo(scorchMatch, PlayerId.Player);

            Assert.AreEqual(5, scorchMatch.GetRowScore(PlayerId.Opponent, CombatRow.Close));

            var drawMatch = TestMatch(Faction.Scoiatael, Faction.Nilfgaard);
            drawMatch.SetDeck(PlayerId.Player, Unit("draw", CombatRow.Ranged, 1));

            Leader(Faction.Scoiatael, "scoiatael_francesca_daisy_of_the_valley").ApplyTo(drawMatch, PlayerId.Player);

            Assert.AreEqual(new[] { "draw" }, drawMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
        }

        [Test]
        public void EveryLeaderHasStructuredAbility()
        {
            foreach (var faction in new[] { Faction.NorthernRealms, Faction.Nilfgaard, Faction.Monsters, Faction.Scoiatael })
            {
                Assert.IsTrue(
                    GwentCatalog.GetDeck(faction).Leaders.All(leader => leader.Ability != GwentLeaderAbility.None),
                    faction + " has a leader without a structured ability.");
            }
        }

        [Test]
        public void WhiteFlameCancelsOpponentLeaderAbility()
        {
            var match = TestMatch(Faction.Nilfgaard, Faction.NorthernRealms);
            match.PlayUnit(PlayerId.Player, Unit("infantry", CombatRow.Close, 8));
            match.ApplyWeather(WeatherEffect.BitingFrost);

            Leader(Faction.Nilfgaard, "nilfgaard_emhyr_the_white_flame").ApplyTo(match, PlayerId.Player);
            Leader(Faction.NorthernRealms, "northern_realms_foltest_lord_commander").ApplyTo(match, PlayerId.Opponent);

            Assert.AreEqual(1, match.GetRowScore(PlayerId.Player, CombatRow.Close));
        }

        [Test]
        public void LeaderDefinitionsUseOriginalNilfgaardMappings()
        {
            AssertLeader(Faction.Nilfgaard, "nilfgaard_emhyr_emperor_of_nilfgaard", "PeekOpponentHand");
            AssertLeader(Faction.Nilfgaard, "nilfgaard_emhyr_his_imperial_majesty", "ApplyWeather", weatherEffect: WeatherEffect.TorrentialRain);
            AssertLeader(Faction.Nilfgaard, "nilfgaard_emhyr_invader_of_the_north", "RandomizeMedicRestores");
            AssertLeader(Faction.Nilfgaard, "nilfgaard_emhyr_the_relentless", "DrawFromOpponentDiscard");
            AssertLeader(Faction.Nilfgaard, "nilfgaard_emhyr_the_white_flame", "CancelOpponentLeader");
        }

        [Test]
        public void LeaderDefinitionsUseOriginalMonsterMappings()
        {
            AssertLeader(Faction.Monsters, "monsters_eredin_breacc_glas_the_treacherous", "DoubleSpyStrengths");
            AssertLeader(Faction.Monsters, "monsters_eredin_bringer_of_death", "RestoreFromOwnDiscard");
            AssertLeader(Faction.Monsters, "monsters_eredin_commander_of_the_red_riders", "ApplyHorn", CombatRow.Close);
            AssertLeader(Faction.Monsters, "monsters_eredin_destroyer_of_worlds", "DiscardTwoDrawOne");
            AssertLeader(Faction.Monsters, "monsters_eredin_king_of_the_wild_hunt", "ApplyAnyWeather");
        }

        [Test]
        public void TreacherousDoublesSpyStrengthForBothPlayers()
        {
            var match = TestMatch(Faction.Monsters, Faction.Nilfgaard);
            match.PlayUnit(PlayerId.Player, Unit("player_spy", CombatRow.Siege, 4, CardAbility.Spy));
            match.PlayUnit(PlayerId.Opponent, Unit("opponent_spy", CombatRow.Ranged, 3, CardAbility.Spy));

            Leader(Faction.Monsters, "monsters_eredin_breacc_glas_the_treacherous").ApplyTo(match, PlayerId.Player);

            Assert.AreEqual(8, match.GetRowScore(PlayerId.Player, CombatRow.Siege));
            Assert.AreEqual(6, match.GetRowScore(PlayerId.Opponent, CombatRow.Ranged));
        }

        [Test]
        public void KingOfTheWildHuntPlaysWeatherFromDeck()
        {
            var match = TestMatch(Faction.Monsters, Faction.Nilfgaard);
            match.SetDeck(PlayerId.Player, Weather("neutral_impenetrable_fog"));
            match.PlayUnit(PlayerId.Opponent, Unit("archer", CombatRow.Ranged, 8));

            Leader(Faction.Monsters, "monsters_eredin_king_of_the_wild_hunt").ApplyTo(match, PlayerId.Player);

            Assert.AreEqual(1, match.GetRowScore(PlayerId.Opponent, CombatRow.Ranged));
            Assert.IsEmpty(match.GetDeck(PlayerId.Player));
            Assert.AreEqual(new[] { "neutral_impenetrable_fog" }, match.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
        }

        [Test]
        public void LeaderDefinitionsApplyDiscardAndMedicEffects()
        {
            var medicRandomMatch = TestMatch(Faction.Nilfgaard, Faction.NorthernRealms);
            var medic = new CardDefinition("medic", "Medic", Faction.Neutral, CardKind.Unit, CombatRow.Ranged, 5, CardAbility.Medic);
            var randomTarget = Unit("random_target", CombatRow.Close, 4);
            var requestedTarget = Unit("requested_target", CombatRow.Close, 6);
            medicRandomMatch.SetDiscard(PlayerId.Player, randomTarget, requestedTarget);

            Leader(Faction.Nilfgaard, "nilfgaard_emhyr_invader_of_the_north").ApplyTo(medicRandomMatch, PlayerId.Player);
            medicRandomMatch.PlayMedic(PlayerId.Player, medic, requestedTarget);

            Assert.AreEqual(new[] { "random_target" }, medicRandomMatch.GetRowCards(PlayerId.Player, CombatRow.Close).Select(card => card.Id).ToArray());
            Assert.AreEqual(new[] { "requested_target" }, medicRandomMatch.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());

            var relentlessMatch = TestMatch(Faction.Nilfgaard, Faction.NorthernRealms);
            relentlessMatch.SetDiscard(PlayerId.Opponent, Unit("opponent_discard", CombatRow.Close, 4));

            Leader(Faction.Nilfgaard, "nilfgaard_emhyr_the_relentless").ApplyTo(relentlessMatch, PlayerId.Player);

            Assert.AreEqual(new[] { "opponent_discard" }, relentlessMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.IsEmpty(relentlessMatch.GetDiscard(PlayerId.Opponent));

            var bringerMatch = TestMatch(Faction.Monsters, Faction.Nilfgaard);
            bringerMatch.SetDiscard(PlayerId.Player, Unit("own_discard", CombatRow.Close, 4));

            Leader(Faction.Monsters, "monsters_eredin_bringer_of_death").ApplyTo(bringerMatch, PlayerId.Player);

            Assert.AreEqual(new[] { "own_discard" }, bringerMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());

            var destroyerMatch = TestMatch(Faction.Monsters, Faction.Nilfgaard);
            destroyerMatch.SetHand(PlayerId.Player, Unit("discard1", CombatRow.Close, 1), Unit("discard2", CombatRow.Close, 1));
            destroyerMatch.SetDeck(PlayerId.Player, Unit("drawn", CombatRow.Ranged, 1));

            Leader(Faction.Monsters, "monsters_eredin_destroyer_of_worlds").ApplyTo(destroyerMatch, PlayerId.Player);

            Assert.AreEqual(new[] { "drawn" }, destroyerMatch.GetHand(PlayerId.Player).Select(card => card.Id).ToArray());
            Assert.AreEqual(new[] { "discard1", "discard2" }, destroyerMatch.GetDiscard(PlayerId.Player).Select(card => card.Id).ToArray());
        }

        private static void AssertCard(
            GwentDeckDefinition deck,
            string id,
            CombatRow row,
            int strength,
            CardAbility ability,
            int copies)
        {
            var entry = deck.Cards.Single(card => card.Card.Id == id);

            Assert.AreEqual(row, entry.Card.Row);
            Assert.AreEqual(strength, entry.Card.Strength);
            Assert.AreEqual(ability, entry.Card.Abilities);
            Assert.AreEqual(copies, entry.Copies);
        }

        private static void AssertLeader(
            Faction faction,
            string id,
            string ability,
            CombatRow? row = null,
            WeatherEffect? weatherEffect = null)
        {
            var leader = Leader(faction, id);

            Assert.AreEqual(ability, leader.Ability.ToString(), id);
            Assert.AreEqual(row, leader.TargetRow, id);
            Assert.AreEqual(weatherEffect, leader.WeatherEffect, id);
        }

        private static bool IsSpecialOrWeather(CardDefinition card)
        {
            return card.Kind == CardKind.Special || card.Kind == CardKind.Weather;
        }

        private static GwentMatch TestMatch(Faction playerFaction = Faction.NorthernRealms, Faction opponentFaction = Faction.Nilfgaard)
        {
            return new GwentMatch(playerFaction, opponentFaction, new DeterministicRandom());
        }

        private static CardDefinition Unit(string id, CombatRow row, int strength, params CardAbility[] abilities)
        {
            return new CardDefinition(id, id, Faction.Neutral, CardKind.Unit, row, strength, abilities);
        }

        private static CardDefinition Weather(string id)
        {
            return new CardDefinition(id, id, Faction.Neutral, CardKind.Weather, CombatRow.Close, 0);
        }

        private static GwentLeaderDefinition Leader(Faction faction, string id)
        {
            return GwentCatalog.GetDeck(faction).Leaders.Single(leader => leader.Id == id);
        }

        private static void AssertNeutralAbility(GwentCardCatalogEntry[] cards, string id, CardAbility ability)
        {
            Assert.IsTrue(cards.Single(entry => entry.Card.Id == id).Card.HasAbility(ability));
        }
    }
}

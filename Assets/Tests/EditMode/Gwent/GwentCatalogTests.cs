using System.Linq;
using Gwent.Core;
using Gwent.Data;
using NUnit.Framework;

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
            AssertCard(scoiatael, "scoiatael_havekar_healer", CombatRow.Ranged, 0, CardAbility.Medic, 3);
            AssertCard(scoiatael, "scoiatael_vrihedd_brigade_veteran", CombatRow.Close, 5, CardAbility.Agile, 2);
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

        private static bool IsSpecialOrWeather(CardDefinition card)
        {
            return card.Kind == CardKind.Special || card.Kind == CardKind.Weather;
        }

        private static void AssertNeutralAbility(GwentCardCatalogEntry[] cards, string id, CardAbility ability)
        {
            Assert.IsTrue(cards.Single(entry => entry.Card.Id == id).Card.HasAbility(ability));
        }
    }
}

using System.Collections.Generic;
using Gwent.Core;

namespace Gwent.Data
{
    public static class GwentAssetCatalog
    {
        public const string CardBorderPath = "Assets/Assets/border-gold.png";
        public const string CardBorderResource = "GwentArt/Frame/border-gold";

        private static readonly IReadOnlyDictionary<Faction, string> FactionBacks =
            new Dictionary<Faction, string>
            {
                { Faction.NorthernRealms, "Assets/Assets/default-northern_realms.png" },
                { Faction.Nilfgaard, "Assets/Assets/default-nilfgaard.png" },
                { Faction.Monsters, "Assets/Assets/default-monster.png" }
            };

        private static readonly IReadOnlyDictionary<Faction, string> FactionBackResources =
            new Dictionary<Faction, string>
            {
                { Faction.NorthernRealms, "GwentArt/Backs/default-northern_realms" },
                { Faction.Nilfgaard, "GwentArt/Backs/default-nilfgaard" },
                { Faction.Monsters, "GwentArt/Backs/default-monster" },
                { Faction.Scoiatael, "GwentArt/Backs/default-monster" },
                { Faction.Neutral, "GwentArt/Backs/default-northern_realms" }
            };

        private static readonly IReadOnlyDictionary<string, string> CardArt =
            new Dictionary<string, string>
            {
                { "neutral_geralt_of_rivia", "Assets/Assets/1016.jpg" },
                { "neutral_cirilla_fiona_elen_riannon", "Assets/Assets/1019.jpg" },
                { "neutral_yennefer_of_vengerberg", "Assets/Assets/1683.jpg" },
                { "neutral_triss_merigold", "Assets/Assets/1145.jpg" },
                { "neutral_vesemir", "Assets/Assets/1641.jpg" },
                { "monsters_ice_giant", "Assets/Assets/1149.jpg" },
                { "monsters_nekker", "Assets/Assets/1162.jpg" },
                { "monsters_leshen", "Assets/Assets/1386.jpg" },
                { "northern_realms_vernon_roche", "Assets/Assets/1532.jpg" },
                { "nilfgaard_letho_of_gulet", "Assets/Assets/1596.jpg" }
            };

        private static readonly IReadOnlyDictionary<string, string> CardArtResources =
            new Dictionary<string, string>
            {
                { "neutral_geralt_of_rivia", "GwentArt/Cards/1016" },
                { "neutral_cirilla_fiona_elen_riannon", "GwentArt/Cards/1019" },
                { "neutral_yennefer_of_vengerberg", "GwentArt/Cards/1683" },
                { "neutral_triss_merigold", "GwentArt/Cards/1145" },
                { "neutral_vesemir", "GwentArt/Cards/1641" },
                { "monsters_ice_giant", "GwentArt/Cards/1149" },
                { "monsters_nekker", "GwentArt/Cards/1162" },
                { "monsters_leshen", "GwentArt/Cards/1386" },
                { "northern_realms_ballista", "GwentArt/Cards/1127" },
                { "northern_realms_vernon_roche", "GwentArt/Cards/1532" },
                { "nilfgaard_letho_of_gulet", "GwentArt/Cards/1596" },
                { "nilfgaard_menno_coehoorn", "GwentArt/Cards/1334" },
                { "scoiatael_eithne", "GwentArt/Cards/1373" },
                { "neutral_scorch", "GwentArt/Cards/1151" }
            };

        private static readonly string[] NeutralFallbacks =
        {
            "GwentArt/Cards/1016",
            "GwentArt/Cards/1019",
            "GwentArt/Cards/1145",
            "GwentArt/Cards/1641",
            "GwentArt/Cards/1683"
        };

        private static readonly string[] NorthernRealmsFallbacks =
        {
            "GwentArt/Cards/1127",
            "GwentArt/Cards/1532",
            "GwentArt/Backs/default-northern_realms"
        };

        private static readonly string[] NilfgaardFallbacks =
        {
            "GwentArt/Cards/1334",
            "GwentArt/Cards/1596",
            "GwentArt/Backs/default-nilfgaard"
        };

        private static readonly string[] MonstersFallbacks =
        {
            "GwentArt/Cards/1149",
            "GwentArt/Cards/1162",
            "GwentArt/Cards/1386",
            "GwentArt/Backs/default-monster"
        };

        private static readonly string[] ScoiataelFallbacks =
        {
            "GwentArt/Cards/1373",
            "GwentArt/Cards/1151",
            "GwentArt/Backs/default-monster"
        };

        private static readonly string[] SpecialFallbacks =
        {
            "GwentArt/Cards/1151",
            "GwentArt/Cards/1373",
            "GwentArt/Frame/border-gold"
        };

        public static bool TryGetFactionBackPath(Faction faction, out string path)
        {
            return FactionBacks.TryGetValue(faction, out path);
        }

        public static bool TryGetCardArtPath(string cardId, out string path)
        {
            return CardArt.TryGetValue(cardId, out path);
        }

        public static string GetFactionBackResource(Faction faction)
        {
            if (FactionBackResources.TryGetValue(faction, out var resource))
            {
                return resource;
            }

            return FactionBackResources[Faction.NorthernRealms];
        }

        public static bool TryGetFactionBackResource(Faction faction, out string resource)
        {
            resource = GetFactionBackResource(faction);
            return !string.IsNullOrEmpty(resource);
        }

        public static string GetCardArtResource(CardDefinition card)
        {
            if (card == null)
            {
                return null;
            }

            if (TryGetCardArtResource(card.Id, out var resource))
            {
                return resource;
            }

            if (card.Kind == CardKind.Special || card.Kind == CardKind.Weather)
            {
                return PickFallback(card.Id, SpecialFallbacks);
            }

            switch (card.Faction)
            {
                case Faction.NorthernRealms:
                    return PickFallback(card.Id, NorthernRealmsFallbacks);
                case Faction.Nilfgaard:
                    return PickFallback(card.Id, NilfgaardFallbacks);
                case Faction.Monsters:
                    return PickFallback(card.Id, MonstersFallbacks);
                case Faction.Scoiatael:
                    return PickFallback(card.Id, ScoiataelFallbacks);
                default:
                    return PickFallback(card.Id, NeutralFallbacks);
            }
        }

        public static bool TryGetCardArtResource(string cardId, out string resource)
        {
            return CardArtResources.TryGetValue(cardId, out resource);
        }

        private static string PickFallback(string key, IReadOnlyList<string> resources)
        {
            if (resources == null || resources.Count == 0)
            {
                return CardBorderResource;
            }

            unchecked
            {
                var hash = 23;
                foreach (var character in key ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }

                return resources[(hash & 0x7fffffff) % resources.Count];
            }
        }
    }
}

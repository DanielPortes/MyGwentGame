using System.Collections.Generic;
using Gwent.Core;

namespace Gwent.Data
{
    public static class GwentAssetCatalog
    {
        public const string CardBorderPath = "Assets/Assets/border-gold.png";

        private static readonly IReadOnlyDictionary<Faction, string> FactionBacks =
            new Dictionary<Faction, string>
            {
                { Faction.NorthernRealms, "Assets/Assets/default-northern_realms.png" },
                { Faction.Nilfgaard, "Assets/Assets/default-nilfgaard.png" },
                { Faction.Monsters, "Assets/Assets/default-monster.png" }
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

        public static bool TryGetFactionBackPath(Faction faction, out string path)
        {
            return FactionBacks.TryGetValue(faction, out path);
        }

        public static bool TryGetCardArtPath(string cardId, out string path)
        {
            return CardArt.TryGetValue(cardId, out path);
        }
    }
}

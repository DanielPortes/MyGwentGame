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

        public static bool TryGetFactionBackPath(Faction faction, out string path)
        {
            return FactionBacks.TryGetValue(faction, out path);
        }
    }
}

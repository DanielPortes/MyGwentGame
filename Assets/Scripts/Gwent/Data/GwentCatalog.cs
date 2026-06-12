using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gwent.Core;

namespace Gwent.Data
{
    public enum GwentLeaderAbility
    {
        None,
        ClearWeather,
        ApplyHorn,
        ApplyWeather,
        ScorchRow,
        DrawCard,
        PeekOpponentHand,
        DisableMedics,
        DrawFromOpponentDiscard,
        RestoreFromOwnDiscard,
        DiscardTwoDrawOne,
        MoveAgileToBestRow,
        CancelOpponentLeader
    }

    public sealed class GwentLeaderDefinition
    {
        public GwentLeaderDefinition(
            string id,
            string name,
            Faction faction,
            string abilityText,
            GwentLeaderAbility ability,
            CombatRow? targetRow,
            WeatherEffect? weatherEffect)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Leader id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Leader name is required.", nameof(name));
            }

            Id = id;
            Name = name;
            Faction = faction;
            AbilityText = abilityText ?? string.Empty;
            Ability = ability;
            TargetRow = targetRow;
            WeatherEffect = weatherEffect;
        }

        public string Id { get; }

        public string Name { get; }

        public Faction Faction { get; }

        public string AbilityText { get; }

        public GwentLeaderAbility Ability { get; }

        public CombatRow? TargetRow { get; }

        public WeatherEffect? WeatherEffect { get; }

        public void ApplyTo(GwentMatch match, PlayerId owner)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            switch (Ability)
            {
                case GwentLeaderAbility.ClearWeather:
                    match.ApplyWeather(Gwent.Core.WeatherEffect.ClearWeather);
                    return;
                case GwentLeaderAbility.ApplyHorn:
                    match.ApplyHorn(owner, RequireTargetRow());
                    return;
                case GwentLeaderAbility.ApplyWeather:
                    match.ApplyWeather(RequireWeatherEffect());
                    return;
                case GwentLeaderAbility.ScorchRow:
                    match.PlayRowScorch(OpponentOf(owner), RequireTargetRow());
                    return;
                case GwentLeaderAbility.DrawCard:
                    match.DrawCards(owner, 1);
                    return;
                case GwentLeaderAbility.DisableMedics:
                    match.DisableMedics();
                    return;
                case GwentLeaderAbility.DrawFromOpponentDiscard:
                    match.MoveTopDiscardToHand(OpponentOf(owner), owner);
                    return;
                case GwentLeaderAbility.RestoreFromOwnDiscard:
                    match.MoveTopDiscardToHand(owner, owner);
                    return;
                case GwentLeaderAbility.DiscardTwoDrawOne:
                    match.DiscardFromHand(owner, 2);
                    match.DrawCards(owner, 1);
                    return;
                case GwentLeaderAbility.MoveAgileToBestRow:
                    match.MoveAgileCardsToBestRows(owner);
                    return;
                case GwentLeaderAbility.PeekOpponentHand:
                case GwentLeaderAbility.CancelOpponentLeader:
                    return;
                case GwentLeaderAbility.None:
                    return;
                default:
                    throw new InvalidOperationException("Unsupported leader ability.");
            }
        }

        private CombatRow RequireTargetRow()
        {
            if (!TargetRow.HasValue)
            {
                throw new InvalidOperationException("Leader ability requires a target row.");
            }

            return TargetRow.Value;
        }

        private WeatherEffect RequireWeatherEffect()
        {
            if (!WeatherEffect.HasValue)
            {
                throw new InvalidOperationException("Leader ability requires a weather effect.");
            }

            return WeatherEffect.Value;
        }

        private static PlayerId OpponentOf(PlayerId player)
        {
            return player == PlayerId.Player ? PlayerId.Opponent : PlayerId.Player;
        }
    }

    public sealed class GwentCardCatalogEntry
    {
        public GwentCardCatalogEntry(CardDefinition card, int copies, bool isBaseGame, string sourcePage)
        {
            if (copies <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(copies), "Card copies must be positive.");
            }

            Card = card ?? throw new ArgumentNullException(nameof(card));
            Copies = copies;
            IsBaseGame = isBaseGame;
            SourcePage = sourcePage ?? string.Empty;
        }

        public CardDefinition Card { get; }

        public int Copies { get; }

        public bool IsBaseGame { get; }

        public string SourcePage { get; }
    }

    public sealed class GwentDeckDefinition
    {
        public GwentDeckDefinition(
            Faction faction,
            IEnumerable<GwentLeaderDefinition> leaders,
            IEnumerable<GwentCardCatalogEntry> cards)
        {
            Faction = faction;
            Leaders = ReadOnlyList(leaders);
            Cards = ReadOnlyList(cards);
        }

        public Faction Faction { get; }

        public IReadOnlyList<GwentLeaderDefinition> Leaders { get; }

        public IReadOnlyList<GwentCardCatalogEntry> Cards { get; }

        private static IReadOnlyList<T> ReadOnlyList<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? throw new ArgumentNullException(nameof(values))).ToList());
        }
    }

    public static class GwentCatalog
    {
        private static readonly GwentDeckDefinition NorthernRealms = new GwentDeckDefinition(
            Faction.NorthernRealms,
            new[]
            {
                Leader("northern_realms_foltest_king_of_temeria", "Foltest: King of Temeria", Faction.NorthernRealms, "Pick an Impenetrable Fog card from your deck and play it instantly.", GwentLeaderAbility.ApplyWeather, weatherEffect: WeatherEffect.ImpenetrableFog),
                Leader("northern_realms_foltest_lord_commander", "Foltest: Lord Commander of the North", Faction.NorthernRealms, "Clear any weather effects in play.", GwentLeaderAbility.ClearWeather),
                Leader("northern_realms_foltest_son_of_medell", "Foltest: The Siegemaster", Faction.NorthernRealms, "Double the strength of all Siege units unless a Commander's Horn is already present.", GwentLeaderAbility.ApplyHorn, CombatRow.Siege),
                Leader("northern_realms_foltest_the_steel_forged", "Foltest: The Steel-Forged", Faction.NorthernRealms, "Destroy your enemy's strongest Siege units if their combined strength is 10 or more.", GwentLeaderAbility.ScorchRow, CombatRow.Siege)
            },
            new[]
            {
                Unit("northern_realms_ballista", "Ballista", Faction.NorthernRealms, CombatRow.Siege, 6, copies: 2),
                Unit("northern_realms_blue_stripes_commando", "Blue Stripes Commando", Faction.NorthernRealms, CombatRow.Close, 4, CardAbility.TightBond, 3),
                Unit("northern_realms_catapult", "Catapult", Faction.NorthernRealms, CombatRow.Siege, 8, CardAbility.TightBond, 2),
                Unit("northern_realms_crinfrid_reavers_dragon_hunter", "Crinfrid Reavers Dragon Hunter", Faction.NorthernRealms, CombatRow.Ranged, 5, CardAbility.TightBond, 3),
                Unit("northern_realms_dethmold", "Dethmold", Faction.NorthernRealms, CombatRow.Ranged, 6),
                Unit("northern_realms_dun_banner_medic", "Dun Banner Medic", Faction.NorthernRealms, CombatRow.Siege, 5, CardAbility.Medic),
                Unit("northern_realms_esterad_thyssen", "Esterad Thyssen", Faction.NorthernRealms, CombatRow.Close, 10, CardAbility.Hero),
                Unit("northern_realms_john_natalis", "John Natalis", Faction.NorthernRealms, CombatRow.Close, 10, CardAbility.Hero),
                Unit("northern_realms_kaedweni_siege_expert", "Kaedweni Siege Expert", Faction.NorthernRealms, CombatRow.Siege, 1, CardAbility.MoraleBoost, 3),
                Unit("northern_realms_keira_metz", "Keira Metz", Faction.NorthernRealms, CombatRow.Ranged, 5),
                Unit("northern_realms_philippa_eilhart", "Philippa Eilhart", Faction.NorthernRealms, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("northern_realms_poor_fucking_infantry", "Poor Fucking Infantry", Faction.NorthernRealms, CombatRow.Close, 1, CardAbility.TightBond, 3),
                Unit("northern_realms_prince_stennis", "Prince Stennis", Faction.NorthernRealms, CombatRow.Close, 5, CardAbility.Spy),
                Unit("northern_realms_redanian_foot_soldier", "Redanian Foot Soldier", Faction.NorthernRealms, CombatRow.Close, 1, copies: 2),
                Unit("northern_realms_sabrina_glevissig", "Sabrina Glevissig", Faction.NorthernRealms, CombatRow.Ranged, 4),
                Unit("northern_realms_sheldon_skaggs", "Sheldon Skaggs", Faction.NorthernRealms, CombatRow.Ranged, 4),
                Unit("northern_realms_siege_tower", "Siege Tower", Faction.NorthernRealms, CombatRow.Siege, 6),
                Unit("northern_realms_siegfried_of_denesle", "Siegfried of Denesle", Faction.NorthernRealms, CombatRow.Close, 5),
                Unit("northern_realms_sigismund_dijkstra", "Sigismund Dijkstra", Faction.NorthernRealms, CombatRow.Close, 4, CardAbility.Spy),
                Unit("northern_realms_sile_de_tansarville", "Sile de Tansarville", Faction.NorthernRealms, CombatRow.Ranged, 5),
                Unit("northern_realms_thaler", "Thaler", Faction.NorthernRealms, CombatRow.Siege, 1, CardAbility.Spy),
                Unit("northern_realms_trebuchet", "Trebuchet", Faction.NorthernRealms, CombatRow.Siege, 6, copies: 2),
                Unit("northern_realms_vernon_roche", "Vernon Roche", Faction.NorthernRealms, CombatRow.Close, 10, CardAbility.Hero),
                Unit("northern_realms_ves", "Ves", Faction.NorthernRealms, CombatRow.Close, 5),
                Unit("northern_realms_yarpen_zigrin", "Yarpen Zigrin", Faction.NorthernRealms, CombatRow.Close, 2)
            });

        private static readonly GwentDeckDefinition Nilfgaard = new GwentDeckDefinition(
            Faction.Nilfgaard,
            new[]
            {
                Leader("nilfgaard_emhyr_emperor_of_nilfgaard", "Emhyr: Emperor of Nilfgaard", Faction.Nilfgaard, "Pick a Torrential Rain card from your deck and play it instantly.", GwentLeaderAbility.ApplyWeather, weatherEffect: WeatherEffect.TorrentialRain),
                Leader("nilfgaard_emhyr_his_imperial_majesty", "Emhyr: His Imperial Majesty", Faction.Nilfgaard, "Look at three random cards in your opponent's hand.", GwentLeaderAbility.PeekOpponentHand),
                Leader("nilfgaard_emhyr_invader_of_the_north", "Emhyr: Invader of the North", Faction.Nilfgaard, "Abilities that restore cards from the discard pile are disabled.", GwentLeaderAbility.DisableMedics),
                Leader("nilfgaard_emhyr_the_relentless", "Emhyr: The Relentless", Faction.Nilfgaard, "Draw a card from your opponent's discard pile.", GwentLeaderAbility.DrawFromOpponentDiscard),
                Leader("nilfgaard_emhyr_the_white_flame", "Emhyr: The White Flame", Faction.Nilfgaard, "Cancel your opponent's leader ability.", GwentLeaderAbility.CancelOpponentLeader)
            },
            new[]
            {
                Unit("nilfgaard_albrich", "Albrich", Faction.Nilfgaard, CombatRow.Ranged, 2),
                Unit("nilfgaard_assire_var_anahid", "Assire var Anahid", Faction.Nilfgaard, CombatRow.Ranged, 6),
                Unit("nilfgaard_black_infantry_archer", "Black Infantry Archer", Faction.Nilfgaard, CombatRow.Ranged, 10, copies: 2),
                Unit("nilfgaard_cahir_mawr_dyffryn_aep_ceallach", "Cahir Mawr Dyffryn aep Ceallach", Faction.Nilfgaard, CombatRow.Close, 6),
                Unit("nilfgaard_cynthia", "Cynthia", Faction.Nilfgaard, CombatRow.Ranged, 4),
                Unit("nilfgaard_etolian_auxiliary_archers", "Etolian Auxiliary Archers", Faction.Nilfgaard, CombatRow.Ranged, 1, CardAbility.Medic, 2),
                Unit("nilfgaard_fringilla_vigo", "Fringilla Vigo", Faction.Nilfgaard, CombatRow.Ranged, 6),
                Unit("nilfgaard_heavy_zerrikanian_fire_scorpion", "Heavy Zerrikanian Fire Scorpion", Faction.Nilfgaard, CombatRow.Siege, 10),
                Unit("nilfgaard_impera_brigade_guard", "Impera Brigade Guard", Faction.Nilfgaard, CombatRow.Close, 3, CardAbility.TightBond, 4),
                Unit("nilfgaard_letho_of_gulet", "Letho of Gulet", Faction.Nilfgaard, CombatRow.Close, 10, CardAbility.Hero),
                Unit("nilfgaard_menno_coehoorn", "Menno Coehoorn", Faction.Nilfgaard, CombatRow.Close, 10, CardAbility.Hero | CardAbility.Medic),
                Unit("nilfgaard_morteisen", "Morteisen", Faction.Nilfgaard, CombatRow.Close, 3),
                Unit("nilfgaard_morvran_voorhis", "Morvran Voorhis", Faction.Nilfgaard, CombatRow.Siege, 10, CardAbility.Hero),
                Unit("nilfgaard_nausicaa_cavalry_rider", "Nausicaa Cavalry Rider", Faction.Nilfgaard, CombatRow.Close, 2, CardAbility.TightBond, 3),
                Unit("nilfgaard_puttkammer", "Puttkammer", Faction.Nilfgaard, CombatRow.Ranged, 3),
                Unit("nilfgaard_rainfarn", "Rainfarn", Faction.Nilfgaard, CombatRow.Close, 4),
                Unit("nilfgaard_renuald_aep_matsen", "Renuald aep Matsen", Faction.Nilfgaard, CombatRow.Ranged, 5),
                Unit("nilfgaard_rotten_mangonel", "Rotten Mangonel", Faction.Nilfgaard, CombatRow.Siege, 3),
                Unit("nilfgaard_shilard_fitz_oesterlen", "Shilard Fitz-Oesterlen", Faction.Nilfgaard, CombatRow.Close, 7, CardAbility.Spy),
                Unit("nilfgaard_siege_engineer", "Siege Engineer", Faction.Nilfgaard, CombatRow.Siege, 6),
                Unit("nilfgaard_siege_technician", "Siege Technician", Faction.Nilfgaard, CombatRow.Siege, 0, CardAbility.Medic),
                Unit("nilfgaard_stefan_skellen", "Stefan Skellen", Faction.Nilfgaard, CombatRow.Close, 9, CardAbility.Spy),
                Unit("nilfgaard_sweers", "Sweers", Faction.Nilfgaard, CombatRow.Ranged, 2),
                Unit("nilfgaard_tibor_eggebracht", "Tibor Eggebracht", Faction.Nilfgaard, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("nilfgaard_vanhemar", "Vanhemar", Faction.Nilfgaard, CombatRow.Ranged, 4),
                Unit("nilfgaard_vattier_de_rideaux", "Vattier de Rideaux", Faction.Nilfgaard, CombatRow.Close, 4, CardAbility.Spy),
                Unit("nilfgaard_vreemde", "Vreemde", Faction.Nilfgaard, CombatRow.Close, 2),
                Unit("nilfgaard_young_emissary", "Young Emissary", Faction.Nilfgaard, CombatRow.Close, 5, CardAbility.TightBond, 2),
                Unit("nilfgaard_zerrikanian_fire_scorpion", "Zerrikanian Fire Scorpion", Faction.Nilfgaard, CombatRow.Siege, 5)
            });

        private static readonly GwentDeckDefinition Monsters = new GwentDeckDefinition(
            Faction.Monsters,
            new[]
            {
                Leader("monsters_eredin_breacc_glas_the_treacherous", "Eredin Breacc Glas: The Treacherous", Faction.Monsters, "Double the strength of all Close Combat units unless a Commander's Horn is already present.", GwentLeaderAbility.ApplyHorn, CombatRow.Close),
                Leader("monsters_eredin_bringer_of_death", "Eredin: Bringer of Death", Faction.Monsters, "Discard two cards and draw one card of your choice from your deck.", GwentLeaderAbility.DiscardTwoDrawOne),
                Leader("monsters_eredin_commander_of_the_red_riders", "Eredin: Commander of the Red Riders", Faction.Monsters, "Pick any weather card from your deck and play it instantly.", GwentLeaderAbility.ApplyWeather, weatherEffect: WeatherEffect.BitingFrost),
                Leader("monsters_eredin_destroyer_of_worlds", "Eredin: Destroyer of Worlds", Faction.Monsters, "Restore one card from your discard pile to your hand.", GwentLeaderAbility.RestoreFromOwnDiscard),
                Leader("monsters_eredin_king_of_the_wild_hunt", "Eredin: King of the Wild Hunt", Faction.Monsters, "Cancel your opponent's leader ability.", GwentLeaderAbility.CancelOpponentLeader)
            },
            new[]
            {
                Unit("monsters_arachas", "Arachas", Faction.Monsters, CombatRow.Close, 4, CardAbility.Muster, 3, "monsters_arachas"),
                Unit("monsters_arachas_behemoth", "Arachas Behemoth", Faction.Monsters, CombatRow.Siege, 6, CardAbility.Muster, musterGroup: "monsters_arachas"),
                Unit("monsters_botchling", "Botchling", Faction.Monsters, CombatRow.Close, 4),
                Unit("monsters_celaeno_harpy", "Celaeno Harpy", Faction.Monsters, CombatRow.Close, 2, CardAbility.Agile),
                Unit("monsters_cockatrice", "Cockatrice", Faction.Monsters, CombatRow.Ranged, 2),
                Unit("monsters_crone_brewess", "Crone: Brewess", Faction.Monsters, CombatRow.Close, 6, CardAbility.Muster, musterGroup: "monsters_crones"),
                Unit("monsters_crone_weavess", "Crone: Weavess", Faction.Monsters, CombatRow.Close, 6, CardAbility.Muster, musterGroup: "monsters_crones"),
                Unit("monsters_crone_whispess", "Crone: Whispess", Faction.Monsters, CombatRow.Close, 6, CardAbility.Muster, musterGroup: "monsters_crones"),
                Unit("monsters_draug", "Draug", Faction.Monsters, CombatRow.Close, 10, CardAbility.Hero),
                Unit("monsters_earth_elemental", "Earth Elemental", Faction.Monsters, CombatRow.Siege, 6),
                Unit("monsters_endrega", "Endrega", Faction.Monsters, CombatRow.Ranged, 2),
                Unit("monsters_fiend", "Fiend", Faction.Monsters, CombatRow.Close, 6),
                Unit("monsters_fire_elemental", "Fire Elemental", Faction.Monsters, CombatRow.Siege, 6),
                Unit("monsters_foglet", "Foglet", Faction.Monsters, CombatRow.Close, 2),
                Unit("monsters_forktail", "Forktail", Faction.Monsters, CombatRow.Close, 5),
                Unit("monsters_frightener", "Frightener", Faction.Monsters, CombatRow.Close, 5),
                Unit("monsters_gargoyle", "Gargoyle", Faction.Monsters, CombatRow.Ranged, 2),
                Unit("monsters_ghoul", "Ghoul", Faction.Monsters, CombatRow.Close, 1, CardAbility.Muster, 3),
                Unit("monsters_grave_hag", "Grave Hag", Faction.Monsters, CombatRow.Ranged, 5),
                Unit("monsters_griffin", "Griffin", Faction.Monsters, CombatRow.Close, 5),
                Unit("monsters_harpy", "Harpy", Faction.Monsters, CombatRow.Close, 2, CardAbility.Agile),
                Unit("monsters_ice_giant", "Ice Giant", Faction.Monsters, CombatRow.Siege, 5),
                Unit("monsters_imlerith", "Imlerith", Faction.Monsters, CombatRow.Close, 10, CardAbility.Hero),
                Unit("monsters_kayran", "Kayran", Faction.Monsters, CombatRow.Close, 8, CardAbility.Hero | CardAbility.MoraleBoost | CardAbility.Agile),
                Unit("monsters_leshen", "Leshen", Faction.Monsters, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("monsters_nekker", "Nekker", Faction.Monsters, CombatRow.Close, 2, CardAbility.Muster, 3),
                Unit("monsters_plague_maiden", "Plague Maiden", Faction.Monsters, CombatRow.Close, 5),
                Unit("monsters_toad", "Toad", Faction.Monsters, CombatRow.Ranged, 7, CardAbility.ScorchRanged),
                Unit("monsters_vampire_bruxa", "Vampire: Bruxa", Faction.Monsters, CombatRow.Close, 4, CardAbility.Muster, musterGroup: "monsters_vampires"),
                Unit("monsters_vampire_ekimmara", "Vampire: Ekimmara", Faction.Monsters, CombatRow.Close, 4, CardAbility.Muster, musterGroup: "monsters_vampires"),
                Unit("monsters_vampire_fleder", "Vampire: Fleder", Faction.Monsters, CombatRow.Close, 4, CardAbility.Muster, musterGroup: "monsters_vampires"),
                Unit("monsters_vampire_garkain", "Vampire: Garkain", Faction.Monsters, CombatRow.Close, 4, CardAbility.Muster, musterGroup: "monsters_vampires"),
                Unit("monsters_vampire_katakan", "Vampire: Katakan", Faction.Monsters, CombatRow.Close, 5, CardAbility.Muster, musterGroup: "monsters_vampires"),
                Unit("monsters_werewolf", "Werewolf", Faction.Monsters, CombatRow.Close, 5),
                Unit("monsters_wyvern", "Wyvern", Faction.Monsters, CombatRow.Ranged, 2)
            });

        private static readonly GwentDeckDefinition Scoiatael = new GwentDeckDefinition(
            Faction.Scoiatael,
            new[]
            {
                Leader("scoiatael_francesca_daisy_of_the_valley", "Francesca Findabair: Daisy of the Valley", Faction.Scoiatael, "Draw an extra card at the beginning of the battle.", GwentLeaderAbility.DrawCard),
                Leader("scoiatael_francesca_hope_of_the_aen_seidhe", "Francesca Findabair: Hope of the Aen Seidhe", Faction.Scoiatael, "Move agile units to the row where they maximize their strength.", GwentLeaderAbility.MoveAgileToBestRow),
                Leader("scoiatael_francesca_pureblood_elf", "Francesca Findabair: Pureblood Elf", Faction.Scoiatael, "Pick a Biting Frost card from your deck and play it instantly.", GwentLeaderAbility.ApplyWeather, weatherEffect: WeatherEffect.BitingFrost),
                Leader("scoiatael_francesca_queen_of_dol_blathanna", "Francesca Findabair: Queen of Dol Blathanna", Faction.Scoiatael, "Destroy your enemy's strongest Close Combat units if their combined strength is 10 or more.", GwentLeaderAbility.ScorchRow, CombatRow.Close),
                Leader("scoiatael_francesca_the_beautiful", "Francesca Findabair: The Beautiful", Faction.Scoiatael, "Double the strength of all Ranged units unless a Commander's Horn is already present.", GwentLeaderAbility.ApplyHorn, CombatRow.Ranged)
            },
            new[]
            {
                Unit("scoiatael_barclay_els", "Barclay Els", Faction.Scoiatael, CombatRow.Close, 6, CardAbility.Agile),
                Unit("scoiatael_ciaran_aep_easnillien", "Ciaran aep Easnillien", Faction.Scoiatael, CombatRow.Close, 3, CardAbility.Agile),
                Unit("scoiatael_dennis_cranmer", "Dennis Cranmer", Faction.Scoiatael, CombatRow.Close, 6),
                Unit("scoiatael_dol_blathanna_archer", "Dol Blathanna Archer", Faction.Scoiatael, CombatRow.Ranged, 4),
                Unit("scoiatael_dol_blathanna_scout", "Dol Blathanna Scout", Faction.Scoiatael, CombatRow.Close, 6, CardAbility.Agile),
                Unit("scoiatael_dwarven_skirmisher", "Dwarven Skirmisher", Faction.Scoiatael, CombatRow.Close, 3, CardAbility.Muster, 3),
                Unit("scoiatael_eithne", "Eithne", Faction.Scoiatael, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("scoiatael_elven_skirmisher", "Elven Skirmisher", Faction.Scoiatael, CombatRow.Ranged, 2, CardAbility.Muster, 3),
                Unit("scoiatael_filavandrel_aen_fidhail", "Filavandrel aen Fidhail", Faction.Scoiatael, CombatRow.Close, 6, CardAbility.Agile),
                Unit("scoiatael_havekar_healer", "Havekar Healer", Faction.Scoiatael, CombatRow.Ranged, 0, CardAbility.Medic, 3),
                Unit("scoiatael_havekar_smuggler", "Havekar Smuggler", Faction.Scoiatael, CombatRow.Close, 5, CardAbility.Muster, 3),
                Unit("scoiatael_ida_emean_aep_sivney", "Ida Emean aep Sivney", Faction.Scoiatael, CombatRow.Ranged, 6),
                Unit("scoiatael_iorveth", "Iorveth", Faction.Scoiatael, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("scoiatael_isengrim_faoiltiarna", "Isengrim Faoiltiarna", Faction.Scoiatael, CombatRow.Close, 10, CardAbility.MoraleBoost | CardAbility.Hero),
                Unit("scoiatael_mahakaman_defender", "Mahakaman Defender", Faction.Scoiatael, CombatRow.Close, 5, copies: 5),
                Unit("scoiatael_milva", "Milva", Faction.Scoiatael, CombatRow.Ranged, 10, CardAbility.MoraleBoost),
                Unit("scoiatael_riordain", "Riordain", Faction.Scoiatael, CombatRow.Ranged, 1),
                Unit("scoiatael_saesenthessis", "Saesenthessis", Faction.Scoiatael, CombatRow.Ranged, 10, CardAbility.Hero),
                Unit("scoiatael_schirru", "Schirru", Faction.Scoiatael, CombatRow.Siege, 8, CardAbility.ScorchSiege),
                Unit("scoiatael_toruviel", "Toruviel", Faction.Scoiatael, CombatRow.Ranged, 2),
                Unit("scoiatael_vrihedd_brigade_recruit", "Vrihedd Brigade Recruit", Faction.Scoiatael, CombatRow.Ranged, 4),
                Unit("scoiatael_vrihedd_brigade_veteran", "Vrihedd Brigade Veteran", Faction.Scoiatael, CombatRow.Close, 5, CardAbility.Agile, 2),
                Unit("scoiatael_yaevinn", "Yaevinn", Faction.Scoiatael, CombatRow.Close, 6, CardAbility.Agile)
            });

        private static readonly IReadOnlyList<GwentCardCatalogEntry> NeutralCards = Array.AsReadOnly(new[]
        {
            Weather("neutral_biting_frost", "Biting Frost"),
            Weather("neutral_clear_weather", "Clear Weather"),
            Special("neutral_commanders_horn", "Commander's Horn", CardAbility.CommandersHorn),
            Unit("neutral_cirilla_fiona_elen_riannon", "Cirilla Fiona Elen Riannon", Faction.Neutral, CombatRow.Close, 15, CardAbility.Hero),
            Special("neutral_decoy", "Decoy", CardAbility.Decoy),
            Unit("neutral_dandelion", "Dandelion", Faction.Neutral, CombatRow.Close, 2, CardAbility.MoraleBoost),
            Unit("neutral_emiel_regis_rohellec_terzieff", "Emiel Regis Rohellec Terzieff", Faction.Neutral, CombatRow.Close, 5),
            Unit("neutral_geralt_of_rivia", "Geralt of Rivia", Faction.Neutral, CombatRow.Close, 15, CardAbility.Hero),
            Weather("neutral_impenetrable_fog", "Impenetrable Fog"),
            Unit("neutral_mysterious_elf", "Mysterious Elf", Faction.Neutral, CombatRow.Close, 0, CardAbility.Hero | CardAbility.Spy),
            Special("neutral_scorch", "Scorch", CardAbility.Scorch),
            Weather("neutral_torrential_rain", "Torrential Rain"),
            Unit("neutral_triss_merigold", "Triss Merigold", Faction.Neutral, CombatRow.Close, 7, CardAbility.Hero),
            Unit("neutral_vesemir", "Vesemir", Faction.Neutral, CombatRow.Close, 6),
            Unit("neutral_villentretenmerth", "Villentretenmerth", Faction.Neutral, CombatRow.Close, 7, CardAbility.ScorchClose),
            Unit("neutral_yennefer_of_vengerberg", "Yennefer of Vengerberg", Faction.Neutral, CombatRow.Ranged, 7, CardAbility.Medic | CardAbility.Hero),
            Unit("neutral_zoltan_chivay", "Zoltan Chivay", Faction.Neutral, CombatRow.Close, 5)
        });

        private static readonly IReadOnlyList<GwentCardCatalogEntry> DefaultNeutralCards = Array.AsReadOnly(new[]
        {
            Neutral("neutral_decoy"),
            Neutral("neutral_commanders_horn"),
            Neutral("neutral_scorch"),
            Neutral("neutral_biting_frost"),
            Neutral("neutral_impenetrable_fog"),
            Neutral("neutral_torrential_rain"),
            Neutral("neutral_clear_weather"),
            Neutral("neutral_geralt_of_rivia"),
            Neutral("neutral_cirilla_fiona_elen_riannon"),
            Neutral("neutral_yennefer_of_vengerberg"),
            Neutral("neutral_mysterious_elf"),
            Neutral("neutral_triss_merigold"),
            Neutral("neutral_vesemir"),
            Neutral("neutral_villentretenmerth"),
            Neutral("neutral_dandelion"),
            Neutral("neutral_zoltan_chivay")
        });

        public static GwentDeckDefinition GetDeck(Faction faction)
        {
            switch (faction)
            {
                case Faction.NorthernRealms:
                    return NorthernRealms;
                case Faction.Nilfgaard:
                    return Nilfgaard;
                case Faction.Monsters:
                    return Monsters;
                case Faction.Scoiatael:
                    return Scoiatael;
                default:
                    throw new ArgumentOutOfRangeException(nameof(faction), "Neutral is not a faction deck.");
            }
        }

        public static IReadOnlyList<GwentCardCatalogEntry> GetNeutralCards()
        {
            return NeutralCards;
        }

        public static IReadOnlyList<CardDefinition> CreateDefaultDeck(Faction faction)
        {
            var cards = new List<CardDefinition>();
            AddExpanded(cards, GetDeck(faction).Cards);
            AddExpanded(cards, DefaultNeutralCards);
            return cards.AsReadOnly();
        }

        private static void AddExpanded(ICollection<CardDefinition> target, IEnumerable<GwentCardCatalogEntry> entries)
        {
            foreach (var entry in entries)
            {
                for (var i = 0; i < entry.Copies; i++)
                {
                    target.Add(entry.Card);
                }
            }
        }

        private static GwentCardCatalogEntry Neutral(string id)
        {
            return NeutralCards.Single(card => card.Card.Id == id);
        }

        private static GwentLeaderDefinition Leader(
            string id,
            string name,
            Faction faction,
            string abilityText,
            GwentLeaderAbility ability = GwentLeaderAbility.None,
            CombatRow? targetRow = null,
            WeatherEffect? weatherEffect = null)
        {
            return new GwentLeaderDefinition(id, name, faction, abilityText, ability, targetRow, weatherEffect);
        }

        private static GwentCardCatalogEntry Unit(
            string id,
            string name,
            Faction faction,
            CombatRow row,
            int strength,
            CardAbility ability = CardAbility.None,
            int copies = 1,
            string musterGroup = null)
        {
            return new GwentCardCatalogEntry(
                new CardDefinition(id, name, faction, CardKind.Unit, row, strength, musterGroup, ability),
                copies,
                true,
                SourcePageFor(faction));
        }

        private static GwentCardCatalogEntry Special(string id, string name, CardAbility ability = CardAbility.None, int copies = 1)
        {
            return new GwentCardCatalogEntry(
                new CardDefinition(id, name, Faction.Neutral, CardKind.Special, CombatRow.Close, 0, ability),
                copies,
                true,
                "Neutral Gwent cards");
        }

        private static GwentCardCatalogEntry Weather(string id, string name, int copies = 1)
        {
            return new GwentCardCatalogEntry(
                new CardDefinition(id, name, Faction.Neutral, CardKind.Weather, CombatRow.Close, 0),
                copies,
                true,
                "Neutral Gwent cards");
        }

        private static string SourcePageFor(Faction faction)
        {
            switch (faction)
            {
                case Faction.NorthernRealms:
                    return "Northern Realms Gwent deck";
                case Faction.Nilfgaard:
                    return "Nilfgaardian Empire Gwent deck";
                case Faction.Monsters:
                    return "Monsters Gwent deck";
                case Faction.Scoiatael:
                    return "Scoia'tael Gwent deck";
                default:
                    return "Neutral Gwent cards";
            }
        }
    }
}

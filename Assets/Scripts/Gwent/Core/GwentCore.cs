using System;
using System.Collections.Generic;
using System.Linq;

namespace Gwent.Core
{
    public enum PlayerId
    {
        Player,
        Opponent
    }

    public enum Faction
    {
        Neutral,
        NorthernRealms,
        Nilfgaard,
        Monsters,
        Scoiatael
    }

    public enum CombatRow
    {
        Close,
        Ranged,
        Siege
    }

    public enum CardKind
    {
        Unit,
        Special,
        Weather,
        Leader
    }

    [Flags]
    public enum CardAbility
    {
        None = 0,
        Hero = 1 << 0,
        TightBond = 1 << 1,
        MoraleBoost = 1 << 2,
        Spy = 1 << 3,
        Medic = 1 << 4,
        Muster = 1 << 5,
        Agile = 1 << 6
    }

    public enum WeatherEffect
    {
        BitingFrost,
        ImpenetrableFog,
        TorrentialRain,
        ClearWeather
    }

    public sealed class CardDefinition
    {
        public CardDefinition(
            string id,
            string name,
            Faction faction,
            CardKind kind,
            CombatRow row,
            int strength,
            params CardAbility[] abilities)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Card name is required.", nameof(name));
            }

            Id = id;
            Name = name;
            Faction = faction;
            Kind = kind;
            Row = row;
            Strength = strength;
            Abilities = MergeAbilities(abilities);
        }

        public string Id { get; }

        public string Name { get; }

        public Faction Faction { get; }

        public CardKind Kind { get; }

        public CombatRow Row { get; }

        public int Strength { get; }

        public CardAbility Abilities { get; }

        public bool HasAbility(CardAbility ability)
        {
            return (Abilities & ability) == ability;
        }

        private static CardAbility MergeAbilities(IEnumerable<CardAbility> abilities)
        {
            var result = CardAbility.None;
            if (abilities == null)
            {
                return result;
            }

            foreach (var ability in abilities)
            {
                result |= ability;
            }

            return result;
        }
    }

    public sealed class DeterministicRandom
    {
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Maximum must be greater than minimum.");
            }

            return minInclusive;
        }
    }

    public sealed class GwentMatch
    {
        private readonly DeterministicRandom _random;
        private readonly Dictionary<PlayerId, PlayerState> _players;
        private readonly HashSet<WeatherEffect> _weather = new HashSet<WeatherEffect>();

        public GwentMatch(Faction playerFaction, Faction opponentFaction, DeterministicRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _players = new Dictionary<PlayerId, PlayerState>
            {
                { PlayerId.Player, new PlayerState(playerFaction) },
                { PlayerId.Opponent, new PlayerState(opponentFaction) }
            };
        }

        public void SetDeck(PlayerId player, params CardDefinition[] cards)
        {
            var state = State(player);
            state.Deck.Clear();
            state.Deck.AddRange(cards ?? Array.Empty<CardDefinition>());
        }

        public void SetHand(PlayerId player, params CardDefinition[] cards)
        {
            var state = State(player);
            state.Hand.Clear();
            state.Hand.AddRange(cards ?? Array.Empty<CardDefinition>());
        }

        public IReadOnlyList<CardDefinition> GetDeck(PlayerId player)
        {
            return State(player).Deck.AsReadOnly();
        }

        public IReadOnlyList<CardDefinition> GetHand(PlayerId player)
        {
            return State(player).Hand.AsReadOnly();
        }

        public IEnumerable<CardDefinition> GetBoardCards(PlayerId player)
        {
            return State(player).Rows.Values.SelectMany(row => row);
        }

        public int GetRoundWins(PlayerId player)
        {
            return State(player).RoundWins;
        }

        public void PlayUnit(PlayerId player, CardDefinition card, CombatRow? row = null)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.Kind != CardKind.Unit)
            {
                throw new InvalidOperationException("Only unit cards can be placed on combat rows.");
            }

            var selectedRow = row ?? card.Row;
            if (card.HasAbility(CardAbility.Agile) && selectedRow == CombatRow.Siege)
            {
                throw new InvalidOperationException("Agile cards can only be placed on Close or Ranged rows.");
            }

            State(player).Rows[selectedRow].Add(card);
        }

        public void PlayFromHand(PlayerId player, int handIndex, CombatRow row)
        {
            var state = State(player);
            if (handIndex < 0 || handIndex >= state.Hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handIndex));
            }

            var card = state.Hand[handIndex];
            state.Hand.RemoveAt(handIndex);
            PlayUnit(player, card, row);

            if (card.HasAbility(CardAbility.Muster))
            {
                PlayMusterMatches(player, card, row);
            }
        }

        public void PlaySpy(PlayerId owner, CardDefinition spy)
        {
            if (spy == null)
            {
                throw new ArgumentNullException(nameof(spy));
            }

            if (!spy.HasAbility(CardAbility.Spy))
            {
                throw new InvalidOperationException("Only spy cards can be played as spies.");
            }

            PlayUnit(OpponentOf(owner), spy, spy.Row);
            Draw(owner, 2);
        }

        public void ApplyWeather(WeatherEffect effect)
        {
            if (effect == WeatherEffect.ClearWeather)
            {
                _weather.Clear();
                return;
            }

            _weather.Add(effect);
        }

        public void ApplyHorn(PlayerId player, CombatRow row)
        {
            State(player).HornRows.Add(row);
        }

        public void PlayScorch(PlayerId player)
        {
            var strongest = GetAllScorchTargets()
                .Select(entry => new ScorchTarget(entry.Owner, entry.Row, entry.Card, GetModifiedCardStrength(entry.Owner, entry.Row, entry.Card)))
                .Where(target => target.Score > 0)
                .ToList();

            if (strongest.Count == 0)
            {
                return;
            }

            var highestScore = strongest.Max(target => target.Score);
            foreach (var target in strongest.Where(target => target.Score == highestScore))
            {
                State(target.Owner).Rows[target.Row].Remove(target.Card);
                State(target.Owner).Discard.Add(target.Card);
            }
        }

        public int GetRowScore(PlayerId player, CombatRow row)
        {
            return State(player).Rows[row].Sum(card => GetModifiedCardStrength(player, row, card));
        }

        public int GetTotalScore(PlayerId player)
        {
            return EnumValues<CombatRow>().Sum(row => GetRowScore(player, row));
        }

        public void Pass(PlayerId player)
        {
            State(player).Passed = true;
            if (State(PlayerId.Player).Passed && State(PlayerId.Opponent).Passed)
            {
                ResolveRound();
            }
        }

        private void ResolveRound()
        {
            var playerScore = GetTotalScore(PlayerId.Player);
            var opponentScore = GetTotalScore(PlayerId.Opponent);

            if (playerScore > opponentScore)
            {
                AwardRound(PlayerId.Player);
            }
            else if (opponentScore > playerScore)
            {
                AwardRound(PlayerId.Opponent);
            }
            else
            {
                ResolveDrawnRound();
            }

            ClearBoardForNextRound();

            foreach (var state in _players.Values)
            {
                state.Passed = false;
                state.HornRows.Clear();
            }

            _weather.Clear();
        }

        private void ResolveDrawnRound()
        {
            var playerIsNilfgaard = State(PlayerId.Player).Faction == Faction.Nilfgaard;
            var opponentIsNilfgaard = State(PlayerId.Opponent).Faction == Faction.Nilfgaard;

            if (playerIsNilfgaard && !opponentIsNilfgaard)
            {
                AwardRound(PlayerId.Player);
            }
            else if (opponentIsNilfgaard && !playerIsNilfgaard)
            {
                AwardRound(PlayerId.Opponent);
            }
        }

        private void AwardRound(PlayerId winner)
        {
            var state = State(winner);
            state.RoundWins++;

            if (state.Faction == Faction.NorthernRealms)
            {
                Draw(winner, 1);
            }
        }

        private void ClearBoardForNextRound()
        {
            foreach (var pair in _players)
            {
                var state = pair.Value;
                var carryOver = GetMonsterCarryOver(state);
                var keptCarryOver = false;

                foreach (var row in EnumValues<CombatRow>())
                {
                    foreach (var card in state.Rows[row])
                    {
                        if (!keptCarryOver && ReferenceEquals(card, carryOver.Card))
                        {
                            keptCarryOver = true;
                        }
                        else
                        {
                            state.Discard.Add(card);
                        }
                    }

                    state.Rows[row].Clear();
                }

                if (carryOver.Card != null)
                {
                    state.Rows[carryOver.Row].Add(carryOver.Card);
                }
            }
        }

        private BoardEntry GetMonsterCarryOver(PlayerState state)
        {
            if (state.Faction != Faction.Monsters)
            {
                return BoardEntry.Empty;
            }

            var eligible = EnumValues<CombatRow>()
                .SelectMany(row => state.Rows[row].Select(card => new BoardEntry(PlayerId.Player, row, card)))
                .Where(entry => entry.Card.Kind == CardKind.Unit && !entry.Card.HasAbility(CardAbility.Hero))
                .ToList();

            if (eligible.Count == 0)
            {
                return BoardEntry.Empty;
            }

            return eligible[_random.Range(0, eligible.Count)];
        }

        private void Draw(PlayerId player, int amount)
        {
            var state = State(player);
            for (var i = 0; i < amount && state.Deck.Count > 0; i++)
            {
                var card = state.Deck[0];
                state.Deck.RemoveAt(0);
                state.Hand.Add(card);
            }
        }

        private void PlayMusterMatches(PlayerId player, CardDefinition source, CombatRow row)
        {
            var state = State(player);
            PlayMatchingCardsFromPile(state.Hand, source.Id, card => PlayUnit(player, card, row));
            PlayMatchingCardsFromPile(state.Deck, source.Id, card => PlayUnit(player, card, row));
        }

        private static void PlayMatchingCardsFromPile(List<CardDefinition> pile, string cardId, Action<CardDefinition> play)
        {
            for (var i = pile.Count - 1; i >= 0; i--)
            {
                if (pile[i].Id != cardId)
                {
                    continue;
                }

                var card = pile[i];
                pile.RemoveAt(i);
                play(card);
            }
        }

        private int GetModifiedCardStrength(PlayerId player, CombatRow row, CardDefinition card)
        {
            if (card.HasAbility(CardAbility.Hero))
            {
                return card.Strength;
            }

            var rowCards = State(player).Rows[row];
            var score = IsWeathered(row) ? 1 : card.Strength;

            if (card.HasAbility(CardAbility.TightBond))
            {
                score *= rowCards.Count(other => !other.HasAbility(CardAbility.Hero)
                                                 && other.HasAbility(CardAbility.TightBond)
                                                 && other.Id == card.Id);
            }

            if (State(player).HornRows.Contains(row))
            {
                score *= 2;
            }

            score += CountMoraleBoosts(rowCards, card);
            return score;
        }

        private static int CountMoraleBoosts(IEnumerable<CardDefinition> rowCards, CardDefinition target)
        {
            var skippedTarget = false;
            var boosts = 0;

            foreach (var card in rowCards)
            {
                if (!skippedTarget && ReferenceEquals(card, target))
                {
                    skippedTarget = true;
                    continue;
                }

                if (!card.HasAbility(CardAbility.Hero) && card.HasAbility(CardAbility.MoraleBoost))
                {
                    boosts++;
                }
            }

            return boosts;
        }

        private bool IsWeathered(CombatRow row)
        {
            return row == CombatRow.Close && _weather.Contains(WeatherEffect.BitingFrost)
                   || row == CombatRow.Ranged && _weather.Contains(WeatherEffect.ImpenetrableFog)
                   || row == CombatRow.Siege && _weather.Contains(WeatherEffect.TorrentialRain);
        }

        private IEnumerable<BoardEntry> GetAllScorchTargets()
        {
            foreach (var player in EnumValues<PlayerId>())
            {
                foreach (var row in EnumValues<CombatRow>())
                {
                    foreach (var card in State(player).Rows[row])
                    {
                        if (card.Kind == CardKind.Unit && !card.HasAbility(CardAbility.Hero))
                        {
                            yield return new BoardEntry(player, row, card);
                        }
                    }
                }
            }
        }

        private PlayerState State(PlayerId player)
        {
            return _players[player];
        }

        private static PlayerId OpponentOf(PlayerId player)
        {
            return player == PlayerId.Player ? PlayerId.Opponent : PlayerId.Player;
        }

        private static IEnumerable<T> EnumValues<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        private sealed class PlayerState
        {
            public PlayerState(Faction faction)
            {
                Faction = faction;
                foreach (var row in EnumValues<CombatRow>())
                {
                    Rows[row] = new List<CardDefinition>();
                }
            }

            public Faction Faction { get; }

            public List<CardDefinition> Deck { get; } = new List<CardDefinition>();

            public List<CardDefinition> Hand { get; } = new List<CardDefinition>();

            public List<CardDefinition> Discard { get; } = new List<CardDefinition>();

            public Dictionary<CombatRow, List<CardDefinition>> Rows { get; } =
                new Dictionary<CombatRow, List<CardDefinition>>();

            public HashSet<CombatRow> HornRows { get; } = new HashSet<CombatRow>();

            public bool Passed { get; set; }

            public int RoundWins { get; set; }
        }

        private readonly struct BoardEntry
        {
            public BoardEntry(PlayerId owner, CombatRow row, CardDefinition card)
            {
                Owner = owner;
                Row = row;
                Card = card;
            }

            public PlayerId Owner { get; }

            public CombatRow Row { get; }

            public CardDefinition Card { get; }

            public static BoardEntry Empty
            {
                get { return new BoardEntry(PlayerId.Player, CombatRow.Close, null); }
            }
        }

        private readonly struct ScorchTarget
        {
            public ScorchTarget(PlayerId owner, CombatRow row, CardDefinition card, int score)
            {
                Owner = owner;
                Row = row;
                Card = card;
                Score = score;
            }

            public PlayerId Owner { get; }

            public CombatRow Row { get; }

            public CardDefinition Card { get; }

            public int Score { get; }
        }
    }
}

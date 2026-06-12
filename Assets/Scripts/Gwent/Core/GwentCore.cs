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
        Agile = 1 << 6,
        Decoy = 1 << 7,
        Scorch = 1 << 8,
        CommandersHorn = 1 << 9,
        ScorchClose = 1 << 10,
        ScorchRanged = 1 << 11,
        ScorchSiege = 1 << 12
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
            : this(id, name, faction, kind, row, strength, null, abilities)
        {
        }

        public CardDefinition(
            string id,
            string name,
            Faction faction,
            CardKind kind,
            CombatRow row,
            int strength,
            string musterGroup,
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
            MusterGroup = string.IsNullOrWhiteSpace(musterGroup) ? id : musterGroup;
            Abilities = MergeAbilities(abilities);
        }

        public string Id { get; }

        public string Name { get; }

        public Faction Faction { get; }

        public CardKind Kind { get; }

        public CombatRow Row { get; }

        public int Strength { get; }

        public string MusterGroup { get; }

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
        private const int OpeningHandSize = 10;

        private readonly DeterministicRandom _random;
        private readonly Dictionary<PlayerId, PlayerState> _players;
        private readonly HashSet<WeatherEffect> _weather = new HashSet<WeatherEffect>();
        private bool _medicsDisabled;

        public GwentMatch(Faction playerFaction, Faction opponentFaction, DeterministicRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _players = new Dictionary<PlayerId, PlayerState>
            {
                { PlayerId.Player, new PlayerState(playerFaction) },
                { PlayerId.Opponent, new PlayerState(opponentFaction) }
            };
        }

        public PlayerId CurrentTurn { get; private set; } = PlayerId.Player;

        public bool IsMatchComplete { get; private set; }

        public PlayerId? MatchWinner { get; private set; }

        public void StartMatch(PlayerId startingPlayer)
        {
            Draw(PlayerId.Player, OpeningHandSize);
            Draw(PlayerId.Opponent, OpeningHandSize);
            CurrentTurn = startingPlayer;
        }

        public void ChooseStartingPlayer(PlayerId chooser, PlayerId startingPlayer)
        {
            if (State(chooser).Faction != Faction.Scoiatael)
            {
                throw new InvalidOperationException("Only Scoia'tael can choose who starts the match.");
            }

            CurrentTurn = startingPlayer;
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

        public void SetDiscard(PlayerId player, params CardDefinition[] cards)
        {
            var state = State(player);
            state.Discard.Clear();
            state.Discard.AddRange(cards ?? Array.Empty<CardDefinition>());
        }

        public IReadOnlyList<CardDefinition> GetDeck(PlayerId player)
        {
            return State(player).Deck.AsReadOnly();
        }

        public IReadOnlyList<CardDefinition> GetHand(PlayerId player)
        {
            return State(player).Hand.AsReadOnly();
        }

        public IReadOnlyList<CardDefinition> GetDiscard(PlayerId player)
        {
            return State(player).Discard.AsReadOnly();
        }

        public int DrawCards(PlayerId player, int amount)
        {
            var before = State(player).Hand.Count;
            Draw(player, amount);
            return State(player).Hand.Count - before;
        }

        public int DiscardFromHand(PlayerId player, int amount)
        {
            var state = State(player);
            var discarded = 0;
            while (discarded < amount && state.Hand.Count > 0)
            {
                var card = state.Hand[0];
                state.Hand.RemoveAt(0);
                state.Discard.Add(card);
                discarded++;
            }

            return discarded;
        }

        public CardDefinition MoveTopDiscardToHand(PlayerId discardOwner, PlayerId handOwner)
        {
            var discard = State(discardOwner).Discard;
            if (discard.Count == 0)
            {
                return null;
            }

            var card = discard[0];
            discard.RemoveAt(0);
            State(handOwner).Hand.Add(card);
            return card;
        }

        public void DisableMedics()
        {
            _medicsDisabled = true;
        }

        public void MoveAgileCardsToBestRows(PlayerId player)
        {
            foreach (var row in new[] { CombatRow.Close, CombatRow.Ranged })
            {
                foreach (var card in State(player).Rows[row].Where(card => card.HasAbility(CardAbility.Agile)).ToList())
                {
                    var targetRow = GetRowScore(player, CombatRow.Ranged) > GetRowScore(player, CombatRow.Close)
                        ? CombatRow.Ranged
                        : CombatRow.Close;
                    if (targetRow == row)
                    {
                        continue;
                    }

                    State(player).Rows[row].Remove(card);
                    State(player).Rows[targetRow].Add(card);
                }
            }
        }

        public IEnumerable<CardDefinition> GetBoardCards(PlayerId player)
        {
            return State(player).Rows.Values.SelectMany(row => row);
        }

        public IReadOnlyList<CardDefinition> GetRowCards(PlayerId player, CombatRow row)
        {
            return State(player).Rows[row].AsReadOnly();
        }

        public int GetRoundWins(PlayerId player)
        {
            return State(player).RoundWins;
        }

        public int GetRoundLosses(PlayerId player)
        {
            return State(player).RoundLosses;
        }

        public CardDefinition Mulligan(PlayerId player, int handIndex)
        {
            var state = State(player);
            if (state.MulligansUsed >= 2)
            {
                throw new InvalidOperationException("Each player can mulligan at most two cards.");
            }

            if (handIndex < 0 || handIndex >= state.Hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handIndex));
            }

            if (state.Deck.Count == 0)
            {
                throw new InvalidOperationException("Cannot mulligan without cards in the deck.");
            }

            var removed = state.Hand[handIndex];
            var replacement = state.Deck[0];
            state.Deck.RemoveAt(0);
            state.Hand[handIndex] = replacement;
            state.Deck.Add(removed);
            state.MulligansUsed++;
            return replacement;
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
            if (card.HasAbility(CardAbility.Agile))
            {
                if (selectedRow == CombatRow.Siege)
                {
                    throw new InvalidOperationException("Agile cards can only be placed on Close or Ranged rows.");
                }
            }
            else if (selectedRow != card.Row)
            {
                throw new InvalidOperationException("Non-agile cards must be placed on their printed row.");
            }

            State(player).Rows[selectedRow].Add(card);
        }

        public void PlayFromHand(PlayerId player, int handIndex, CombatRow row)
        {
            EnsureCanAct(player);

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

            ApplyRowScorchAbility(player, card);

            AdvanceTurnAfterAction(player);
        }

        public void PlayCardFromHand(PlayerId player, int handIndex, CombatRow? row = null, CardDefinition target = null)
        {
            EnsureCanAct(player);

            var state = State(player);
            if (handIndex < 0 || handIndex >= state.Hand.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handIndex));
            }

            var card = state.Hand[handIndex];
            state.Hand.RemoveAt(handIndex);

            try
            {
                PlayCard(player, card, row, target);
            }
            catch
            {
                state.Hand.Insert(handIndex, card);
                throw;
            }

            AdvanceTurnAfterAction(player);
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

        public void PlayMedic(PlayerId player, CardDefinition medic, CardDefinition restored)
        {
            if (medic == null)
            {
                throw new ArgumentNullException(nameof(medic));
            }

            if (restored == null)
            {
                throw new ArgumentNullException(nameof(restored));
            }

            if (!medic.HasAbility(CardAbility.Medic))
            {
                throw new InvalidOperationException("Only medic cards can restore units from discard.");
            }

            if (_medicsDisabled)
            {
                throw new InvalidOperationException("Medic abilities are disabled.");
            }

            if (!IsMedicTarget(restored))
            {
                throw new InvalidOperationException("Medic can only restore non-hero unit cards.");
            }

            var state = State(player);
            if (!state.Discard.Remove(restored))
            {
                throw new InvalidOperationException("Medic target must be in the player's discard pile.");
            }

            PlayUnit(player, medic, medic.Row);
            PlayUnit(player, restored, restored.Row);
        }

        public void PlayDecoy(PlayerId player, CardDefinition decoy, CardDefinition target)
        {
            if (decoy == null)
            {
                throw new ArgumentNullException(nameof(decoy));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (decoy.Kind != CardKind.Special || !decoy.HasAbility(CardAbility.Decoy))
            {
                throw new InvalidOperationException("Only Decoy special cards can be played as decoys.");
            }

            if (target.HasAbility(CardAbility.Hero))
            {
                throw new InvalidOperationException("Decoy cannot target hero cards.");
            }

            var entry = FindBoardCard(player, target);
            if (entry.Card == null)
            {
                throw new InvalidOperationException("Decoy target must be on the player's board.");
            }

            var state = State(player);
            state.Rows[entry.Row].Remove(target);
            state.Hand.Add(target);
            state.Discard.Add(decoy);
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
                .Where(target => target.Score >= 10)
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

        public void PlayRowScorch(PlayerId targetOwner, CombatRow row)
        {
            if (GetRowScore(targetOwner, row) < 10)
            {
                return;
            }

            var targets = State(targetOwner).Rows[row]
                .Where(card => card.Kind == CardKind.Unit && !card.HasAbility(CardAbility.Hero))
                .Select(card => new ScorchTarget(targetOwner, row, card, GetModifiedCardStrength(targetOwner, row, card)))
                .Where(target => target.Score > 0)
                .ToList();

            if (targets.Count == 0)
            {
                return;
            }

            var highestScore = targets.Max(target => target.Score);
            foreach (var target in targets.Where(target => target.Score == highestScore))
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
            EnsureCanAct(player);

            State(player).Passed = true;
            if (State(PlayerId.Player).Passed && State(PlayerId.Opponent).Passed)
            {
                ResolveRound();
                return;
            }

            var opponent = OpponentOf(player);
            if (!State(opponent).Passed)
            {
                CurrentTurn = opponent;
            }
        }

        private void ResolveRound()
        {
            var playerScore = GetTotalScore(PlayerId.Player);
            var opponentScore = GetTotalScore(PlayerId.Opponent);

            PlayerId? winner = null;

            if (playerScore > opponentScore)
            {
                winner = PlayerId.Player;
                AwardRound(PlayerId.Player, PlayerId.Opponent);
            }
            else if (opponentScore > playerScore)
            {
                winner = PlayerId.Opponent;
                AwardRound(PlayerId.Opponent, PlayerId.Player);
            }
            else
            {
                winner = ResolveDrawnRound();
            }

            ClearBoardForNextRound();

            foreach (var state in _players.Values)
            {
                state.Passed = false;
                state.HornRows.Clear();
            }

            _weather.Clear();
            CurrentTurn = winner.HasValue ? OpponentOf(winner.Value) : PlayerId.Player;
        }

        private PlayerId? ResolveDrawnRound()
        {
            var playerIsNilfgaard = State(PlayerId.Player).Faction == Faction.Nilfgaard;
            var opponentIsNilfgaard = State(PlayerId.Opponent).Faction == Faction.Nilfgaard;

            if (playerIsNilfgaard && !opponentIsNilfgaard)
            {
                AwardRound(PlayerId.Player, PlayerId.Opponent);
                return PlayerId.Player;
            }

            if (opponentIsNilfgaard && !playerIsNilfgaard)
            {
                AwardRound(PlayerId.Opponent, PlayerId.Player);
                return PlayerId.Opponent;
            }

            State(PlayerId.Player).RoundLosses++;
            State(PlayerId.Opponent).RoundLosses++;
            CheckMatchCompletion();
            return null;
        }

        private void AwardRound(PlayerId winner, PlayerId loser)
        {
            var state = State(winner);
            state.RoundWins++;
            State(loser).RoundLosses++;

            if (state.Faction == Faction.NorthernRealms)
            {
                Draw(winner, 1);
            }

            CheckMatchCompletion();
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

        private void EnsureCanAct(PlayerId player)
        {
            if (IsMatchComplete)
            {
                throw new InvalidOperationException("Cannot act after the match is complete.");
            }

            if (State(player).Passed)
            {
                throw new InvalidOperationException("A player who has passed cannot act again this round.");
            }

            if (CurrentTurn != player)
            {
                throw new InvalidOperationException("It is not this player's turn.");
            }
        }

        private void AdvanceTurnAfterAction(PlayerId player)
        {
            var opponent = OpponentOf(player);
            CurrentTurn = State(opponent).Passed ? player : opponent;
        }

        private static bool IsMedicTarget(CardDefinition card)
        {
            return card.Kind == CardKind.Unit && !card.HasAbility(CardAbility.Hero);
        }

        private void PlayCard(PlayerId player, CardDefinition card, CombatRow? row, CardDefinition target)
        {
            switch (card.Kind)
            {
                case CardKind.Unit:
                    PlayUnitCard(player, card, row, target);
                    return;
                case CardKind.Special:
                    PlaySpecialCard(player, card, row, target);
                    return;
                case CardKind.Weather:
                    ApplyWeather(GetWeatherEffect(card));
                    State(player).Discard.Add(card);
                    return;
                default:
                    throw new InvalidOperationException("This card kind cannot be played from hand.");
            }
        }

        private void PlayUnitCard(PlayerId player, CardDefinition card, CombatRow? row, CardDefinition target)
        {
            if (card.HasAbility(CardAbility.Spy))
            {
                PlaySpy(player, card);
                return;
            }

            if (card.HasAbility(CardAbility.Medic) && target != null)
            {
                PlayMedic(player, card, target);
                return;
            }

            var selectedRow = row ?? card.Row;
            PlayUnit(player, card, selectedRow);

            if (card.HasAbility(CardAbility.Muster))
            {
                PlayMusterMatches(player, card, selectedRow);
            }

            ApplyRowScorchAbility(player, card);
        }

        private void PlaySpecialCard(PlayerId player, CardDefinition card, CombatRow? row, CardDefinition target)
        {
            if (card.HasAbility(CardAbility.Decoy))
            {
                PlayDecoy(player, card, target);
                return;
            }

            if (card.HasAbility(CardAbility.CommandersHorn))
            {
                if (!row.HasValue)
                {
                    throw new InvalidOperationException("Commander's Horn requires a target row.");
                }

                ApplyHorn(player, row.Value);
                State(player).Discard.Add(card);
                return;
            }

            if (card.HasAbility(CardAbility.Scorch))
            {
                PlayScorch(player);
                State(player).Discard.Add(card);
                return;
            }

            State(player).Discard.Add(card);
        }

        private static WeatherEffect GetWeatherEffect(CardDefinition card)
        {
            var id = card.Id.ToLowerInvariant();
            var name = card.Name.ToLowerInvariant();
            if (id.Contains("biting_frost") || name == "biting frost")
            {
                return WeatherEffect.BitingFrost;
            }

            if (id.Contains("impenetrable_fog") || name == "impenetrable fog")
            {
                return WeatherEffect.ImpenetrableFog;
            }

            if (id.Contains("torrential_rain") || name == "torrential rain")
            {
                return WeatherEffect.TorrentialRain;
            }

            if (id.Contains("clear_weather") || name == "clear weather")
            {
                return WeatherEffect.ClearWeather;
            }

            throw new InvalidOperationException("Unknown weather card.");
        }

        private void PlayMusterMatches(PlayerId player, CardDefinition source, CombatRow row)
        {
            var state = State(player);
            PlayMatchingCardsFromPile(state.Hand, source.MusterGroup, card => PlayUnit(player, card, row));
            PlayMatchingCardsFromPile(state.Deck, source.MusterGroup, card => PlayUnit(player, card, row));
        }

        private static void PlayMatchingCardsFromPile(List<CardDefinition> pile, string musterGroup, Action<CardDefinition> play)
        {
            for (var i = pile.Count - 1; i >= 0; i--)
            {
                if (!pile[i].HasAbility(CardAbility.Muster) || pile[i].MusterGroup != musterGroup)
                {
                    continue;
                }

                var card = pile[i];
                pile.RemoveAt(i);
                play(card);
            }
        }

        private void ApplyRowScorchAbility(PlayerId player, CardDefinition card)
        {
            var opponent = OpponentOf(player);
            if (card.HasAbility(CardAbility.ScorchClose))
            {
                PlayRowScorch(opponent, CombatRow.Close);
            }

            if (card.HasAbility(CardAbility.ScorchRanged))
            {
                PlayRowScorch(opponent, CombatRow.Ranged);
            }

            if (card.HasAbility(CardAbility.ScorchSiege))
            {
                PlayRowScorch(opponent, CombatRow.Siege);
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

        private BoardEntry FindBoardCard(PlayerId player, CardDefinition target)
        {
            foreach (var row in EnumValues<CombatRow>())
            {
                foreach (var card in State(player).Rows[row])
                {
                    if (ReferenceEquals(card, target))
                    {
                        return new BoardEntry(player, row, card);
                    }
                }
            }

            return BoardEntry.Empty;
        }

        private void CheckMatchCompletion()
        {
            foreach (var player in EnumValues<PlayerId>())
            {
                if (State(player).RoundWins >= 2)
                {
                    IsMatchComplete = true;
                    MatchWinner = player;
                    return;
                }
            }

            var playerLost = State(PlayerId.Player).RoundLosses >= 2;
            var opponentLost = State(PlayerId.Opponent).RoundLosses >= 2;
            if (playerLost && opponentLost)
            {
                IsMatchComplete = true;
                MatchWinner = null;
            }
            else if (playerLost)
            {
                IsMatchComplete = true;
                MatchWinner = PlayerId.Opponent;
            }
            else if (opponentLost)
            {
                IsMatchComplete = true;
                MatchWinner = PlayerId.Player;
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

            public int RoundLosses { get; set; }

            public int MulligansUsed { get; set; }
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

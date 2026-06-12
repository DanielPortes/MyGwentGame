using System;
using System.Collections;
using System.Linq;
using Gwent.Core;
using Gwent.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentGameController : MonoBehaviour
    {
        private GwentMatch _match;
        private Canvas _canvas;
        private Transform _uiRoot;
        private GwentDeckSelectionView _selectionView;
        private int _selectedHandIndex = -1;
        private bool _initializedForTests;
        private bool _mulligansLocked;
        private bool _playerLeaderUsed;

        public Faction PlayerFaction { get; private set; }

        public Faction OpponentFaction { get; private set; }

        public GwentBoardView Board { get; private set; }

        public int PlayerHandCount
        {
            get { return _match == null ? 0 : _match.GetHand(PlayerId.Player).Count; }
        }

        public int OpponentHandCount
        {
            get { return _match == null ? 0 : _match.GetHand(PlayerId.Opponent).Count; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapGameScene()
        {
            if (SceneManager.GetActiveScene().name != "Game")
            {
                return;
            }

            if (FindFirstObjectByType<GwentGameController>() != null)
            {
                return;
            }

            DisableLegacySceneObjects();

            EnsureEventSystem();
            new GameObject("Witcher 3 Gwent Runtime").AddComponent<GwentGameController>();
        }

        private void Start()
        {
            if (!_initializedForTests)
            {
                EnsureEventSystem();
                ShowDeckSelection();
            }
        }

        public void InitializeForTests(Faction playerFaction, Faction opponentFaction)
        {
            _initializedForTests = true;
            BeginMatch(playerFaction, opponentFaction);
        }

        public void ShowDeckSelection()
        {
            EnsureCanvas();
            ClearChildren(_uiRoot);
            Board = null;
            _selectionView = GwentViewFactory.CreateDeckSelection(_uiRoot);

            foreach (var factionButton in _selectionView.FactionButtons)
            {
                var faction = factionButton.Faction;
                factionButton.Button.onClick.AddListener(() => BeginMatch(faction, DefaultOpponentFor(faction)));
            }
        }

        public void BeginMatch(Faction playerFaction, Faction opponentFaction)
        {
            EnsureCanvas();
            ClearChildren(_uiRoot);

            PlayerFaction = playerFaction;
            OpponentFaction = opponentFaction;
            _selectedHandIndex = -1;
            _mulligansLocked = false;
            _playerLeaderUsed = false;
            _selectionView = null;

            _match = new GwentMatch(playerFaction, opponentFaction, new DeterministicRandom());
            _match.SetDeck(PlayerId.Player, GwentCatalog.CreateDefaultDeck(playerFaction).ToArray());
            _match.SetDeck(PlayerId.Opponent, GwentCatalog.CreateDefaultDeck(opponentFaction).ToArray());
            _match.StartMatch(PlayerId.Player);

            Board = GwentViewFactory.CreateBoard(_uiRoot);
            WireBoardControls();
            Render();
        }

        private void WireBoardControls()
        {
            Board.PassButton.onClick.AddListener(OnPassClicked);
            Board.LeaderButton.onClick.AddListener(OnLeaderClicked);
            Board.MulliganButton.onClick.AddListener(OnMulliganClicked);

            foreach (var rowView in Board.Rows.Where(row => row.Owner == PlayerId.Player))
            {
                var row = rowView.Row;
                rowView.Button.onClick.AddListener(() => PlaySelectedCardToRow(row));
            }
        }

        private void OnPassClicked()
        {
            if (!CanPlayerAct())
            {
                return;
            }

            TryPlayerAction(() => _match.Pass(PlayerId.Player));
        }

        private void OnLeaderClicked()
        {
            if (_playerLeaderUsed || !CanPlayerAct())
            {
                Board.StatusText.text = "Líder indisponível";
                return;
            }

            var leader = GwentCatalog.GetDeck(PlayerFaction).Leaders.FirstOrDefault();
            if (leader == null)
            {
                return;
            }

            leader.ApplyTo(_match, PlayerId.Player);
            _playerLeaderUsed = true;
            Render();
            Board.CardZoomText.text = leader.Name + Environment.NewLine + leader.AbilityText;
            Board.StatusText.text = "Líder usado";
        }

        private void OnMulliganClicked()
        {
            if (_mulligansLocked)
            {
                Board.StatusText.text = "Mulligan encerrado";
                return;
            }

            if (_selectedHandIndex < 0 || _selectedHandIndex >= _match.GetHand(PlayerId.Player).Count)
            {
                Board.StatusText.text = "Escolha uma carta para o mulligan";
                return;
            }

            try
            {
                _match.Mulligan(PlayerId.Player, _selectedHandIndex);
                _selectedHandIndex = -1;
                Render();
            }
            catch (Exception exception)
            {
                Board.StatusText.text = exception.Message;
            }
        }

        private void OnPlayerCardClicked(int handIndex)
        {
            if (!CanPlayerAct() || handIndex < 0 || handIndex >= _match.GetHand(PlayerId.Player).Count)
            {
                return;
            }

            var card = _match.GetHand(PlayerId.Player)[handIndex];
            _selectedHandIndex = handIndex;
            UpdateCardZoom(card);

            if (RequiresRowChoice(card))
            {
                Board.StatusText.text = "Escolha uma fileira";
                return;
            }

            TryPlayPlayerCard(handIndex, null);
        }

        private void PlaySelectedCardToRow(CombatRow row)
        {
            if (_selectedHandIndex < 0)
            {
                Board.StatusText.text = "Escolha uma carta";
                return;
            }

            TryPlayPlayerCard(_selectedHandIndex, row);
        }

        private void TryPlayPlayerCard(int handIndex, CombatRow? row)
        {
            TryPlayerAction(() =>
            {
                var card = _match.GetHand(PlayerId.Player)[handIndex];
                _match.PlayCardFromHand(PlayerId.Player, handIndex, row, ResolveAutoTarget(PlayerId.Player, card));
                _selectedHandIndex = -1;
            });
        }

        private void TryPlayerAction(Action action)
        {
            try
            {
                action();
                _mulligansLocked = true;
                Render();

                if (!_initializedForTests && _match != null && _match.CurrentTurn == PlayerId.Opponent && !_match.IsMatchComplete)
                {
                    StartCoroutine(PlayOpponentTurn());
                }
            }
            catch (Exception exception)
            {
                if (Board != null)
                {
                    Board.StatusText.text = exception.Message;
                }
            }
        }

        private IEnumerator PlayOpponentTurn()
        {
            yield return new WaitForSeconds(0.6f);

            while (_match != null && !_match.IsMatchComplete && _match.CurrentTurn == PlayerId.Opponent)
            {
                PlayOpponentAction();
                Render();
                yield return new WaitForSeconds(0.4f);
            }
        }

        private void PlayOpponentAction()
        {
            if (_match.GetHand(PlayerId.Opponent).Count == 0 || ShouldOpponentPass())
            {
                _match.Pass(PlayerId.Opponent);
                return;
            }

            var hand = _match.GetHand(PlayerId.Opponent);
            for (var i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                try
                {
                    _match.PlayCardFromHand(PlayerId.Opponent, i, ChooseOpponentRow(card), ResolveAutoTarget(PlayerId.Opponent, card));
                    return;
                }
                catch (InvalidOperationException)
                {
                }
            }

            _match.Pass(PlayerId.Opponent);
        }

        private bool ShouldOpponentPass()
        {
            return _match.GetHand(PlayerId.Opponent).Count <= 3
                   && _match.GetTotalScore(PlayerId.Opponent) > _match.GetTotalScore(PlayerId.Player);
        }

        private CombatRow? ChooseOpponentRow(CardDefinition card)
        {
            if (card.Kind == CardKind.Special && card.HasAbility(CardAbility.CommandersHorn))
            {
                return BestOccupiedRow(PlayerId.Opponent);
            }

            if (card.Kind != CardKind.Unit)
            {
                return null;
            }

            return card.HasAbility(CardAbility.Agile) ? CombatRow.Ranged : card.Row;
        }

        private CombatRow BestOccupiedRow(PlayerId owner)
        {
            return new[] { CombatRow.Close, CombatRow.Ranged, CombatRow.Siege }
                .OrderByDescending(row => _match.GetRowScore(owner, row))
                .First();
        }

        private CardDefinition ResolveAutoTarget(PlayerId owner, CardDefinition card)
        {
            if (card.HasAbility(CardAbility.Decoy))
            {
                return _match.GetBoardCards(owner)
                    .FirstOrDefault(candidate => candidate.Kind == CardKind.Unit && !candidate.HasAbility(CardAbility.Hero));
            }

            if (card.HasAbility(CardAbility.Medic))
            {
                return _match.GetDiscard(owner)
                    .FirstOrDefault(candidate => candidate.Kind == CardKind.Unit && !candidate.HasAbility(CardAbility.Hero));
            }

            return null;
        }

        private bool RequiresRowChoice(CardDefinition card)
        {
            if (card.Kind == CardKind.Unit && !card.HasAbility(CardAbility.Spy))
            {
                return true;
            }

            return card.Kind == CardKind.Special && card.HasAbility(CardAbility.CommandersHorn);
        }

        private bool CanPlayerAct()
        {
            return _match != null && !_match.IsMatchComplete && _match.CurrentTurn == PlayerId.Player;
        }

        private void Render()
        {
            ClearChildren(Board.PlayerHand);
            ClearChildren(Board.OpponentHand);

            foreach (var rowView in Board.Rows)
            {
                ClearChildren(rowView.CardsRoot);
                rowView.SetWeatherActive(false);
                foreach (var card in _match.GetRowCards(rowView.Owner, rowView.Row))
                {
                    var cardView = GwentViewFactory.CreateCard(rowView.CardsRoot, card, true);
                    cardView.CompleteEntranceAnimation();
                }

                rowView.SetScore(_match.GetRowScore(rowView.Owner, rowView.Row));
            }

            var playerHand = _match.GetHand(PlayerId.Player);
            for (var i = 0; i < playerHand.Count; i++)
            {
                var handIndex = i;
                var cardView = GwentViewFactory.CreateCard(Board.PlayerHand, playerHand[i], true);
                cardView.Button.onClick.AddListener(() => OnPlayerCardClicked(handIndex));
                if (handIndex == _selectedHandIndex)
                {
                    cardView.SetHighlighted(true);
                }
            }

            foreach (var card in _match.GetHand(PlayerId.Opponent))
            {
                var cardView = GwentViewFactory.CreateCard(Board.OpponentHand, card, false);
                cardView.CompleteEntranceAnimation();
            }

            Board.PlayerScoreText.text = "Você: " + _match.GetTotalScore(PlayerId.Player);
            Board.OpponentScoreText.text = "Oponente: " + _match.GetTotalScore(PlayerId.Opponent);
            Board.PlayerRoundsText.text = "Rodadas: " + _match.GetRoundWins(PlayerId.Player);
            Board.OpponentRoundsText.text = "Rodadas OP: " + _match.GetRoundWins(PlayerId.Opponent);
            Board.PlayerDeckCount.text = "Baralho: " + _match.GetDeck(PlayerId.Player).Count;
            Board.OpponentDeckCount.text = "Baralho OP: " + _match.GetDeck(PlayerId.Opponent).Count;
            Board.PlayerDiscardCount.text = "Descarte: " + _match.GetDiscard(PlayerId.Player).Count;
            Board.OpponentDiscardCount.text = "Descarte OP: " + _match.GetDiscard(PlayerId.Opponent).Count;
            Board.WeatherText.text = "Clima: ativo no tabuleiro";
            Board.RoundBannerText.text = StatusText();
            Board.StatusText.text = StatusText();
            Board.PassButton.interactable = CanPlayerAct();
            Board.MulliganButton.interactable = !_mulligansLocked;
            if (_selectedHandIndex < 0 || _selectedHandIndex >= playerHand.Count)
            {
                Board.CardZoomText.text = "Selecione uma carta";
            }
            else
            {
                UpdateCardZoom(playerHand[_selectedHandIndex]);
            }
        }

        private void UpdateCardZoom(CardDefinition card)
        {
            Board.CardZoomText.text = card.Name + Environment.NewLine + "Força: " + card.Strength + Environment.NewLine + "Tipo: " + card.Kind;
        }

        private string StatusText()
        {
            if (_match.IsMatchComplete)
            {
                if (!_match.MatchWinner.HasValue)
                {
                    return "Empate";
                }

                return _match.MatchWinner.Value == PlayerId.Player ? "Você venceu" : "Você perdeu";
            }

            return _match.CurrentTurn == PlayerId.Player ? "Sua vez" : "Vez do oponente";
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                var canvasObject = new GameObject("Gwent Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                _canvas = canvasObject.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            _uiRoot = _canvas.transform;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void DisableLegacySceneObjects()
        {
            var legacyManager = GameObject.Find("GameManager");
            if (legacyManager != null)
            {
                legacyManager.SetActive(false);
            }

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.gameObject.SetActive(false);
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static Faction DefaultOpponentFor(Faction playerFaction)
        {
            return playerFaction == Faction.Nilfgaard ? Faction.NorthernRealms : Faction.Nilfgaard;
        }
    }
}

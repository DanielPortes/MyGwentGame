using System;
using System.Collections.Generic;
using Gwent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentBoardView
    {
        private readonly List<GwentRowView> _rows;

        public GwentBoardView(
            GameObject root,
            IEnumerable<GwentRowView> rows,
            Transform playerHand,
            Transform opponentHand,
            Button passButton,
            Button leaderButton,
            Button mulliganButton,
            Text statusText,
            Text cardZoomText,
            Text playerScoreText,
            Text opponentScoreText,
            Text playerRoundsText,
            Text opponentRoundsText,
            Text playerDeckCount,
            Text opponentDeckCount,
            Text playerDiscardCount,
            Text opponentDiscardCount,
            Text weatherText,
            Text roundBannerText)
        {
            Root = root;
            _rows = new List<GwentRowView>(rows);
            PlayerHand = playerHand;
            OpponentHand = opponentHand;
            PassButton = passButton;
            LeaderButton = leaderButton;
            MulliganButton = mulliganButton;
            StatusText = statusText;
            CardZoomText = cardZoomText;
            PlayerScoreText = playerScoreText;
            OpponentScoreText = opponentScoreText;
            PlayerRoundsText = playerRoundsText;
            OpponentRoundsText = opponentRoundsText;
            PlayerDeckCount = playerDeckCount;
            OpponentDeckCount = opponentDeckCount;
            PlayerDiscardCount = playerDiscardCount;
            OpponentDiscardCount = opponentDiscardCount;
            WeatherText = weatherText;
            RoundBannerText = roundBannerText;
        }

        public GameObject Root { get; }

        public IReadOnlyList<GwentRowView> Rows
        {
            get { return _rows; }
        }

        public Transform PlayerHand { get; }

        public Transform OpponentHand { get; }

        public Button PassButton { get; }

        public Button LeaderButton { get; }

        public Button MulliganButton { get; }

        public Text StatusText { get; }

        public Text CardZoomText { get; }

        public Text PlayerScoreText { get; }

        public Text OpponentScoreText { get; }

        public Text PlayerRoundsText { get; }

        public Text OpponentRoundsText { get; }

        public Text PlayerDeckCount { get; }

        public Text OpponentDeckCount { get; }

        public Text PlayerDiscardCount { get; }

        public Text OpponentDiscardCount { get; }

        public Text WeatherText { get; }

        public Text RoundBannerText { get; }
    }

    public static class GwentViewFactory
    {
        private static readonly Color BackgroundColor = new Color(0.05f, 0.06f, 0.055f, 0.98f);
        private static readonly Color PanelColor = new Color(0.13f, 0.12f, 0.10f, 0.94f);
        private static readonly Color OpponentRowColor = new Color(0.16f, 0.10f, 0.11f, 0.88f);
        private static readonly Color PlayerRowColor = new Color(0.09f, 0.14f, 0.12f, 0.88f);
        private static readonly Color AccentGold = new Color(0.78f, 0.58f, 0.26f, 1f);
        private static Font _font;

        public static GwentDeckSelectionView CreateDeckSelection(Transform parent)
        {
            var root = CreatePanel("Gwent Deck Selection", parent, BackgroundColor);
            Stretch(root.GetComponent<RectTransform>());

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(72, 72, 72, 72);
            layout.spacing = 18;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            var title = CreateText("Escolha sua facção", root.transform, 34, TextAnchor.MiddleCenter, AccentGold);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;

            var selection = root.AddComponent<GwentDeckSelectionView>();
            selection.Configure(title);

            foreach (var faction in new[] { Faction.NorthernRealms, Faction.Nilfgaard, Faction.Monsters, Faction.Scoiatael })
            {
                var button = CreateButton(FactionName(faction), root.transform, 24, new Color(0.20f, 0.19f, 0.15f, 1f));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                selection.AddFactionButton(new GwentFactionButton(faction, button, button.GetComponentInChildren<Text>()));
            }

            var note = CreateText("The Witcher 3 Gwent", root.transform, 18, TextAnchor.MiddleCenter, new Color(0.78f, 0.80f, 0.72f, 1f));
            note.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            return selection;
        }

        public static GwentBoardView CreateBoard(Transform parent)
        {
            var root = CreatePanel("Gwent Board", parent, BackgroundColor);
            Stretch(root.GetComponent<RectTransform>());

            var opponentHand = CreateZone("Opponent Hand", root.transform, 76);
            AnchorTop(opponentHand.GetComponent<RectTransform>(), 18, 18, 8, 76);

            var middle = CreatePanel("Board Middle", root.transform, new Color(0f, 0f, 0f, 0f));
            StretchBetween(middle.GetComponent<RectTransform>(), 18, 18, 90, 204);
            var middleElement = middle.gameObject.AddComponent<LayoutElement>();
            middleElement.preferredHeight = 420;
            var middleLayout = middle.gameObject.AddComponent<HorizontalLayoutGroup>();
            middleLayout.spacing = 10;
            middleLayout.childControlWidth = true;
            middleLayout.childControlHeight = true;
            middleLayout.childForceExpandWidth = false;

            var statusPanel = CreatePanel("Status Panel", middle.transform, PanelColor);
            statusPanel.gameObject.AddComponent<LayoutElement>().preferredWidth = 232;
            var statusLayout = statusPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            statusLayout.padding = new RectOffset(10, 10, 10, 10);
            statusLayout.spacing = 8;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = false;

            var statusText = CreateText("Escolha uma facção", statusPanel.transform, 18, TextAnchor.MiddleCenter, AccentGold);
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            var roundBanner = CreateText("Rodada 1", statusPanel.transform, 21, TextAnchor.MiddleCenter, AccentGold);
            roundBanner.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            var cardZoom = CreateText("Selecione uma carta", statusPanel.transform, 14, TextAnchor.UpperLeft, new Color(0.90f, 0.88f, 0.76f, 1f));
            cardZoom.gameObject.AddComponent<LayoutElement>().preferredHeight = 92;
            var opponentScore = CreateText("Oponente: 0", statusPanel.transform, 18, TextAnchor.MiddleLeft, Color.white);
            var playerScore = CreateText("Você: 0", statusPanel.transform, 18, TextAnchor.MiddleLeft, Color.white);
            var opponentRounds = CreateText("Rodadas OP: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var playerRounds = CreateText("Rodadas: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var opponentDeck = CreateText("Baralho OP: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var playerDeck = CreateText("Baralho: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var opponentDiscard = CreateText("Descarte OP: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var playerDiscard = CreateText("Descarte: 0", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var weather = CreateText("Clima: limpo", statusPanel.transform, 15, TextAnchor.MiddleLeft, Color.white);
            var leaderButton = CreateButton("Líder", statusPanel.transform, 18, new Color(0.28f, 0.25f, 0.14f, 1f));
            leaderButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            var mulliganButton = CreateButton("Mulligan", statusPanel.transform, 18, new Color(0.15f, 0.28f, 0.30f, 1f));
            mulliganButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            var passButton = CreateButton("Passar", statusPanel.transform, 20, new Color(0.45f, 0.17f, 0.12f, 1f));
            passButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;

            var rowsPanel = CreatePanel("Combat Rows", middle.transform, new Color(0.02f, 0.025f, 0.022f, 0.72f));
            rowsPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var rowsLayout = rowsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.padding = new RectOffset(8, 8, 8, 8);
            rowsLayout.spacing = 6;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;

            var rows = new List<GwentRowView>
            {
                CreateRow(rowsPanel.transform, PlayerId.Opponent, CombatRow.Siege),
                CreateRow(rowsPanel.transform, PlayerId.Opponent, CombatRow.Ranged),
                CreateRow(rowsPanel.transform, PlayerId.Opponent, CombatRow.Close),
                CreateRow(rowsPanel.transform, PlayerId.Player, CombatRow.Close),
                CreateRow(rowsPanel.transform, PlayerId.Player, CombatRow.Ranged),
                CreateRow(rowsPanel.transform, PlayerId.Player, CombatRow.Siege)
            };

            var playerHand = CreateZone("Player Hand", root.transform, 190);
            AnchorBottom(playerHand.GetComponent<RectTransform>(), 18, 18, 8, 190);

            return new GwentBoardView(
                root,
                rows,
                playerHand,
                opponentHand,
                passButton,
                leaderButton,
                mulliganButton,
                statusText,
                cardZoom,
                playerScore,
                opponentScore,
                playerRounds,
                opponentRounds,
                playerDeck,
                opponentDeck,
                playerDiscard,
                opponentDiscard,
                weather,
                roundBanner);
        }

        public static GwentCardView CreateCard(Transform parent, CardDefinition card, bool faceUp)
        {
            return CreateCard(parent, card, faceUp, false);
        }

        public static GwentCardView CreateCard(Transform parent, CardDefinition card, bool faceUp, bool compact)
        {
            var root = CreatePanel(card.Id, parent, new Color(0.22f, 0.18f, 0.12f, 1f));
            root.AddComponent<CanvasGroup>();
            var rect = root.GetComponent<RectTransform>();
            var cardWidth = compact ? 56 : 112;
            var cardHeight = compact ? 56 : 168;
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            var element = root.AddComponent<LayoutElement>();
            element.preferredWidth = cardWidth;
            element.preferredHeight = cardHeight;
            element.minWidth = compact ? 52 : 108;
            element.minHeight = compact ? 52 : 156;

            var image = root.GetComponent<Image>();
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = compact ? new RectOffset(3, 3, 3, 3) : new RectOffset(6, 6, 6, 6);
            layout.spacing = compact ? 1 : 2;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var name = CreateText(string.Empty, root.transform, compact ? 9 : 13, TextAnchor.UpperCenter, Color.white);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = compact ? 6 : 8;
            name.resizeTextMaxSize = compact ? 9 : 13;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 0 : 24;
            name.gameObject.SetActive(!compact);

            var strength = CreateText(string.Empty, root.transform, compact ? 13 : 24, TextAnchor.MiddleCenter, AccentGold);
            strength.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 13 : 24;

            var artObject = CreatePanel("Art", root.transform, new Color(1f, 1f, 1f, 0f));
            var artImage = artObject.GetComponent<Image>();
            artImage.raycastTarget = false;
            artObject.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 36 : 70;

            var row = CreateText(string.Empty, root.transform, compact ? 7 : 10, TextAnchor.MiddleCenter, Color.white);
            row.resizeTextForBestFit = true;
            row.resizeTextMinSize = 7;
            row.resizeTextMaxSize = compact ? 7 : 10;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 0 : 16;
            row.gameObject.SetActive(!compact);

            var ability = CreateText(string.Empty, root.transform, compact ? 7 : 10, TextAnchor.MiddleCenter, new Color(0.86f, 0.88f, 0.78f, 1f));
            ability.resizeTextForBestFit = true;
            ability.resizeTextMinSize = 7;
            ability.resizeTextMaxSize = compact ? 7 : 10;
            ability.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 0 : 14;
            ability.gameObject.SetActive(!compact);

            var view = root.AddComponent<GwentCardView>();
            view.Configure(name, strength, row, ability, button, image, artImage);
            view.Bind(card, faceUp);
            return view;
        }

        private static GwentRowView CreateRow(Transform parent, PlayerId owner, CombatRow row)
        {
            var rowObject = CreatePanel($"{owner} {row}", parent, owner == PlayerId.Player ? PlayerRowColor : OpponentRowColor);
            var rowElement = rowObject.gameObject.AddComponent<LayoutElement>();
            rowElement.minHeight = 48;
            rowElement.preferredHeight = 60;
            rowElement.flexibleHeight = 1;
            var image = rowObject.GetComponent<Image>();
            var button = rowObject.AddComponent<Button>();
            button.targetGraphic = image;

            var layout = rowObject.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            var label = CreateText(RowName(owner, row), rowObject.transform, 15, TextAnchor.MiddleLeft, Color.white);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 124;

            var score = CreateText("0", rowObject.transform, 26, TextAnchor.MiddleCenter, AccentGold);
            score.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;

            var cardsRoot = CreatePanel("Cards", rowObject.transform, new Color(0f, 0f, 0f, 0f));
            cardsRoot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var cardLayout = cardsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 4;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childAlignment = TextAnchor.MiddleLeft;

            var overlay = CreatePanel("Weather Overlay", rowObject.transform, new Color(0.66f, 0.80f, 0.92f, 0f));
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            overlay.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch(overlay.GetComponent<RectTransform>());

            var view = rowObject.AddComponent<GwentRowView>();
            view.Configure(owner, row, cardsRoot.transform, score, label, button, overlayImage);
            return view;
        }

        private static Transform CreateZone(string name, Transform parent, float height)
        {
            var zone = CreatePanel(name, parent, PanelColor);
            zone.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var layout = zone.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 5;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return zone.transform;
        }

        private static Button CreateButton(string text, Transform parent, int size, Color color)
        {
            var buttonObject = CreatePanel(text, parent, color);
            var image = buttonObject.GetComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = CreateText(text, buttonObject.transform, size, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.GetComponent<RectTransform>());
            return button;
        }

        private static Text CreateText(string text, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var gameObject = new GameObject("Text", typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var label = gameObject.AddComponent<Text>();
            label.text = text;
            label.font = Font();
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchBetween(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AnchorTop(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void AnchorBottom(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static Font Font()
        {
            if (_font != null)
            {
                return _font;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return _font;
        }

        private static string FactionName(Faction faction)
        {
            switch (faction)
            {
                case Faction.NorthernRealms:
                    return "Reinos do Norte";
                case Faction.Nilfgaard:
                    return "Império Nilfgaardiano";
                case Faction.Monsters:
                    return "Monstros";
                case Faction.Scoiatael:
                    return "Scoia'tael";
                default:
                    throw new ArgumentOutOfRangeException(nameof(faction), faction, null);
            }
        }

        private static string RowName(PlayerId owner, CombatRow row)
        {
            var ownerLabel = owner == PlayerId.Player ? "Você" : "Oponente";
            switch (row)
            {
                case CombatRow.Close:
                    return ownerLabel + " - Corpo";
                case CombatRow.Ranged:
                    return ownerLabel + " - Alcance";
                case CombatRow.Siege:
                    return ownerLabel + " - Cerco";
                default:
                    return ownerLabel;
            }
        }
    }
}

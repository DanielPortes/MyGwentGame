using System.Linq;
using System.Reflection;
using Gwent.Core;
using Gwent.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gwent.Tests
{
    public class GwentUiTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }

            foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        [Test]
        public void ViewFactoryBuildsDeckSelectionAndSixCombatRows()
        {
            var root = Root();
            var selection = GwentViewFactory.CreateDeckSelection(root.transform);
            var board = GwentViewFactory.CreateBoard(root.transform);

            Assert.AreEqual(4, selection.FactionButtons.Count);
            Assert.IsTrue(selection.FactionButtons.Any(button => button.Faction == Faction.NorthernRealms));
            Assert.IsTrue(selection.FactionButtons.Any(button => button.Faction == Faction.Nilfgaard));
            Assert.IsTrue(selection.FactionButtons.Any(button => button.Faction == Faction.Monsters));
            Assert.IsTrue(selection.FactionButtons.Any(button => button.Faction == Faction.Scoiatael));
            Assert.AreEqual(6, board.Rows.Count);
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Player && row.Row == CombatRow.Close));
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Player && row.Row == CombatRow.Ranged));
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Player && row.Row == CombatRow.Siege));
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Opponent && row.Row == CombatRow.Close));
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Opponent && row.Row == CombatRow.Ranged));
            Assert.IsTrue(board.Rows.Any(row => row.Owner == PlayerId.Opponent && row.Row == CombatRow.Siege));
            Assert.NotNull(board.PassButton);
            Assert.NotNull(board.LeaderButton);
            Assert.NotNull(board.MulliganButton);
            Assert.NotNull(board.PlayerHand);
            Assert.NotNull(board.StatusText);
            Assert.NotNull(board.CardZoomText);
        }

        [Test]
        public void CardViewDisplaysDefinitionAndFaceDownState()
        {
            var root = Root();
            var card = new CardDefinition(
                "test_blue_stripes",
                "Blue Stripes Commando",
                Faction.NorthernRealms,
                CardKind.Unit,
                CombatRow.Close,
                4,
                CardAbility.TightBond);

            var faceUp = GwentViewFactory.CreateCard(root.transform, card, true);
            var faceDown = GwentViewFactory.CreateCard(root.transform, card, false);

            Assert.AreEqual(card, faceUp.Card);
            Assert.AreEqual("Blue Stripes Commando", faceUp.NameText.text);
            Assert.AreEqual("4", faceUp.StrengthText.text);
            Assert.IsTrue(faceUp.AbilityText.text.Contains("Tight Bond"));
            Assert.AreEqual("Gwent Card", faceDown.NameText.text);
            Assert.AreEqual(string.Empty, faceDown.StrengthText.text);
        }

        [Test]
        public void CardViewUsesRuntimeArtForFaceUpAndFaceDownCards()
        {
            var root = Root();
            var geralt = new CardDefinition(
                "neutral_geralt_of_rivia",
                "Geralt of Rivia",
                Faction.Neutral,
                CardKind.Unit,
                CombatRow.Close,
                15,
                CardAbility.Hero);
            var fallbackCard = new CardDefinition(
                "northern_realms_ballista",
                "Ballista",
                Faction.NorthernRealms,
                CardKind.Unit,
                CombatRow.Siege,
                6);

            var knownArt = GwentViewFactory.CreateCard(root.transform, geralt, true);
            var fallbackArt = GwentViewFactory.CreateCard(root.transform, fallbackCard, true);
            var faceDown = GwentViewFactory.CreateCard(root.transform, fallbackCard, false);

            Assert.NotNull(knownArt.ArtImage.sprite, "Known local card art should render from runtime Resources.");
            Assert.NotNull(fallbackArt.ArtImage.sprite, "Cards without exact art should still render a faction fallback.");
            Assert.NotNull(faceDown.ArtImage.sprite, "Face-down cards should render a faction card back.");
            Assert.Greater(faceDown.ArtImage.color.a, 0.9f);
        }

        [Test]
        public void CardViewAllocatesReadableArtAreaForPlayableWebGl()
        {
            var root = Root();
            var view = GwentViewFactory.CreateCard(
                root.transform,
                new CardDefinition("neutral_geralt_of_rivia", "Geralt of Rivia", Faction.Neutral, CardKind.Unit, CombatRow.Close, 15),
                true);

            var cardLayout = view.GetComponent<LayoutElement>();
            var artLayout = view.ArtImage.GetComponent<LayoutElement>();
            var verticalLayout = view.GetComponent<VerticalLayoutGroup>();

            Assert.GreaterOrEqual(cardLayout.preferredHeight, 156f);
            Assert.GreaterOrEqual(cardLayout.preferredWidth, 108f);
            Assert.GreaterOrEqual(artLayout.preferredHeight, 68f);
            Assert.IsTrue(verticalLayout.childControlHeight);
            Assert.IsFalse(verticalLayout.childForceExpandHeight);
        }

        [Test]
        public void ViewFactoryAddsAnimationAndPolishSurfaces()
        {
            var root = Root();
            var board = GwentViewFactory.CreateBoard(root.transform);
            var card = GwentViewFactory.CreateCard(
                root.transform,
                new CardDefinition("test", "Test", Faction.Neutral, CardKind.Unit, CombatRow.Close, 1),
                false);

            Assert.NotNull(board.RoundBannerText);
            Assert.IsTrue(board.Rows.All(row => row.WeatherOverlay != null));
            Assert.NotNull(card.CanvasGroup);
            Assert.NotNull(card.RectTransform);
        }

        [Test]
        public void BoardRowsUseMostOfThePlayableWebGlWidth()
        {
            var root = Root();
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
            var board = GwentViewFactory.CreateBoard(root.transform);

            LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(board.Root.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            var playerCloseRow = board.Rows.Single(row => row.Owner == PlayerId.Player && row.Row == CombatRow.Close);
            var rowRect = playerCloseRow.GetComponent<RectTransform>().rect;
            var rowLayout = playerCloseRow.GetComponent<LayoutElement>();
            var middleLayout = board.Root.transform.Find("Board Middle").GetComponent<HorizontalLayoutGroup>();

            Assert.IsTrue(middleLayout.childControlWidth);
            Assert.IsFalse(middleLayout.childForceExpandWidth);
            Assert.GreaterOrEqual(rowRect.width, 620f);
            Assert.GreaterOrEqual(rowRect.height, 48f);
            Assert.LessOrEqual(rowLayout.preferredHeight, 64f);
        }

        [Test]
        public void BoardLayoutKeepsHandsAndRowsInsidePlayableWebGlHeight()
        {
            var root = Root();
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 720);
            var board = GwentViewFactory.CreateBoard(root.transform);
            var opponentHand = board.OpponentHand.GetComponent<LayoutElement>();
            var middle = board.Root.transform.Find("Board Middle").GetComponent<LayoutElement>();
            var middleRect = board.Root.transform.Find("Board Middle").GetComponent<RectTransform>();
            var playerHand = board.PlayerHand.GetComponent<LayoutElement>();

            var requiredHeight = 8f + opponentHand.preferredHeight + 6f + middle.preferredHeight + 6f + playerHand.preferredHeight + 8f;

            Assert.LessOrEqual(requiredHeight, 720f);
            Assert.AreEqual(new Vector2(0f, 1f), board.OpponentHand.GetComponent<RectTransform>().anchorMin);
            Assert.AreEqual(Vector2.one, board.OpponentHand.GetComponent<RectTransform>().anchorMax);
            Assert.AreEqual(Vector2.zero, middleRect.anchorMin);
            Assert.AreEqual(Vector2.one, middleRect.anchorMax);
            Assert.AreEqual(204f, middleRect.offsetMin.y, 0.01f);
            Assert.AreEqual(-90f, middleRect.offsetMax.y, 0.01f);
            Assert.AreEqual(Vector2.zero, board.PlayerHand.GetComponent<RectTransform>().anchorMin);
            Assert.AreEqual(new Vector2(1f, 0f), board.PlayerHand.GetComponent<RectTransform>().anchorMax);
            Assert.GreaterOrEqual(opponentHand.preferredHeight, 68f);
            Assert.LessOrEqual(opponentHand.preferredHeight, 80f);
            Assert.GreaterOrEqual(middle.preferredHeight, 390f);
            Assert.LessOrEqual(middle.preferredHeight, 430f);
            Assert.GreaterOrEqual(playerHand.preferredHeight, 180f);
            Assert.LessOrEqual(playerHand.preferredHeight, 200f);
        }

        [Test]
        public void CardViewCanFlipSlideAndHighlightImmediately()
        {
            var root = Root();
            var card = new CardDefinition("test", "Test Card", Faction.Neutral, CardKind.Unit, CombatRow.Close, 6);
            var view = GwentViewFactory.CreateCard(root.transform, card, false);

            view.SetEntranceOffset(new Vector2(24, 0));
            view.CompleteEntranceAnimation();
            view.CompleteFlipAnimation(true);
            view.SetHighlighted(true);

            Assert.IsTrue(view.FaceUp);
            Assert.AreEqual("Test Card", view.NameText.text);
            Assert.AreEqual(1f, view.CanvasGroup.alpha);
            Assert.AreEqual(Vector2.zero, view.RectTransform.anchoredPosition);
            Assert.Greater(view.transform.localScale.x, 1f);
        }

        [Test]
        public void CardEntranceAnimationDoesNotOverrideLayoutGroupPosition()
        {
            var root = Root();
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(root.transform, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 180);
            row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
            row.GetComponent<HorizontalLayoutGroup>().childControlHeight = false;
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;

            GwentViewFactory.CreateCard(
                row.transform,
                new CardDefinition("first", "First", Faction.Neutral, CardKind.Unit, CombatRow.Close, 1),
                true);
            var second = GwentViewFactory.CreateCard(
                row.transform,
                new CardDefinition("second", "Second", Faction.Neutral, CardKind.Unit, CombatRow.Close, 2),
                true);

            Canvas.ForceUpdateCanvases();
            var layoutPosition = second.RectTransform.anchoredPosition;

            second.SetEntranceOffset(new Vector2(24, 0));
            second.CompleteEntranceAnimation();

            Assert.AreEqual(layoutPosition.x, second.RectTransform.anchoredPosition.x, 0.01f);
            Assert.AreEqual(layoutPosition.y, second.RectTransform.anchoredPosition.y, 0.01f);
        }

        [Test]
        public void RowWeatherOverlayAndScorePulseCanBeToggled()
        {
            var root = Root();
            var board = GwentViewFactory.CreateBoard(root.transform);
            var row = board.Rows.First();

            row.SetWeatherActive(true);
            row.PulseScoreImmediate();

            Assert.Greater(row.WeatherOverlay.color.a, 0f);
            Assert.Greater(row.ScoreText.transform.localScale.x, 1f);

            row.SetWeatherActive(false);

            Assert.AreEqual(0f, row.WeatherOverlay.color.a);
        }

        [Test]
        public void GameControllerStartsMatchAndRendersOpeningHand()
        {
            var root = Root();
            var controller = root.AddComponent<GwentGameController>();

            controller.InitializeForTests(Faction.NorthernRealms, Faction.Nilfgaard);

            Assert.AreEqual(Faction.NorthernRealms, controller.PlayerFaction);
            Assert.AreEqual(10, controller.PlayerHandCount);
            Assert.AreEqual(10, controller.OpponentHandCount);
            Assert.AreEqual(6, controller.Board.Rows.Count);
            Assert.AreEqual(10, controller.Board.PlayerHand.childCount);
            Assert.Greater(controller.Board.OpponentDeckCount.text.Length, 0);
        }

        [Test]
        public void GameControllerCreatesEventSystemForRuntimeDeckSelection()
        {
            var root = new GameObject("Runtime Controller");
            _root = root;
            var controller = root.AddComponent<GwentGameController>();

            typeof(GwentGameController)
                .GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);

            Assert.NotNull(Object.FindFirstObjectByType<EventSystem>());
            Assert.NotNull(root.GetComponentInChildren<Canvas>());
        }

        [Test]
        public void GameControllerUsesPlayableWebGlReferenceResolution()
        {
            var root = new GameObject("Runtime Controller");
            _root = root;
            var controller = root.AddComponent<GwentGameController>();

            controller.InitializeForTests(Faction.NorthernRealms, Faction.Nilfgaard);

            var scaler = root.GetComponentInChildren<CanvasScaler>();
            Assert.NotNull(scaler);
            Assert.AreEqual(new Vector2(1280, 720), scaler.referenceResolution);
            Assert.GreaterOrEqual(controller.Board.PlayerHand.GetComponent<LayoutElement>().preferredHeight, 168f);
            Assert.GreaterOrEqual(controller.Board.OpponentHand.GetComponent<LayoutElement>().preferredHeight, 68f);
            Assert.IsFalse(controller.Board.PlayerHand.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight);
            Assert.IsFalse(controller.Board.OpponentHand.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight);
        }

        private GameObject Root()
        {
            _root = new GameObject("Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return _root;
        }
    }
}

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

        private GameObject Root()
        {
            _root = new GameObject("Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return _root;
        }
    }
}

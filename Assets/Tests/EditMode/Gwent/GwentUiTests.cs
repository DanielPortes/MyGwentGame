using System.Linq;
using Gwent.Core;
using Gwent.UI;
using NUnit.Framework;
using UnityEngine;
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

        private GameObject Root()
        {
            _root = new GameObject("Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return _root;
        }
    }
}

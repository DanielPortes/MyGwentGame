using Gwent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentRowView : MonoBehaviour
    {
        public PlayerId Owner { get; private set; }

        public CombatRow Row { get; private set; }

        public Transform CardsRoot { get; private set; }

        public Text ScoreText { get; private set; }

        public Text LabelText { get; private set; }

        public Button Button { get; private set; }

        public void Configure(PlayerId owner, CombatRow row, Transform cardsRoot, Text scoreText, Text labelText, Button button)
        {
            Owner = owner;
            Row = row;
            CardsRoot = cardsRoot;
            ScoreText = scoreText;
            LabelText = labelText;
            Button = button;
        }

        public void SetScore(int score)
        {
            ScoreText.text = score.ToString();
        }
    }
}

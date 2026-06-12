using System.Collections;
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

        public Image WeatherOverlay { get; private set; }

        public void Configure(PlayerId owner, CombatRow row, Transform cardsRoot, Text scoreText, Text labelText, Button button, Image weatherOverlay)
        {
            Owner = owner;
            Row = row;
            CardsRoot = cardsRoot;
            ScoreText = scoreText;
            LabelText = labelText;
            Button = button;
            WeatherOverlay = weatherOverlay;
        }

        public void SetScore(int score)
        {
            ScoreText.text = score.ToString();
        }

        public void SetWeatherActive(bool active)
        {
            WeatherOverlay.color = active ? new Color(0.66f, 0.80f, 0.92f, 0.28f) : new Color(0.66f, 0.80f, 0.92f, 0f);
        }

        public void PulseScoreImmediate()
        {
            ScoreText.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
        }

        public IEnumerator PulseScore(float duration)
        {
            PulseScoreImmediate();
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            ScoreText.transform.localScale = Vector3.one;
        }
    }
}

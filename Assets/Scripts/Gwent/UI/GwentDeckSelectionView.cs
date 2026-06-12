using System.Collections.Generic;
using Gwent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentFactionButton
    {
        public GwentFactionButton(Faction faction, Button button, Text label)
        {
            Faction = faction;
            Button = button;
            Label = label;
        }

        public Faction Faction { get; }

        public Button Button { get; }

        public Text Label { get; }
    }

    public sealed class GwentDeckSelectionView : MonoBehaviour
    {
        private readonly List<GwentFactionButton> _factionButtons = new List<GwentFactionButton>();

        public IReadOnlyList<GwentFactionButton> FactionButtons
        {
            get { return _factionButtons; }
        }

        public Text TitleText { get; private set; }

        public void Configure(Text titleText)
        {
            TitleText = titleText;
        }

        public void AddFactionButton(GwentFactionButton factionButton)
        {
            _factionButtons.Add(factionButton);
        }
    }
}

using System.Collections.Generic;
using Gwent.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentCardView : MonoBehaviour
    {
        public CardDefinition Card { get; private set; }

        public bool FaceUp { get; private set; }

        public Text NameText { get; private set; }

        public Text StrengthText { get; private set; }

        public Text RowText { get; private set; }

        public Text AbilityText { get; private set; }

        public Button Button { get; private set; }

        public Image Background { get; private set; }

        public void Configure(Text nameText, Text strengthText, Text rowText, Text abilityText, Button button, Image background)
        {
            NameText = nameText;
            StrengthText = strengthText;
            RowText = rowText;
            AbilityText = abilityText;
            Button = button;
            Background = background;
        }

        public void Bind(CardDefinition card, bool faceUp)
        {
            Card = card;
            FaceUp = faceUp;

            if (!faceUp)
            {
                NameText.text = "Gwent Card";
                StrengthText.text = string.Empty;
                RowText.text = string.Empty;
                AbilityText.text = string.Empty;
                Background.color = new Color(0.10f, 0.13f, 0.18f, 1f);
                return;
            }

            NameText.text = card.Name;
            StrengthText.text = card.Kind == CardKind.Unit ? card.Strength.ToString() : string.Empty;
            RowText.text = FormatRow(card);
            AbilityText.text = FormatAbilities(card.Abilities);
            Background.color = ColorFor(card.Faction);
        }

        private static string FormatRow(CardDefinition card)
        {
            if (card.Kind == CardKind.Special)
            {
                return "Especial";
            }

            if (card.Kind == CardKind.Weather)
            {
                return "Clima";
            }

            switch (card.Row)
            {
                case CombatRow.Close:
                    return card.HasAbility(CardAbility.Agile) ? "Corpo a corpo / Longo alcance" : "Corpo a corpo";
                case CombatRow.Ranged:
                    return "Longo alcance";
                case CombatRow.Siege:
                    return "Cerco";
                default:
                    return string.Empty;
            }
        }

        private static string FormatAbilities(CardAbility abilities)
        {
            if (abilities == CardAbility.None)
            {
                return string.Empty;
            }

            var labels = new List<string>();
            Add(abilities, CardAbility.Hero, "Hero");
            Add(abilities, CardAbility.TightBond, "Tight Bond");
            Add(abilities, CardAbility.MoraleBoost, "Morale");
            Add(abilities, CardAbility.Spy, "Spy");
            Add(abilities, CardAbility.Medic, "Medic");
            Add(abilities, CardAbility.Muster, "Muster");
            Add(abilities, CardAbility.Agile, "Agile");
            Add(abilities, CardAbility.Decoy, "Decoy");
            Add(abilities, CardAbility.Scorch, "Scorch");
            Add(abilities, CardAbility.CommandersHorn, "Horn");
            return string.Join(" | ", labels);

            void Add(CardAbility source, CardAbility flag, string label)
            {
                if ((source & flag) == flag)
                {
                    labels.Add(label);
                }
            }
        }

        private static Color ColorFor(Faction faction)
        {
            switch (faction)
            {
                case Faction.NorthernRealms:
                    return new Color(0.17f, 0.29f, 0.48f, 1f);
                case Faction.Nilfgaard:
                    return new Color(0.18f, 0.16f, 0.13f, 1f);
                case Faction.Monsters:
                    return new Color(0.36f, 0.12f, 0.10f, 1f);
                case Faction.Scoiatael:
                    return new Color(0.13f, 0.32f, 0.18f, 1f);
                default:
                    return new Color(0.31f, 0.27f, 0.19f, 1f);
            }
        }
    }
}

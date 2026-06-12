using System.Collections;
using System.Collections.Generic;
using Gwent.Core;
using Gwent.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Gwent.UI
{
    public sealed class GwentCardView : MonoBehaviour
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public CardDefinition Card { get; private set; }

        public bool FaceUp { get; private set; }

        public Text NameText { get; private set; }

        public Text StrengthText { get; private set; }

        public Text RowText { get; private set; }

        public Text AbilityText { get; private set; }

        public Button Button { get; private set; }

        public Image Background { get; private set; }

        public Image ArtImage { get; private set; }

        public RectTransform RectTransform { get; private set; }

        public CanvasGroup CanvasGroup { get; private set; }

        public void Configure(Text nameText, Text strengthText, Text rowText, Text abilityText, Button button, Image background, Image artImage)
        {
            NameText = nameText;
            StrengthText = strengthText;
            RowText = rowText;
            AbilityText = abilityText;
            Button = button;
            Background = background;
            ArtImage = artImage;
            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
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
                ApplyBack(card);
                return;
            }

            NameText.text = card.Name;
            StrengthText.text = card.Kind == CardKind.Unit ? card.Strength.ToString() : string.Empty;
            RowText.text = FormatRow(card);
            AbilityText.text = FormatAbilities(card.Abilities);
            Background.color = ColorFor(card.Faction);
            ApplyArt(card);
        }

        private void ApplyArt(CardDefinition card)
        {
            ArtImage.sprite = null;
            ArtImage.color = new Color(1f, 1f, 1f, 0f);

            var sprite = LoadSprite(GwentAssetCatalog.GetCardArtResource(card));
            if (sprite == null)
            {
                return;
            }

            ArtImage.sprite = sprite;
            ArtImage.preserveAspect = true;
            ArtImage.color = Color.white;
        }

        private void ApplyBack(CardDefinition card)
        {
            ArtImage.sprite = null;
            ArtImage.color = new Color(1f, 1f, 1f, 0f);

            var faction = card == null ? Faction.Neutral : card.Faction;
            var sprite = LoadSprite(GwentAssetCatalog.GetFactionBackResource(faction));
            if (sprite == null)
            {
                return;
            }

            ArtImage.sprite = sprite;
            ArtImage.preserveAspect = true;
            ArtImage.color = Color.white;
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(resourcePath, out var cached))
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            sprite.name = resourcePath;
            SpriteCache[resourcePath] = sprite;
            return sprite;
        }

        public void SetEntranceOffset(Vector2 offset)
        {
            CanvasGroup.alpha = 0f;
        }

        public void CompleteEntranceAnimation()
        {
            CanvasGroup.alpha = 1f;
        }

        public IEnumerator AnimateEntrance(float duration)
        {
            if (duration <= 0f)
            {
                CompleteEntranceAnimation();
                yield break;
            }

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var progress = Mathf.Clamp01(elapsed / duration);
                CanvasGroup.alpha = progress;
                yield return null;
            }

            CompleteEntranceAnimation();
        }

        public void CompleteFlipAnimation(bool faceUp)
        {
            Bind(Card, faceUp);
            transform.localScale = Vector3.one;
        }

        public IEnumerator AnimateFlip(bool faceUp, float duration)
        {
            if (duration <= 0f)
            {
                CompleteFlipAnimation(faceUp);
                yield break;
            }

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var progress = Mathf.Clamp01(elapsed / duration);
                var width = Mathf.Abs(Mathf.Cos(progress * Mathf.PI));
                transform.localScale = new Vector3(width, transform.localScale.y, transform.localScale.z);
                if (progress >= 0.5f && FaceUp != faceUp)
                {
                    Bind(Card, faceUp);
                }

                yield return null;
            }

            CompleteFlipAnimation(faceUp);
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
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
            Add(abilities, CardAbility.ScorchClose, "Scorch Close");
            Add(abilities, CardAbility.ScorchRanged, "Scorch Ranged");
            Add(abilities, CardAbility.ScorchSiege, "Scorch Siege");
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

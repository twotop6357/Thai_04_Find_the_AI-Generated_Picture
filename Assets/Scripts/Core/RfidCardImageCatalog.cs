using System.Collections.Generic;
using Hardware;
using UnityEngine;

namespace Core
{
    /// <summary>Maps RFID card IDs to Figma card images.</summary>
    internal sealed class RfidCardImageCatalog
    {
        // Per-card sprites shared by the center preview and classification thumbnails.
        private readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();

        /// <summary>Prepares the six Figma images matching the card list.</summary>
        internal RfidCardImageCatalog()
        {
            LoadImages(RfidCardCatalog.AiCardIds);
            LoadImages(RfidCardCatalog.HumanCardIds);
        }

        /// <summary>Returns the source image for a card; unregistered or empty cards are hidden.</summary>
        /// <param name="cardId">Card ID recognized by a reader; null for an empty slot.</param>
        /// <returns>The shared image, or null for an unregistered card or a missing resource.</returns>
        internal Sprite GetImage(string cardId)
        {
            return cardId != null && images.TryGetValue(cardId, out Sprite image) ? image : null;
        }

        /// <summary>Registers the Resources image numbered to match the card order.</summary>
        /// <param name="cardIds">Cards of one origin registered with the RFID input.</param>
        private void LoadImages(string[] cardIds)
        {
            // Index used to derive the image number from the card list.
            for (int index = 0; index < cardIds.Length; index++)
            {
                // All display areas share one source sprite; there are no separate thumbnails.
                // Confirmed left-to-right: Human, AI, Human, AI, Human, AI.
                int pictureNumber = index * 2 + (RfidCardCatalog.GetCardType(cardIds[index]) == RfidCardType.Ai ? 2 : 1);
                Sprite image = Resources.Load<Sprite>("FigmaCards/Card" + pictureNumber);
                images[cardIds[index]] = image;
                if (image == null)
                    Debug.LogWarning($"[RfidCardImageCatalog] Figma card sprite is missing for {cardIds[index]}. Check FigmaCards/Card{pictureNumber}.");
            }
        }
    }
}

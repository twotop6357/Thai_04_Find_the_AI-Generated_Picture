using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Hardware
{
    /// <summary>
    /// The seven installed RFID readers: one on the center board, three AI slots at bottom left, three Human slots at bottom right.
    /// </summary>
    public enum RfidReaderSlot
    {
        Center,
        Ai1,
        Ai2,
        Ai3,
        Human1,
        Human2,
        Human3,
    }

    /// <summary>
    /// The actual origin of the card's image.
    /// </summary>
    public enum RfidCardType
    {
        Unknown,
        Ai,
        Human,
    }

    /// <summary>
    /// Event published when a card is placed on (IsPlaced) or removed from a reader.
    /// </summary>
    public readonly struct RfidCardEvent
    {
        public RfidReaderSlot Slot { get; }
        public string CardId { get; }
        public bool IsPlaced { get; }
        public RfidCardType CardType => RfidCardCatalog.GetCardType(CardId);

        public RfidCardEvent(RfidReaderSlot slot, string cardId, bool isPlaced)
        {
            Slot = slot;
            CardId = cardId;
            IsPlaced = isPlaced;
        }

        public override string ToString() => $"[{Slot}] {(IsPlaced ? "Placed" : "Removed")} {CardId} ({CardType})";
    }

    /// <summary>
    /// Card IDs and types used by the game. RfidSettings.json maps them to physical card HEX values.
    /// </summary>
    public static class RfidCardCatalog
    {
        public static readonly string[] AiCardIds = { "AI_1", "AI_2", "AI_3" };
        public static readonly string[] HumanCardIds = { "HUMAN_1", "HUMAN_2", "HUMAN_3" };

        public static RfidCardType GetCardType(string cardId)
        {
            if (Array.IndexOf(AiCardIds, cardId) >= 0) return RfidCardType.Ai;
            if (Array.IndexOf(HumanCardIds, cardId) >= 0) return RfidCardType.Human;
            return RfidCardType.Unknown;
        }
    }

    /// <summary>
    /// Single entry point for RFID reader input. RfidReaderService feeds input through NotifyPlaced/NotifyRemoved
    /// and game logic reads state only through OnCardChanged or GetCard,
    /// so game code does not change when the input source changes.
    /// This class enforces the physical constraints: one card per reader, and each card on only one reader.
    /// </summary>
    public static class RfidInput
    {
        private static readonly Dictionary<RfidReaderSlot, string> SlotCards = new Dictionary<RfidReaderSlot, string>();
        private static Subject<RfidCardEvent> _cardChangedSubject = new Subject<RfidCardEvent>();

        /// <summary>Stream of card placed/removed events, published on the main thread.</summary>
        public static Observable<RfidCardEvent> OnCardChanged => _cardChangedSubject;

        /// <summary>
        /// Resets so state and subscriptions from a previous play session do not survive with domain reload disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SlotCards.Clear();
            _cardChangedSubject?.Dispose();
            _cardChangedSubject = new Subject<RfidCardEvent>();
        }

        /// <summary>Whether all readers are empty.</summary>
        public static bool IsBoardClear => SlotCards.Count == 0;

        /// <summary>The card ID on this reader, or null if empty.</summary>
        public static string GetCard(RfidReaderSlot slot)
        {
            return SlotCards.TryGetValue(slot, out string cardId) ? cardId : null;
        }

        /// <summary>Finds the reader holding a card; false if there is none.</summary>
        public static bool TryFindSlot(string cardId, out RfidReaderSlot slot)
        {
            foreach (KeyValuePair<RfidReaderSlot, string> pair in SlotCards)
            {
                if (pair.Value == cardId)
                {
                    slot = pair.Key;
                    return true;
                }
            }

            slot = default;
            return false;
        }

        /// <summary>
        /// A card was detected on a reader. If the same card is on another reader it is removed from there first,
        /// and a different card already on the target reader gets a removal event before the new card is placed.
        /// </summary>
        public static void NotifyPlaced(RfidReaderSlot slot, string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (GetCard(slot) == cardId) return;

            if (TryFindSlot(cardId, out RfidReaderSlot previousSlot))
            {
                NotifyRemoved(previousSlot);
            }

            NotifyRemoved(slot);

            SlotCards[slot] = cardId;
            Publish(new RfidCardEvent(slot, cardId, true));
        }

        /// <summary>A card left a reader. Ignored if the reader was already empty.</summary>
        public static void NotifyRemoved(RfidReaderSlot slot)
        {
            if (!SlotCards.TryGetValue(slot, out string cardId)) return;

            SlotCards.Remove(slot);
            Publish(new RfidCardEvent(slot, cardId, false));
        }

        /// <summary>Empties all readers.</summary>
        public static void ClearAll()
        {
            foreach (RfidReaderSlot slot in (RfidReaderSlot[])Enum.GetValues(typeof(RfidReaderSlot)))
            {
                NotifyRemoved(slot);
            }
        }

        private static void Publish(RfidCardEvent cardEvent)
        {
            Debug.Log($"[RfidInput] {cardEvent}");
            _cardChangedSubject.OnNext(cardEvent);
        }
    }
}

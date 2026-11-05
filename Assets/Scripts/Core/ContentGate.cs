using System.Collections.Generic;
using Hardware;
using R3;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Something that can block content. ContentGate calls RefreshBlocking on every RFID event to update the blocked state.
    /// </summary>
    public interface IContentBlocker
    {
        /// <summary>Re-evaluates the current state and returns true if content must be blocked.</summary>
        bool RefreshBlocking();
    }

    /// <summary>
    /// Global gate that manages the GamePanel RFID input lifetime and content pauses caused by popups and similar.
    /// Content code subscribes to ContentGate.OnCardChanged instead of RfidInput.OnCardChanged, and
    /// features driven by non-RFID input (touch, timers, etc.) must check IsBlocked before running.
    /// <para>
    /// If blockers subscribed to RfidInput directly, the result would depend on subscription order
    /// (e.g. the last card removal leaking to content right after unblocking). So on every event the gate
    /// evaluates blockers first and does not forward the event if it was blocked either before or after it.
    /// </para>
    /// </summary>
    public static class ContentGate
    {
        private static readonly List<IContentBlocker> Blockers = new List<IContentBlocker>();
        private static readonly HashSet<IContentBlocker> ActiveBlockers = new HashSet<IContentBlocker>();
        private static readonly Dictionary<RfidReaderSlot, string> GameCards = new Dictionary<RfidReaderSlot, string>();
        private static MonoBehaviour _gameInputOwner;
        private static Subject<RfidCardEvent> _cardChangedSubject = new Subject<RfidCardEvent>();
        private static System.IDisposable _rfidSubscription;

        /// <summary>Whether content features are blocked.</summary>
        public static bool IsBlocked => ActiveBlockers.Count > 0;

        /// <summary>RFID event stream delivered while GamePanel input is active and not blocked.</summary>
        public static Observable<RfidCardEvent> OnCardChanged => _cardChangedSubject;

        /// <summary>
        /// Resets static state and reconnects the RfidInput subscription, for setups with domain reload disabled.
        /// Must run after RfidInput.ResetStatics (SubsystemRegistration), so it runs at BeforeSceneLoad.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Blockers.Clear();
            ActiveBlockers.Clear();
            GameCards.Clear();
            _gameInputOwner = null;
            _rfidSubscription?.Dispose();
            _cardChangedSubject?.Dispose();
            _cardChangedSubject = new Subject<RfidCardEvent>();
            _rfidSubscription = RfidInput.OnCardChanged.Subscribe(HandleCardChanged);
        }

        /// <summary>Registers a blocker and immediately evaluates its current state.</summary>
        public static void Register(IContentBlocker blocker)
        {
            if (blocker == null || Blockers.Contains(blocker)) return;

            Blockers.Add(blocker);
            Refresh(blocker);
        }

        /// <summary>Unregisters a blocker and releases any block it holds.</summary>
        public static void Unregister(IContentBlocker blocker)
        {
            Blockers.Remove(blocker);
            ActiveBlockers.Remove(blocker);
        }

        /// <summary>Starts on GamePanel entry. Cards already placed are not copied and must be tagged again.</summary>
        public static void BeginGameInput(MonoBehaviour owner)
        {
            GameCards.Clear();
            _gameInputOwner = owner;
        }

        public static void EndGameInput(MonoBehaviour owner)
        {
            if (_gameInputOwner != owner) return;
            _gameInputOwner = null;
            GameCards.Clear();
        }

        /// <summary>Returns only cards newly recognized since entering GamePanel. RfidInput keeps the physical state.</summary>
        public static string GetGameCard(RfidReaderSlot slot)
        {
            return _gameInputOwner != null && _gameInputOwner.isActiveAndEnabled &&
                GameCards.TryGetValue(slot, out string cardId) ? cardId : null;
        }

        /// <summary>Whether GamePanel currently recognizes at least one card.</summary>
        public static bool HasGameCard => _gameInputOwner != null && _gameInputOwner.isActiveAndEnabled && GameCards.Count > 0;

        private static void HandleCardChanged(RfidCardEvent cardEvent)
        {
            // Apply removals even while blocked so old cards do not come back after a popup closes.
            if (!cardEvent.IsPlaced) GameCards.Remove(cardEvent.Slot);
            bool wasBlocked = IsBlocked;

            for (int i = Blockers.Count - 1; i >= 0; i--)
            {
                Refresh(Blockers[i]);
            }

            if (wasBlocked || IsBlocked || _gameInputOwner == null || !_gameInputOwner.isActiveAndEnabled) return;

            if (cardEvent.IsPlaced) GameCards[cardEvent.Slot] = cardEvent.CardId;

            _cardChangedSubject.OnNext(cardEvent);
        }

        private static void Refresh(IContentBlocker blocker)
        {
            if (blocker.RefreshBlocking())
            {
                ActiveBlockers.Add(blocker);
            }
            else
            {
                ActiveBlockers.Remove(blocker);
            }
        }
    }
}

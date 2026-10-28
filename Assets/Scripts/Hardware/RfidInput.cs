using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Hardware
{
    /// <summary>
    /// 설치된 RFID 리더 7개. 중앙 인터랙션 보드 1칸, 하단 좌측 AI 분류 3칸, 하단 우측 인간 분류 3칸.
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
    /// 카드가 지칭하는 이미지의 실제 출처.
    /// </summary>
    public enum RfidCardType
    {
        Unknown,
        Ai,
        Human,
    }

    /// <summary>
    /// 리더 위에 카드가 올라가거나(IsPlaced) 내려갔을 때 발행되는 이벤트.
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
    /// 카드 UID와 카드 종류의 매핑. 실제 카드 UID가 확정되면 이 값만 교체할 것.
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
    /// RFID 리더 입력의 단일 진입점. 실제 하드웨어 파서(시리얼 등)와 RfidTester가 모두
    /// NotifyPlaced/NotifyRemoved로 입력을 넣고, 게임 로직은 OnCardChanged 구독 또는 GetCard 조회로만
    /// 상태를 읽으므로 입력 출처가 바뀌어도 게임 코드는 수정할 필요가 없음.
    /// 리더당 카드 1장, 카드 1장은 한 리더에만 존재한다는 물리적 제약을 이 클래스가 보장함.
    /// </summary>
    public static class RfidInput
    {
        private static readonly Dictionary<RfidReaderSlot, string> SlotCards = new Dictionary<RfidReaderSlot, string>();
        private static Subject<RfidCardEvent> _cardChangedSubject = new Subject<RfidCardEvent>();

        /// <summary>카드 올림/내림 이벤트 스트림. 메인 스레드에서 발행됨.</summary>
        public static Observable<RfidCardEvent> OnCardChanged => _cardChangedSubject;

        /// <summary>
        /// 도메인 리로드 비활성화 환경에서 이전 플레이 세션의 상태와 구독이 남지 않도록 초기화함.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SlotCards.Clear();
            _cardChangedSubject?.Dispose();
            _cardChangedSubject = new Subject<RfidCardEvent>();
        }

        /// <summary>모든 리더가 비어 있는지 여부.</summary>
        public static bool IsBoardClear => SlotCards.Count == 0;

        /// <summary>해당 리더 위의 카드 ID. 비어 있으면 null.</summary>
        public static string GetCard(RfidReaderSlot slot)
        {
            return SlotCards.TryGetValue(slot, out string cardId) ? cardId : null;
        }

        /// <summary>카드가 올라가 있는 리더를 찾음. 없으면 false.</summary>
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
        /// 리더에 카드가 감지됨. 같은 카드가 다른 리더에 있으면 먼저 그 리더에서 제거하고,
        /// 대상 리더에 다른 카드가 있으면 그 카드도 제거 이벤트를 낸 뒤 올림.
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

        /// <summary>리더에서 카드가 사라짐. 비어 있던 리더면 무시함.</summary>
        public static void NotifyRemoved(RfidReaderSlot slot)
        {
            if (!SlotCards.TryGetValue(slot, out string cardId)) return;

            SlotCards.Remove(slot);
            Publish(new RfidCardEvent(slot, cardId, false));
        }

        /// <summary>모든 리더를 비움.</summary>
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

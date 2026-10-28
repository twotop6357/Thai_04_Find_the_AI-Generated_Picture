using System.Collections.Generic;
using Hardware;
using R3;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// 콘텐츠 동작을 막는 주체. ContentGate가 RFID 이벤트마다 RefreshBlocking을 호출해 차단 여부를 갱신함.
    /// </summary>
    public interface IContentBlocker
    {
        /// <summary>현재 상태를 다시 평가하고, 콘텐츠를 막아야 하면 true를 반환함.</summary>
        bool RefreshBlocking();
    }

    /// <summary>
    /// GamePanel의 RFID 입력 수명과 팝업 등에 의한 콘텐츠 일시 정지를 관리하는 전역 게이트.
    /// 콘텐츠 코드는 RfidInput.OnCardChanged 대신 ContentGate.OnCardChanged를 구독하고,
    /// RFID 외 입력(터치, 타이머 등)으로 동작하는 기능은 실행 전에 IsBlocked를 확인할 것.
    /// <para>
    /// 차단 갱신을 차단자가 RfidInput을 직접 구독해 처리하면 구독 순서에 따라 결과가 달라짐
    /// (예: 마지막 카드 제거 이벤트가 차단 해제 직후 콘텐츠로 새어 나감). 그래서 게이트가 이벤트마다
    /// 차단자 평가를 먼저 수행하고, 이벤트 전후 어느 쪽이든 차단 상태였다면 전달하지 않음.
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

        /// <summary>콘텐츠 기능이 막혀 있는지 여부.</summary>
        public static bool IsBlocked => ActiveBlockers.Count > 0;

        /// <summary>GamePanel 입력이 활성화되어 있고 차단 중이 아닐 때 전달되는 RFID 이벤트 스트림.</summary>
        public static Observable<RfidCardEvent> OnCardChanged => _cardChangedSubject;

        /// <summary>
        /// 도메인 리로드 비활성화 환경을 고려해 정적 상태를 초기화하고 RfidInput 구독을 다시 연결함.
        /// RfidInput.ResetStatics(SubsystemRegistration) 이후에 실행되어야 하므로 BeforeSceneLoad에서 처리함.
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

        /// <summary>차단자를 등록하고 즉시 현재 상태로 평가함.</summary>
        public static void Register(IContentBlocker blocker)
        {
            if (blocker == null || Blockers.Contains(blocker)) return;

            Blockers.Add(blocker);
            Refresh(blocker);
        }

        /// <summary>차단자를 해제함. 해당 차단자가 걸어 둔 차단도 함께 풀림.</summary>
        public static void Unregister(IContentBlocker blocker)
        {
            Blockers.Remove(blocker);
            ActiveBlockers.Remove(blocker);
        }

        /// <summary>GamePanel 진입 시 시작한다. 이미 놓인 카드는 복사하지 않아 재태그가 필요하다.</summary>
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

        /// <summary>현재 GamePanel 진입 이후 새로 인식한 카드만 반환한다. 물리 상태는 RfidInput이 보존한다.</summary>
        public static string GetGameCard(RfidReaderSlot slot)
        {
            return _gameInputOwner != null && _gameInputOwner.isActiveAndEnabled &&
                GameCards.TryGetValue(slot, out string cardId) ? cardId : null;
        }

        private static void HandleCardChanged(RfidCardEvent cardEvent)
        {
            // 차단 중 제거도 반영해, 팝업이 닫힌 뒤 이전 카드가 되살아나지 않도록 한다.
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

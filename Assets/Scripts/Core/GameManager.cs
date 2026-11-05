using System;
using Cysharp.Threading.Tasks;
using Hardware;
using MessagePipe;
using R3;
using UnityEngine.SceneManagement;
using VContainer;
using HuliacDev.App;
using HuliacDev.Core;

namespace Core
{
    /// <summary>
    /// 프로젝트 전용 게임 매니저.
    /// 싱글톤 유지, 디버그 토글, Settings 비동기 로드는 GameManagerBase가 제공하며
    /// 프로젝트 고유의 전역 흐름 제어를 이곳에 추가함.
    /// </summary>
    public class GameManager : GameManagerBase
    {
        private ISubscriber<InactivityTimeoutEvent> _inactivityTimeoutSubscriber;
        private IDisposable _inactivityTimeoutSubscription;
        private IPublisher<MoveIdleEvent> _moveIdlePublisher;
        private InactivityTimer _inactivityTimer;
        private IDisposable _rfidActivitySubscription;
        private bool _inactivityPaused;

        /// <summary>
        /// GameManagerBase.Construct와 별개의 [Inject] 메서드. VContainer는 타입 계층의 각
        /// 레벨에서 선언된 [Inject] 메서드를 모두 수집해 주입하므로, 베이스 시그니처를 건드리지
        /// 않고 이 프로젝트에서만 필요한 의존성을 추가할 수 있음.
        /// </summary>
        [Inject]
        public void ConstructGameManager(ISubscriber<InactivityTimeoutEvent> inactivityTimeoutSubscriber,
            IPublisher<MoveIdleEvent> moveIdlePublisher, IObjectResolver resolver)
        {
            _inactivityTimeoutSubscriber = inactivityTimeoutSubscriber;
            _moveIdlePublisher = moveIdlePublisher;
            _inactivityTimer = resolver.ResolveOrDefault<InactivityTimer>();
            // 주입이 OnEnable보다 늦게 끝나도 타임아웃 구독이 빠지지 않게 한다.
            if (isActiveAndEnabled) SubscribeInactivityTimeout();
        }

        private void SubscribeInactivityTimeout()
        {
            if (_inactivityTimeoutSubscription != null || _inactivityTimeoutSubscriber == null) return;
            _inactivityTimeoutSubscription = _inactivityTimeoutSubscriber.Subscribe(_ => HandleInactivityTimeout());
        }

        private void HandleInactivityTimeout()
        {
            ReturnToFirstScreen();
            // 타이머는 한 번 발행하면 입력 전까지 멈추므로, 전환을 시작하지 못했으면 다시 측정한다.
            if (!ScreenTransition.IsBusy && _inactivityTimer != null) _inactivityTimer.ResetTimer();
        }

        /// <summary>
        /// 비활동 복귀 예외 구간에서는 타이머를 멈추고, 구간이 끝나면 처음부터 다시 잰다.
        /// 예외: 화면 전환 중, 이미 최초 화면, GamePanel에서 카드 인식 중, 영상 재생~카드 제거 팝업, 운영자 매핑 창.
        /// </summary>
        private void Update()
        {
            if (_inactivityTimer == null) return;
            // NextButton은 Input Manager 입력이라 타이머의 Input System 감지와 별개로 활동을 알린다.
            if (SingleButtonInput.IsPressed) _inactivityTimer.ResetTimer();

            bool exempt = ScreenTransition.IsBusy || ClassificationEntryController.IsTitleShowing ||
                ContentGate.HasGameCard || FinishVideoPresentation.HoldsInactivityReturn || RfidMappingWindow.IsOpen;
            if (exempt == _inactivityPaused) return;
            _inactivityPaused = exempt;
            if (exempt) _inactivityTimer.Pause();
            else _inactivityTimer.Resume();
        }

        public void ReturnHome()
        {
            if (ScreenTransition.IsBusy) return;
            _moveIdlePublisher?.Publish(new MoveIdleEvent());
            ReturnToFirstScreen();
        }

        /// <summary>
        /// InactivityTimer의 타임아웃 이벤트를 구독함. move_idle_timeout 로그는 ApiManagerBase가
        /// 같은 이벤트를 자체 구독해 자동으로 보내므로, 여기서는 화면 복귀만 담당함.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeInactivityTimeout();
            _rfidActivitySubscription = RfidInput.OnCardChanged.Subscribe(_ =>
            {
                if (_inactivityTimer != null) _inactivityTimer.ResetTimer();
            });
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _inactivityTimeoutSubscription?.Dispose();
            _inactivityTimeoutSubscription = null;
            _rfidActivitySubscription?.Dispose();
            _rfidActivitySubscription = null;
        }

        /// <summary>
        /// 공통 암전 전환을 거쳐 대상 프로젝트의 첫 화면으로 복귀한다.
        /// 기존 DI와 타임아웃/수동복귀 로그 책임은 유지한다.
        /// <para>
        /// 수동 복귀 경로(예: 콘텐츠 종료 버튼)를 추가할 때는 이 메서드를 호출하기 전후로
        /// IPublisher&lt;MoveIdleEvent&gt;.Publish(new MoveIdleEvent())를 직접 호출해 move_idle
        /// 로그를 보낼 것. 이 메서드 자체는 로그를 보내지 않음 — 타임아웃 경로(move_idle_timeout,
        /// ApiManagerBase가 InactivityTimeoutEvent를 자체 구독해 자동 전송)와 이 메서드를
        /// 공유하므로, 여기서 MoveIdleEvent까지 함께 발행하면 타임아웃 복귀 때도 move_idle이
        /// 중복으로 나가게 됨.
        /// </para>
        /// </summary>
        protected virtual void ReturnToFirstScreen()
        {
            if (!ScreenTransition.IsBusy)
                ScreenTransition.SceneAsync("Assets/Scenes/0_Idle.unity", default, TransitionSound.ReturnHome).Forget();
        }
    }
}

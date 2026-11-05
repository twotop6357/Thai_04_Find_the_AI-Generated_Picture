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
    /// Project-specific game manager.
    /// GameManagerBase provides the singleton, debug toggles and async Settings loading;
    /// project-specific global flow control goes here.
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
        /// An [Inject] method separate from GameManagerBase.Construct. VContainer collects [Inject] methods
        /// declared at every level of the type hierarchy, so project-only dependencies can be added
        /// without changing the base signature.
        /// </summary>
        [Inject]
        public void ConstructGameManager(ISubscriber<InactivityTimeoutEvent> inactivityTimeoutSubscriber,
            IPublisher<MoveIdleEvent> moveIdlePublisher, IObjectResolver resolver)
        {
            _inactivityTimeoutSubscriber = inactivityTimeoutSubscriber;
            _moveIdlePublisher = moveIdlePublisher;
            _inactivityTimer = resolver.ResolveOrDefault<InactivityTimer>();
            // Keep the timeout subscription even if injection finishes after OnEnable.
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
            // The timer stays latched after publishing until input arrives, so restart it if no transition started.
            if (!ScreenTransition.IsBusy && _inactivityTimer != null) _inactivityTimer.ResetTimer();
        }

        /// <summary>
        /// Pauses the timer during inactivity-return exemptions and restarts it from zero when they end.
        /// Exemptions: screen transitions, already on the first screen, a card recognized on GamePanel, video playback through the card removal popup, and the operator mapping window.
        /// </summary>
        private void Update()
        {
            if (_inactivityTimer == null) return;
            // NextButton is Input Manager input, so report it as activity separately from the timer's Input System detection.
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
        /// Subscribes to the InactivityTimer timeout event. ApiManagerBase subscribes to the same event and
        /// sends the move_idle_timeout log itself, so this class only handles the screen return.
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
        /// Returns to this project's first screen through the shared fade-to-black transition.
        /// Existing DI and timeout/manual return logging responsibilities are unchanged.
        /// <para>
        /// When adding a manual return path (e.g. an end-of-content button), call
        /// IPublisher&lt;MoveIdleEvent&gt;.Publish(new MoveIdleEvent()) around this method to send the move_idle
        /// log. This method sends no log itself: it is shared with the timeout path (move_idle_timeout,
        /// sent automatically by ApiManagerBase from its own InactivityTimeoutEvent subscription),
        /// so publishing MoveIdleEvent here would also send a duplicate move_idle
        /// on timeout returns.
        /// </para>
        /// </summary>
        protected virtual void ReturnToFirstScreen()
        {
            if (!ScreenTransition.IsBusy)
                ScreenTransition.SceneAsync("Assets/Scenes/0_Idle.unity", default, TransitionSound.ReturnHome).Forget();
        }
    }
}

using Hardware;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    /// <summary>
    /// HelpPanel이 켜져 있는 동안 보드에 카드가 남아 있으면 BoardClearPopup을 띄우고 콘텐츠 기능을 막음.
    /// 모든 리더가 비면 팝업을 닫고 차단을 해제하며, 그 상태에서만 다음 단계로 진행할 수 있음.
    /// HelpPanel 오브젝트에 붙여 두면 패널 활성화 여부에 따라 자동으로 동작/중지됨.
    /// </summary>
    public class HelpPanelController : MonoBehaviour, IContentBlocker
    {
        [SerializeField] private GameObject boardClearPopup;
        [SerializeField] private CanvasGroup blackBox;
        private Sequence popupTween;
        private Vector3 popupRestScale = Vector3.one;
        private bool showing;

        private void Awake()
        {
            if (boardClearPopup != null) popupRestScale = boardClearPopup.transform.localScale;
        }

        [Tooltip("보드가 비어 있는 상태에서 TryProceed가 호출되면 실행됨.")]
        [SerializeField] private UnityEvent onProceed;

        /// <summary>BoardClearPopup이 표시 중인지 여부.</summary>
        public bool IsPopupShown => boardClearPopup != null && boardClearPopup.activeSelf;

        private void OnEnable()
        {
            showing = false;
            ContentGate.Register(this);
        }

        /// <summary>
        /// 팝업은 HelpPanel의 자식이라 함께 가려지므로 여기서 끄지 않음(부모 비활성화 도중 자식 SetActive는 오류 발생).
        /// 다시 켜질 때 Register가 즉시 RefreshBlocking을 호출해 팝업 상태를 맞춤.
        /// </summary>
        private void OnDisable()
        {
            ContentGate.Unregister(this);
            popupTween?.Kill();
            popupTween = null;
            showing = false;
        }

        /// <summary>
        /// ContentGate가 등록 시점과 RFID 이벤트마다 호출함. 카드가 하나라도 인식되어 있으면 팝업을 띄우고 차단함.
        /// </summary>
        public bool RefreshBlocking()
        {
            bool isBlocking = !RfidInput.IsBoardClear;
            SetPopupActive(isBlocking);
            return isBlocking;
        }

        /// <summary>
        /// 다음 단계로 진행을 시도함. 리더에 카드가 남아 있으면 팝업을 띄우고 false를 반환하며,
        /// 모두 비어 있을 때만 onProceed를 실행함. 진행 트리거(버튼, 타이머 등)는 이 메서드를 호출할 것.
        /// </summary>
        public bool TryProceed()
        {
            if (ScreenTransition.IsBusy || RefreshBlocking()) return false;

            onProceed?.Invoke();
            return true;
        }

        private void SetPopupActive(bool isActive)
        {
            if (boardClearPopup == null) return;
            if (isActive && showing) return;
            showing = isActive;
            popupTween?.Kill();
            popupTween = null;
            if (isActive && blackBox != null)
                popupTween = PopupAnimation.Show(blackBox, (RectTransform)boardClearPopup.transform, popupRestScale, gameObject);
            else
            {
                boardClearPopup.transform.localScale = popupRestScale;
                boardClearPopup.SetActive(isActive);
                if (blackBox != null) blackBox.gameObject.SetActive(false);
            }
        }
    }
}

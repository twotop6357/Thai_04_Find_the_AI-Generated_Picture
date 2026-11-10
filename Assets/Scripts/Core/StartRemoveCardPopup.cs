using DG.Tweening;
using Hardware;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// On Title, any card on a reader shows the remove-cards popup with the same entrance as the Finish popup,
    /// and start input waits until every reader is empty. Tutorial does not show the popup.
    /// The popup image is language-specific through ClassificationUiLanguage, so a Thai image can be assigned later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StartRemoveCardPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup blackBox;
        [SerializeField] private RectTransform popup;
        [SerializeField] private ClassificationEntryController entryController;

        private Sequence popupAnimation;
        private Vector3 popupRestScale;
        private bool showing;
        private static StartRemoveCardPopup current;

        /// <summary>True while cards on the readers block the start of the experience.</summary>
        public static bool BlocksStart => current != null && current.isActiveAndEnabled && current.showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => current = null;

        private void Awake()
        {
            if (blackBox == null || popup == null)
            {
                Debug.LogError("[StartRemoveCardPopup] BlackBox and the popup are required.", this);
                enabled = false; return;
            }
            popupRestScale = popup.localScale;
        }

        private void OnEnable()
        {
            current = this;
            HideImmediate();
        }

        private void Update()
        {
            bool cardsOnBoard = !RfidInput.IsBoardClear;
            // Only Title shows the popup; Tutorial ignores cards. A start already in progress keeps its transition,
            // and the operator mapping window owns the screen while open.
            bool canShow = ClassificationEntryController.IsTitleShowing && !RfidMappingWindow.IsOpen &&
                (entryController == null || !entryController.IsStarting);
            if (!showing && cardsOnBoard && canShow) Show();
            else if (showing && (!cardsOnBoard || !canShow)) HideImmediate();
        }

        private void Show()
        {
            showing = true;
            popupAnimation?.Kill();
            popupAnimation = PopupAnimation.Show(blackBox, popup, popupRestScale, gameObject);
            Debug.Log("[StartRemoveCardPopup] Cards are on the board; waiting for them to be removed before starting.", this);
        }

        private void HideImmediate()
        {
            bool wasShowing = showing;
            showing = false;
            popupAnimation?.Kill(); popupAnimation = null;
            blackBox.alpha = 0;
            blackBox.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);
            popup.localScale = popupRestScale;
            if (wasShowing) Debug.Log("[StartRemoveCardPopup] The board is clear; start is available.", this);
        }

        private void OnDisable()
        {
            if (blackBox != null && popup != null) HideImmediate();
            if (current == this) current = null;
        }
    }
}

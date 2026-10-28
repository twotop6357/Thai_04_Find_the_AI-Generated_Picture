using Hardware;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace Core
{
    /// <summary>Captures one completed board and presents its score in reader order.</summary>
    [DisallowMultipleComponent]
    public sealed class ClassificationResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text statisticsText;
        [SerializeField] private ImageClassificationChart[] imageCharts = new ImageClassificationChart[6];
        [SerializeField] private ResultMarkGraphic[] marks = new ResultMarkGraphic[6];

        [SerializeField] private RfidImagePanel rfidImages;
        [SerializeField] private ClassificationPresentation presentation;
        [SerializeField] private StatisticsPresentation statisticsPresentation;
        [SerializeField] private string finishScenePath = "Assets/Scenes/2_Finish.unity";
        private CancellationTokenSource finishCancellation;
        [SerializeField] private GameObject retryPopup;
        [SerializeField] private CanvasGroup retryBlackBox;
        private Sequence retryAnimation;
        private ImageClassificationStatistics statistics;
        private readonly string[] submittedCards = new string[6];
        private bool hasResult;
        private bool transitioning;
        private int transitionVersion;
        public bool IsTransitioning
        {
            get
            {
                if (transitioning || ScreenTransition.IsBusy) return true;
                if (gamePanel != null && gamePanel.activeInHierarchy && rfidImages != null && rfidImages.HasPendingCardAnimations) return true;
                if (resultPanel != null && resultPanel.activeInHierarchy && presentation != null && presentation.IsAnimating) return true;
                if (statisticsPanel != null && statisticsPanel.activeInHierarchy && statisticsPresentation != null && statisticsPresentation.IsAnimating) return true;
                foreach (var chart in imageCharts) if (chart != null && chart.isActiveAndEnabled && chart.IsAnimating) return true;
                return false;
            }
        }
        private bool waitingForClear;
        private int rounds;

        public bool HasResult => hasResult;
        public int CorrectCount { get; private set; }
        public int CompletedRounds => rounds;

        private void Awake()
        {
            statistics = new ImageClassificationStatistics();
        }

        private void LateUpdate()
        {
            if (ContentGate.IsBlocked) return;
            if (waitingForClear)
            {
                if (RfidInput.IsBoardClear && !transitioning && !ScreenTransition.IsBusy) BeginNextRound();
                return;
            }
            if (IsTransitioning) return;

            if (SingleButtonInput.WasPressedThisFrame)
            {
                PressButton1();
            }
        }

        /// <summary>Submit the board, or open response charts from the score screen. Call on Unity's main thread.</summary>
        public void PressButton1()
        {
            if (!isActiveAndEnabled || waitingForClear || IsTransitioning) return;
            if (hasResult) PressRedButton();
            else TryShowResults();
        }

        public bool TryShowResults()
        {
            if (!isActiveAndEnabled || IsTransitioning || ContentGate.IsBlocked || hasResult || gamePanel == null || !gamePanel.activeInHierarchy) return false;
            if (resultPanel == null || statisticsPanel == null || (scoreText == null && presentation == null) ||
                (presentation == null && (marks == null || marks.Length != 6 || System.Array.Exists(marks, mark => mark == null))))
                return false;

            for (int i = 0; i < 6; i++)
            {
                string card = ContentGate.GetGameCard((RfidReaderSlot)(i + 1));
                if (RfidCardCatalog.GetCardType(card) == RfidCardType.Unknown) return false;
                submittedCards[i] = card;
            }

            if (!statistics.TryRecordRound(submittedCards)) return false;

            CorrectCount = 0;
            for (int i = 0; i < 6; i++)
            {
                bool correct = RfidCardCatalog.GetCardType(submittedCards[i]) == (i < 3 ? RfidCardType.Ai : RfidCardType.Human);
                if (marks != null && i < marks.Length && marks[i] != null) marks[i].SetResult(correct);
                if (correct) CorrectCount++;
            }

            if (presentation != null) presentation.Prepare(CorrectCount, submittedCards);
            hasResult = true;
            rounds++;
            if (scoreText != null) scoreText.text = $"You got {CorrectCount}\nout of 6 right!";
            if (instructionText != null) instructionText.text = "Press button 1 or the red button to see how others did.";
            statisticsPanel.SetActive(false);
            ChangePanel(gamePanel, resultPanel);
            return true;
        }

        /// <summary>UI button, keyboard, and future hardware button share this entry point.</summary>
        public void PressRedButton()
        {
            if (!isActiveAndEnabled || IsTransitioning || !hasResult || waitingForClear || ContentGate.IsBlocked) return;
            if (statisticsPanel.activeInHierarchy)
            {
                OpenFinishAsync().Forget();
                return;
            }
            if (!resultPanel.activeInHierarchy) return;
            foreach (var chart in imageCharts) if (chart != null) chart.Refresh(statistics);
            if (statisticsText != null) statisticsText.text = $"All recorded responses on this device, including yours • {statistics.CompletedRounds:N0} completed rounds";
            ChangePanel(resultPanel, statisticsPanel);
        }

        private async UniTask OpenFinishAsync()
        {
            transitioning = true;
            int version = ++transitionVersion;
            var cancellation = new CancellationTokenSource();
            finishCancellation = cancellation;
            try { await ScreenTransition.SceneAsync(finishScenePath, cancellation.Token); }
            finally
            {
                if (finishCancellation == cancellation) finishCancellation = null;
                cancellation.Dispose();
                if (this != null && version == transitionVersion) transitioning = false;
            }
        }

        public void CloseStatistics()
        {
            if (isActiveAndEnabled && !ContentGate.IsBlocked && !IsTransitioning && statisticsPanel.activeInHierarchy)
                ChangePanel(statisticsPanel, resultPanel);
        }

        public void TryAgain()
        {
            if (!isActiveAndEnabled || IsTransitioning || !hasResult || ContentGate.IsBlocked) return;
            waitingForClear = true;
            ShowRetryPopup();
            if (instructionText != null) instructionText.text = "Remove all cards from the board to play again.";
            if (RfidInput.IsBoardClear) BeginNextRound();
            else if (statisticsPanel.activeInHierarchy) ChangePanel(statisticsPanel, resultPanel);
        }


        private void ChangePanel(GameObject from, GameObject to)
        {
            if (to == resultPanel && presentation != null) presentation.HideForEntry();
            if (to == statisticsPanel && statisticsPresentation != null) statisticsPresentation.HideForEntry();
            transitioning = true;
            int version = ++transitionVersion;
            ChangePanelAsync(from, to, version).Forget();
        }

        private async UniTask ChangePanelAsync(GameObject from, GameObject to, int version)
        {
            try
            {
                bool completed = await ScreenTransition.PanelsAsync(this, from, to);
                if (version != transitionVersion || !isActiveAndEnabled) return;
                if (!completed) RestoreRoundPanel();
                else if (to == resultPanel && presentation != null) presentation.Play();
                else if (to == statisticsPanel)
                {
                    if (statisticsPresentation != null) statisticsPresentation.Play();
                    else for (int i = 0; i < imageCharts.Length; i++) if (imageCharts[i] != null) imageCharts[i].Play();
                }
            }
            finally { if (version == transitionVersion) transitioning = false; }
        }

        private void OnEnable()
        {
            RestoreRoundPanel();
            if (hasResult && !waitingForClear && resultPanel != null && resultPanel.activeInHierarchy && presentation != null) presentation.Play();
            if (hasResult && statisticsPanel != null && statisticsPanel.activeInHierarchy && statisticsPresentation != null) statisticsPresentation.Play();
            if (waitingForClear) ShowRetryPopup();
        }

        private void RestoreRoundPanel()
        {
            if (gamePanel == null || resultPanel == null || statisticsPanel == null) return;
            // Keep a round already saved before a cancelled fade; never submit it twice.
            if (hasResult && gamePanel.activeSelf)
            {
                ScreenTransition.ShowImmediate(gamePanel, false);
                ScreenTransition.ShowImmediate(resultPanel, true);
            }
            else if (!hasResult)
            {
                ScreenTransition.ShowImmediate(statisticsPanel, false);
                ScreenTransition.ShowImmediate(resultPanel, false);
                ScreenTransition.ShowImmediate(gamePanel, true);
            }
        }

        private void OnDisable()
        {
            ++transitionVersion;
            ScreenTransition.Cancel(this);
            // Loading a new active scene owns the final fade, even while this old scene is being unloaded.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() == gameObject.scene) finishCancellation?.Cancel();
            if (presentation != null) presentation.Stop();
            if (statisticsPresentation != null) statisticsPresentation.Stop();
            HideRetryPopup();
            transitioning = false;
        }

        private void ShowRetryPopup()
        {
            if (retryPopup == null || retryBlackBox == null) return;
            retryAnimation?.Kill();
            retryAnimation = PopupAnimation.Show(retryBlackBox, (RectTransform)retryPopup.transform, Vector3.one, resultPanel);
        }

        private void HideRetryPopup()
        {
            retryAnimation?.Kill(); retryAnimation = null;
            if (retryPopup != null) retryPopup.SetActive(false);
            if (retryBlackBox != null) retryBlackBox.gameObject.SetActive(false);
        }

        private void BeginNextRound()
        {
            HideRetryPopup();
            waitingForClear = false;
            hasResult = false;
            CorrectCount = 0;
            System.Array.Clear(submittedCards, 0, submittedCards.Length);
            var source = statisticsPanel.activeInHierarchy ? statisticsPanel : resultPanel;
            ChangePanel(source, gamePanel);
        }
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hardware;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public enum ClassificationLanguage { Thai, English }

    /// <summary>At Title, a two-second hold switches the language in place and a short press starts in that language.</summary>
    public sealed class ClassificationEntryController : MonoBehaviour
    {
        public const double LanguageHoldSeconds = 2;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TutorialCardPresentation tutorialPresentation;
        [SerializeField] private string gameScenePath = "Assets/Scenes/1_Game.unity";
        private bool starting;
        private bool autoAdvanceRequested;
        private bool physicalHeld;
        private bool pressed;
        private bool waitForRelease;
        private double pressedAt;
        private int version;
        private CancellationTokenSource cancellation;
        private static ClassificationEntryController current;
        public bool IsStarting => starting;
        /// <summary>No inactivity return is needed while already on the first screen.</summary>
        public static bool IsTitleShowing => current != null && current.isActiveAndEnabled &&
            current.titlePanel != null && current.titlePanel.activeInHierarchy;
        public static ClassificationLanguage SelectedLanguage { get; private set; } = ClassificationLanguage.Thai;
        /// <summary>The argument is true when the change should play the language fade.</summary>
        public static event Action<bool> LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLanguage()
        {
            SelectedLanguage = ClassificationLanguage.Thai;
            LanguageChanged = null;
            current = null;
        }

        private void OnEnable()
        {
            current = this;
            ++version; starting = false; pressed = false; autoAdvanceRequested = false;
            waitForRelease = IsHeld();
        }

        private void Start()
        {
            // Keep the language of the last experience; the app itself starts in Thai.
            // Preserve the designer's editor visibility, but always enter the experience at Title.
            ScreenTransition.ShowImmediate(tutorialPanel, false);
            ScreenTransition.ShowImmediate(titlePanel, true);
        }

        private bool IsHeld()
        {
            return physicalHeld || SingleButtonInput.IsPressed;
        }

        private void Update()
        {
            bool held = IsHeld();
            if (SingleButtonInput.IsSuppressed)
            {
                pressed = false; waitForRelease = true; return;
            }
            // While the Title remove-cards popup is up, starting and the language switch wait until the cards are removed.
            if (starting || ScreenTransition.IsBusy || ClassificationUiLanguage.IsFading || RfidMappingWindow.IsOpen ||
                StartRemoveCardPopup.BlocksStart)
            {
                pressed = false; waitForRelease = held; return;
            }
            if (tutorialPanel.activeInHierarchy && tutorialPresentation != null && tutorialPresentation.IsComplete && !autoAdvanceRequested)
            {
                autoAdvanceRequested = true;
                AdvanceAsync(false, version, TransitionSound.None).Forget();
                return;
            }
            if (waitForRelease)
            {
                if (!held) waitForRelease = false;
                return;
            }
            if (titlePanel.activeInHierarchy)
            {
                // Check the duration before release handling, including a release exactly at 2 seconds.
                if (pressed && Time.realtimeSinceStartupAsDouble - pressedAt >= LanguageHoldSeconds)
                {
                    // Require a release so one hold switches once and never also starts.
                    pressed = false; waitForRelease = true;
                    SetLanguage(SelectedLanguage == ClassificationLanguage.Thai
                        ? ClassificationLanguage.English : ClassificationLanguage.Thai, true);
                    TransitionSoundManager.Play(TransitionSound.LanguageChange);
                    return;
                }
                if (held)
                {
                    if (!pressed) { pressed = true; pressedAt = Time.realtimeSinceStartupAsDouble; }
                }
                else if (pressed)
                {
                    pressed = false;
                    StartTutorial();
                }
            }
            else
            {
                if (held && !pressed) { pressed = true; PressButton1(); }
                if (!held) pressed = false;
            }
        }

        /// <summary>A completed short press; hardware that supplies down/up must use SetPhysicalButtonPressed.</summary>
        public void PressButton1()
        {
            if (!isActiveAndEnabled || starting || ScreenTransition.IsBusy ||
                ClassificationUiLanguage.IsFading || RfidMappingWindow.IsOpen || StartRemoveCardPopup.BlocksStart) return;
            if (titlePanel.activeInHierarchy) StartTutorial();
            else if (tutorialPanel.activeInHierarchy && tutorialPresentation != null && tutorialPresentation.IsComplete)
            {
                autoAdvanceRequested = true;
                AdvanceAsync(false, version, TransitionSound.Button).Forget();
            }
        }

        /// <summary>Button down/up on Unity's main thread. The saved button binding follows the same duration logic.</summary>
        public void SetPhysicalButtonPressed(bool held)
        {
            if (isActiveAndEnabled) physicalHeld = held;
        }

        private void SetLanguage(ClassificationLanguage language, bool animated)
        {
            if (SelectedLanguage == language) return;
            SelectedLanguage = language;
            LanguageChanged?.Invoke(animated);
            Debug.Log($"[ClassificationEntryController] Language changed to {language}.", this);
        }

        private void StartTutorial()
        {
            if (starting || ScreenTransition.IsBusy) return;
            autoAdvanceRequested = false;
            Debug.Log($"[ClassificationEntryController] Starting {SelectedLanguage} mode.", this);
            AdvanceAsync(true, version, TransitionSound.Button).Forget();
        }

        private async UniTask AdvanceAsync(bool toTutorial, int operationVersion, TransitionSound sound)
        {
            starting = true;
            var source = new CancellationTokenSource(); cancellation = source;
            try
            {
                if (toTutorial) await ScreenTransition.PanelsAsync(this, titlePanel, tutorialPanel, source.Token, sound);
                else await ScreenTransition.SceneAsync(gameScenePath, source.Token, sound);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogError($"[ClassificationEntryController] Entry transition failed: {exception}", this); }
            finally
            {
                if (cancellation == source) cancellation = null;
                source.Dispose();
                if (version == operationVersion && isActiveAndEnabled) starting = false;
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            pressed = false; waitForRelease = true;
        }

        private void OnDisable()
        {
            ++version; pressed = false; physicalHeld = false;
            ScreenTransition.Cancel(this);
            if (SceneManager.GetActiveScene() == gameObject.scene) cancellation?.Cancel();
        }
    }
}

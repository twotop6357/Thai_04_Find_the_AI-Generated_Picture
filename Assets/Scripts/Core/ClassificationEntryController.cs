using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hardware;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public enum ClassificationLanguage { Thai, English }

    /// <summary>Title button duration selects a logged language; artwork remains English.</summary>
    public sealed class ClassificationEntryController : MonoBehaviour
    {
        public const double EnglishHoldSeconds = 2;
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
        public bool IsStarting => starting;
        public static ClassificationLanguage SelectedLanguage { get; private set; } = ClassificationLanguage.English;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLanguage() => SelectedLanguage = ClassificationLanguage.English;

        private void OnEnable()
        {
            ++version; starting = false; pressed = false; autoAdvanceRequested = false;
            waitForRelease = IsHeld();
        }

        private void Start()
        {
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
            if (starting || ScreenTransition.IsBusy)
            {
                pressed = false; waitForRelease = held; return;
            }
            if (tutorialPanel.activeInHierarchy && tutorialPresentation != null && tutorialPresentation.IsComplete && !autoAdvanceRequested)
            {
                autoAdvanceRequested = true;
                AdvanceAsync(false, version).Forget();
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
                if (pressed && Time.realtimeSinceStartupAsDouble - pressedAt >= EnglishHoldSeconds)
                {
                    pressed = false; waitForRelease = true;
                    StartTutorial(ClassificationLanguage.English);
                    return;
                }
                if (held)
                {
                    if (!pressed) { pressed = true; pressedAt = Time.realtimeSinceStartupAsDouble; }
                }
                else if (pressed)
                {
                    pressed = false;
                    StartTutorial(ClassificationLanguage.Thai);
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
            if (!isActiveAndEnabled || starting || ScreenTransition.IsBusy || SingleButtonInput.IsSettingsOpen) return;
            if (titlePanel.activeInHierarchy) StartTutorial(ClassificationLanguage.Thai);
            else if (tutorialPanel.activeInHierarchy && tutorialPresentation != null && tutorialPresentation.IsComplete)
            {
                autoAdvanceRequested = true;
                AdvanceAsync(false, version).Forget();
            }
        }

        /// <summary>Button down/up on Unity's main thread. The saved button binding follows the same duration logic.</summary>
        public void SetPhysicalButtonPressed(bool held)
        {
            if (isActiveAndEnabled) physicalHeld = held;
        }

        private void StartTutorial(ClassificationLanguage language)
        {
            if (starting || ScreenTransition.IsBusy) return;
            autoAdvanceRequested = false;
            SelectedLanguage = language;
            Debug.Log($"[ClassificationEntryController] Starting {language} mode. Language selection is logged only; English artwork remains active.", this);
            AdvanceAsync(true, version).Forget();
        }

        private async UniTask AdvanceAsync(bool toTutorial, int operationVersion)
        {
            starting = true;
            var source = new CancellationTokenSource(); cancellation = source;
            try
            {
                if (toTutorial) await ScreenTransition.PanelsAsync(this, titlePanel, tutorialPanel, source.Token);
                else await ScreenTransition.SceneAsync(gameScenePath, source.Token);
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

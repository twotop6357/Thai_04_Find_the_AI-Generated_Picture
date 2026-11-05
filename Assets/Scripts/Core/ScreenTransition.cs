using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core
{
    public interface ISceneDisplayReady
    {
        bool IsDisplayReady { get; }
    }

    /// <summary>Scene-independent single owner of transitions and the input lock. Does not touch animations inside panels.</summary>
    public sealed class ScreenTransition : MonoBehaviour
    {
        public const float FadeDuration = 0.5f;
        private static ScreenTransition _instance;
        private CanvasGroup _overlay;
        private Operation _current;
        private bool _nativeLoadPending;

        private sealed class Operation
        {
            public UnityEngine.Object Owner;
            public CancellationTokenSource Cancellation;
            public Tween Tween;
            public Action Recover;
        }

        public static bool IsBusy => _instance != null && _instance._current != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var root = new GameObject("ScreenTransition", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<ScreenTransition>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _instance._overlay = root.GetComponent<CanvasGroup>();
            var image = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(root.transform, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;
            _instance.ReleaseOverlay();
        }

        private Operation Acquire(UnityEngine.Object owner, CancellationToken token)
        {
            if (_current != null || token.IsCancellationRequested) return null;
            var operation = new Operation { Owner = owner,
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token) };
            _current = operation;
            _overlay.blocksRaycasts = true;
            _overlay.interactable = true;
            return operation;
        }

        public static void Cancel(UnityEngine.Object owner)
        {
            if (_instance == null || _instance._current == null || _instance._current.Owner != owner) return;
            _instance.Abort(_instance._current);
        }

        private void Abort(Operation operation)
        {
            operation.Cancellation.Cancel();
            operation.Tween?.Kill();
            if (_current != operation) return;
            try { operation.Recover?.Invoke(); }
            finally { _current = null; ReleaseOverlay(); }
        }

        private void ReleaseOverlay()
        {
            if (_overlay == null) return;
            _overlay.alpha = 0;
            _overlay.blocksRaycasts = false;
            _overlay.interactable = false;
        }

        // Advances only its own paused tween and clamps the large unscaledDeltaTime of scene load frames.
        private async UniTask FadeAsync(CanvasGroup group, float alpha, Operation operation, Func<bool> valid)
        {
            var token = operation.Cancellation.Token;
            var tween = group.DOFade(alpha, FadeDuration).SetEase(Ease.InOutSine)
                .SetUpdate(true).SetAutoKill(false).Pause();
            operation.Tween = tween;
            float elapsed = 0;
            try
            {
                while (elapsed < FadeDuration)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    token.ThrowIfCancellationRequested();
                    if (_current != operation || group == null || !valid())
                        throw new OperationCanceledException(token);
                    elapsed = Mathf.Min(FadeDuration, elapsed + Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
                    tween.Goto(elapsed, false);
                }
            }
            finally { tween.Kill(); if (operation.Tween == tween) operation.Tween = null; }
        }

        private static CanvasGroup Group(GameObject panel)
        {
            var group = panel.GetComponent<CanvasGroup>();
            return group != null ? group : panel.AddComponent<CanvasGroup>();
        }

        public static void ShowImmediate(GameObject panel, bool visible)
        {
            if (panel == null) return;
            Group(panel).alpha = 1;
            panel.SetActive(visible);
        }

        public static async UniTask<bool> PanelsAsync(MonoBehaviour owner, GameObject from, GameObject to,
            CancellationToken token = default, TransitionSound sound = TransitionSound.None)
        {
            if (_instance == null || IsBusy || owner == null || !owner.isActiveAndEnabled || from == null || to == null)
                return false;
            if (from == to) return true;
            var host = _instance;
            var operation = host.Acquire(owner, token);
            if (operation == null) return false;
            TransitionSoundManager.Play(sound);
            bool success = false;
            bool switched = false;
            var outgoing = Group(from);
            var incoming = Group(to);
            operation.Recover = () =>
            {
                // Restore the valid new screen if the exit already finished; otherwise restore the original screen.
                bool keepNext = to != null && (from == null || (switched && to.activeInHierarchy));
                ShowImmediate(to, keepNext);
                ShowImmediate(from, !keepNext);
            };
            try
            {
                await host.FadeAsync(outgoing, 0, operation,
                    () => owner != null && owner.isActiveAndEnabled && from != null && from.activeInHierarchy && to != null);
                // OnEnable from this activation also runs at alpha 0.
                incoming.alpha = 0;
                from.SetActive(false);
                to.SetActive(true);
                switched = true;
                await host.FadeAsync(incoming, 1, operation,
                    () => owner != null && owner.isActiveAndEnabled && to != null && to.activeInHierarchy);
                success = true;
                return true;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception exception)
            {
                Debug.LogError($"[ScreenTransition] Panel transition failed: {exception}");
                return false;
            }
            finally
            {
                if (host != null && host._current == operation)
                {
                    if (!success) operation.Recover();
                    host._current = null;
                    host.ReleaseOverlay();
                }
                operation.Cancellation.Dispose();
            }
        }

        /// <summary>Loads additively only while the screen is black, to preserve the original scene if preparation fails or is cancelled.</summary>
        public static async UniTask<bool> SceneAsync(string scenePath, CancellationToken token = default,
            TransitionSound sound = TransitionSound.None)
        {
            if (_instance == null || IsBusy || _instance._nativeLoadPending) return false;
            var host = _instance;
            var operation = host.Acquire(host, token);
            if (operation == null) return false;
            var previous = SceneManager.GetActiveScene();
            var handles = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++) handles.Add(SceneManager.GetSceneAt(i).handle);
            AsyncOperation loading = null;
            Scene next = default;
            bool committed = false;
            var suspendedInput = new List<EventSystem>();
            var sceneToken = operation.Cancellation.Token;
            UnityAction<Scene, LoadSceneMode> onLoaded = (loaded, mode) =>
            {
                if (handles.Contains(loaded.handle) || mode != LoadSceneMode.Additive) return;
                next = loaded;
                if (sceneToken.IsCancellationRequested)
                    foreach (var root in loaded.GetRootGameObjects()) root.SetActive(false);
            };
            try
            {
                using var timeout = operation.Cancellation.CancelAfterSlim(TimeSpan.FromSeconds(30), DelayType.Realtime);
                if (!Application.CanStreamedLevelBeLoaded(scenePath))
                    throw new ArgumentException("Scene is unavailable in Build Settings: " + scenePath);
                TransitionSoundManager.Play(sound);
                await host.FadeAsync(host._overlay, 1, operation, () => host.isActiveAndEnabled);
                // Avoid overlapping active EventSystems during the additive load; restore them on cancellation.
                foreach (var root in previous.GetRootGameObjects())
                    foreach (var input in root.GetComponentsInChildren<EventSystem>())
                        if (input.enabled) { suspendedInput.Add(input); input.enabled = false; }
                host._nativeLoadPending = true;
                SceneManager.sceneLoaded += onLoaded;
                loading = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                if (loading == null) throw new InvalidOperationException("Scene loading did not start.");
                while (!loading.isDone) await UniTask.NextFrame(cancellationToken: operation.Cancellation.Token);
                if (!next.IsValid()) next = FindNewScene(handles);
                if (!next.IsValid()) throw new InvalidOperationException("Loaded scene was not found.");
                // Finish Start and async data display preparation while the screen is black.
                await UniTask.NextFrame(cancellationToken: operation.Cancellation.Token);
                var readiness = new List<ISceneDisplayReady>();
                foreach (var root in next.GetRootGameObjects())
                    foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>())
                        if (behaviour.isActiveAndEnabled && behaviour is ISceneDisplayReady ready) readiness.Add(ready);
                await UniTask.WaitUntil(() => readiness.TrueForAll(item => item.IsDisplayReady),
                    cancellationToken: operation.Cancellation.Token);
                operation.Cancellation.Token.ThrowIfCancellationRequested();
                SceneManager.SetActiveScene(next);
                committed = true;
                // A cancellation after the swap is committed shows the new scene immediately and restores input.
                var unload = SceneManager.UnloadSceneAsync(previous);
                if (unload != null)
                    while (!unload.isDone) await UniTask.NextFrame(cancellationToken: operation.Cancellation.Token);
                Canvas.ForceUpdateCanvases();
                await UniTask.NextFrame(cancellationToken: operation.Cancellation.Token);
                await host.FadeAsync(host._overlay, 0, operation, () => host.isActiveAndEnabled);
                return true;
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested && host != null && host.isActiveAndEnabled)
                    Debug.LogWarning("[ScreenTransition] Scene transition timed out or was interrupted. Restoring display and input.");
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ScreenTransition] Scene transition failed: {exception}");
                return false;
            }
            finally
            {
                // Unity's native load cannot be cancelled, so only discard the additive scene when it arrives late.
                if (!committed && loading != null)
                    host.CleanupLoadAsync(loading, handles, previous, onLoaded, suspendedInput).Forget();
                else
                {
                    SceneManager.sceneLoaded -= onLoaded;
                    host._nativeLoadPending = false;
                }
                if (!committed && loading == null) RestoreInput(suspendedInput);
                if (host._current == operation) { host._current = null; host.ReleaseOverlay(); }
                operation.Cancellation.Dispose();
            }
        }

        private static Scene FindNewScene(HashSet<int> oldHandles)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!oldHandles.Contains(scene.handle)) return scene;
            }
            return default;
        }

        private async UniTask CleanupLoadAsync(AsyncOperation loading, HashSet<int> handles, Scene previous,
            UnityAction<Scene, LoadSceneMode> onLoaded, List<EventSystem> suspendedInput)
        {
            try
            {
                float inputDeadline = Time.realtimeSinceStartup + 1;
                bool restored = false;
                while (!loading.isDone)
                {
                    // Usually done within a few frames. Even if the native load stalls, existing UI input recovers within one second.
                    if (!restored && Time.realtimeSinceStartup >= inputDeadline)
                    {
                        RestoreInput(suspendedInput);
                        restored = true;
                    }
                    await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
                }
                var abandoned = FindNewScene(handles);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (abandoned.IsValid())
                {
                    foreach (var root in abandoned.GetRootGameObjects()) root.SetActive(false);
                    RestoreInput(suspendedInput);
                    var unload = SceneManager.UnloadSceneAsync(abandoned);
                    if (unload != null) await unload.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogError($"[ScreenTransition] Abandoned scene cleanup failed: {exception}"); }
            finally
            {
                RestoreInput(suspendedInput);
                SceneManager.sceneLoaded -= onLoaded;
                _nativeLoadPending = false;
            }
        }

        private static void RestoreInput(List<EventSystem> inputs)
        {
            foreach (var input in inputs) if (input != null) input.enabled = true;
        }

        private void OnDisable()
        {
            if (_current != null) Abort(_current);
            ReleaseOverlay();
        }
    }
}

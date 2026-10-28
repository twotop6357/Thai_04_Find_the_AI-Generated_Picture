using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Hardware;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Core
{
    /// <summary>One decoder and one reusable GPU target, revealed only after a decoded frame is ready.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class FinishVideoPresentation : MonoBehaviour, ISceneDisplayReady
    {
        [SerializeField] private VideoClip clip;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private CanvasGroup blackBox;
        [SerializeField] private RectTransform removeCardPopup;
        private VideoPlayer player;
        private RenderTexture target;
        private Sequence popupAnimation;
        private Vector3 popupRestScale;
        private bool firstFrameReady;
        private bool ended;
        private bool returningToIdle;
        private CancellationTokenSource returnCancellation;
        public bool IsDisplayReady => firstFrameReady;
        public bool IsComplete => ended && (popupAnimation == null || !popupAnimation.IsActive() || !popupAnimation.IsPlaying());

        private void Awake()
        {
            player = GetComponent<VideoPlayer>();
            popupRestScale = removeCardPopup.localScale;
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            videoImage.raycastTarget = false;
        }

        private void OnEnable()
        {
            ResetVisuals();
            if (clip == null)
            {
                Debug.LogError("[FinishVideoPresentation] The finish video clip is missing.", this);
                return;
            }
            if (target == null)
            {
                // This clip is 1080p; do not allocate a second copy or a CPU frame cache.
                float fit = Mathf.Min(1, 1920f / clip.width, 1080f / clip.height);
                target = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(clip.width * fit)),
                    Mathf.Max(1, Mathf.RoundToInt(clip.height * fit)), 0, RenderTextureFormat.ARGB32)
                {
                    name = "FinishVideoTarget",
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }
            if (!target.IsCreated()) target.Create();
            var previous = RenderTexture.active;
            try { RenderTexture.active = target; GL.Clear(false, true, Color.black); }
            finally { RenderTexture.active = previous; }
            player.targetTexture = target;
            videoImage.texture = target;
            player.skipOnDrop = false; // Guarantee the first decoded frame before allowing normal frame dropping.
            player.sendFrameReadyEvents = true;
            player.prepareCompleted += OnPrepared;
            player.frameReady += OnFirstFrame;
            player.loopPointReached += OnEnded;
            player.errorReceived += OnError;
            player.Prepare();
        }

        private void OnPrepared(VideoPlayer source)
        {
            if (isActiveAndEnabled && source == player) source.Play();
        }

        private void Update()
        {
            // 영상과 안내 등장 연출이 끝난 뒤에만 실제 일곱 리더의 빈 상태로 복귀한다.
            if (!IsComplete || !removeCardPopup.gameObject.activeInHierarchy || !RfidInput.IsBoardClear ||
                returningToIdle || ScreenTransition.IsBusy) return;
            ReturnToIdleAsync().Forget();
        }

        private async UniTask ReturnToIdleAsync()
        {
            returningToIdle = true;
            var cancellation = new CancellationTokenSource();
            returnCancellation = cancellation;
            try { await ScreenTransition.SceneAsync("Assets/Scenes/0_Idle.unity", cancellation.Token); }
            finally
            {
                if (returnCancellation == cancellation) returnCancellation = null;
                cancellation.Dispose();
                if (this != null) returningToIdle = false;
            }
        }

        private void OnFirstFrame(VideoPlayer source, long frame)
        {
            if (!isActiveAndEnabled || firstFrameReady || source != player || frame < 0) return;
            firstFrameReady = true;
            videoImage.enabled = true;
            source.skipOnDrop = true;
            source.sendFrameReadyEvents = false;
        }

        private void OnEnded(VideoPlayer source)
        {
            if (!isActiveAndEnabled || ended || source != player || !firstFrameReady) return;
            ended = true;
            // Pause preserves the decoder's final frame in the same GPU target. Never Stop here.
            source.Pause();
            popupAnimation = PopupAnimation.Show(blackBox, removeCardPopup, popupRestScale, gameObject);
        }

        private void OnError(VideoPlayer source, string message)
        {
            if (isActiveAndEnabled)
                Debug.LogError("[FinishVideoPresentation] Video playback failed: " + message, this);
        }

        private void ResetVisuals()
        {
            firstFrameReady = false; ended = false;
            popupAnimation?.Kill(); popupAnimation = null;
            videoImage.enabled = false;
            videoImage.texture = null;
            blackBox.alpha = 0;
            blackBox.gameObject.SetActive(false);
            removeCardPopup.gameObject.SetActive(false);
            removeCardPopup.localScale = popupRestScale;
        }

        private void OnDisable()
        {
            // 새 씬이 활성화된 정상 언로드에서는 전환 소유자의 마지막 페이드를 취소하지 않는다.
            if (SceneManager.GetActiveScene() == gameObject.scene) returnCancellation?.Cancel();
            // Hide and detach before resetting the decoder, so a previous frame can never flash on reentry.
            ResetVisuals();
            player.prepareCompleted -= OnPrepared;
            player.frameReady -= OnFirstFrame;
            player.loopPointReached -= OnEnded;
            player.errorReceived -= OnError;
            player.sendFrameReadyEvents = false;
            player.Stop();
            player.targetTexture = null;
        }

        private void OnDestroy()
        {
            if (target == null) return;
            target.Release();
            Destroy(target);
            target = null;
        }
    }
}

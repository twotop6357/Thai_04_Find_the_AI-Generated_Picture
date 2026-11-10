using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Core
{
    /// <summary>
    /// Loops the Title background video without sound. The panel's still image (the video's first frame) stays visible
    /// underneath until the first decoded frame is ready, so the screen never flashes black.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DisallowMultipleComponent]
    public sealed class IdleBackgroundVideo : MonoBehaviour
    {
        [SerializeField] private VideoClip clip;
        [SerializeField] private RawImage videoImage;

        private VideoPlayer player;
        private RenderTexture target;
        private bool firstFrameReady;

        private void Awake()
        {
            player = GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.source = VideoSource.VideoClip;
            player.audioOutputMode = VideoAudioOutputMode.None;
            if (videoImage != null) videoImage.raycastTarget = false;
        }

        private void OnEnable()
        {
            if (videoImage != null) { videoImage.enabled = false; videoImage.texture = null; }
            firstFrameReady = false;
            if (clip == null || videoImage == null)
            {
                Debug.LogError("[IdleBackgroundVideo] The Title background clip or video image is missing; the still image stays visible.", this);
                return;
            }
            if (target == null)
            {
                target = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "IdleBackgroundVideoTarget",
                    useMipMap = false,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }
            if (!target.IsCreated()) target.Create();
            player.clip = clip;
            player.targetTexture = target;
            player.sendFrameReadyEvents = true;
            player.prepareCompleted += OnPrepared;
            player.frameReady += OnFirstFrame;
            player.errorReceived += OnError;
            player.Prepare();
        }

        private void OnPrepared(VideoPlayer source)
        {
            if (isActiveAndEnabled && source == player) source.Play();
        }

        private void OnFirstFrame(VideoPlayer source, long frame)
        {
            if (!isActiveAndEnabled || firstFrameReady || source != player) return;
            firstFrameReady = true;
            videoImage.texture = target;
            videoImage.enabled = true;
            source.sendFrameReadyEvents = false;
        }

        private void OnError(VideoPlayer source, string message)
        {
            Debug.LogError("[IdleBackgroundVideo] Video playback failed; the still image stays visible: " + message, this);
            if (videoImage != null) videoImage.enabled = false;
        }

        private void OnDisable()
        {
            if (videoImage != null) { videoImage.enabled = false; videoImage.texture = null; }
            player.prepareCompleted -= OnPrepared;
            player.frameReady -= OnFirstFrame;
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

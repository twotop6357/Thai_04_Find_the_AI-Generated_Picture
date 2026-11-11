using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Core
{
    /// <summary>
    /// Loops the Title background video without sound and without a visible seam.
    /// VideoPlayer's own looping seeks the running decoder back to frame 0, which stalls and can briefly show a
    /// frame decoded about eight frames earlier. Instead two players take turns: while one plays, the other is
    /// restarted and paused on frame 0, and the display switches to it once the last frame has been shown for one
    /// frame duration. The panel's still image (the video's first frame) stays visible underneath until the first
    /// frame is ready.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DisallowMultipleComponent]
    public sealed class IdleBackgroundVideo : MonoBehaviour
    {
        [SerializeField] private VideoClip clip;
        [SerializeField] private RawImage videoImage;

        private enum SlotState { Idle, WaitingForFirstFrame, Pausing, Checking, Ready, Playing }

        /// <summary>One decoder, its render target and a 1x1 texture used to check that the first frame arrived.</summary>
        private sealed class Slot
        {
            public VideoPlayer Player;
            public RenderTexture Target;
            public RenderTexture Check;
            public SlotState State;
            /// <summary>Time.frameCount when frameReady reported the first frame.</summary>
            public int FirstFrameEventAt;
            /// <summary>Incremented on every restart so a late readback from an earlier attempt is ignored.</summary>
            public int Attempt;
        }

        private readonly Slot[] slots = { new Slot(), new Slot() };
        private VideoPlayer addedPlayer;
        /// <summary>Index of the slot on screen, or -1 before the first frame.</summary>
        private int active = -1;
        /// <summary>Unscaled time (seconds) the last frame of the active slot was first seen, or -1.</summary>
        private double lastFrameSeenAt = -1;
        private long lastFrame;
        /// <summary>Duration of one video frame in seconds.</summary>
        private double frameSeconds;
        private bool failed;
        private bool canCheckFrames;

        private void Awake()
        {
            slots[0].Player = GetComponent<VideoPlayer>();
            addedPlayer = gameObject.AddComponent<VideoPlayer>();
            slots[1].Player = addedPlayer;
            foreach (var slot in slots) Configure(slot.Player);
            if (videoImage != null) videoImage.raycastTarget = false;
            canCheckFrames = SystemInfo.supportsAsyncGPUReadback;
        }

        private static void Configure(VideoPlayer player)
        {
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.source = VideoSource.VideoClip;
            player.audioOutputMode = VideoAudioOutputMode.None;
        }

        private void OnEnable()
        {
            HideVideo();
            active = -1; lastFrameSeenAt = -1; failed = false;
            if (clip == null || videoImage == null)
            {
                Debug.LogError("[IdleBackgroundVideo] The Title background clip or video image is missing; the still image stays visible.", this);
                failed = true;
                return;
            }
            lastFrame = (long)clip.frameCount - 1;
            frameSeconds = clip.frameRate > 0 ? 1.0 / clip.frameRate : 1.0 / 30.0;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.Target == null)
                {
                    slot.Target = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32)
                    {
                        name = "IdleBackgroundVideoTarget" + i,
                        useMipMap = false,
                        autoGenerateMips = false,
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    slot.Check = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) { name = "IdleBackgroundVideoCheck" + i };
                }
                if (!slot.Target.IsCreated()) slot.Target.Create();
                if (!slot.Check.IsCreated()) slot.Check.Create();
                slot.Player.clip = clip;
                slot.Player.targetTexture = slot.Target;
                slot.Player.frameReady += OnFrameReady;
                slot.Player.errorReceived += OnError;
                Preroll(slot);
            }
        }

        /// <summary>Restarts the slot from a fresh decoder; it is paused on frame 0 once that frame is in the target.</summary>
        private static void Preroll(Slot slot)
        {
            ++slot.Attempt;
            slot.State = SlotState.WaitingForFirstFrame;
            slot.Player.Stop();
            // Clear to transparent so the check below can tell a delivered frame (opaque) from a missing one.
            var previous = RenderTexture.active;
            RenderTexture.active = slot.Target;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
            slot.Player.sendFrameReadyEvents = true;
            slot.Player.Play();
        }

        private void OnFrameReady(VideoPlayer source, long frame)
        {
            var slot = Find(source);
            if (slot == null || slot.State != SlotState.WaitingForFirstFrame) return;
            source.sendFrameReadyEvents = false;
            slot.FirstFrameEventAt = Time.frameCount;
            slot.State = SlotState.Pausing;
        }

        /// <summary>
        /// frameReady can arrive before the frame is copied into the render target, so the pause waits one rendered
        /// frame. Occasionally the restarted player never copies frame 0 at all; a 1x1 GPU readback confirms the
        /// target is opaque, and an empty target is prepared again instead of being shown.
        /// </summary>
        private void AdvancePreroll(Slot slot)
        {
            if (slot.State != SlotState.Pausing || Time.frameCount <= slot.FirstFrameEventAt) return;
            if (slot.Player.frame > 0)
            {
                // A hitch let the player run past frame 0.
                Preroll(slot);
                return;
            }
            slot.Player.Pause();
            if (!canCheckFrames)
            {
                slot.State = SlotState.Ready;
                return;
            }
            slot.State = SlotState.Checking;
            Graphics.Blit(slot.Target, slot.Check);
            int attempt = slot.Attempt;
            AsyncGPUReadback.Request(slot.Check, 0, TextureFormat.RGBA32, request => OnChecked(slot, attempt, request));
        }

        private void OnChecked(Slot slot, int attempt, AsyncGPUReadbackRequest request)
        {
            if (this == null || !isActiveAndEnabled || slot.Attempt != attempt || slot.State != SlotState.Checking) return;
            if (request.hasError)
            {
                // The check itself failed; trust the player rather than stall the loop.
                slot.State = SlotState.Ready;
                return;
            }
            if (request.GetData<byte>()[3] >= 250)
            {
                slot.State = SlotState.Ready;
                return;
            }
            Debug.Log("[IdleBackgroundVideo] The first frame did not reach the render target; preparing that player again.", this);
            Preroll(slot);
        }

        private void Update()
        {
            if (failed) return;
            foreach (var slot in slots) AdvancePreroll(slot);
            if (active < 0)
            {
                if (slots[0].State == SlotState.Ready) Show(0);
                return;
            }
            var current = slots[active];
            if (current.Player.frame < lastFrame)
            {
                lastFrameSeenAt = -1;
                return;
            }
            double now = Time.unscaledTimeAsDouble;
            if (lastFrameSeenAt < 0) lastFrameSeenAt = now;
            // Hold the last frame for one frame duration; if the next slot is late, keep holding rather than jump.
            if (now - lastFrameSeenAt + 0.001 >= frameSeconds && slots[1 - active].State == SlotState.Ready)
            {
                Show(1 - active);
                Preroll(current);
            }
        }

        private void Show(int index)
        {
            var slot = slots[index];
            active = index;
            lastFrameSeenAt = -1;
            slot.State = SlotState.Playing;
            videoImage.texture = slot.Target;
            videoImage.enabled = true;
            slot.Player.Play();
        }

        private Slot Find(VideoPlayer player)
        {
            foreach (var slot in slots) if (slot.Player == player) return slot;
            return null;
        }

        private void OnError(VideoPlayer source, string message)
        {
            Debug.LogError("[IdleBackgroundVideo] Video playback failed; the still image stays visible: " + message, this);
            failed = true;
            HideVideo();
        }

        private void HideVideo()
        {
            if (videoImage == null) return;
            videoImage.enabled = false;
            videoImage.texture = null;
        }

        private void OnDisable()
        {
            HideVideo();
            foreach (var slot in slots)
            {
                if (slot.Player == null) continue;
                slot.Player.frameReady -= OnFrameReady;
                slot.Player.errorReceived -= OnError;
                slot.Player.sendFrameReadyEvents = false;
                slot.Player.Stop();
                slot.Player.targetTexture = null;
                slot.State = SlotState.Idle;
                ++slot.Attempt;
            }
            active = -1;
        }

        private void OnDestroy()
        {
            foreach (var slot in slots)
            {
                if (slot.Target != null) { slot.Target.Release(); Destroy(slot.Target); slot.Target = null; }
                if (slot.Check != null) { slot.Check.Release(); Destroy(slot.Check); slot.Check = null; }
            }
            if (addedPlayer != null) Destroy(addedPlayer);
        }
    }
}

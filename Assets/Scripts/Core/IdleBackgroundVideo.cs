using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

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
        /// <summary>
        /// Seconds after a switch before the finished player is restarted. Restarting a hardware decoder can block
        /// the main thread on integrated GPUs, so it is kept away from the loop point.
        /// </summary>
        private const double RestartDelaySeconds = 0.5;
        /// <summary>Seconds between diagnostics summary lines in Player.log (development builds: every minute).</summary>
        private const double DiagnosticsIntervalSeconds = 600;

        [SerializeField] private VideoClip clip;
        [SerializeField] private RawImage videoImage;

        private enum SlotState { Idle, Finished, Stopped, WaitingForFirstFrame, Pausing, Checking, Ready, Playing }

        /// <summary>One decoder, its render target and a 1x1 texture used to check that the first frame arrived.</summary>
        private sealed class Slot
        {
            public VideoPlayer Player;
            public RenderTexture Target;
            public RenderTexture Check;
            public SlotState State;
            /// <summary>Unscaled time (seconds) when a Finished slot is restarted.</summary>
            public double RestartAt;
            /// <summary>Time.frameCount of the last state step, so Stop, Play and Pause land on different frames.</summary>
            public int StepFrame;
            /// <summary>Incremented on every restart so a late readback from an earlier attempt is ignored.</summary>
            public int Attempt;
        }

        private readonly Slot[] slots = { new Slot(), new Slot() };
        private readonly Stopwatch callTimer = new Stopwatch();
        private VideoPlayer addedPlayer;
        private VideoLoopDiagnostics diagnostics;
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
            // Late frames are shown late rather than skipped: a skip reads as a jump in a slow background loop.
            player.skipOnDrop = false;
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
            VideoLoopDiagnostics.LogEnvironmentOnce(this);
            diagnostics = new VideoLoopDiagnostics(this, frameSeconds, Debug.isDebugBuild ? 60 : DiagnosticsIntervalSeconds);
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
                Stop(slot);
            }
        }

        /// <summary>First half of a restart: stops the decoder. Play follows on a later frame.</summary>
        private void Stop(Slot slot)
        {
            ++slot.Attempt;
            slot.State = SlotState.Stopped;
            slot.StepFrame = Time.frameCount;
            callTimer.Restart();
            slot.Player.Stop();
            diagnostics?.RecordCall(callTimer.Elapsed.TotalMilliseconds);
        }

        /// <summary>Second half of a restart: clears the target and plays from frame 0.</summary>
        private void Play(Slot slot)
        {
            // Clear to transparent so the check below can tell a delivered frame (opaque) from a missing one.
            var previous = RenderTexture.active;
            RenderTexture.active = slot.Target;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
            slot.State = SlotState.WaitingForFirstFrame;
            slot.StepFrame = Time.frameCount;
            slot.Player.sendFrameReadyEvents = true;
            callTimer.Restart();
            slot.Player.Play();
            diagnostics?.RecordCall(callTimer.Elapsed.TotalMilliseconds);
        }

        private void OnFrameReady(VideoPlayer source, long frame)
        {
            var slot = Find(source);
            if (slot == null || slot.State != SlotState.WaitingForFirstFrame) return;
            source.sendFrameReadyEvents = false;
            slot.StepFrame = Time.frameCount;
            slot.State = SlotState.Pausing;
        }

        /// <summary>
        /// Moves a slot that is not on screen one step toward Ready. frameReady can arrive before the frame is copied
        /// into the render target, so the pause waits one rendered frame. Occasionally the restarted player never
        /// copies frame 0 at all; a 1x1 GPU readback confirms the target is opaque, and an empty target is restarted.
        /// </summary>
        private void AdvanceSlot(Slot slot, double now)
        {
            if (Time.frameCount <= slot.StepFrame) return;
            switch (slot.State)
            {
                case SlotState.Finished:
                    if (now >= slot.RestartAt) Stop(slot);
                    break;
                case SlotState.Stopped:
                    Play(slot);
                    break;
                case SlotState.Pausing:
                    if (slot.Player.frame > 0)
                    {
                        // A hitch let the player run past frame 0.
                        Stop(slot);
                        break;
                    }
                    slot.Player.Pause();
                    if (!canCheckFrames)
                    {
                        slot.State = SlotState.Ready;
                        break;
                    }
                    slot.State = SlotState.Checking;
                    Graphics.Blit(slot.Target, slot.Check);
                    int attempt = slot.Attempt;
                    AsyncGPUReadback.Request(slot.Check, 0, TextureFormat.RGBA32, request => OnChecked(slot, attempt, request));
                    break;
            }
        }

        private void OnChecked(Slot slot, int attempt, AsyncGPUReadbackRequest request)
        {
            if (this == null || !isActiveAndEnabled || slot.Attempt != attempt || slot.State != SlotState.Checking) return;
            if (request.hasError || request.GetData<byte>()[3] >= 250)
            {
                // A failed check trusts the player rather than stall the loop.
                slot.State = SlotState.Ready;
                return;
            }
            diagnostics?.RecordRetry();
            Stop(slot);
        }

        private void Update()
        {
            if (failed) return;
            double now = Time.unscaledTimeAsDouble;
            foreach (var slot in slots) AdvanceSlot(slot, now);
            if (active < 0)
            {
                if (slots[0].State == SlotState.Ready) Show(0);
                return;
            }
            var current = slots[active];
            long frame = current.Player.frame;
            diagnostics.SampleShownFrame(frame);
            if (frame < lastFrame)
            {
                lastFrameSeenAt = -1;
                return;
            }
            if (lastFrameSeenAt < 0) lastFrameSeenAt = now;
            // Hold the last frame for one frame duration; if the next slot is late, keep holding rather than jump.
            if (now - lastFrameSeenAt + 0.001 >= frameSeconds && slots[1 - active].State == SlotState.Ready)
            {
                diagnostics.RecordLoop(now - lastFrameSeenAt);
                current.State = SlotState.Finished;
                current.RestartAt = now + RestartDelaySeconds;
                current.StepFrame = Time.frameCount;
                Show(1 - active);
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
            diagnostics = null;
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

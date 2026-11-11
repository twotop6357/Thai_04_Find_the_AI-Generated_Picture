using UnityEngine;

namespace Core
{
    /// <summary>
    /// Counts playback problems of a looping background video and writes one summary line per interval, so the
    /// cause of a stutter on an installed PC can be read from Player.log without a debugger.
    /// </summary>
    public sealed class VideoLoopDiagnostics
    {
        /// <summary>A video frame held longer than this many frame durations counts as late.</summary>
        private const double LateFrameFactor = 2.5;
        /// <summary>A main-thread frame longer than this (seconds) counts as a hitch.</summary>
        private const float HitchSeconds = 0.05f;
        /// <summary>A decoder Stop or Play call longer than this (milliseconds) counts as slow.</summary>
        private const double SlowCallMilliseconds = 8;

        private static bool environmentLogged;

        private readonly Object context;
        private readonly double frameSeconds;
        private readonly double intervalSeconds;
        private double intervalStart;
        private long lastFrame = -1;
        private double lastFrameChangeAt = -1;
        private int loops, lateFrames, skippedFrames, hitches, retries, slowCalls, lateSwaps;
        private double worstHoldMs, worstCallMs;

        /// <param name="frameSeconds">Duration of one video frame in seconds.</param>
        /// <param name="intervalSeconds">Seconds between summary lines.</param>
        public VideoLoopDiagnostics(Object context, double frameSeconds, double intervalSeconds)
        {
            this.context = context;
            this.frameSeconds = frameSeconds;
            this.intervalSeconds = intervalSeconds;
            intervalStart = Time.unscaledTimeAsDouble;
        }

        /// <summary>Logs the GPU, refresh rate and vSync once per app run.</summary>
        public static void LogEnvironmentOnce(Object context)
        {
            if (environmentLogged) return;
            environmentLogged = true;
            Debug.Log($"[VideoLoopDiagnostics] GPU '{SystemInfo.graphicsDeviceName}' ({SystemInfo.graphicsDeviceType}), " +
                $"display {Screen.currentResolution.width}x{Screen.currentResolution.height} at " +
                $"{Screen.currentResolution.refreshRateRatio.value:F2} Hz, vSyncCount {QualitySettings.vSyncCount}, " +
                $"targetFrameRate {Application.targetFrameRate}.", context);
        }

        /// <summary>Called every Update with the frame index currently shown.</summary>
        public void SampleShownFrame(long frame)
        {
            double now = Time.unscaledTimeAsDouble;
            if (Time.unscaledDeltaTime > HitchSeconds) ++hitches;
            if (frame != lastFrame)
            {
                if (lastFrame >= 0 && lastFrameChangeAt >= 0)
                {
                    double held = now - lastFrameChangeAt;
                    if (held > frameSeconds * LateFrameFactor) ++lateFrames;
                    if (held * 1000 > worstHoldMs) worstHoldMs = held * 1000;
                    if (frame > lastFrame + 1) skippedFrames += (int)(frame - lastFrame - 1);
                }
                lastFrame = frame;
                lastFrameChangeAt = now;
            }
            if (now - intervalStart >= intervalSeconds) Flush(now);
        }

        /// <summary>The display switched to the other player; the frame index restarts at 0.</summary>
        /// <param name="lastFrameHeldSeconds">How long the clip's last frame was shown before the switch.</param>
        public void RecordLoop(double lastFrameHeldSeconds)
        {
            ++loops;
            if (lastFrameHeldSeconds > frameSeconds * LateFrameFactor) ++lateSwaps;
            lastFrame = -1;
        }

        public void RecordRetry() => ++retries;

        /// <summary>Records how long a VideoPlayer Stop or Play call blocked the main thread.</summary>
        public void RecordCall(double milliseconds)
        {
            if (milliseconds > SlowCallMilliseconds) ++slowCalls;
            if (milliseconds > worstCallMs) worstCallMs = milliseconds;
        }

        private void Flush(double now)
        {
            Debug.Log($"[VideoLoopDiagnostics] {now - intervalStart:F0}s: {loops} loops, {lateSwaps} late loop switches, " +
                $"{lateFrames} late frames (worst hold {worstHoldMs:F0} ms), {skippedFrames} skipped frames, " +
                $"{hitches} main-thread hitches over {HitchSeconds * 1000:F0} ms, {retries} first-frame retries, " +
                $"{slowCalls} slow decoder calls (worst {worstCallMs:F1} ms).", context);
            intervalStart = now;
            loops = lateFrames = skippedFrames = hitches = retries = slowCalls = lateSwaps = 0;
            worstHoldMs = worstCallMs = 0;
        }
    }
}

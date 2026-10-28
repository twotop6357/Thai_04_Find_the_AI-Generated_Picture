# Finish video — 2026-10-27

StatisticsPanel button1 enters Assets/Scenes/2_Finish.unity through ScreenTransition.SceneAsync after the statistics guide finishes. The scene is enabled in Build Settings. Finish's copied ClassificationResultPanel was removed; FinishVideoPresentation owns VideoPanel.

The existing Resources/_PROTO/_Videos/4번영상_임시.mp4 is referenced directly (1920×1080,30fps,3150 frames,105seconds). One native VideoPlayer decodes into one depth-free1080p ARGB32 RenderTexture (about7.9MiB), with direct native audio. There are no frame copies, CPU frame caches, per-frame polling or multiple decoders in production.

On entry the RawImage stays disabled, the previous texture is detached, overlays reset, and the GPU target clears to black. Prepare completes before Play. Only the first decoded frame enables VideoImage and satisfies ISceneDisplayReady; the common scene transition stays dark until then. Playback continues immediately; there is no paused poster. First-frame events switch off after this single callback. Playback uses unscaled time.

At native loopPointReached, Pause preserves the actual last frame in the same target. The shared PopupAnimation fades BlackBox over0.25seconds, then activates RemoveCardPopup and scales it to its original size over0.3seconds with OutBack overshoot1.70158. It never stops or clears the video at the end.

On disable, the image is hidden and detached before the decoder is stopped; event handlers and tweens are removed. Reentry reuses the single GPU target and prepares from frame0. Scene destruction releases and destroys it. Artwork, popup rest size, physical RFID state and saved responses are preserved.

2026-10-28: Once the video has ended and RemoveCardPopup has finished appearing, FinishVideoPresentation waits for the physical board to clear, then returns to Assets/Scenes/0_Idle.unity through ScreenTransition.SceneAsync. All seven readers count, including Center and unknown cards. Clearing the board during playback does not skip the video. A board already empty when the popup appears returns after its entrance animation. Duplicate returns are blocked while a transition is pending; disabling the panel cancels its own pending return without interrupting a successful scene unload's final fade. GamePanel input filtering never clears or hides physical RFID presence from this check.

Play verification:36/36 checks passed, including three reentries at frame0 with identical nonblank first-frame hashes, immediate frame advancement, native end after seeking near the end, final frame3149, held texture hash through the popup, fade/pop ordering and overshoot, target reuse/release, and the real Game→Result→Statistics→Finish button route. No additional round was recorded; production response hashes matched. Temp/FinishVideo contains results.json, samples.json, Verify.cs and FinalHold.png. The earlier VideoEnded.png captured the next test reset frame and is not the final visual evidence.

Console: no feature/compile errors. The source MP4 triggers a WindowsMediaFoundation unknown color-primaries warning; the original video was preserved. A verification query of an already auto-killed tween emitted a DOTween warning, separate from production playback. MCP reconnection warnings and existing unconfigured-apiUrl logs also occurred. Actual hardware and Player build were not tested.

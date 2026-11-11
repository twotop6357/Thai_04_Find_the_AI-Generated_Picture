# Title Background Video

`Assets/Resources/Videos/c04.mp4` loops behind the Title (`0_Idle` TitlePanel/TitleVideo, `IdleBackgroundVideo`).

## Encoding
The supplied clip (H.264 Main, 31.6 Mbps, keyframe every 30 frames, no colour tags) had a visible change in background grain at the loop point because frame 0 is a keyframe and frame 119 the end of a drifting P-frame run. It was re-encoded with ffmpeg 9.0.2:

```bash
ffmpeg -stream_loop 2 -i c04_source.mp4 \
  -vf "hqdn3d=0:0:6:6,trim=start_frame=120:end_frame=240,setpts=PTS-STARTPTS" \
  -c:v libx264 -preset slow -profile:v high -level:v 4.1 -maxrate 40M -bufsize 40M -pix_fmt yuv420p -crf 10 \
  -g 120 -keyint_min 120 -sc_threshold 0 -bf 0 \
  -x264-params "colorprim=bt709:transfer=bt709:colormatrix=bt709:ipratio=1.0:mbtree=0" \
  -color_range tv -an -movflags +faststart c04.mp4
```

- The input is looped three times and only the middle copy is kept, so the temporal denoise treats frame 119 → 0 like any other step.
- One keyframe per loop, no B-frames and equal I/P quality keep the background stable across the seam.
- BT.709 tags remove Unity's "color primaries unknown" warning; Unity's decode of frame 0 still matches `Shared/TitleBackground.png` (mean difference 0.8 levels).

- `-level:v 4.1` with a 40 Mbps VBV cap: without it x264 marked the stream level 5.0, and some integrated-GPU hardware decoders only accept up to 4.1 and fall back to CPU decoding.

Result: 120 frames, 30 fps, 8.3 MB, H.264 High@4.1. Background change at the seam dropped from 1.29 to 0.50 levels (a normal frame step is 0.05 to 0.08).

## Playback
VideoPlayer's built-in looping seeks the running decoder back to frame 0. Measured in play mode it held frame 119 for about 50 ms and, on some loops, showed frame 112 for one frame before frame 0, so the cards jumped backward. `IdleBackgroundVideo` therefore uses two VideoPlayers in turn: the idle one is restarted, paused on frame 0, and checked with a 1x1 GPU readback (a restarted player occasionally never writes frame 0, and that player is restarted again). The display switches when frame 119 has been shown for one frame duration. A 3-minute play-mode recording (46 loops) showed every frame in order, no empty texture, and a 119 → 0 image change equal to a normal step.

## Integrated GPUs and installed PCs
- The finished player is restarted 0.5 s after a switch, and Stop and Play run on different frames, so a slow hardware decoder restart never lands on the loop point.
- `skipOnDrop` is off: a late frame is shown late instead of being skipped, which would read as a jump.
- VSync comes from `StreamingAssets/Settings.json` on each PC, not from the quality level: the template's `GameManagerBase` turns VSync on when `targetFrameRate` is 0 or missing, and turns it off when a value is set, which tears full-screen video. Keep `targetFrameRate` at 0 on exhibition PCs. Player.log shows `[GameManagerBase] targetFrameRate ...` at startup.
- `VideoLoopDiagnostics` writes the GPU, refresh rate and VSync once, then one summary line every 10 minutes (every minute in development builds): loops, late loop switches, late frames with the worst hold, skipped frames, main-thread hitches over 50 ms, first-frame retries and slow decoder calls. Read these lines from Player.log on the installed PC to see which cause applies.

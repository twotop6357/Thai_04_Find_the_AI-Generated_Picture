# TODO

<!-- Work to do in this project. The user writes it and Claude updates it while working (Antigravity only reads it). -->
<!-- Format: - [ ] Task — Owner: Claude / Verified by: Antigravity -->
<!-- When done, change to [x], add the completion date and move it to "Done". Old done items may be deleted (git keeps them). -->

## In progress

## To do

## Done

- [x] Updated the HuliacDev Template package from 26.10.12-1 to 26.10.16-1 (packages-lock.json pinned commit 1c5e127 → a036657; added the `alwaysOnTop` option to keep the window on top, off by default so behavior is unchanged), added `"alwaysOnTop": false` to Settings.json, set Player Settings Version to 26.10.16, and also recorded the operator-facing changes of 26.10.12-1 that were missing from the CHANGELOG — Owner: Claude / Verified by: Antigravity (done: 2026-10-16)
- [x] Changed the README usage to "keep one clone of the template, copy only Assets, Packages, ProjectSettings, .gitignore, .gitattributes and CLAUDE.md to a new folder, open it in Unity Hub, change Product Name in Player Settings" (removed the clone→upstream and fork guidance). Confirmed in a temp folder that those six copy 1,080 files identical to the template and push without LFS files — Owner: Claude / Verified by: Antigravity (done: 2026-10-13)
- [x] Put clone, fork and ZIP usage at the top of the README and rewrote it more simply. Tested the clone-then-push-to-new-repository steps in a temp folder (added the `lfs.allowincompletepush` setting because an LFS hook blocks the push when older commits' LFS files are missing) — Owner: Claude / Verified by: Antigravity (done: 2026-10-13)
- [x] Moved the remaining 28 third-party LFS files (NuGet icons, DOTween editor images, TextMesh Pro images, fonts and PDF) out of LFS and added a note to the README that Git is required. The end of `.gitattributes` makes only third-party folders an exception, so the project's own images, videos and fonts stay in LFS. Confirmed zero LFS pointers in `git archive` output (equivalent to the ZIP) and zero compile errors opening from scratch in Unity batch mode — Owner: Claude / Verified by: Antigravity (done: 2026-10-13)
- [x] Moved 126 DLLs out of Git LFS and committed them as ordinary git binaries (`*.dll lfs` → `*.dll binary` in `.gitattributes`, contents SHA-256 identical to the LFS originals). This fixes script errors in DOTween, R3, ZLogger and others caused by DLLs arriving as LFS pointers through GitHub "Download ZIP". Earlier commits stay in LFS — Owner: Claude / Verified by: Antigravity (done: 2026-10-13)
- [x] Updated the HuliacDev Template package from 26.10.10-1 to 26.10.10-2 (packages-lock.json pinned commit 36ec69a → 8afd2a8; a 90-second inactivity return fallback when Settings.json cannot be read, JsonLoader cancellation propagation and atomic saves, added ConsecutiveClickCounter and ApiRetryUtil.GetTextWithRetryAsync; this project's code does not use the changed APIs) — Owner: Claude / Verified by: Antigravity (done: 2026-10-10)
- [x] Updated the HuliacDev Template package from 26.10.9-1 to 26.10.10-1 (packages-lock.json pinned commit 640d05e → 36ec69a; added window focus restore WindowFocusRestorer and the F key toggle, fixed a Reporter memory leak), added `focusRestoreDelay` and `focusRestoreRetryInterval` (3 seconds) to Settings.json, set Player Settings Version to 26.10.10 — Owner: Claude / Verified by: Antigravity (done: 2026-10-10)

- [x] Updated the HuliacDev Template package from 26.9.25-3 to 26.10.9-1 (packages-lock.json pinned commit b4547f3 → 640d05e; removed the depth buffer from the VideoManager video RenderTexture, an API this project does not use), set Player Settings Version to 26.10.9 — Owner: Claude / Verified by: Claude (in place of Antigravity, which was over its limit) (done: 2026-10-09)
- [x] Updated the MCP for Unity package from 10.2.0 to 10.3.0 (updated the packages-lock.json pinned commit), set Player Settings Version to 26.10.4 — Owner: Claude / Verified by: Antigravity (done: 2026-10-04)
- [x] Updated the README for the current structure and packages — Owner: Claude / Verified by: Claude (in place of Antigravity, which was stopped) (done: 2026-10-02)
- [x] Changed the Test Framework from prerelease (2.0.1-pre.18) to a release version — Owner: Claude / Verified by: Claude (in place of Antigravity, which was stopped) (done: 2026-10-02)
- [x] Removed the ineffective Directional Light and Global Volume from the 0_Idle scene — Owner: Claude / Verified by: Claude (in place of Antigravity, which was stopped) (done: 2026-10-02)
- [x] Made the Roslyn DLLs for MCP Editor only, excluding them from player builds — Owner: Claude / Verified by: Claude (in place of Antigravity, which was stopped) (done: 2026-10-02)

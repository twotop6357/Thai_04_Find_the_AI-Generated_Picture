# Changelog
Records changes that affect the visitor or operator experience. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2026-11-11]

### Changed
- Thai Title subtitle and the Thai result board "Human" label follow the updated Figma font and line breaks.

## [2026-11-10]

### Added
- Title plays `Resources/Videos/c04.mp4` as a silent looping background. `Shared/TitleBackground.png` is now the video's first frame as Unity decodes it and stays underneath until the first video frame is ready, so the screen never flashes.

### Changed
- The top-right NSM logo is a separate image (`Shared/Logo.png`, Figma 1x export) placed at (1746, 40) on Title, Tutorial, Game, Result and Statistics. It was removed from the Tutorial, Game, Answer and Statistics backgrounds by exporting temporary Figma clones without the logo; the Figma originals are unchanged.
- The English Title text overlay is rebuilt from the Figma title layers so it composites correctly over the video, like the Thai one.

## [2026-11-09]

### Added
- On Title, a card on any reader shows the "remove all the cards to Start" popup with the same backdrop fade and pop entrance as the Finish popup. Every button action (start and the language switch) waits until every reader is empty, and the popup closes as soon as the board is clear. Tutorial does not show the popup. Thai and English use the same English image for now; a Thai image can be assigned to the popup pair in the Idle Canvas `ClassificationUiLanguage`.
- `TransitionSoundManager` plays `language.mp3` when a 2-second hold on Title switches the language, and `selected.mp3` when GamePanel accepts a registered card on any reader (Center, AI or Human). Unregistered cards, the same card read again, removals, cards placed outside GamePanel and cards ignored while the F10 mapping window is open stay silent.

## [2026-11-05]

### Added
- Enabled the inactivity return to Title after a period without input. The time is set with `resetTime` in `StreamingAssets/Settings.json` (default 30 seconds), and `resetTime` or `useInactivityTimer` is added on startup if missing from an existing file. It is not measured while a card is recognized on GamePanel, from video playback through the card removal popup, during screen transitions, on Title, or in the F10 mapping window.
- `TransitionSoundManager` in the `GameLifetimeScope` prefab can be assigned a button transition sound and a return-to-first-screen sound. The button sound plays only when a button press actually starts a screen transition.

### Changed
- `2_Finish` now plays the new video for the selected language (Thai or English) instead of the temporary video.
- Replaced the `_PROTO` folder with `Assets/Resources/Videos`. The videos are now `FinishVideo_Thai.mp4` and `FinishVideo_English.mp4`.
- Renamed Korean asset files to English and translated comments and documents to English. Asset GUIDs are unchanged, so scene references are preserved.

### Removed
- Removed resources nothing uses: `TEMP_MasterpieceCards`, the six `_PROTO` images and temporary video, unused `Assets/UI/English` images, `Figma20261104/*/Title.png`, and `Lang_En.json` / `Lang_Th.json`.
- Removed the unused scripts `ButtonMappingPanel` and `HelpPanelController`.

## [2026-11-04]

### Added
- Connected the RFID readers over TCP. The readers are clients and the program is the server; the entire value a reader sends is used as the card value with no length limit. Recognition is released when no value arrives for 1 second after the last one. Settings are edited in `StreamingAssets/RfidSettings.json`.
- Added an RFID card mapping window toggled with F10. Select a card picture, then place the physical card to register it. A card whose value changes can register several values.

### Removed
- Removed the F9 RFID tester.

### Changed
- When changing language, the background stays while the current language's text and button guide fade out over 0.5 seconds and the new language's text and guide fade in over 0.5 seconds.
- Moved the game button to `NextButton` in the Input Manager. The primary input is `joystick button 0` and the secondary input is keyboard digit 1. The button is changed in the Input Manager; the F8 button settings screen and runtime re-registration are no longer provided.
- Holding the button for 2 seconds on Title switches between Thai and English while staying on the screen, and a short press starts in the current language. Returning to Idle keeps the language of the last experience; the app starts in Thai.

## [2026-10-27]

### Changed
- After the statistics guide finishes, button 1 moves to 2_Finish/VideoPanel and plays the temporary video #4. The screen stays covered until the first decoded frame. At the end the last frame is held, then BlackBox fades over 0.25 seconds and the card removal popup plays a 0.3-second OutBack. A single VideoPlayer and GPU texture are reused and released on scene unload, preventing reentry ghosting and unnecessary frame caches.
- Adjusted StatisticsPanel to the supplied design's six-column layout with horizontal stacked graphs. Photo masks follow the actual inner contour of the existing comparison board, and each graph's boundary and percentage move together over 0.5 seconds inside one rounded mask. Values use the bold face of the specified default font, the AI/Human lettering in the existing image is kept, and unnecessary text is removed. After everything finishes, the existing bottom guide image fades in over 0.25 seconds.
- ResultPanel photos use a mask fitted to the inside of the card. FeedbackArtwork keeps its original 174×274 size and moves and rotates briefly outside the mask for a stamp effect. After all feedback finishes, the ScoreArtwork for 0 to 6 correct answers fades in over 0.25 seconds, then ButtonInstruction fades in over 0.25 seconds, and only then is button 1 accepted. Handled deleted references to the old correct-answer display and a starting state with several panels active.
- Removed the GamePanel photo scale animation; photos now fade in and out over 0.25 seconds at a fixed size. ButtonInstruction appears over 0.25 seconds when all six classification readers recognize cards, and exits over 0.25 seconds and deactivates if any recognition is lost.
- Replaced the RFID card doodle previews with six temporary masterpieces (3 AI-generated, 3 real public-domain) and showed center, classification and statistics photos with mask cover fill plus 1% overscale. Previous original doodles, card IDs and statistics were preserved; materials, sources and prompts were recorded in Resources/TEMP_MasterpieceCards (removed 2026-11-05).
- Made references to the removed old score and guide text optional on the image result screen, so submission, scoring and statistics transitions work with artwork UI alone.

- Corrected the seven GamePanel photo masks to the actual inner contour of the background frame and tied them to normalized background anchors, including the central decorative curves, preserving photos, readers and animations. At 1920×1080, an actual render matched the extracted inner reference with zero differing pixels for all seven.

- A short press of 1 on Title selected Thai and a 2-second hold selected English, logging the choice once in English and moving to Tutorial (the actual UI stayed English). Tutorial plays arrow and guide blinks, Card1 enlarging and moving to a precise placement excluding its shadow, and a decoration entrance, twice at 75% speed. Right after the first play, only that card fades out over 0.5 seconds and fades back in at its original position over 0.5 seconds. The board and other cards stay, and after holding the second placement for 0.5 seconds it moves to Game automatically. Transitions, reentry and mid-way cancellation are cleaned up.

- Applied a shared 0.5-second exit and 0.5-second entrance fade to the existing photo classification → score → response statistics screens and their return, and blocked repeated presses during transitions. A round already saved before a cancellation is restored without saving it again.
- Applied a shared fade-to-black then pop animation to the card removal guide. Tweens are cleaned up when the screen closes, and the actual RFID recognition state is preserved.

### Added
- Roughly connected scores 0 to 6, correct/incorrect cards, the response comparison board and the removal popup using only the 31 user-added Thai_04 UI images. Images imported from another project were removed. The En/Th lists were rewritten for 31 targets and 53 bindings (Thai placeholder, runtime language switching not connected).
- Added per-reader photo display and a submission block during animations. Photo display later changed to a fixed-size 0.25-second fade on request, with removal, replacement, re-recognition and panel lifetime cleaned up.
- Added correct/incorrect feedback, score and button guides, and actual AI/Human response rate animations. On later requests, results became fixed-size stamps and statistics became a 0.5-second horizontal stack. Existing response storage, answer checking and physical reader state are preserved.
- Connected digit 1, numpad 1 and the public PressButton1() in Idle to the shared Title→Tutorial→Game transition and added the Game scene to the build list. The user adjusts the detailed UI layout afterward.

## [2026-10-21]

### Added
- Added per-image AI/Human response charts with persistent device-local counts, response totals, and backup recovery. Accepted button 1 submissions add one vote per image; reopening charts adds none.
- Added an English results screen submitted with button 1 after all six classification readers are filled, with total score, per-slot circle/cross feedback, session statistics, and board-clear retry. Keyboard 1 and numpad 1 share the future Arduino entry point.
- Shows the original image of the card on the RFID center reader in GamePanel, and thumbnails of the same image on each of the three AI and three Human classification readers. Each slot updates as cards move, swap, are removed or all cleared, using six prototype images.

## [2026-10-16]

### Added
- Setting `alwaysOnTop` to true in the settings file keeps the app window on top so other programs' windows cannot cover it. It is off by default; when on, the `F` key toggles focus restore and always-on-top together.

## [2026-10-12]

### Added
- Toggling window focus restore with the `F` key briefly shows the new state and the key to re-enable it at the top of the screen.

### Fixed
- Fixed the app possibly freezing while bringing its window forward when an unresponsive program's window was in front.
- Fixed the hidden close button appearing as a white square in the middle of the screen when running without a readable settings file; it now sits transparently at the top left.

## [2026-10-10]

### ⚠ Breaking Changes
- In Windows builds the app returns to the front 3 seconds after another window appears, so to use other programs during maintenance press `F` in the app to turn this off (it turns back on when the app restarts).
- Running with `Settings.json` missing or malformed now returns to the first screen after 90 seconds without input. To avoid the inactivity return, keep the settings file valid and set `useInactivityTimer` to false.

### Added
- Brings the app window back to the front after notifications or update dialogs take focus, so keyboard-style barcode and QR scanner input is not interrupted. The wait and retry interval are adjustable in the settings file (`focusRestoreDelay` and `focusRestoreRetryInterval`, default 3 seconds).

### Fixed
- Fixed the log viewer growing memory the longer the app stayed on.
- Fixed the inactivity return silently turning off, indistinguishable from turning it off on purpose, when the settings file could not be read. It now logs an error and runs with fallback settings with a 90-second inactivity return.

## [2026-09-25]

### ⚠ Breaking Changes
- Leaving opacity, position or time limit out of the close button entry (`closeSetting`) in the settings file now uses defaults instead of 0, so write the values explicitly if you expected 0.
- Setting a font size of 0 in the settings file no longer hides text; turn off the text object to hide it.
- Button text located deeper than directly under the button overlaps when settings are applied, so move the text directly under the button.

### Changed
- With no close button entry in the settings file, pressing the hidden close button 10 times within 3 seconds closes the app (the previous default of 10 times in 0.5 seconds made closing practically impossible).
- The close button's consecutive click limit is at least 1 second; shorter values are raised to 1 second.
- If fewer than 3 clicks are required to close, a warning is logged that visitors could close the app by accident.
- Invalid settings, such as a missing sound key or out-of-range close button settings, are now logged as warnings instead of being silently ignored.

### Fixed
- Fixed background music restarting after loading finished when it was stopped or faded out during loading.
- Fixed a late-arriving previous track overwriting the new one when switching tracks during background music loading.
- Fixed text not showing on buttons with background images.
- Fixed text disappearing when font size was left out of the settings file.
- Fixed the button disappearing or closing not working when only some close button settings were written, because the rest became 0.
- Fixed a possible error and stall at the moment the Arduino connection dropped.

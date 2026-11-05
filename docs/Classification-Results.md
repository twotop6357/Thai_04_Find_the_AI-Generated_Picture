# Current presentation — 2026-10-27

The classification, scoring and response storage flow is preserved. Existing Thai_04 interface artwork is used; imported Thai_03 artwork has been removed. The temporary six-painting set of that date (Resources/TEMP_MasterpieceCards) was replaced by Resources/FigmaCards and removed on 2026-11-05. Layout is deliberately approximate for the user's manual adjustments.

The Canvas controller owns transitions. GamePanel/RfidImagePanel keeps all seven photos at their fixed cover size and fades each recognized photo in independently over 0.25 seconds. Removal fades that photo out over 0.25 seconds; replacement waits for the old photo to disappear. ButtonInstruction starts inactive, fades in over 0.25 seconds only when all six Ai1..3/Human1..3 readers recognize known cards, and fades out over 0.25 seconds if any one is lost. Center does not count and correctness does not affect this cue. Rapid re-detection reverses from the current alpha. Panel hiding cancels fades without clearing hardware state.

ResultPanel freezes the six submitted photos in reader order. PhotoMask contours are traced from the existing Result card's inner rectangle (native bounds16.01,17.01,145.98,244.98,42 vertices); EnvelopeParent and 1% overscan cover the aperture without distortion or gaps. FeedbackArtwork remains at its original174×274 size, outside PhotoMask with maskable=false. Six fixed-size stamps use short position/rotation impact and settling, starting0.2 seconds apart and lasting0.22 seconds each. After the last stamp settles, the correct0..6 ScoreArtwork fades in over0.25 seconds, followed by ButtonInstruction over0.25 seconds. Button1/red-button input is ignored through the final guide fade and is never queued. Deleted legacy procedural marks are optional when this presentation is assigned. Startup hides Result/Statistics when no round exists, including scenes saved with multiple panels active.

Result verification:82/82 Play Mode checks plus a real-time frame capture passed for fixed feedback sizes, stamp movement, masks/cover, frozen photos,4-correct submission, score mappings0..6, input gating, cancellation/replay and unchanged production statistics. Temp/ResultStamp contains final-results.json, realtime-result.json, frame traces and Final-4.png. Initial verification exposed the multiple-active-panel startup condition, fixed before the final run.

StatisticsPanel matches the supplied E04-S05 reference using the existing background, ComparisonBoard and guide artwork. The board's baked AI/Human labels remain; only twelve changing percentage texts use the default font's configured bold face (Inter_18pt-ExtraBold SDF). Image labels, response counts and redundant percentage strings were removed. Six photos use contours traced from the board's exact inner frames, EnvelopeParent and 1% overscan. The board is at233,-372; each card's horizontal graph is174×24 with one12-radius capsule mask. Human is a solid backing and AI overlays up to a single moving boundary, leaving no seam or separately rounded inner ends.

StatisticsPresentation starts all six graphs together: the boundary and complementary integer percentages move from50/50 to actual local response shares in0.5 seconds. Empty data shows em dashes and a neutral track. The bottom guide image (now Figma20261104/*/StatisticsInstruction.png) appears only after all graphs finish, fading in over0.25 seconds. Input remains blocked through the guide fade. Storage, image identities and raw vote counts are unchanged. Temp/HorizontalStatistics/final-results.json records53/53 Play checks; the final capture and seam scan cover80/20,20/80,30/70,70/30,100/0,0/100. Production statistics were unchanged. Try Again uses the shared removal popup and waits for all seven readers to clear. Button1 after the guide opens the Finish video flow described below.

Idle Canvas/ClassificationEntryController interprets the NextButton press duration only at Title: holding for 2 seconds switches between Thai and English once and stays on Title; a short release starts in the current language. Entering Idle resets the language to Thai. Physical adapters provide down/up through SetPhysicalButtonPressed(bool); PressButton1() remains a completed short-press entry point.

TutorialPanel/TutorialCardPresentation performs the arrow and guide cues, lift/travel/precise placement, and decoration pop at Playback Speed 0.75. It plays twice. Immediately after the first play, only the moved card fades out for 0.5 real seconds, resets while hidden, and the original card fades in from alpha 0 to 1 for 0.5 seconds. Boards, the background and the other five cards remain opaque. There is no one-second pause. On the second play the landed card remains still for 0.5 real seconds from placement, including the decoration pop, then EntryController automatically opens Game through the common black-screen scene transition. Tutorial button presses cannot skip the animation. Replay Reset Duration=1 is independent of playback speed. Source artwork is borrowed by one reusable runtime Image so the six-card layout stays fixed. Card Body calibrates the opaque area excluding the existing shadow. Language JSON is still preparation only.

The earlier implementation history follows; its placeholder artwork and older layout descriptions are historical.

# Classification results

## Scene and flow

`Assets/Scenes/1_Game.unity` contains an initially hidden `Canvas/ResultPanel`.
`ClassificationResultPanel` lives on Canvas so it remains active when GamePanel is hidden.
Results open only when button 1 is pressed while GamePanel is active. Keyboard number 1
and numpad 1 call `PressButton1()`. The future Arduino adapter should call this same method
on the Unity main thread. All six classification readers must contain known cards at the
time of the press. Early presses are ignored and are not queued. Filling the last slot
alone does not submit the board. Center is excluded from scoring.
ContentGate blocks both scoring and result actions.

The completed board is captured once. AI reader positions are the top row; Human positions
are the bottom row, each ordered 1, 2, 3. The score compares each submitted card's catalog
type with its reader category. Correct positions show a blue circle and incorrect positions
show a red cross. Subsequent card removal or movement does not change the captured result.

All new visitor-facing text is English. Numbered reference annotations are omitted.
The gray cells, round button and procedural ResultMarkGraphic shapes are placeholders.
The serialized UI references can be reassigned when final artwork is available.

## Actions

- Button 1 submits the completed board from GamePanel. A new press on the score screen
  opens the image response charts; keyboard digit 1, numpad 1 and the Arduino entry point
  `PressButton1()` all follow this flow. Opening charts never records another round.
- The red button opens six image cards in a single horizontal row. Each shows its source
  image, a continuous horizontal AI/Human bar and two complementary percentages.
- After the Statistics guide is fully visible, button1/numpad1 loads Finish/VideoPanel.
  Early or repeated presses are ignored. Space/Enter and `PressRedButton()` share this route.
- Back to Results closes the statistics overlay.
- Try Again displays an instruction to remove all cards, including Center. Once all seven
  readers are empty, GamePanel reopens. Physical input is never cleared by the game.

Statistics include the current round and all recorded rounds on this device. Counts follow
card identity, not current slot order or actual AI/Human origin. Every accepted button 1
submission records exactly one vote for each image using the reader category chosen by
the player. Reopening the screen, repeated button presses, and invalid submissions add no
votes. Display percentages are empirical response shares, not probabilities of correctness;
they are rounded to whole percentages with a complementary Human percentage.

ImageClassificationStatistics saves raw integer counts immediately to
`Application.persistentDataPath/image-classification-statistics-v1.json`. Scene reloads and
application restarts preserve them. A temporary file and atomic replacement protect the
main file and retain its previous version as `.bak`. Load validates the schema and counts;
if the main file is unreadable, the previous backup is used with a warning. If both files
are unreadable, they are preserved and new submissions are refused. Save failures also
refuse submission, preserving in-memory counts. There is no cross-device synchronization.
Empty charts show em dashes and a neutral track rather than invented50/50 data. The older session
score histogram was replaced. Hardware protocol integration remains separate.

## Validation

Unity Editor Play Mode checks passed for all 720 card permutations (10,808 conditions):
scores, six individual marks, panel transitions, frozen results, no duplicate counting,
button callbacks, statistics, board-clear retry, unknown/incomplete cards and HelpPanel
blocking. After adding the required CanvasRenderer to procedural marks, additional checks
passed for 0/6 and 6/6, twelve nonempty rendered mark meshes, and three-round statistics.
Game View captures `Temp/ResultPanel/Result-4-of-6.png` and `Statistics.png` were inspected.
No NUnit suite, physical RFID/button test, or Player build was run.

Button 1 revision: eleven Play Mode checks passed using a temporary Input System keyboard
device. Verified digit 1 and numpad 1 submission, full-board waiting, ignored early presses,
unknown/incomplete rejection, duplicate suppression, retry, and the public Arduino entry
point. The temporary device was removed and the simulated board cleared afterward.

Per-image chart revision: fifteen isolated storage checks passed for saves, reloads,
identity stability, duplicate/unknown rejection and backup creation. Play Mode verified
six non-null image sprites, exact bar ratios, label fit, explicit submission and duplicate
suppression. Additional checks passed for empty data, 100/0, 33.3/66.7 display rounding,
backup recovery after deliberate test-file corruption and saving after recovery. The
expected recovery warning was observed. Production statistics were not populated by tests;
all test files were under Temp/ClassificationCharts. Distribution.png was visually reviewed.

The removed legacy score/instruction TMP objects and procedural marks are optional when ClassificationPresentation supplies the score and feedback artwork. Per-image chart and reader photos use EnvelopeParent plus1% overscan to avoid letterbox gaps.

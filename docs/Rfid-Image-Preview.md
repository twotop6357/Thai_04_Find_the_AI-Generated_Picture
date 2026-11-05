# Current GamePanel fades — 2026-10-27

All seven GamePanel photos now fade in independently over 0.25 seconds, with no scale animation. Existing cover fitting and 1% overscan remain fixed. Removed photos fade out over 0.25 seconds. ButtonInstruction starts inactive and fades in over 0.25 seconds only when all six classification readers recognize known cards; losing any one fades it out over 0.25 seconds and deactivates it. Center is excluded. Wrong answers still count as recognized cards. Rapid re-detection continues from the current alpha. As of 2026-10-28, panel entry starts with no accepted cards: existing physical cards require removal and a fresh placement, as described in Rfid-Game-Flow.md.

Play Mode verification passed 40/40 checks, including each reader's removal, rapid re-detection, fixed scales, unknown cards, panel cleanup and score submission. Evidence: Temp/GamePhotoFades/final-results.json. Real statistics remained unchanged. The older implementation notes below are historical.

# Temporary artwork — 2026-10-27 (superseded)

The current card images are Resources/FigmaCards/Card1..6.png. The temporary folder and _PROTO images described here were removed on 2026-11-05.

Resources/TEMP_MasterpieceCards replaces the previous drawn previews via RfidCardImageCatalog: three generated AI originals and three real public-domain paintings from The Met. Existing _PROTO files remain preserved. Six prebuilt Sprite assets share their original textures and use the exact Game ImageMask ratio. Each painting keeps its original aspect and uses a presentation crop; provenance and prompts are inside the temporary folder.

The central preview, six reader photos and six response-chart photos use EnvelopeParent fitting and 1% overscan. Their image rectangles cover the entire mask while maintaining the sprite ratio. Existing masks and reader identities are unchanged. Score/instruction TMP references are optional when the artwork presentation is assigned, so removing obsolete text objects no longer blocks score submission.

# GamePanel mask alignment — 2026-10-27

The seven apertures now use normalized contours traced from this project's existing background, rather than approximate rounded rectangles. AI/Human group transforms stretch with GamePanel, and each mask uses normalized background coordinates. The custom contour is serialized on RoundedRectMaskGraphic; other rounded masks retain the original implementation. The source PNG and photo sprite references are unchanged.

At native1920×1080, an actual stencil-fill capture matched all seven extracted reference regions with zero differing pixels. Point-coordinate checks also passed at1200×1000 and2560×900 panel shapes. A changed background drawing requires recalibration; arbitrary display antialiasing results were not measured. Extraction and verification evidence is in Temp/GameMaskAlignment.

# RFID image preview and classification thumbnails

User request of 2026-10-21. The implementation target is Thai_04_Find_the_AI-Generated_Picture.

## Behavior

- Placing a card on the Center reader of GamePanel shows the original in RFIDImageBox.
- Ai1, Ai2 and Ai3 correspond to the first, second and third Image of AI_ImageGroup.
- Human1, Human2 and Human3 correspond to the first, second and third Image of Human_ImageGroup.
- Thumbnails share the same sprite as the original and preserve the image ratio. Existing Image coordinates, sizes and group order are kept.
- When a card is removed, that Image's sprite is cleared and only the Image is hidden. Empty slot objects stay on, so layout positions are kept.
- Moving or swapping a card follows the existing RfidInput remove/place events and preserves other slots. Clearing all empties the seven slots.
- A card's actual origin and the classification reader position are independent. An AI card placed on a Human slot still shows the AI card's image, and this work adds no answer checking or automatic classification.
- Unregistered cards and missing images clear the slot rather than leaving the previous image. Missing files produce a warning when the catalog initializes.
- Entering or re-entering GamePanel clears the accepted game state. Existing physical placement is kept, but only cards removed and tagged again are used for display and judging. Subscriptions are released while disabled.

## Cards and images

| Card ID | Resources image (current) |
| --- | --- |
| HUMAN_1 | FigmaCards/Card1.png |
| AI_1 | FigmaCards/Card2.png |
| HUMAN_2 | FigmaCards/Card3.png |
| AI_2 | FigmaCards/Card4.png |
| HUMAN_3 | FigmaCards/Card5.png |
| AI_3 | FigmaCards/Card6.png |

Images are imported as Sprite/Single with NPOT resize and mipmaps off.

RfidCardImageCatalog handles card ID and image lookup; RfidImagePanel handles only the display and subscription lifetime of the seven UI elements. RfidImagePanel is attached to GamePanel, with previewImage and the two Image arrays wired as scene references. Runtime state and resource caches are not serialized.

Content input subscribes to the existing ContentGate.OnCardChanged. Display updates stop while a blocker holds ContentGate, and new card input applies normally after unblocking. RfidInput, the shared entry point for input, was not changed. Adding real UIDs and communication was out of scope for this work.

## Verification

In Unity 2022.3.62f3 1_Game scene Play Mode, the center coordinates of each reader rectangle were passed to the real RfidTester.DropCard to test all 42 combinations of six cards and seven readers. 443 conditions were checked automatically, covering exact sprites, empty slots, aspect ratios, repeated input, card moves, shared sprites, swaps, removal, clear all, disable/reentry and unregistered cards. HelpPanel blocking, unblocking after clearing the board and resumed input were checked separately.

The center original and same-image classification thumbnails were visually checked in the Game View capture Temp/RfidImagePreview/GamePanel.png. This verification is automated simulated input in Play Mode and is not recorded as Unity Test Runner NUnit tests or as real mouse or physical RFID verification. A Windows Player build was not run.

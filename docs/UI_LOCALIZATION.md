# UI language resources

The current implementation follows the [2026-11-04 Figma UI update](Figma-UI-2026-11-04.md).

`ClassificationUiLanguage` switches the Thai and English UI through Image/Sprite references serialized in the three scenes. Holding for 2 seconds on Title fades out the current language's text and button guide over 0.5 seconds, then fades in the new language's text and guide over 0.5 seconds, while the background stays. Button input is ignored during this. A short press starts in the current language.

Title is split into the shared background of `TitlePanel` (`Shared/TitleBackground.png`) and the per-language layer of its child `TitleLanguageText` (`Thai|English/TitleText.png`, containing all text and the button guide). The language animation fades only the active images connected to `ClassificationUiLanguage`, so put elements to fade in this list and leave the background out.

The Figma export has no text-free Title background, so this layer was made by comparing the former `Thai|English/Title.png` files. Where only one language had text, the other language's original pixels were used; where both languages' text overlapped, the area was restored from its surroundings, so part of the top-left leaves is blurred. The background under the guide uses original pixels from a guide-free English intro background. The composited result matched the original `Title.png` pixel for pixel. The source images used for this were removed on 2026-11-05. If Figma provides a text-free background and per-language layers, replace the PNGs at the same paths. Both languages' tutorials use the same six pictures from the English Figma. The card pictures in game, result and statistics were replaced with the same image set.

The per-language resources in `Assets/UI/Figma20261104` and `Assets/Resources/FigmaCards` are the current reference. Shared sources, boards and animation elements in `Assets/UI/English` that still fit were reused.

Check each scene's `ClassificationUiLanguage` for the actual wiring. Card IDs, answers and the statistics storage format are unchanged.

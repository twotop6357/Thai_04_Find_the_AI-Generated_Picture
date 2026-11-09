# Single button mapping

The game button is read from one axis, **NextButton**, in the Input Manager (`Edit > Project Settings > Input Manager`).

| Item | Value |
| --- | --- |
| Positive Button (primary input) | `joystick button 0` |
| Alt Positive Button (secondary input) | `1` (keyboard digit 1) |
| Joy Num | Get Motion from all Joysticks |

Change the values above to change the button. They are stored in `ProjectSettings/InputManager.asset` and included in builds, and cannot be changed at runtime. If the axis is renamed or deleted, `SingleButtonInput` logs one error and blocks button input.

`joystick button 0` corresponds to the Trigger (first HID button) of the DragonRise Generic USB Joystick saved in the old F8 settings. Numpad 1 is not part of the secondary input.

## Button behavior

| Screen | Short press and release | Long press |
| --- | --- | --- |
| Title | Move to Tutorial in the current language | Thai ↔ English switch as soon as 2 seconds is reached, staying on Title |
| Tutorial | None (moves to Game automatically) | None |
| Game | Submit answers | Return home without submitting when 5 seconds is reached |
| Result, Statistics | Next screen | Return home when 5 seconds is reached |
| Finish | None | Return home when 5 seconds is reached |

On Title, one long press changes the language only once and does not start on release. Returning to the Idle scene keeps the language of the last experience; the app starts in Thai.

A button already held when window focus returns or input is re-enabled must be released and pressed again.

## Implementation

`Hardware.SingleButtonInput` persists across scenes and reads `Input.GetButton("NextButton")`. `ClassificationEntryController` and `ClassificationButtonGesture` use the same input to measure press duration.

The old F8 Button Settings and runtime re-registration were removed. `Application.persistentDataPath/thai04-button-binding-v1.json` is no longer read.

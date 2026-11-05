# RFID input and Finish return — 2026-10-28

RFID input before entering GamePanel does not affect the game. `RfidInput` keeps the state of the seven physical readers at all times, and the F10 mapping window, block removal guide and Finish return condition use this physical state.

`RfidImagePanel.OnEnable` starts an empty game input session through `ContentGate.BeginGameInput`. Blocks already placed at entry are not copied. Repeated detection of the same block creates no new placement event, so it must be removed and placed again to count. The existing remove/place rules for moving to another slot or swapping to another block still apply. On disable, only the session owned by that panel ends and its accepted state is cleared.

Images, the center preview, the six-slot completion guide and score submission all use `ContentGate.GetGameCard`. So a previous block hidden on screen cannot slip into the score. While a popup blocks, removals still delete from the accepted state, and placements are not accepted.

After the Finish video and RemoveCardPopup entrance finish, the app returns to the Idle scene through the shared fade-to-black transition once all seven physical readers are empty. The center slot and unregistered blocks must also be removed. Removing everything during playback still plays the video to the end, and a board already empty returns after the popup entrance finishes.

The verification code is in `Temp/RfidFlow/Verify.cs`. It uses the shared `RfidInput` path instead of real hardware, and normal score submissions are recorded in a separate temporary statistics file.

85 conditions passed in Unity 2022.3.62f3 Play Mode. Confirmed the actual Title → Tutorial → Game → Result → Statistics → Finish → Idle flow, ignoring seven-reader input before entry, ignoring repeated detection of the same block, individual removal and re-tagging, moves and the center preview, removal and placement during popup blocking, GamePanel reentry and correct submission. In Finish, confirmed waiting on partial removal, returning after removing an unregistered center block last, no early return on an empty board during playback, returning after the video and popup on reentry with an already empty board, and unloading the previous scene. Final results are in `Temp/RfidFlow/result.txt` and the check list is in `Temp/RfidFlow/checks.txt`. There were no compile or feature errors, and the source video's existing WindowsMediaFoundation color primaries warning occurred. Physical RFID and a Player build were not verified.

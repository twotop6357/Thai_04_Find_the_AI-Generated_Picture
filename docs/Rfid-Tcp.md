# RFID TCP input and card mapping

Readers connect as TCP clients and Unity receives values as the server. All settings are in one file, `Assets/StreamingAssets/RfidSettings.json`. In a build it is `<App>_Data/StreamingAssets/RfidSettings.json`, and rebuilding overwrites it with the project's file, so keep a separate copy of a file mapped on site.

## Value handling rules

- **The whole message is the card value.** There is no length limit or use of only the first part. Everything the reader sent becomes a HEX string for comparison.
- **Joining fragments:** TCP has no message boundaries, so fragmented values can look like other cards. A message is complete when receiving pauses for `frameGapMilliseconds`. If the reader sends an end marker, `frameDelimiterHex` is more accurate.
- **Release:** recognition is released when no value arrives for `releaseSeconds` from the arrival of the last value. Receiving the same value again also restarts the time.
- **Cards whose value changes:** one card can register several values. All registered values count as the same card, so a changing value is not seen as a card swap.
- **Unregistered values:** recognized as a placed card but Unknown to the game. They cannot be submitted and must be removed before the return.

## RfidSettings.json

| Item | Default | Description |
| --- | --- | --- |
| `listenAddress` | `0.0.0.0` | Address accepting connections |
| `port` | `5000` | Default port |
| `releaseSeconds` | `1.0` | Time until release (seconds) |
| `frameGapMilliseconds` | `50` | Receive gap treated as message completion. Must be shorter than the reader's repeat interval |
| `frameDelimiterHex` | `""` | Message end marker (e.g. `0D0A`). When set, this separates messages instead of the receive gap |
| `dataFormat` | `Auto` | `Auto`, `HexText` (HEX strings, per line), `Binary` (raw bytes) |
| `ignoredHexValues` | `[]` | Values not treated as cards (e.g. reader status signals) |
| `readers` | | Connection of the 7 slots to readers |
| `cards` | | Registered values per card ID |

If the file is edited while the game runs, it is reread when the F10 window opens. If the file cannot be read, the game runs with defaults and leaves the file intact so it can be fixed.

### Connecting readers to slots

Readers are distinguished by `remoteIp`. If all readers use the same IP, set `listenPort` per slot to distinguish them by port. When both are set, both must match.

## F10 mapping window

Press **F10** during the game to open and close it. Game input stops while the window is open, and placed cards must be placed again after closing to count in the game.

1. **Assign readers:** press a slot button for a connected reader in the bottom right list. Check which physical reader it is from the indicator in the bottom left area.
2. **Register cards:** press a card picture and place that card on any reader. The value is saved immediately. For a card whose value changes, leave it placed briefly so all incoming values register.
3. **Delete values:** press a card picture, then **Remove** next to the value.

A value already registered to another card is not moved; a message is shown instead. Delete it from that card first. Esc clears the card selection, and with no selection closes the window.

## Checking without a reader

Values can be sent from PowerShell. Stop sending to see the release after 1 second.

```powershell
$c = [Net.Sockets.TcpClient]::new('127.0.0.1', 5000); $s = $c.GetStream()
$b = [byte[]](0xE2,0x00,0x1A,0x2B)
1..10 | ForEach-Object { $s.Write($b, 0, $b.Length); Start-Sleep -Milliseconds 300 }
$c.Close()
```

## Installation checks

- Allow inbound TCP on `port` in Windows Firewall.
- Set the site PC's IP and port as the readers' connection target.
- While a card is placed, the reader must repeat its value at an interval shorter than `releaseSeconds`. A reader that sends only once is released right away.
- If values keep changing, compare the last received value in the F10 window. A value that doubles in length means the repeat interval is shorter than `frameGapMilliseconds`, so reduce this value or set `frameDelimiterHex`.

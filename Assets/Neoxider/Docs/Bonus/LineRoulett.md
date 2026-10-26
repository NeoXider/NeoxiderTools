# LineRoulett

Scrolls an ordered strip of UI images, slows it down, and reports the symbol nearest the arrow.

## Setup

- Assign an arrow, at least two non-null images in left-to-right order, and a non-empty sprite table.
- Use **Update Setting** or **Update Visual** to derive spacing from the first two images.
- Bind a button to `StartRolling()` and grant the result through `OnWin(int spriteIndex)`.

The same API is available in C#: `roulette.OnWin.AddListener(id => GrantPrize(id)); roulette.StartRolling();`.
Calls made while inactive, without an arrow, or with incomplete image references are ignored.
A new roll replaces the previous one. Disabling the component or its GameObject cancels both full-speed
and deceleration phases, stops motion, and emits no prize for the cancelled roll. Re-enable and call
`StartRolling()` to begin again. Roll timing uses scaled game time.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `speed` | Roll speed (units/sec). |
| `timeRoll` | Full-speed roll duration before slow-down. |
| `OnWin` | On Win. |
| `arrow` | Arrow. |
| `images` | Images. |
| `resetX` | Reset X. |
| `slowDownTime` | Slow Down Time. |
| `space` | Space. |
| `sprites` | Sprites. |
| `updateSetting` | Update Setting. |

## See Also

- [Module Root](../README.md)

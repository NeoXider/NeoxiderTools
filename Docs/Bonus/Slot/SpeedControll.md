# SpeedControll

**What it is:** a serializable struct with the reel speed settings of a slot machine: start speed and the length of
the constant-speed phase before braking. Script: `Scripts/Bonus/Slot/SpeedControll.cs`, namespace `Neo.Bonus`.

**How to use:**
1. Edit it on [SpinController](SpinController.md) (**Speed Controll**, default speed `5000`, time `1`). With
   **Is Single Speed** on, the controller copies it to every [Row](Row.md) when it lays out the rows.
2. For per-reel speeds turn **Is Single Speed** off and set each Row's **Speed Controll** (header *Speed setup*).
3. From code create one with `SpeedControll.Default(speed, timeSpin)`.

---

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `speed` | `float` | Start speed in units per second; the sign is the direction (`+` up, `-` down). With `0`, the Row uses its `defaultStartSpeed` in the last direction. |
| `timeSpin` | `float` | Seconds of constant speed before the reel starts braking (negative values count as `0`). |

The phase timer uses real time (`Time.realtimeSinceStartup`), so the spin length does not change with
`Time.timeScale`.

## See also

- [Row](Row.md), [SpinController](SpinController.md)
- [Slot module](README.md)

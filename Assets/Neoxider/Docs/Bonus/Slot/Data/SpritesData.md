# SpritesData

**What it is:** a `ScriptableObject` with the visual catalog of slot symbols (`SlotVisualData`: id, sprite,
description). Script: `Scripts/Bonus/Slot/Data/SpritesData.cs`, namespace `Neo.Bonus`.

**How to use:**
1. Create an asset via **Create > Neoxider > Bonus > Slot > Sprites Data** and add one entry per symbol with
   its sprite.
2. Assign it to the **All Sprites Data** field of [SpinController](../SpinController.md); the controller passes it
   to every [Row](../Row.md) when a spin starts.
3. Use the same asset for [SpriteMultiplayerData](SpriteMultiplayerData.md) generation so payout rows match the
   symbol ids.

---

## Fields

| Field (Inspector) | Property | Type | Description |
|-------------------|----------|------|-------------|
| Visuals | `visuals` | `SlotVisualData[]` | Symbol catalog. |
| Visuals > Id | `SlotVisualData.id` | `int` | Symbol id; `OnValidate` rewrites it to the array index. |
| Visuals > Sprite | `SlotVisualData.sprite` | `Sprite` | Image shown by `SlotElement`. |
| Visuals > Description | `SlotVisualData.description` | `string` | Optional text for your own UI. |

Symbol ids are array indices, so reordering the array changes which symbol a stored id or a payout row refers to.
Results (`SpinController.FinalElementIDs`, forced outcomes) use these ids; an id without a visual entry is not
displayed.

## See also

- [SpinController](../SpinController.md)
- [SpriteMultiplayerData](SpriteMultiplayerData.md) — payouts per symbol.
- [Slot data assets](README.md)

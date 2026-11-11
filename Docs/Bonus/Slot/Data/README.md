# Bonus / Slot / Data

**What it is:** the reusable `ScriptableObject` assets a slot machine reads: symbol visuals, payout multipliers,
paylines and bets. Scripts live in `Scripts/Bonus/Slot/Data/`, namespace `Neo.Bonus`. The assets hold
configuration only and never reference scene objects.

**Contents:**

| Page | Asset |
|------|-------|
| [SpritesData](./SpritesData.md) | Symbol catalog: id, sprite, description. |
| [SpriteMultiplayerData](./SpriteMultiplayerData.md) | Payout multiplier per symbol and run length. |
| [LinesData](./LinesData.md) | Payline definitions (window row per column). |
| [BetsData](./BetsData.md) | Selectable bets (price per line). |

**Navigation:** [← Slot](../README.md)

---

Assign all four to [SpinController](../SpinController.md): **All Sprites Data** and **Bets Data** on the
controller, **Lines Data** and the multiplier asset under **Check Spin**. Per-machine economy rules live in
[SlotEconomyDefinition](../SlotEconomyDefinition.md).

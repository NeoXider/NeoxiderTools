# Bonus module

Collections, slot machines, wheels of fortune, and time-based rewards. Scripts in `Scripts/Bonus/`. Links below point to the per-component documentation.

## Submodules

| Submodule | Description | Docs |
|-----------|-------------|--------------|
| **Slot** | Slot machine: reels, lines, spin, bets; optional winning-line **LineRenderer** (`SpinController` → Win Line Playback) | [Slot/README](./Slot/README.md) |
| **Collection** | Collectibles, boxes, visual manager | [Collection/README](./Collection/README.md) |
| **TimeReward** | CooldownReward, TimeReward (time-gated rewards) | [TimeReward/README](./TimeReward/README.md) |
| **WheelFortune** | Wheel of fortune (`WheelFortune`), WheelMoneyWin | [WheelFortune/README](./WheelFortune/README.md) |
| **LineRoulett** | Linear roulette (root script) | [LineRoulett](./LineRoulett.md) |

## Runtime API and scene workflows

Use `Collection` by reference for persistent unlocks, `CheckSpin` for payline evaluation without a scene,
and `SlotEconomyDefinition` for shared symbol data and deterministic weighted picks. The components
`Box`, `SpinController`, `WheelFortune`, and `CooldownReward` adapt these mechanics to scene objects and
UnityEvents. `ResourceRegen` binds a cooldown to a wallet and an optional countdown label.

Assign scene references explicitly in the Inspector. Collection item/economy ScriptableObjects contain
reusable data; views and wallets belong on scene components. Hook each grant to one recipient:
`SpinController` already pays its configured wallet when **Auto Payout** is enabled.

Reward claims consume their state before grant callbacks and reject recursive claims. Slot configuration
is locked throughout an accepted spin, including gaps between staggered reels. See the submodule entries
for persistence keys, cancellation, numeric limits, and the equivalent C# calls.

## See also

- [Tools/Time](../Tools/Time/README.md)
- [Save](../Save/README.md)
- [Tools/Random](../Tools/Random/README.md)

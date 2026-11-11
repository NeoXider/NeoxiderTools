# Bonus / WheelFortune

**What it is:** the prize wheel subsystem. [WheelFortune](./WheelFortune.md) spins a uGUI wheel and reports the
sector under the arrow; [WheelMoneyWin](./WheelMoneyWin.md) turns that sector id into coins in a `Neo.Shop.Money`
wallet.

**How to use:** put `Prefabs/Bonus/WheelFortune.prefab` on a Canvas (or use **Neoxider/Bonus/WheelFortune** in the
create menu), connect `OnWinIdVariant(int)` to `WheelMoneyWin.Win` or to your own grant, and call `Spin()` / `Stop()`
from buttons or code.

**Contents:** [WheelFortune](./WheelFortune.md), [WheelMoneyWin](./WheelMoneyWin.md) and the
[demo scene](#demo-scene).

**Navigation:** [← Bonus](../README.md)

---

## Demo scene

`Scenes/Bonuses/WheelFortuneExample.unity` in the Demo download (`Assets/NeoxiderDemos/Demo`). The whole setup is saved in the scene, so the wiring is visible in the Inspector
without entering Play Mode:

| Object | What to look at |
|--------|-----------------|
| `Canvas/WheelFortune` | The package prefab. SPIN / STOP call `Spin()` / `Stop()`; `OnWinIdVariant` calls `Wheel Money Win.Win` and `Demo Controller.OnWheelResult`. Each sector label shows the coins that sector pays. |
| `[Demo] Services/Wallet (Money)` | Session-only wallet (**Persist Money** off); its text list drives the **Coins** label. |
| `[Demo] Services/Wheel Money Win` | `wins` = coins per sector id, `prize` = the **Last prize** label, **Wallet** = the wallet above. |
| `[Demo] Services/Demo Controller` | `WheelFortuneDemoController`: shows `State` and `canUse`, writes the log, **Re-arm wheel** sets `canUse = true` on the single-use wheel, **Spin from code** calls `Spin()` and `Stop()` 1.5 s later. |

The scene is saved with its UI and wiring.

Equivalent C#:

```csharp
wheel.OnWinIdVariant.AddListener(sector => wallet.Add(prizes[sector]));
wheel.Spin();
// ...when the player presses stop:
wheel.Stop();
```

## Documents

- [WheelFortune](./WheelFortune.md)
- [WheelMoneyWin](./WheelMoneyWin.md)

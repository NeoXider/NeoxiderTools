# TextMoney

**What it is:** a `SetText` component (`Scripts/Shop/TextMoney.cs`, namespace `Neo.Shop`) that shows one `Money`
balance — current, level or all-time — and updates it through the wallet's reactive property.

**How to use:**
1. Add `TextMoney` to an object with a `TextMeshPro` text (`Add Component > Neoxider > Shop > TextMoney`).
2. Pick the `Display Mode` (`Money`, `LevelMoney`, `AllMoney`).
3. Optionally assign `Money Source` or `Money Save Key`; otherwise `Money.I` is shown.

---

## Wallet resolution

- Non-empty `Money Save Key`: the first registered/found `Money` with that `SaveKey`.
- Otherwise (or when no keyed wallet exists): the explicit `Money Source`, then `Money.I`.

When no wallet exists yet (for example the HUD loads before the wallet prefab), the component waits for the next
wallet registration (`Money` Awake/OnEnable) and binds then. It does not poll the scene every frame. A wallet object
that never becomes active does not register and is not picked up.

## Runtime Source Switching

`TextMoney.SetMoneySource(Money money)` changes the displayed wallet at runtime and safely re-subscribes to the selected `Money` reactive property.

`TextMoney.SetMoneySaveKey(string saveKey)` selects a wallet by `Money.SaveKey`.

Both setters store the requested source while the component is disabled. The display stays unsubscribed until enable, then immediately shows the selected wallet's current balance. This also applies to Inspector/UnityEvent calls made while a panel is hidden.

`TextMoney` calls the protected base `SetText` enable/disable hooks. With base animation enabled, disabling the component pauses its current number tween as well as wallet subscriptions; enabling refreshes from the current wallet and uses the normal base animation path. Unsubscribing always targets the property that was actually subscribed, even if the wallet was destroyed meanwhile.

```csharp
TextMoney gemsText = GetComponent<TextMoney>();
gemsText.SetMoneySaveKey("Gems");
Money shown = gemsText.BoundMoney; // null while waiting for a wallet or while disabled
```

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_displayMode` | Balance display mode: `Money` (main), `LevelMoney` (level only), `AllMoney` (total sum). |
| `_moneySource` | Currency source (`Money` component). If left empty, the global singleton `Money.I` is used. This is useful if you have multiple resource types (e.g., Energy, Stars) with their own local `Money` instances. |
| `_moneySaveKey` | Optional `Money.SaveKey` to display; takes precedence over `_moneySource` when a matching wallet exists. |
| `amount` | (Info) The current displayed value. |

## Public API

| Member | Description |
|--------|-------------|
| `SetMoneySource(Money)` | Display another wallet. |
| `SetMoneySaveKey(string)` | Display the wallet with this save key. |
| `BoundMoney : Money` | Wallet currently displayed; null while waiting or disabled. |
| `amount : float` | Last displayed value. |

## See Also

- [Money](Money.md) - Currency manager.
- [Module Root](README.md)

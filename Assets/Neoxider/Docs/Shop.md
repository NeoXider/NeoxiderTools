# Shop Module

**What it is:** an overview of the Shop module (store, currency, purchases). Class navigation is in [Shop/README](Shop/README.md).

**How to use:** see the sections below or [Shop/README](Shop/README.md).

---


## 1. Introduction

The **Shop** module provides a full-featured in-game store system. It makes it easy to create, configure, and manage items, process purchases, and update the user interface. The system works flexibly with currency through the `Money` component and supports both one-time and repeatable purchases.

The main goal of the module is to provide a ready-made solution for quickly integrating a store into a game, reducing the time spent developing standard purchase logic and data persistence.

---

## 2. Class Descriptions

### Money
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/Money.cs`

**Description**
`Money` is a singleton class responsible for managing all money operations in the game. It tracks the total balance, money earned per level, and automatically saves data between sessions.

**Key features**
- **Three balance types**: `money` (current), `levelMoney` (for the current level), and `allMoney` (all money ever earned).
- **Save and load**: Persists the balance through `SaveProvider` (toggle with the `_persistMoney` field; off = session-only).
- **Optional soft cap**: `MaxMoney` clamps `Add` / `SetMoney`; `AddOverflow` deliberately ignores the cap for bonus rewards.
- **Multi-wallet**: several `Money` components can coexist (different `SaveKey`); look one up with `FindBySaveKey` / `TryFindBySaveKey`.
- **UI integration**: Can directly update `TextMeshPro` text fields.
- **Reactive state**: Balances are exposed as `ReactivePropertyFloat` (`CurrentMoney`, `LevelMoney`, `AllMoney`, `LastChangeMoney`) — subscribe to those instead of UnityEvents.
- **Optional networking**: implements `IMoneySpendAuthority`; with the Mirror define and `isNetworked` enabled the wallet is server-authoritative.

**Public methods**
- `AddLevelMoney(float count)`: Adds money to the current level balance.
- `SetLevelMoney(float count = 0)`: Sets the level balance to the given value. Returns a `float` — the amount that was on the balance before the reset.
- `SetMoney(float count = 0)` / `SetCurrentMoney(float amount)`: Sets the main balance (clamped to `MaxMoney` when set). `SetCurrentMoney` is the `void` UnityEvent-friendly wrapper.
- `SetMoneyForLevel(bool resetLevelMoney = true)`: Transfers the money earned during the level to the main balance. Returns a `float` — the transferred amount.
- `CanSpend(float count)`: Checks whether there is enough money to make a purchase. Returns a `bool`.
- `Spend(float count)`: Deducts the specified amount of money from the main balance. Returns a `bool` — `true` on success, `false` if funds are insufficient.
- `TrySpend(float amount)`: Like `Spend` but returns a `MoneySpendResult` (confirmed / insufficient / pending server authority / invalid amount).
- `SpendFromButton(float count)`: A `void` wrapper for `Button.onClick` / `UnityEvent` when the spend result is not needed.
- `Add(float count)`: Adds money to the main balance (clamped to `MaxMoney`).
- `AddOverflow(float amount)`: Adds money ignoring `MaxMoney`.
- `ReloadBalanceFromSave()` / `ClearSavedMoneyAndReset()`: Reload or wipe the persisted balance.

**Balance change notifications**

`Money` does not expose `UnityEvent`s for balance changes. Subscribe to the reactive properties instead, e.g. `money.CurrentMoney.OnValueChanged += v => ...`. For plain UI text, add a `TextMoney` component and pick the balance mode — it wires itself.

---

### Shop
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/Shop.cs`

**Description**
`Shop` is the main component that manages store logic. It organizes items, processes purchases, and updates their visual representation.

**Purchase flow.** The `ShopPurchaseFlow` field selects behaviour: `BuyAndEquip` (default), `BuyOnly`, `EquipOnly`, or `Browse`. Owned single-purchase items never spend money again; free items are granted without a wallet.

**Public methods (canonical, stable string / asset API)**
- `Buy(ShopItemData item)` / `Buy(string itemId)`: Runs the purchase/equip flow for an item.
- `BuyBundle(ShopBundleData bundle)` / `BuyBundle(string bundleId)`: Purchases a bundle and grants all its items.
- `Select(...)` / `ShowPreview(...)`: Equips or previews an item (asset, id, or — via preview — index).
- `IsOwned(...)` / `IsBundleOwned(...)` / `CanAfford(...)`: Query ownership and affordability.
- `GetPrice(...)` / `SetRuntimePrice(id, price)` / `ClearRuntimePrice(id)`: Read the effective price and manage runtime discounts.
- `SetItems(ShopItemData[])` / `SetBundles(ShopBundleData[])`: Replace the runtime catalog.
- `ResolveCurrencyMoney(itemId)`: Returns the `Money` wallet a purchase of that item would spend from (respects per-item currency overrides).

The integer-indexed methods (`Buy(int)`, `ShowPreview(int)`, `Id`, `Prices`) are `[Obsolete]` proxies kept for legacy scene wiring and slated for removal in v9.

**Unity Events**
- `OnSelectId` / `OnPurchasedId` / `OnPurchaseFailedId` (`ShopStringEvent`, passes the item id) — the canonical events.
- `OnPurchasedBundle` (`ShopBundleEvent`, passes the bundle) and `OnShopChanged`.
- `OnSelect` / `OnPurchased` / `OnPurchaseFailed` (`int`): legacy index proxies, fired only when the index is resolvable.
- `OnLoad`: Invoked once after the shop finishes loading its saved profile.

---

### ShopItemData
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/ShopItemData.cs`

**Description**
`ShopItemData` is a `ScriptableObject` that stores all information about an item: its name, price, icons, and purchase type (one-time or not).

**Key features**
- **Centralized storage**: Makes it easy to configure items in the Unity Inspector.
- **Flexibility**: The same item can be used in different stores.

---

### ShopItem
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/ShopItem.cs`

**Description**
`ShopItem` is the component responsible for the visual representation of a single item in the UI. It displays data from `ShopItemData` and reacts to user actions.

**Public methods**
- `Visual(ShopItemData shopItemData, int price)`: Updates the element's appearance (name, price, icon) based on the data.
- `Select(bool active)`: Sets the visual state to "selected" or "not selected".

**Unity Events**
- `OnSelectItem`: Invoked when the element becomes selected.
- `OnDeselectItem`: Invoked when the element is deselected.

---

### TextMoney
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/TextMoney.cs`

**Description**
`TextMoney` is a UI component for displaying a selected balance from `Money`. It supports the `Money`, `LevelMoney`, and `AllMoney` modes, automatically subscribes to the appropriate event, and updates the text field without any extra code.

---

### Interfaces (IMoneySpend, IMoneyAdd)
- **Namespace**: `(global)`
- **File path**: `Assets/Neoxider/Scripts/Shop/InterfaceMoney.cs`

**Description**
These interfaces implement the dependency inversion pattern. The `Shop` component depends not on the concrete `Money` class but on these interfaces. This makes it easy to swap out the currency system in the future without changing the store code.
- `IMoneySpend`: Defines the `Spend(float count)` method, which must return a `bool` (the operation result).
- `IMoneyAdd`: Defines the `Add(float count)` method.

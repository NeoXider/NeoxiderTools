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
- **Save and load**: Automatically saves the balance to `PlayerPrefs`.
- **UI integration**: Can directly update `TextMeshPro` text fields.
- **Events**: Notifies other systems about balance changes via `UnityEvent`.

**Public methods**
- `AddLevelMoney(float count)`: Adds money to the current level balance.
- `SetLevelMoney(float count = 0)`: Sets the level balance to the given value. Returns a `float` — the amount that was on the balance before the reset.
- `SetMoneyForLevel(bool resetLevelMoney = true)`: Transfers the money earned during the level to the main balance. Returns a `float` — the transferred amount.
- `CanSpend(float count)`: Checks whether there is enough money to make a purchase. Returns a `bool`.
- `Spend(float count)`: Deducts the specified amount of money from the main balance. Returns a `bool` — `true` on success, `false` if funds are insufficient.
- `SpendFromButton(float count)`: A `void` wrapper for `Button.onClick` / `UnityEvent` when the spend result is not needed.
- `Add(float count)`: Adds money to the main balance.

**Unity Events**
- `OnChangedLevelMoney`: Invoked when the level balance changes. Passes a `float` (the new balance).
- `OnChangedMoney`: Invoked when the main balance changes. Passes a `float` (the new balance).
- `OnChangeLastMoney`: Invoked on any balance change. Passes a `float` (the amount of the last change).
- `OnChangeAllMoney`: Invoked when money is added. Passes a `float` (the total amount of money earned over all time).

---

### Shop
- **Namespace**: `Neo.Shop`
- **File path**: `Assets/Neoxider/Scripts/Shop/Shop.cs`

**Description**
`Shop` is the main component that manages store logic. It organizes items, processes purchases, and updates their visual representation.

**Public methods**
- `ShowPreview(int id)`: Displays a preview of the item by its `id`.
- `Buy(int id)`: Purchases the item with the given `id`.
- `Buy()`: Purchases the item currently selected in the preview.
- `Visual()`: Updates the visual state of all items in the store.

**Unity Events**
- `OnSelect`: Invoked when an item is selected. Passes an `int` (the item's `id`).
- `OnPurchased`: Invoked on a successful purchase. Passes an `int` (the `id` of the purchased item).
- `OnPurchaseFailed`: Invoked on a failed purchase attempt (insufficient funds). Passes an `int` (the item's `id`).
- `OnLoad`: Invoked after saved price data has been loaded.

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

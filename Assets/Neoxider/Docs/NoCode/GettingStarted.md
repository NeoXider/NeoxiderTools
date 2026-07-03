# NoCode Getting Started — build gameplay without writing code

NeoxiderTools lets you assemble real game logic entirely in the Unity Inspector. Every building block is a regular component: add it from `Add Component → Neoxider/…` (or the `GameObject → Neoxider` create menu), wire UnityEvents, press Play.

This page is the beginner path. Each step names the component to drop in and what to connect — no C# involved.

## The core idea

1. **Components expose UnityEvents** (`OnClick`, `OnWin`, `OnPurchased`, `OnValueChanged`…).
2. **You connect those events in the Inspector** to methods of other components.
3. **Bindings keep UI in sync** — `NoCode*` binding components push values (money, health, progress) into Text/Image/Slider automatically.

That's the whole model: *event → action*, *value → binding*.

## 10-minute tour

### 1. React to anything: `NeoCondition`

The Swiss-army knife. Watches a field, property, method result, or GameObject state, combines checks with AND/OR, and fires `OnTrue` / `OnFalse` events. "Open the door when the player has 3 keys" is one component and zero scripts.
→ [Condition/NeoCondition.md](../Condition/NeoCondition.md)

### 2. Count things: `Counter`

Kills, coins, clicks — `Add()`, `Subtract()`, min/max limits, events at thresholds.
→ [Tools/Components/Counter.md](../Tools/Components/Counter.md)

### 3. Time things: `Timer` and `CooldownReward`

Countdown timers with `OnFinished` events; daily/interval rewards with automatic claim.
→ [Tools/Time/Timer.md](../Tools/Time/Timer.md), [Bonus/TimeReward/README.md](../Bonus/TimeReward/README.md)

### 4. Money and shop — no code

- `Money` — a wallet with optional cap and persistence. Show it with `TextMoney`.
- `Shop` + `ShopItemData` assets — buy/own/equip flow, `OnPurchased` event.
- `ShopListView` + `ShopCategorySelector` — a scrolling catalog with category tabs or prev/next pill.
- `EquipmentManager` — dress-up/skins: one item per category, applied to a sprite and saved automatically.
→ [Shop.md](../Shop.md)

### 5. Mini-games as components

- **Slots**: `SpinController` + `SlotEconomyDefinition` (weighted symbols, payouts, special symbol).
- **Wheel of fortune**: `WheelFortune`.
- **Collections**: `Collection` + collectible items.
→ [Bonus.md](../Bonus.md)

### 6. UI, pages, and juice

- `PM` + `UIPage` (NeoxiderPages sample) — menu/game/settings page navigation via `BtnChangePage`.
- `ButtonScale`, `ButtonShake`, `AnimationFly` — feedback and reward-flight effects.
- `VisualToggle` — one component for on/off visuals (sprites, colors, objects).
→ [UI/README.md](../UI/README.md), [NeoxiderPages/README.md](../NeoxiderPages/README.md)

### 7. Sound in one click: `AM` + `PlayAudioBtn`

Drop the `AM` audio manager into the scene, fill its sound list, add `PlayAudioBtn` to any button — it plays sound id 0 (your click) by default.
→ [Audio.md](../Audio.md)

### 8. Save without thinking

Most components above persist themselves (`Money`, `Shop`, `EquipmentManager`, `CooldownReward`). For your own values use `SaveManager` with `[SaveField]`-marked fields.
→ [Save.md](../Save.md)

### 9. Bind values to UI: `NoCode*` bindings

`ComponentFloatBinding` and friends push any component's value into a Slider/Image fill/Text — health bars and progress bars without a view script.
→ [NoCode/README.md](./README.md)

### 10. Multiplayer — still no code

With Mirror installed: `NetworkPropertySync` replicates a field, `NetworkActionRelay` relays a UnityEvent, `NeoNetworkDiscovery.QuickPlay()` gives one-button LAN play. Without Mirror everything above keeps working solo.
→ [Network/Multiplayer_Guide.md](../Network/Multiplayer_Guide.md)

## Where to go next

- Full NoCode binding reference: [NoCode/README.md](./README.md)
- Module map for everything else: [README.md](../README.md)
- When you outgrow the inspector: every component here has a clean C# API — the same `Shop`, `Money`, `Timer` objects are meant to be driven from code too.

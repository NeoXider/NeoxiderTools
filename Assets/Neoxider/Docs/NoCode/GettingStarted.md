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

1. Add `NoCodeBindText` to a TMP label, or `SetProgress` to a UI object and assign its Slider/Image target.
2. Under **Binding**, assign **Source Root**, select its component type, then select a readable numeric field/property. If that type is repeated, set **Component Occurrence** (`0` = first matching component, `1` = second); unrelated component types do not count. Leave Source Root empty to read from the binding's own GameObject.
3. Choose **Reactive** for `ReactivePropertyFloat`/`Int`/`Bool`, or **Poll** for values such as a cooldown. Set the progress Min/Max range when using `SetProgress`.
4. For `HP {0:0}/{1:0}`, use `NoCodeFormattedText`, add two entries under **Values**, and enter that pattern under **Format**. Missing entries display as zero.
5. For a source that spawns later, enable **Use Scene Search**, set **Search Object Name**, and enable **Wait For Object**. Keep **Poll In Late Update** enabled so Reactive mode can retry unresolved sources. Prefer explicit scene references when available.

Disabling a binding releases its reactive listeners. Re-enable it after changing the source, or refresh explicitly from C#: `NoCodeFormattedText.RefreshFromSource()` is public; custom float views can expose the base class's protected refresh method. Text targets created later are populated on the next refresh without a duplicate formatted-value event.

Changing binding settings or Poll Interval in the Inspector queues reconciliation for the next active `LateUpdate`; source getters are not called from validation. Reactive fallback continues at the next allowed poll. Once stays nonpolling, and a missing/negative Component Occurrence never silently uses the first component. Existing bindings default to occurrence `0`; the legacy Editor component-list index keeps its own meaning.

For `NoCodeFormattedText`, assigning a new text target or editing only Poll Interval redraws the current result without another `OnTextChanged` if the string stayed the same. A later changed value/format result notifies once. Re-enable still publishes an initial result; Once requires explicit refresh or re-enable to redraw after validation.

→ [NoCode/README.md](./README.md)

### 10. Abilities and combat — castable without code

The Dota-style ability system is fully wireable: author `AbilityDefinition` / `ModifierDefinition` assets, drop `AbilityUnitBehaviour` + `AbilityCasterBehaviour` on a unit, then:

- `AbilityNoCodeAction` — cast / grant / level / apply-modifier / damage / heal from any UnityEvent (a button's `OnClick` → `Execute()`).
- `AbilityAutoCaster` — Survivor-style auto-fire: every ability casts the moment it is ready, locking onto the nearest enemy.
- `AbilityCooldownSource` — exposes `CooldownNormalized` / `SecondsRemaining` for the bindings below, so cooldown bars are just `SetProgress` in Poll mode.
→ [Abilities/README.md](../Abilities/README.md)

### 11. Multiplayer — still no code

With Mirror installed: `NetworkPropertySync` replicates a field, `NetworkActionRelay` relays a UnityEvent, `NeoNetworkDiscovery.QuickPlay()` gives one-button LAN play. Without Mirror everything above keeps working solo.
→ [Network/Multiplayer_Guide.md](../Network/Multiplayer_Guide.md)

## Where to go next

- Full NoCode binding reference: [NoCode/README.md](./README.md)
- Module map for everything else: [README.md](../README.md)
- When you outgrow the inspector: every component here has a clean C# API — the same `Shop`, `Money`, `Timer` objects are meant to be driven from code too.

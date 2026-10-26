# LevelComponent

**Purpose:** a scene adapter for `LevelModel` that implements `ILevelProvider`, exposes reactive state, and persists a `LevelProfileData` through `SaveProvider`. File: `Scripts/Core/Level/Components/LevelComponent.cs`. Namespace: `Neo.Core.Level`; assembly: `Neo.Core.Level.Components`.

## Setup

- Add via **Add Component → Neoxider → Core → Level Component**.
- Assign a `Level Curve` asset, or leave it empty to use the model's default linear curve: 100 XP per level, starting at level 1 with 0 XP.
- With `Use Xp` enabled, `Start Xp` determines the initial level. With it disabled, `Start Level` sets the level directly and `AddXp` only changes the XP total.
- Give every independently persisted profile its own `Save Key`. Multiple components with the default key share the same stored profile. An empty or whitespace key disables persistence.
- `Load On Awake` loads saved progress during the first initialization; `Auto Save` saves when an initialized component is disabled. Saved progress replaces the starting XP, level, and max level. The curve and `Use Xp` remain component configuration.

## Key Fields (Inspector)

| Inspector field | Description |
|-------|-------------|
| `Level Curve` | Optional reusable `LevelCurveDefinition` asset. |
| `Use Xp` | Derive level from total XP, or use direct levels. |
| `Start Xp`, `Start Level` | Starting values used without a saved profile and by `ResetProgress()`. |
| `Has Max Level`, `Max Level` | Enable a level cap; 0 means uncapped. |
| `Save Key` | Key used by the active `SaveProvider`. |
| `Load On Awake`, `Auto Save` | Enable initial loading and saving on disable. |
| `Level State`, `Xp State`, `Xp To Next Level State` | Reactive output for UI and Inspector listeners. |

## Runtime Contract

- `EnsureInitialized()` creates the model and optionally loads the profile. `Awake`, provider getters, `AddXp`, `SetLevel`, `ResetProgress`, and `GetProfileSnapshot` call it automatically. Repeated calls are safe, including reads from initialization listeners. A provider read failure propagates to the caller and initialization can be retried.
- `AddXp(amount)` delegates to the model; nonpositive amounts are ignored. With XP enabled, the model evaluates the curve. `SetLevel(level)` synchronizes XP to the target level's minimum threshold; an unreachable level resolves according to the curve. With XP disabled, it changes the level directly. Levels are at least 1 and respect the active cap.
- `Level`, `TotalXp`, `XpToNextLevel`, `UseXp`, `HasMaxLevel`, and `MaxLevel` read the model. `LevelState`, `XpState`, and `XpToNextLevelState` are output mirrors. Their `*StateValue` convenience getters read those mirrors without initializing; call `EnsureInitialized()` before using them before `Awake`.
- All three reactive values are synchronized before XP/level callbacks. Use `LevelState.AddListener(...)` or `XpState.Subscribe(...)` for code subscriptions, and dispose/remove subscriptions when the consumer is destroyed. Reactive refreshes can notify more than once during one API call; callbacks should tolerate refreshes of the same value. A listener can perform another mutation before later callbacks run; do not assume that a captured `OnLevelUp` argument equals a later read of `Level`. Notifications do not form a transaction around arbitrary callback mutations.
- Assigning `LevelCurveDefinition` after initialization reevaluates XP and refreshes the mirrors. Assigning `null` restores the default model curve. Curve reassignment does not raise `OnLevelUp`.
- `GetProfileSnapshot()` returns an independent data object containing the current XP, level, and cap, including initial saved progress. Editing the snapshot does not modify the component.
- `ResetProgress()` restores configured starting values and the configured cap. It initializes first, so a later getter cannot load an old save over the reset. It does not delete or immediately overwrite the stored profile; call `Save()` to persist the reset.
- `Save()` writes the profile and flushes `SaveProvider`. `Load()` reads the current key, sanitizes saved data, replaces the cap before restoring the level, and refreshes reactive state. For manual persistence before `Awake`, call `EnsureInitialized()` first: `Save()` and `Load()` require an existing model. Missing or malformed JSON leaves the current model state intact; malformed JSON is reported through gated diagnostics.

```csharp
using Neo.Core.Level;

// Inside a MonoBehaviour with a LevelComponent on the same object:
LevelComponent levels = GetComponent<LevelComponent>();
levels.EnsureInitialized();
levels.AddXp(250); // Adds to the current profile; from 0 XP on the default curve, this reaches level 3.
LevelProfileData snapshot = levels.GetProfileSnapshot();
levels.Save();
```

## Initialization and component lifecycle

Subscribe to `OnProfileLoaded` before the first initialization if you need the initial completion event. `Awake` normally initializes the component, so subscribing in `Start` is too late for that event. An inactive GameObject allows configuring the component and subscribing before activation; calling `EnsureInitialized()` explicitly also starts the initial load when enabled.

With `Auto Save` enabled, disabling an initialized component or deactivating its GameObject saves its current profile. Re-enabling keeps the existing model and does not run the initial load again. Use `Load()` explicitly to replace live progress with newly changed storage. Disabling before initialization, disabling auto-save, or clearing the key prevents this automatic write. Save-provider failures propagate; `OnProfileSaved` is raised only after the save call returns successfully.

## Events and scene/NoCode workflow

| Event | Timing |
|-------|--------|
| `OnXpGained` | After a positive XP addition; all reactive values reflect the new model state. |
| `OnLevelUp(int)` | After an XP addition or `SetLevel` changes the level. Despite its name, direct level decreases also raise it. Loading/resetting a profile does not emit intermediate level-change notifications. |
| `OnProfileLoaded` | After a keyed load attempt and reactive synchronization, including missing or malformed JSON. It is a completion notification, not a successful-data indicator. |
| `OnProfileSaved` | After profile serialization and the provider's save operation complete. |

For Inspector wiring, connect gameplay UnityEvents to `AddXp(int)`, `SetLevel(int)`, `ResetProgress()`, or the persistence methods. Bind UI to the reactive fields or read `LevelStateValue`, `XpStateValue`, and `XpToNextLevelStateValue` with a condition/binding component. Configure persistence before initialization.

The optional [LevelNoCodeAction](../Bridge/LevelNoCodeAction.md) calls the same public `AddXp`/`SetLevel` methods. Assign this component as its provider, choose the action and amount/level, and invoke `Execute()`. Its own level-change output uses the action's result captured before its `OnSuccess` listeners run; a success listener's later mutation does not rewrite that result. Ordinary C# consumers can depend on `ILevelProvider` without using the NoCode component. Custom game assemblies reference `Neo.Core` for that interface and `Neo.Core.Level.Components` when naming this component; add `Neo.Core.Level.Bridge` only when using the optional bridges.

Кратко: `EnsureInitialized()` подготавливает модель и начальную загрузку; `ResetProgress()` возвращает настроенный старт, а `Save()` сохраняет его. Повторное включение компонента не загружает профиль заново. Для ранних `Save()`/`Load()` сначала выполните инициализацию; для независимых профилей задайте разные ключи.

## See Also

- [Core module and plain C# APIs](../../README.md)
- [Level setup](../../Level.md)
- [Level curve assets and formulas](../../LevelCurveDefinition.md)
- [Optional Level bridges](../Bridge/README.md)
- [Save providers](../../../Save/README.md)
- [Reactive properties and subscriptions](../../../Reactive/README.md)

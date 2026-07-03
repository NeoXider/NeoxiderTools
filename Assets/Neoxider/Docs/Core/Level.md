# Level Component

**What it is:** A MonoBehaviour that implements `ILevelProvider`. A universal source of level and experience: player, battle pass, chapter stage. Path: `Scripts/Core/Level/Components/LevelComponent.cs`, namespace `Neo.Core.Level`.

**How to use:**
1. Add the component to a GameObject (Add Component → Neoxider/Core/Level Component).
2. Optionally assign a `LevelCurveDefinition` (ScriptableObject) — three modes: **Formula** (Linear, Quadratic, Exponential, Power, etc.), **Curve** (AnimationCurve: X = level, Y = cumulative XP), **Custom** (manual level → XP table).
3. Set the starting level/XP, optionally a max level. When saving is enabled, specify a SaveKey.
4. Call `AddXp(int)` or `SetLevel(int)` from code/NoCode; subscribe to OnLevelUp / OnXpGained for UI or rewards.
5. Progression and RPG obtain the level through a reference to this component (the Level Provider field).

---

## Fields and Properties

| Name | Type | Purpose |
|-----|-----|------------|
| LevelCurveDefinition | LevelCurveDefinition | Level curve (optional). |
| LevelState, XpState, XpToNextLevelState | ReactivePropertyInt | Reactive state for UI binding. |
| LevelStateValue, XpStateValue, XpToNextLevelStateValue | int | Current values of the reactive fields (for NeoCondition). |
| Level | int | Current level (≥ 1). |
| TotalXp | int | Accumulated experience. |
| XpToNextLevel | int | XP to the next level (0 at max level). |
| UseXp | bool | Level is computed from XP via the curve. |
| HasMaxLevel, MaxLevel | bool, int | Max level cap (0 = no limit). |

## Methods

| Signature | Returns | Description |
|-----------|---------|----------|
| AddXp(int amount) | void | Adds experience; the level is recalculated via the curve. |
| SetLevel(int level) | void | Sets the level directly (respecting MaxLevel). |
| GetProfileSnapshot() | LevelProfileData | Data snapshot for saving. |
| Save(), Load() | void | Save/load by SaveKey via SaveProvider. |
| Reset() | void | Reset to the starting level/XP. |

## Events (UnityEvent)

| Event | When Invoked | Parameters |
|---------|------------------|-----------|
| OnLevelUp | When the level increases after AddXp | int — new level |
| OnXpGained | After XP is added | — |
| OnProfileLoaded | After the profile is loaded | — |
| OnProfileSaved | After the profile is saved | — |

## Examples

```csharp
ILevelProvider p = player.GetComponent<LevelComponent>();
p.AddXp(100);
int tier = battlePass.GetComponent<LevelComponent>().Level;
```

NoCode: the **LevelNoCodeAction** component (AddXp, SetLevel actions), **LevelConditionAdapter** (LevelAtLeast, XpAtLeast, XpToNextLevelAtMost).

More on the level curve: **[LevelCurveDefinition.md](./LevelCurveDefinition.md)** — Formula / Curve / Custom modes, formula types, fields, and API.

## See Also

- [LevelCurveDefinition](./LevelCurveDefinition.md) — level curve definition (Formula, Curve, Custom).
- [HealthComponent](./HealthComponent.md) — HP/Mana pools.
- [Progression/README.md](../Progression/README.md) — level rewards, perks.
- [Rpg/README.md](../Rpg/README.md) — level and stats in combat.

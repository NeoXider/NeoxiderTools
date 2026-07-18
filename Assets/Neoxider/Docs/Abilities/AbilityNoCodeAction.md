# AbilityNoCodeAction

**What it is:** the universal Inspector-driven bridge for ability actions. Pick an action type, fill the matching parameters, and invoke the parameterless `Execute()` from any UnityEvent (button click, trigger, timer) — no code. Follows the same pattern as `LevelNoCodeAction` / `RpgNoCodeAction` / `QuestNoCodeAction`.

**How to use:** add the component anywhere (on the unit, on a UI button, on a trigger), choose **Action Type**, assign the target references (or leave them empty to search this GameObject and its parents), and wire something to `Execute()`.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Bridge/AbilityNoCodeAction.cs`
- Component menu: `Neoxider/Abilities/Ability NoCode Action`

## Actions

| Action | Does | Uses |
|--------|------|------|
| `CastFirstAbility` | Casts the first entry of the caster's **Abilities** list. | Caster |
| `CastById` | No-target cast of **Ability Id**. | Caster, Ability/Ability Id |
| `CastAtUnit` | Unit-targeted cast at **Target Unit** (must be assigned). | Caster, Ability/Ability Id, Target Unit |
| `CastAtSelf` | Casts at the caster's own unit (self-buffs, `Unit`-targeted heals). | Caster, Ability/Ability Id |
| `GrantAbility` | Grants the ability to the acting unit. With an **Ability** asset it registers the blueprint first, so the ability does not need to be in a library. | Unit, Ability/Ability Id |
| `RevokeAbility` | Removes the granted ability. | Unit, Ability/Ability Id |
| `SetAbilityLevel` | Sets a granted ability's level (leveled values scale). | Unit, Ability/Ability Id, Level |
| `SetUnitLevel` | Sets the domain unit level. | Unit, Level |
| `ApplyModifier` | Applies the modifier to **Target Unit** (or the acting unit). With a **Modifier** asset it registers the blueprint first. | Unit (as source), Modifier/Modifier Id, Target Unit |
| `RemoveModifier` | Removes every active instance of the modifier from the target. | Modifier/Modifier Id, Target Unit |
| `ApplyDamage` | Pure damage through the full pipeline (same as `AbilityUnitBehaviour.ApplyDamage`). | Amount, Target Unit |
| `Heal` | Heals via `AbilityUnitBehaviour.ApplyHeal` (honors `healing_received_mul`, never revives). | Amount, Target Unit |

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Action Type** | `AbilityNoCodeActionType` | Which action `Execute()` performs. |
| **Caster** | `AbilityCasterBehaviour` | For cast actions. Empty = searched on this GameObject and its parents. |
| **Unit** | `AbilityUnitBehaviour` | Acting unit for grant/level/modifier actions. Empty = the caster's unit, else searched on this GameObject and its parents. |
| **Target Unit** | `AbilityUnitBehaviour` | Explicit target for `CastAtUnit` / `ApplyModifier` / `RemoveModifier` / `ApplyDamage` / `Heal`. Empty = the acting unit (`CastAtUnit` requires it). |
| **Ability** | `AbilityDefinition` | Optional asset; overrides **Ability Id**. `GrantAbility` registers its blueprint. |
| **Ability Id** | `string` | Ability id used when no asset is assigned. |
| **Modifier** | `ModifierDefinition` | Optional asset; overrides **Modifier Id**. `ApplyModifier` registers its blueprint. |
| **Modifier Id** | `string` | Modifier id used when no asset is assigned. |
| **Level** | `int` | For `SetAbilityLevel` / `SetUnitLevel`. |
| **Amount** | `float` | For `ApplyDamage` / `Heal`. |

### Unity Events

| Event | Fires when | Passes |
|-------|-----------|--------|
| **On Success** `UnityEvent` | The action completed. | — |
| **On Failed** `UnityEvent<string>` | The action could not run. | Reason, e.g. `Cast failed: fireball (OnCooldown)`, `Unknown ability id: firebal`. |
| **On Result Message** `UnityEvent<string>` | Always (success or failure). | Human-readable receipt for debug text fields. |

Failures are always graceful: no exceptions, no console spam — wire **On Failed** to a text field to see why.

## Example — a cast button

1. On the player: `AbilityUnitBehaviour` + `AbilityCasterBehaviour` (as usual).
2. On a UI Button: add `AbilityNoCodeAction`, set **Action Type** `CastById`, **Ability Id** `fireball`, drag the player into **Caster**.
3. Wire the Button's `OnClick` → `AbilityNoCodeAction.Execute`.
4. Optional: wire **On Failed (string)** → a `SetText` to show `OnCooldown` / `NotEnoughResources`.

## Pitfalls

- **Cast actions need the ability granted.** `CastById` with an ungranted id fails with `NotGranted` — grant it via the caster's **Abilities** list, a `UnitTemplate`, or a `GrantAbility` action first.
- **Cast failure reasons also fire on the caster's `OnCastFailed`** — both events carry the same `CastFailureReason` name.
- Unit actions require the unit to be registered (Play mode, component enabled).

## See also

- [AbilityAutoCaster](./AbilityAutoCaster.md) — auto-fire abilities without a button
- [AbilityCooldownSource](./AbilityCooldownSource.md) — cooldown values for NoCode UI bindings
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — the C# cast API underneath
- Back: [Abilities module](./README.md)

# AbilityCooldownSource

**What it is:** a read-only binding source for one ability's cooldown. It exposes cheap, allocation-free properties computed from the caster on every read, so the NoCode bindings (`SetProgress`, `NoCodeBindText`) can poll them for cooldown bars and timers — no view script.

**How to use:** add it near an `AbilityCasterBehaviour` (or assign one), set **Ability Id**, then point a NoCode binding at one of the properties.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilityCooldownSource.cs`
- Component menu: `Neoxider/Abilities/Ability Cooldown Source`

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Caster** | `AbilityCasterBehaviour` | Empty = searched on this GameObject and its parents. |
| **Ability Id** | `string` | Ability whose cooldown is exposed. Also assignable at runtime via the `AbilityId` property (e.g. per hotbar slot). |

## Properties (bindable)

| Property | Range | Meaning |
|----------|-------|---------|
| `CooldownNormalized` | `0..1` | Remaining cooldown: `1` = just cast, `0` = ready. **Unknown/ungranted ids read `0`** — the same "reads as ready" convention as `AbilityCasterBehaviour.GetCooldownNormalized`, so an empty hotbar slot shows an empty overlay, not a stuck full one. |
| `ReadyNormalized` | `0..1` | `1 - CooldownNormalized`: fill amount for bars that "charge up" toward ready. |
| `SecondsRemaining` | `>= 0` | Seconds until ready; `0` when ready or unknown. |
| `IsReady` | `bool` | Granted and castable right now. |

## Example — cooldown overlay on an ability button

1. On the ability button: `AbilityCooldownSource` (**Caster** = player, **Ability Id** = `fireball`).
2. Add `SetProgress` targeting the button's radial overlay Image; source = this component, member = `CooldownNormalized`, update mode **Poll**.
3. Optional: `NoCodeBindText` on a small label, member = `SecondsRemaining`.

The overlay fills on cast and drains to empty as the ability comes back.

## See also

- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — the caster being read
- [AbilityNoCodeAction](./AbilityNoCodeAction.md) — cast from the same button
- NoCode bindings: `Assets/Neoxider/Docs/NoCode/README.md`
- Back: [Abilities module](./README.md)

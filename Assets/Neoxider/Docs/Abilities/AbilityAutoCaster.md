# AbilityAutoCaster

**What it is:** the Survivor-demo auto-cast pattern as a reusable component — fires abilities the moment they come off cooldown (or on a fixed interval), with automatic nearest-target lock-on through the scene hub. Vampire-Survivors-style games become pure inspector wiring.

**How to use:** add it next to (or point it at) an `AbilityCasterBehaviour`, optionally list the ability ids to manage, press Play. Disable the component to stop casting.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilityAutoCaster.cs`
- Component menu: `Neoxider/Abilities/Ability Auto Caster`

## Targeting per ability

Read from each ability's `AbilityDefinition` **Targeting**:

| Targeting | Auto-cast behavior |
|-----------|-------------------|
| `NoTarget` / `Self` | Plain cast, no search. |
| `Unit` | Nearest alive unit matching the ability's **Team Filter** within range; none in range → waits silently. |
| `Point` | Cast at the nearest matching unit's position; none → waits. |
| `Direction` | Cast towards the nearest matching unit; none → waits. |

Search radius is the ability's **Range**; when Range is `0` (unlimited) the serialized **Target Search Range** is used. Equidistant candidates resolve deterministically by unit id. Note: for `Allies`/`Any` filtered `Unit` abilities the nearest valid unit can be the caster itself.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Caster** | `AbilityCasterBehaviour` | Empty = searched on this GameObject and its parents. |
| **Ability Ids** | `List<string>` | Attempted in list order. Empty = every granted ability, sorted by id. Ungranted ids are skipped silently (an upgrade may grant them later). |
| **Mode** | `WhenReady` / `Interval` | `WhenReady` casts each ability the moment its cooldown ends; `Interval` runs one cast pass every **Interval** seconds. |
| **Interval** | `float` | Seconds between passes in `Interval` mode. |
| **Failed Retry Delay** | `float` | Seconds before a failed ability is retried (prevents per-frame failure spam while e.g. out of mana). `0` = retry immediately. |
| **Target Search Range** | `float` | Search radius for abilities whose Range is `0`. |

### Unity Events

| Event | Fires when | Passes |
|-------|-----------|--------|
| **On Cast** `UnityEvent<string>` | An auto-cast succeeded. | Ability id. |
| **On Cast Failed** `UnityEvent<string>` | An attempted cast was rejected. | Ability id (the `CastFailureReason` fires on the caster's **On Cast Failed**). |

## Key API

| Method | Description |
|--------|-------------|
| `CastReadyAbilities()` | Runs one cast pass immediately (also an inspector button). `Update` calls it on the configured schedule. |

Passes are skipped while the unit is dead or the hub is `Paused`.

## Example — auto-fire survivor player

1. Player GameObject: `AbilityUnitBehaviour` (template with a `health` pool) + `AbilityCasterBehaviour` (drag in `zap`, `nova`) + `AbilityAutoCaster` (defaults).
2. Enemies: `AbilityUnitBehaviour` with an enemy-team template.
3. Press Play — `zap` (Unit-targeted) locks onto the nearest enemy, `nova` (NoTarget) fires around the player, both the moment their cooldowns end. Zero code.

## See also

- [AbilityNoCodeAction](./AbilityNoCodeAction.md) — one-shot ability actions from UnityEvents
- [AbilityCooldownSource](./AbilityCooldownSource.md) — show these cooldowns in UI
- [Survivor Demo](./SurvivorDemo.md) — the pattern this component generalizes
- Back: [Abilities module](./README.md)

# AbilityCasterBehaviour

**What it is:** the cast API of a unit for gameplay code, UI buttons, and NPC brains. It grants extra abilities on enable and exposes `TryCast*` methods plus success/failure `UnityEvent`s. Input binding stays in your game code — you call the `TryCast` methods from your controls.

**How to use:** add it next to an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) (required component). List any abilities to grant beyond the unit template, then call a `TryCast*` method with an ability id.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilityCasterBehaviour.cs`
- Component menu: `Neoxider/Abilities/Ability Caster`
- `[RequireComponent(typeof(AbilityUnitBehaviour))]`

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Abilities** | `List<AbilityDefinition>` | Abilities registered and granted on enable, in addition to the unit template's list. |

### Unity Events

| Event | Fires when | Passes |
|-------|-----------|--------|
| **On Cast Success** `UnityEvent<string>` | A cast is accepted. | Ability id. |
| **On Cast Failed** `UnityEvent<string>` | A cast is rejected. | `CastFailureReason` name (e.g. `OnCooldown`, `NotEnoughResources`). |

## Key API

| Method / property | Returns | Description |
|-------------------|---------|-------------|
| `TryCast(string abilityId)` | `bool` | No-target / self cast. |
| `TryCastAtUnit(string abilityId, AbilityUnitBehaviour target)` | `bool` | Unit-targeted cast. Fails with `InvalidTarget` if `target` is null. |
| `TryCastAtPoint(string abilityId, Vector3 point)` | `bool` | Point-targeted (ground) cast. |
| `TryCastTowards(string abilityId, Vector3 direction)` | `bool` | Direction / skillshot cast; a projectile flies to the end of its reach. With a positive **Range** only the direction counts: the cast reaches `Range + cast_range_bonus`. With Range `0` (unlimited) the vector's length is the reach, at least 10: `transform.forward` flies 10 units, `mouseWorld - transform.position` arrives at the cursor. |
| `Cast(CastRequest request)` | `CastResult` | Typed path: casts a fully specified request (custom seed, explicit target id) through this component, fires the same UnityEvents and returns the precise `CastFailureReason` and `CastId`. The request's `Caster` is replaced by this component's unit. |
| `CastResult LastCastResult` | — | Outcome of the most recent `TryCast*` / `Cast` call on this caster (default before the first call). |
| `GetCooldownNormalized(string abilityId)` | `float` | Cooldown for UI: `1` = just used, `0` = ready. |
| `CastFirstAbility()` | `void` | Inspector button: casts `Abilities[0]`. |
| `AbilityUnitBehaviour UnitBehaviour` | — | The owning unit component. |
| `IReadOnlyList<AbilityDefinition> Abilities` | — | The granted-on-enable list. |

Each `TryCast*` returns `true` on success and fires the matching UnityEvent. They forward to `AbilitySystem.Cast`, which enforces grants, cooldown/charges, states (`stunned`, `silenced`), costs, target validity, team filter, and range. Failure names are cached, so retrying a blocked cast every frame (AI, auto-fire) does not allocate a new string per attempt.

## Example

**Inspector:** add the component (the required `AbilityUnitBehaviour` is added automatically), drag `fireball` and `frost_nova` into **Abilities**, and wire a cooldown UI to poll `GetCooldownNormalized`.

**Code (bind to input):**

```csharp
[SerializeField] private AbilityCasterBehaviour caster;

void Update()
{
    if (Input.GetKeyDown(KeyCode.Q))
        caster.TryCast("frost_nova"); // no-target nuke around the caster

    if (Input.GetMouseButtonDown(0))
    {
        Vector3 point = GetMouseGroundPoint();
        caster.TryCastAtPoint("fireball", point);
    }
}

// UI cooldown fill:
cooldownImage.fillAmount = caster.GetCooldownNormalized("fireball");

// Typed result (branch on the reason instead of parsing the event string):
CastResult result = caster.Cast(CastRequest.AtUnit(UnitId.None, "fireball", target.UnitId, seed: 42u));
if (!result.Success && result.Failure == CastFailureReason.NotEnoughResources)
    ShowOutOfMana();
```

## Pitfalls

- **The ability id must match the targeting mode.** `TryCastAtPoint` on a `Unit`-targeted ability, or `TryCast` on one that needs a point, will not resolve targets sensibly — pick the `TryCast*` that matches the [AbilityDefinition](./AbilityDefinition.md)'s **Targeting**.
- **Abilities are granted on `OnEnable`.** Adding to the list at runtime does not grant until re-enable; grant directly with `AbilitySystem.GrantAbility` instead.
- **UnityEvent failure reasons are strings** (the `CastFailureReason` name). In code, branch on `Cast(...)`'s returned `CastResult.Failure` or on `LastCastResult` instead of comparing strings.
- Range enforcement needs world positions — see [AbilitySystemBehaviour](./AbilitySystemBehaviour.md).

## See also

- [AbilityDefinition](./AbilityDefinition.md) — the abilities being cast
- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — the required unit component
- Back: [Abilities module](./README.md)

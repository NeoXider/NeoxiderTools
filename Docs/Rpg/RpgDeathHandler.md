# RpgDeathHandler

**What it is:** a NoCode death reaction for an [RpgCharacter](RpgCharacter.md) (`MonoBehaviour`,
`Scripts/Rpg/Components/RpgDeathHandler.cs`, namespace `Neo.Rpg`). When the character's `OnDeathEvent` fires, it
deactivates or destroys the GameObject and raises its own begin/complete events.

**How to use:**
1. Add the component (menu `Neoxider/RPG/RpgDeathHandler`) to the object that has the `RpgCharacter`; the
   component requires one.
2. Choose **Action**: `Deactivate` for pooled or respawned objects, `Destroy`, or `DestroyDelayed` to let a death
   animation play for **Destroy Delay** seconds.
3. Wire **On Death Begin** (effects, sounds, score) and **On Death Complete** in the Inspector, or subscribe to
   `OnDeathBegin` / `OnDeathComplete` from code.

---

## Fields

| Header | Field | Default | Description |
|--------|-------|---------|-------------|
| Death Settings | Action | `Deactivate` | `RpgDeathHandler.DeathAction`: `Deactivate`, `Destroy`, `DestroyDelayed`. |
| Death Settings | Destroy Delay | `2` | Seconds before destruction; used only by `DestroyDelayed`. |
| Debug | Debug Log | off | Logs each death through `NeoDiagnostics`. |

## Events

| Event | When |
|-------|------|
| `UnityEvent OnDeathBegin` | First, before the action runs. |
| `UnityEvent OnDeathComplete` | After `SetActive(false)` for `Deactivate`; before `Destroy` is issued for `Destroy` and `DestroyDelayed`. |

The handler subscribes in `Awake` and unsubscribes in `OnDestroy`, so a deactivated object that is reactivated
keeps reacting to later deaths.

## See also

- [RpgCharacter](RpgCharacter.md) — death, revive and resources.
- [RPG module](README.md)

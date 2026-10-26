# ProgressionManager

**What it is:** a `Singleton<ProgressionManager>` scene adapter from `Scripts/Progression/Components/ProgressionManager.cs` that owns perk points, unlock nodes, perks, premium entitlement, and persistent profile storage through `SaveProvider`. Its Core `LevelComponent` owns XP and level calculations and persistence.

**How to use:**
1. Add `ProgressionManager` and a `LevelComponent` to a scene object (such as a Player or Weapon). When no `LevelProvider` is assigned the manager auto-binds to a `LevelComponent` on the same GameObject.
2. Assign the Core XP curve to `LevelComponent`. Assign the optional Progression reward track (`Neo.Progression.LevelCurveDefinition`), unlock tree, and perk tree to the manager.
3. Set `Save Key` if the project needs a separate profile namespace (e.g. `Weapon_Sword_Progression`).
4. Call `AddXp`, `TryUnlockNode`, `TryBuyPerk`, `ResetProgression`, `LoadProfile`, or `SaveProfile`.
5. Bind UI to the reactive state fields or the exposed UnityEvents.
6. Support for `Premium` (BattlePass-like) progressions is available via `ActivatePremium()`.

**Navigation:** [← Progression](./README.md)

---

## Main fields

| Field | Purpose |
|------|---------|
| `_levelProvider` | Core `LevelComponent` supplying XP and resolved levels |
| `_levelCurve` | Reward track mapping resolved levels to rewards and perk point grants |
| `_unlockTree` | Unlock node graph |
| `_perkTree` | Perk graph |
| `_saveKey` | Persistent profile key |
| `_conditionContext` | Context object for `ConditionEntry` evaluation |

## Reactive state

| Field | Purpose |
|------|---------|
| `XpState` | Current XP |
| `LevelState` | Current resolved level |
| `PerkPointsState` | Current unspent perk points |
| `XpToNextLevelState` | Remaining XP to the next defined level |

These mirror the assigned Core provider through disposable subscriptions to its XP, level, and remaining-XP reactive properties. Direct `LevelComponent.AddXp`, `SetLevel`, `Load`, and `ResetProgress` changes update the manager's mirrors as well as its live getters. Repeated Core notifications of the same snapshot do not produce another mirror notification. Replacing/clearing the provider or destroying the manager releases its subscriptions; callbacks captured from an old binding cannot affect the new one.

Mirror-only updates never save the Progression profile, grant rewards, or change the rewarded-level watermark. Normal level rewards still use `OnLevelUp`. A direct Core level-up can publish the provider change and the subsequent reward change as separate snapshots.

In 11.0, manager-owned XP/level commands, premium activation, and reset stage runtime notifications until nested provider/reward/save callbacks finish. Their state notifications publish a settled batch instead of duplicate intermediate batches; requested mirror refreshes update cached values silently while notifications are staged. Reentrant changes stop the older batch before it emits trailing stale payloads. Reactive fields and UnityEvents continue publishing a complete snapshot, including values that did not change. Consumers should use the current values rather than rely on duplicate callback counts.

## C# contracts

| API | Behavior |
|-----|----------|
| `EnsureInitialized()` | Initializes definitions, provider subscriptions, and optional profile loading. Failed setup/read can be retried after correcting the dependency. |
| `TryAddXp(int, out string)` | Accepts positive XP when a provider is assigned; otherwise returns false with a reason and does not save an unchanged profile. |
| `AddXp(int)` | Void wrapper around `TryAddXp`; a missing-provider warning is gated by `NeoDiagnostics` and emitted once per manager. |
| `TrySetLevel(int, out string)` / `SetLevel(int)` | Changes the Core level without resetting perks/unlocks. |
| `AddPerkPoints(int)` | Ignores nonpositive grants; direct and level grants saturate at `int.MaxValue`. |
| `CanUnlockNode` / `TryUnlockNode` | Checks level, prerequisite nodes, and conditions; successful unlocks dispatch rewards once for that identifier. |
| `CanBuyPerk` / `TryBuyPerk` | Also checks point cost, prerequisite perks, and required unlock nodes. |
| `GetProfileSnapshot()` | Returns an independent copy of the Progression profile; XP/level remain in the Core profile. |
| `SetDefinitions(...)` | Assigns assets. Configure before initialization, or call `LoadProfile()` to reapply defaults after changing trees. |
| `LevelProvider` | Rebinds level listeners and refreshes state when replaced, including null. Incoming levels are treated as already rewarded. Reassigning the same instance preserves the reward watermark. |
| `OnProfileLoaded` | Read-only `UnityEvent` accessor for the existing Inspector profile-loaded event; C# can add/remove listeners on the same notification. |

Subscribe with `manager.OnProfileLoaded.AddListener(HandleProfileLoaded)` and remove the same handler with `RemoveListener`. Inspector **On Profile Loaded** bindings and C# listeners share the existing serialized UnityEvent. Profile loading applies the profile and refreshes runtime state before invoking it. Initial loading uses that same path when `Load On Awake` is enabled; deliberately fresh initialization with loading disabled does not invoke it. The accessor adds no separate event or dispatch path.

Initialization completes when the profile, default nodes/perks, active key, and incoming reward baseline have been applied, before notifying external observers. A dependency, binding, or profile-read exception before that point propagates and releases only that attempt's subscriptions. Correct the dependency and call `EnsureInitialized()` again; a subsequent command also ensures initialization before changing or saving the profile. The retry loads saved points before applying a new grant. A newer provider binding created by a reentrant callback is retained; retry binds the currently selected provider once.

While setup is unfinished, reentrant `EnsureInitialized()` and profile commands that call it throw `InvalidOperationException` instead of recursing or saving an unapplied default profile. Setup/read callbacks may configure `LevelProvider`, but must defer profile commands until initialization commits. Provider notifications do not publish profile state or grant rewards before that commit.

An exception from a runtime-state or `OnProfileLoaded` observer after the profile commits remains observable, but initialization stays complete. Calling `EnsureInitialized()` again does not reread the profile, replay rewards, reset the committed watermark, or detach a newer provider binding. This boundary does not roll back external observer side effects.

`LoadProfile()` restores only the Progression profile. Reloading the same active key preserves the maximum of this session's rewarded level and the incoming Core level, so downgrade/reload/recovery cannot grant those level rewards again. A different loaded key establishes a new baseline at the current Core level. The active identity is the last loaded/reset key; assigning `SaveKey` or saving to another key does not itself load a different profile.

`SaveProfile()` flushes only the selected Progression profile through the active save provider. `ResetProgression()` also resets and saves the configured Core level provider, then clears the Progression profile, resets its reward baseline, and reapplies default nodes/perks. A later fresh level-up can award rewards again. To restore/switch accounts or seasons, configure both keys, load the Core component first, then call `LoadProfile()` to establish the Progression baseline. The manager remains a singleton even when its save key changes.

Level rewards are claimed for increasing levels after the provider's initial level. Lowering and restoring a level does not replay rewards during that initialized session. Premium activation replays only premium rewards for levels achieved when activation begins; new levels reached by those XP rewards go through normal level reward dispatch. Repeated activation does nothing. The profile does not contain a durable per-reward claim ledger, so external reward delivery and profile saves are not an atomic transaction.

## Typical use cases

- Persistent player progression outside scene-local saves.
- Runtime UI for XP bars, level labels, perk counters, and unlock trees.
- No-code gameplay flows driven by progression state.

## Code example

```csharp
using Neo.Progression;
using UnityEngine;

public class ProgressionRewardExample : MonoBehaviour
{
    [SerializeField] private ProgressionManager targetProgression;
    [SerializeField] private int xpReward = 50;
    [SerializeField] private string unlockNodeId = "weapon-tier-2";

    public void GrantXp()
    {
        if (targetProgression != null)
        {
            targetProgression.AddXp(xpReward);
        }
    }

    public void UnlockTier()
    {
        if (targetProgression != null)
        {
            targetProgression.TryUnlockNode(unlockNodeId, out _);
        }
    }

    public void PurchaseBattlepass()
    {
        if (targetProgression != null)
        {
            // Activate Premium track retroactively granting rewards for past levels
            targetProgression.ActivatePremium();
        }
    }
}
```

## Common setup patterns

### Minimal metagame

- `LevelCurveDefinition` only
- no unlock tree
- no perk tree
- level rewards focused on currency or cosmetics

### Classic RPG

- `LevelCurveDefinition` + `UnlockTreeDefinition` + `PerkTreeDefinition`
- `GrantedPerkPoints` awarded by level ups
- perks purchased with perk points
- unlock nodes open new branches or systems

### Battle Pass

- `LevelCurveDefinition` with multiple `ProgressionReward` entries
- Some rewards marked with `Is Premium = true`
- Use `ProgressionManager.ActivatePremium()` when purchased by user
- Independent profiles per season by switching `Save Key`

### Narrative game

- light XP curve
- unlock tree used as the main content gate
- `ConditionContext` points to the object that owns story flags

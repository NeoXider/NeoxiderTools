# StateAction

**What it is:** abstract `[Serializable]` base class for NoCode actions listed on `StateData` assets (`On Enter Actions`, `On Update Actions`, `On Exit Actions`). Path: `Scripts/StateMachine/NoCode/StateAction.cs`, namespace `Neo.StateMachine.NoCode`.

**How to use:**
1. In a `StateData` asset, add built-in actions to the enter/update/exit lists.
2. For custom behaviour, derive from `StateAction` and override `Execute()`, or `Execute(GameObject contextObject)` to receive the StateMachineBehaviour owner.
3. Keep scene objects out of the asset: use context slots (`SetContextGameObjectActiveAction`) and assign the objects in the behaviour's **Context Overrides**.

---

## Built-in actions

| Action | Description |
|--------|-------------|
| `LogStateAction` | Logs a message (Log / Warning / Error) through the State Machine debug log, so it prints only while the machine running it has its debug log on (the owning behaviour's **Enable Debug Log**); a shared state asset stays silent in a quiet machine. With logging off it does not build the message (safe in On Update lists). |
| `SetGameObjectActiveAction` | Enables/disables a directly referenced GameObject. Legacy: a ScriptableObject can only keep prefab/asset references, not scene objects. |
| `SetContextGameObjectActiveAction` | Enables/disables the Owner or an Override1..5 object resolved from the StateMachineBehaviour context. Safe for ScriptableObject data. |
| `InvokeUnityEventAction` | Invokes a `UnityEvent`. |
| `ChangeSceneAction` | Loads a scene by name or build index. |

## Custom action

```csharp
using System;
using Neo.StateMachine.NoCode;
using UnityEngine;

[Serializable]
public sealed class PlayAnimatorTriggerAction : StateAction
{
    [SerializeField] private string _trigger = "Enter";

    public override void Execute()
    {
    }

    public override void Execute(GameObject contextObject)
    {
        if (contextObject != null && contextObject.TryGetComponent(out Animator animator))
        {
            animator.SetTrigger(_trigger);
        }
    }
}
```

Actions run in list order; an exception in one action is logged (gated) and the next action still runs. Actions may edit the list they belong to without aborting the pass:

- a pass runs only the entries present when it started, so an appended action first runs on the next pass (an action that keeps appending cannot loop forever);
- removing the running action or an earlier entry shifts the later entries down, so the action right after it is skipped for that pass.

Action instances live on the shared asset, so do not keep per-machine runtime state in their fields.

## See also

- [StateMachineBehaviour](StateMachineBehaviour.md)
- [StateData](NoCode/StateData.md)
- ← [StateMachine](README.md)

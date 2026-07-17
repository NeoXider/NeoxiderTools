# StateAction

**Purpose:** Abstract base class for No-Code actions listed on `StateData` assets (`On Enter Actions`, `On Update Actions`, `On Exit Actions`). Derive from it and override `Execute()` (or `Execute(GameObject contextObject)` for context-aware actions).

## Built-in actions

| Action | Description |
|--------|-------------|
| `LogStateAction` | Logs a message (Log / Warning / Error). |
| `SetGameObjectActiveAction` | Enables/disables a directly referenced GameObject. Legacy — for old assets with direct scene references. |
| `SetContextGameObjectActiveAction` | Enables/disables a scene GameObject resolved from a `StateMachineBehaviour` context slot. Safe for ScriptableObject data. |
| `InvokeUnityEventAction` | Invokes a `UnityEvent`. |
| `ChangeSceneAction` | Loads a scene by name or build index. |

## See Also
- [StateMachineBehaviour](StateMachineBehaviour.md)
- [StateData](NoCode/StateData.md)
- <- [StateMachine](README.md)

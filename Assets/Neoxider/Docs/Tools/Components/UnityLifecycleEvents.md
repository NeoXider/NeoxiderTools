# UnityLifecycleEvents

**What it is:** a NoCode `MonoBehaviour` that forwards Unity lifecycle callbacks (Awake, OnEnable, Start, OnDisable,
OnDestroy, application focus/pause) and, optionally, per-frame Update / FixedUpdate / LateUpdate to UnityEvents.
Script: `Scripts/Tools/Components/UnityLifecycleEvents.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Components`.

**How to use:**
1. Add **Neoxider / Tools / UnityLifecycleEvents** to a GameObject.
2. Wire the lifecycle UnityEvents in the Inspector (e.g. **On Enable** → play a sound).
3. Enable **Emit Update** / **Emit Fixed Update** / **Emit Late Update** only when a per-frame event is really
   needed; each receives the frame's delta time.

---

## Fields

| Field | Event argument | Fired |
|-------|----------------|-------|
| `_onAwake` | — | In `Awake`. |
| `_onEnable` | — | In every `OnEnable`. |
| `_onStart` | — | In `Start` (once). |
| `_onDisable` | — | In every `OnDisable`. |
| `_onDestroy` | — | In `OnDestroy`. |
| `_emitUpdate` + `_onUpdate` | `float` (`Time.deltaTime`) | Every `Update` while enabled. |
| `_emitFixedUpdate` + `_onFixedUpdate` | `float` (`Time.fixedDeltaTime`) | Every `FixedUpdate` while enabled. |
| `_emitLateUpdate` + `_onLateUpdate` | `float` (`Time.deltaTime`) | Every `LateUpdate` while enabled. |
| `_onApplicationPause` | `bool` paused | `OnApplicationPause`. |
| `_onApplicationFocus` | `bool` focused | `OnApplicationFocus`. |

## C# access

Every event is exposed as a read-only property for code subscriptions: `OnAwake`, `OnEnableEvent`, `OnStart`,
`OnDisableEvent`, `OnDestroyEvent`, `OnUpdateEvent`, `OnFixedUpdateEvent`, `OnLateUpdateEvent`,
`OnApplicationPauseEvent`, `OnApplicationFocusEvent`.

```csharp
UnityLifecycleEvents lifecycle = GetComponent<UnityLifecycleEvents>();
lifecycle.OnEnableEvent.AddListener(HandleShown);
```

In C# gameplay code prefer the component's own Unity messages; this component exists so scene authors can attach
behavior to lifecycle moments without a script. Per-frame UnityEvents cost a managed invocation each frame, so keep
the emit flags off unless needed.

## See also

- [Components](README.md)

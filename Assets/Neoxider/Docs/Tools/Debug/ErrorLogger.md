# ErrorLogger

Displays selected Unity log messages in a `TextMeshProUGUI` label. Each entry contains a colored severity and message, the stack trace supplied by Unity, and an optional highlighted script location. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Debug`.

## Setup

1. Add `ErrorLogger` using **Add Component → Neoxider → Tools → ErrorLogger**.
2. Assign **Text Mesh** to a `TextMeshProUGUI` label on a canvas.
3. Choose **Log Type Filter** flags; the default captures errors and exceptions.
4. Enable **Show Script Path** to highlight the first recognized C# source location. Stack traces must be available in Unity's logging settings. Asset/package paths and absolute paths can contain spaces and parentheses.

The component subscribes to `Application.logMessageReceived` while enabled and unsubscribes when disabled. It makes the assigned label ignore raycasts on enable. Missing or destroyed labels are tolerated.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `logTypeFilter` | Flags from `LogTypeFilter`: `None`, `Error`, `Exception`, `Warning`, `Log`, `Assert`. |
| `addText` | Append entries when true; replace the label with the latest accepted entry when false. |
| `checkExistingErrors` | Suppress an identical formatted entry already captured by this component. Includes message, stack and formatting. |
| `showScriptPath` | Add a cyan source location above the complete stack trace. |
| `textMesh` | Output label; assign before enabling. |
| `errorText` | Legacy serialized field retained for compatibility; it is not a mirror of the label. |

Captured-entry history survives disabling/re-enabling and replacing the displayed text. With duplicate checking enabled, an earlier entry remains suppressed even after another entry replaces it. Disable duplicate checking when every occurrence must be shown. History and appended text have no capacity limit; use this component for controlled diagnostic sessions.

## Public C# API and UnityEvents

Given an assigned `ErrorLogger logger`:

```csharp
logger.logTypeFilter = LogTypeFilter.Warning | LogTypeFilter.Error;
logger.UpdateText("Diagnostics ready");
logger.AppendText("\nWaiting for messages");
```

`UpdateText(string)` replaces the label and `AppendText(string)` appends directly. Both can be called from UnityEvents and safely ignore a missing label. They do not emit Unity logs, apply the severity filter, or clear captured-entry history. Automatic capture applies the filter independently.

For a scene/NoCode workflow, select the assigned `ErrorLogger` in a string UnityEvent and choose either text method. These direct calls still work while the component is disabled; disabling stops the automatic Unity log subscription. There is currently no public method to submit a typed log entry or clear captured-entry history.

## See Also

- [Tools / Debug](./README.md)

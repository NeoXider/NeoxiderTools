# ErrorLogger

**What it is:** a `MonoBehaviour` that shows selected Unity log messages in a `TextMeshProUGUI` label: a colored
severity, the message, an optional highlighted script location and the stack trace. History is bounded by
**Max Entries** and optionally by **Max Characters**. Script: `Scripts/Tools/Debug/ErrorLogger.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Debug`.

**How to use:**
1. Add **Add Component → Neoxider → Tools → ErrorLogger** (or the prefab from the Create menu).
2. Assign **Text Mesh** to a `TextMeshProUGUI` label on a canvas.
3. Choose **Log Type Filter** flags; the default captures errors and exceptions.
4. Keep **Max Entries** bounded (default 50) and enable **Show Script Path** if you want the source location.
5. Optionally set **Max Characters** (for example 16000) when long stack traces could make the label huge.

---

The component subscribes to `Application.logMessageReceived` while enabled and unsubscribes when disabled. It makes
the assigned label ignore raycasts on enable. Missing or destroyed labels are tolerated. Stack traces must be enabled
in Unity's logging settings for the script path; asset/package paths and absolute paths can contain spaces and
parentheses.

## Fields

| Field | Description |
|-------|-------------|
| `logTypeFilter` | Flags from `LogTypeFilter`: `None`, `Error`, `Exception`, `Warning`, `Log`, `Assert`. |
| `addText` | Append entries when true; replace the label with the latest accepted entry when false. |
| `checkExistingErrors` | Suppress an identical formatted entry still kept in history. Includes message, stack and formatting. |
| `showScriptPath` | Add a cyan source location above the complete stack trace. |
| `_maxEntries` (`MaxEntries`) | Entries kept in history and on screen (default 50); the oldest are dropped first. `0` = unlimited. Lowering it in the Inspector during Play trims at once. |
| `_maxCharacters` (`MaxCharacters`) | Character budget of history and label, color tags and stack traces included (default `0` = unlimited). The oldest entries are dropped until the rest fits; the newest entry always stays. Lowering it in the Inspector during Play trims at once. |
| `textMesh` | Output label; assign before enabling. |
| `errorText` | Legacy serialized field retained for compatibility; it is not a mirror of the label. |

## History

Captured-entry history survives disabling/re-enabling and replacing the displayed text. When a new entry exceeds
**Max Entries**, the oldest entry is dropped from history and from the duplicate filter, and in append mode the label
is rebuilt from the remaining entries (text added through `AppendText` is not kept by that rebuild). A dropped entry
can therefore be shown again later. With `MaxEntries = 0` both memory and label size grow with every new message; use
that only for short, controlled diagnostic sessions.

**Max Characters** adds a size budget on top of the entry count: a few errors with long stack traces can otherwise
build a label of tens of thousands of characters, which is slow for TextMeshPro to rebuild and can exceed its mesh
vertex limit. Both limits apply together; `HistoryCharacterCount` reports the current total.

## Public C# API and UnityEvents

Given an assigned `ErrorLogger logger`:

```csharp
logger.logTypeFilter = LogTypeFilter.Warning | LogTypeFilter.Error;
logger.MaxEntries = 20; // lowering it trims the history immediately
logger.UpdateText("Diagnostics ready");
logger.AppendText("\nWaiting for messages");
logger.MaxCharacters = 16000; // also bound the label size
int shown = logger.Entries.Count;
logger.Clear(); // forget history and the duplicate filter, empty the label
```

| Member | Description |
|--------|-------------|
| `void UpdateText(string)` | Replaces the label. |
| `void AppendText(string)` | Appends to the label. |
| `void Clear()` | Clears captured history, the duplicate filter and the label. |
| `IReadOnlyList<string> Entries` | Captured entries, oldest first, formatted as displayed. |
| `int MaxEntries` | History limit (0 = unlimited); lowering it drops the oldest entries and rebuilds the label in append mode. |
| `int MaxCharacters` | Character budget (0 = unlimited); the newest entry always stays. Lowering it trims like `MaxEntries`. |
| `int HistoryCharacterCount` | Total characters of `Entries`. |

`UpdateText` and `AppendText` do not emit Unity logs, apply the severity filter, or change captured history.
Automatic capture applies the filter independently.

For a scene/NoCode workflow, select the assigned `ErrorLogger` in a UnityEvent and choose `UpdateText`, `AppendText`
or `Clear` (for example on a "clear log" button). These direct calls still work while the component is disabled;
disabling stops the automatic Unity log subscription.

## See Also

- [Tools / Debug](./README.md)

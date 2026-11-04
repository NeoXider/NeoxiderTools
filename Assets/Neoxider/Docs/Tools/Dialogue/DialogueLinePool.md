# DialogueLinePool

**What it is:** Pooled ("bark") lines for situations a linear dialogue tree does not model: pick a random line by tag, never repeat the previous line of a tag back to back, restrict lines to time windows or to the first pick, and let a contextual line interrupt idle chatter. `DialogueLinePool` is the plain C# core and `DialogueLinePlayer` the optional scene wrapper, both in `Scripts/Tools/Dialogue/` (namespace `Neo.Tools`, assembly `Neo.Tools.Dialogue`). Neither needs `DialogueController`.

**How to use (scene):**
1. Add **Neoxider > Tools > Dialogue > DialogueLinePlayer**.
2. Fill **Lines** (tag, speaker, text, weight, time window, slot) and optionally assign **Line Text** / **Speaker Text** TMP labels.
3. Set **Idle Interval** for automatic idle lines and **Line Duration** for how long a line stays.
4. Wire gameplay UnityEvents to `RequestContext("hit")` (interrupts idle lines) or `RequestIdle()`; listen to `OnLineStarted` / `OnLineFinished`.

**How to use (code):** create `new DialogueLinePool(lines)`, subscribe to `LineStarted`, then call `TryPlay(tag, contextTime, priority, out line)` and `Finish()`.

---

## PooledDialogueLine fields

| Field | Description |
|-------|-------------|
| `Tag` | Group the line belongs to (`idle`, `hit`, `win`, ...). Ordinal, case-sensitive. |
| `Speaker`, `Text` | Display data. |
| `Weight` | Relative pick weight inside the tag; `0` disables the line. |
| `MinTime`, `MaxTime` | Context-time window in seconds; `MaxTime <= 0` means no upper limit. |
| `Slot` | `Any`, `FirstOnly` (only the first pick of the tag since the last history reset) or `AfterFirst` (only later picks). Combine with windows for "first line from one window, the rest from another". |

## DialogueLinePool API

| Member | Description |
|--------|-------------|
| `DialogueLinePool(IEnumerable<PooledDialogueLine> lines = null)` | Creates a pool; null lines are skipped. |
| `SetLines(...)`, `Add(line)`, `Lines` | Replace/extend/read the content. History and the playing line are kept. |
| `bool TryPick(string tag, float contextTime, out PooledDialogueLine line)` | Weighted random line of the tag whose window contains `contextTime` (`float.NaN` ignores windows). Records it as the tag's last line and increments the tag's pick count. Does not touch `Current`. |
| `bool TryPick(string tag, out PooledDialogueLine line)` | Same, ignoring windows. |
| `bool TryPlay(string tag, float contextTime, int priority, out PooledDialogueLine line)` | Picks and starts a line unless a line of equal or higher priority is playing; a strictly higher priority interrupts the playing line. Returns `false` (and keeps the playing line) when nothing is eligible. |
| `void Finish()` | Ends the playing line. |
| `Current`, `CurrentPriority`, `IsPlaying` | Playing state. |
| `AvoidBackToBackRepeat` | Default `true`: the previous line of a tag is skipped while another candidate exists; a single candidate may repeat. |
| `RandomProvider` | `Func<float>` in [0..1); `null` uses `UnityEngine.Random.value`. |
| `ResetHistory()`, `GetPickCount(tag)` | Restart first-pick slots and back-to-back tracking (for example per round). |
| `IdlePriority` (0), `ContextPriority` (10) | Suggested priorities. |
| `event LineStarted`, `LineInterrupted`, `LineFinished` | C# events with the affected line. |

## DialogueLinePlayer

| Member | Description |
|--------|-------------|
| Inspector: **Lines**, **Avoid Back To Back Repeat** | Authoring list copied into the pool. Call `RebuildPool()` after editing `Lines` at runtime. |
| Inspector: **Idle Tag**, **Idle Interval**, **Context Priority** | Idle scheduling (0 interval = off) and the priority used by `RequestContext`. |
| Inspector: **Line Duration**, **Use Unscaled Time** | Seconds before a line finishes (0 = until `FinishLine()`), and the time domain. C#: `LineDuration`, `UseUnscaledTime`. |
| Inspector: **Line Text**, **Speaker Text**, **Clear Text On Finish** | Optional TMP output. C#: `LineText`, `SpeakerText`, `ClearTextOnFinish`. |
| `IdleTag`, `IdleInterval`, `Lines` | C# access to the idle settings and the authoring list. |
| `bool PlayIdle()`, `bool PlayContext(string tag)`, `bool Play(string tag, int priority)` | Typed C# requests. |
| `void RequestIdle()`, `void RequestContext(string tag)`, `void FinishLine()`, `void ResetSession()` | UnityEvent-friendly void methods. |
| `void Tick(float deltaTime)` | Advances context time, line duration and the idle scheduler. The enabled component's `Update` calls it with Unity's delta; call it directly from tests, replays or a loop that owns time. Non-positive and non-finite deltas are ignored. |
| `DialogueLinePool Pool`, `float ContextTime` | The core and the seconds since enable / `ResetSession()`, used as context time. |
| `OnLineStarted(string text)`, `OnSpeakerChanged(string speaker)`, `OnLineFinished` | UnityEvents. |

## Example

```csharp
using Neo.Tools;

PooledDialogueLine[] lines =
{
    new PooledDialogueLine("round", "Fight!") { Slot = PooledLineSlot.FirstOnly },
    new PooledDialogueLine("round", "Keep going") { Slot = PooledLineSlot.AfterFirst, MaxTime = 60f },
    new PooledDialogueLine("round", "Last minute!") { Slot = PooledLineSlot.AfterFirst, MinTime = 60f },
    new PooledDialogueLine("idle", "..."),
    new PooledDialogueLine("hit", "Ouch!"),
    new PooledDialogueLine("hit", "That hurt!")
};

DialogueLinePool pool = new DialogueLinePool(lines);
pool.LineStarted += line => subtitle.text = line.Text;

pool.TryPlay("idle", roundSeconds, DialogueLinePool.IdlePriority, out _);
pool.TryPlay("hit", roundSeconds, DialogueLinePool.ContextPriority, out _); // interrupts "..."
pool.Finish();
```

## See also

- [DialogueController](DialogueController.md) for linear conversations
- ← [Tools/Dialogue](README.md)

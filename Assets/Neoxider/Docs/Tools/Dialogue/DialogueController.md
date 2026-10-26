# DialogueController

`Neo.Tools.DialogueController` plays a linear `Dialogue[]` graph using UniTask and `TypewriterEffect`. It works without `DialogueUI`; assigned views receive names, portraits and text.

## Setup

- Add **Neoxider → Tools → Dialogue → DialogueController**.
- Populate `dialogues`, monologues and sentences inline, or open **Open Dialogue Editor** in the Inspector. Authoring the graph does not require Play Mode.
- Assign `_dialogueUI`, or place `DialogueUI` on the same GameObject for the `Awake` fallback.
- Configure the typewriter speed, punctuation and time mode. Enable `autoStart` or call `StartDialogue()` after initialization.
- Bind a UI button's `onClick` to `SkipOrNext()`. Bind scene reactions to the controller/data UnityEvents. The same methods and events are available from C#; no UnityEvent chain is required to control playback.

## Public API

| Method/property | Behavior |
|-------|-------------|
| `StartDialogue(d = 0, m = 0, s = 0)` | Cancel previous work and start at a selection. Dialogue index is clamped; negative monolog/sentence indices become zero. An index beyond a monolog/sentence array follows the next boundary. Missing top-level data leaves `DialogueStarted` false. |
| `SkipOrNext()` | Start if stopped, complete if typing, otherwise `Advance()`. |
| `Advance()` | Cancel current typing/delay and choose the next sentence, monolog or dialogue. Does nothing while stopped. |
| `NextSentence()`, `NextMonolog()`, `NextDialogue()` | Explicit cursor jumps; later-level indices reset as appropriate. Call `StartDialogue()` before using these navigation methods. |
| `CompleteTypewriter()` | Show the full line and complete the underlying effect (`Progress == 1`, `CurrentText == FullText`), notifying its completion/progress listeners. Schedule automatic progression once, if enabled. |
| `RestartDialogue()` | Restart the current dialogue if `allowRestart` is enabled. |
| `RestartAll()` | Start at `(0, 0, 0)`, independently of `allowRestart`. |
| `GetCurrentDialogue/Monolog/Sentence()` | Return the current data object, or null for an invalid selection. |
| `CurrentDialogueId`, `CurrentMonologId`, `CurrentSentenceId` | Read-only cursor properties. |
| `DialogueStarted`, `IsTyping`, `Typewriter` | Playback state and configurable effect. Use controller navigation/completion methods while the controller owns playback. |

Inspector playback actions are restricted to Play Mode. The editor window action remains available for authoring.

## Automatic progression and lifetime

`autoNextSentence` schedules `Advance()` after text presentation/typing completion using `autoNextSentenceDelay`. At a boundary, `autoNextMonolog` or `autoNextDialogue` adds its own delay; when that flag is false, an explicit `Advance()` crosses the boundary immediately. Boundary flags alone do not initiate sentence progression.

Automatic delays use scaled time. `Typewriter.UseUnscaledTime` controls only typing. Zero/negative automatic delays yield until the next frame, keeping long sequences cancellable and avoiding recursive playback. Use finite delay values from C#.

Starting, navigating, disabling and destroying cancel owned work. A cancelled typewriter continuation cannot schedule another automatic transition. Disable preserves the cursor and started flag; re-enable does not replay or resume the interrupted line. To replay it, capture the three indices and call `StartDialogue(d, m, s)`.

Null dialogue/monolog/sentence entries produce gated warnings and stop that presentation; author complete data or explicitly navigate past missing entries. Null/empty text is displayed as an empty line.

Missing or empty `monologues`/`sentences` arrays are different from null entries. Immediate traversal crosses these containers iteratively, so a long sparse graph can reach its next line synchronously without growing the call stack per container. Each visited container keeps its data and boundary notifications in the existing order; a non-empty graph containing only empty containers emits `OnAllDialoguesEnd` once. A scheduled monolog/dialogue delay exits the traversal, and the scheduled navigation resumes at its next selection. A callback that starts/navigates another dialogue or disables the controller stops the interrupted traversal. The same behavior applies to C# navigation and UI buttons calling these public methods; no new component or session API is required.

## Event timing

| Event | Timing |
|-------|--------|
| Data `OnChangeDialog`, `OnChangeMonolog`, `OnChangeSentence` | In hierarchy order while visiting a selection, before presentation. Dialogue/monolog notifications also occur when their child array is empty; they can repeat while traversing consecutive monologs/sentences in that container. |
| Controller `OnCharacterChange` | Every sentence presentation, after assigning the name and portrait. `DialogueUI.OnCharacterChange` separately suppresses unchanged names. |
| `OnSentenceEnd` | Historical presentation notification, after launching typing or setting the immediate text. It does not wait for typing to finish. |
| `OnCharacterTyped`, `OnTypewriterProgress` | Forwarded from the effect while typing; skip completion reports progress 1. |
| `Typewriter.OnComplete` | Natural typing completion or `CompleteTypewriter()`. Immediate display with `useTypewriterEffect = false` does not run the effect. |
| `OnMonologEnd`, `OnDialogueEnd` | When progression reaches those boundaries. Direct cursor jumps do not synthesize events for skipped content. |
| `OnAllDialoguesEnd` | After setting `DialogueStarted` false, including an explicit jump beyond the final dialogue. |

Presentation/boundary listeners may call controller navigation or disable the component. The interrupted call stops processing the previous selection. An `OnAllDialoguesEnd` listener may restart playback; its started state is preserved. When constructing components/data entirely in C#, initialize a UnityEvent before calling `AddListener` if the field is null.

## See Also

- [Module Root](./README.md)
- [Dialogue data](./DialogueData.md)
- [Dialogue UI](./DialogueUI.md)

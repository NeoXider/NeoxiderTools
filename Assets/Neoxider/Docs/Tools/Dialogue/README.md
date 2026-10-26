# Tools / Dialogue

Linear dialogue playback with an optional UI and the reusable `TypewriterEffect`. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Dialogue`. Scripts live in `Scripts/Tools/Dialogue/`.

## Start here

Add `DialogueController` and, optionally, `DialogueUI` to the same GameObject. Populate `dialogues` inline as `Dialogue[]` → `Monolog[]` → `Sentence[]`. These are serializable classes, not ScriptableObject assets. Assign TMP name/body labels and an optional portrait on `DialogueUI`. A UI on a different GameObject can be assigned in the controller's Inspector.

Use `autoStart` or call `StartDialogue()`. Connect a UI button to `SkipOrNext()` to complete the current line on the first press and advance on the next. Inspector playback buttons are available in Play Mode.

## C# workflow

```csharp
using Neo.Tools;

// controller is an assigned DialogueController reference.
controller.useTypewriterEffect = false;
controller.dialogues = new[]
{
    new Dialogue
    {
        monologues = new[]
        {
            new Monolog
            {
                characterName = "Guide",
                sentences = new[] { new Sentence { sentence = "Welcome." } }
            }
        }
    }
};
controller.StartDialogue();
controller.SkipOrNext();
```

No UI is required for cursor navigation and events. Custom asmdefs must reference `Neo.Tools.Dialogue`; reference `Neo.Tools.Components` when directly configuring the exposed `TypewriterEffect`.

## Playback contracts

- `SkipOrNext()` completes typing before advancing; `Advance()` directly moves to the next line/boundary and cancels current typing.
- `autoNextSentence` starts automatic progression. The monolog/dialogue auto flags apply their respective delays when a boundary is reached. A zero automatic delay yields until the next frame.
- Missing/empty child arrays are crossed iteratively when their boundary auto flag is off: `StartDialogue()` and manual navigation reach the next available line synchronously, with constant traversal stack depth. Configured boundary delays stop that traversal until the scheduled transition. Null data entries still stop presentation with a gated warning.
- Disabling cancels typing and pending delays while preserving the cursor and `DialogueStarted`. Re-enabling does not resume cancelled work; call `StartDialogue(currentDialogue, currentMonolog, currentSentence)` to replay that selection.
- Completion sets `DialogueStarted` to `false` before `OnAllDialoguesEnd`. Event listeners may start a replacement dialogue; the interrupted controller call then stops.
- `OnSentenceEnd` retains its historical timing: it fires when a sentence is presented/typing is launched. Use `Typewriter.OnComplete` for typing completion, including `CompleteTypewriter()`.

## Component guides

| Page | Description |
|------|-------------|
| [DialogueController](./DialogueController.md) | Main controller |
| [DialogueUI](./DialogueUI.md) | UI binding |
| [DialogueData](./DialogueData.md) | Inline serializable data and events |

## See also

- [Tools/Components](../Components/README.md)
- [UI](../../UI/README.md)

# Dialogue data types

**Purpose:** The plain `[Serializable]` data classes the Dialogue system works on. They live in `Scripts/Tools/Dialogue/DialogueData.cs` — there is no `DialogueData` type itself, and none of them are ScriptableObjects: they are authored inline on [DialogueController](./DialogueController.md).

| Type | Contents |
|------|----------|
| `Dialogue` | One dialogue: `Monolog[] monologues`, plus `UnityEvent<int> OnChangeDialog`. |
| `Monolog` | One character's turn: `string characterName`, `Sentence[] sentences`, plus `UnityEvent<int> OnChangeMonolog`. |
| `Sentence` | A single line: `string sentence` (multi-line), optional `Sprite sprite` (portrait), plus `UnityEvent OnChangeSentence`. |

The controller invokes `OnChangeDialog`, then `OnChangeMonolog`, then `OnChangeSentence` before presenting a sentence. A container's change event also runs when visiting it with a missing/empty child array, before its boundary event. Container events can repeat for consecutive monologs/sentences; they are not one-time enter events. Use them for repeatable scene reactions, or track the current index in C# when a reaction should happen once.

Populate all three array levels in the Inspector/Dialogue Editor or assign them from C#. The arrays and fields remain public for existing integrations. Avoid replacing the graph directly from a playback callback; use `StartDialogue(...)` or a navigation method to establish a new selection. These methods cancel the interrupted presentation safely.

For optional portraits, `Sentence.sprite == null` follows `DialogueUI.clearImageWhenNoSprite`. Null text becomes an empty line. Missing array contents/entries are not validated as a complete graph in advance; consult [controller behavior](./DialogueController.md) for navigation and warning rules. UnityEvents can contain scene references because this data is serialized on the scene component, not stored in a shared ScriptableObject.

Null/empty child arrays permit progression to the next container. With boundary auto flags disabled, that progression is synchronous and iterative, including for long sparse graphs. A null `Dialogue`, `Monolog` or `Sentence` element instead stops the current presentation with a gated warning; it is not silently skipped. Enable the existing boundary auto flags when progression should wait for their configured delay.

## See Also
- ← [Tools/Dialogue](README.md)

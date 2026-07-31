# Dialogue data types

**Purpose:** The plain `[Serializable]` data classes the Dialogue system works on. They live in `Scripts/Tools/Dialogue/DialogueData.cs` — there is no `DialogueData` type itself, and none of them are ScriptableObjects: they are authored inline on [DialogueController](./DialogueController.md).

| Type | Contents |
|------|----------|
| `Dialogue` | One dialogue: `Monolog[] monologues`, plus `UnityEvent<int> OnChangeDialog`. |
| `Monolog` | One character's turn: `string characterName`, `Sentence[] sentences`, plus `UnityEvent<int> OnChangeMonolog`. |
| `Sentence` | A single line: `string sentence` (multi-line), optional `Sprite sprite` (portrait), plus `UnityEvent OnChangeSentence`. |

Every level fires its own event when it advances, so you can hook portraits, audio or camera moves per dialogue, per monolog or per line without extra code.

## See Also
- ← [Tools/Dialogue](README.md)

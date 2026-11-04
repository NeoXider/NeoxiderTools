# DialogueUI

**What it is:** `Neo.Tools.DialogueUI` binds a dialogue name, portrait and body to TMP/uGUI elements. It is optional for the controller and can also be driven directly from C# or UnityEvents. Script: `Scripts/Tools/Dialogue/DialogueUI.cs`.

**How to use:**
- Add **Neoxider → Tools → Dialogue → DialogueUI** next to `DialogueController`, or assign it in the controller's Inspector.
- Assign `characterNameText` and `dialogueText` to TMP labels with valid font assets/materials. Assign an optional uGUI `Image` to `characterImage`.
- Choose whether missing portraits clear the image and whether new portraits use their native size. Wire `OnCharacterChange` to scene reactions if needed.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `HasFontError` | Result of the most recent primary font/material validation. |
| `OnCharacterChange` | Fires when the requested name differs from the cached name. |
| `characterImage` | Optional portrait image. |
| `characterNameText`, `dialogueText` | Optional TMP name and body labels. |
| `clearImageWhenNoSprite` | Clear and disable the image on a null sprite; otherwise retain the previous portrait. |
| `setNativeSize` | Call `Image.SetNativeSize()` when assigning a non-null portrait. |

## Public API

```csharp
ui.SetCharacterName("Guide");
ui.SetCharacterSprite(portrait);
ui.SetDialogueText("Welcome.");
ui.ClearDialogueText();

// Reveal a line without building a string per character (what the controller's MaxVisibleCharacters mode does).
ui.SetDialogueTextRevealed("A longer line that keeps its layout.", 0);
ui.SetVisibleCharacters(5);
ui.ShowAllCharacters();
```

`SetDialogueTextRevealed(text, visibleCharacters)` assigns the whole body once and shows only its first characters through TMP `maxVisibleCharacters` (rich-text tags do not count); `SetVisibleCharacters(count)` then reveals more without reassigning the text. Negative counts are treated as zero. `ShowAllCharacters()` lifts that limit without touching the text; `SetDialogueText` and `ClearDialogueText` lift it too. Lifting puts back the `maxVisibleCharacters` the label had before the UI first lowered it (TMP's default, 99999, means no limit), so a limit the project assigned to the label survives a reveal. A value written to the label while the UI's limit is active is overwritten when the limit is lifted. If `dialogueText` is rebound while a limit is active, the next lift, or the first reveal on the new label, restores the label the limit was set on.

`SetCharacterName` always refreshes a valid assigned label, including an empty or unchanged name. Only the change event is deduplicated, so assigning a replacement label and repeating the same name works. `Reset()` clears body text and resets the cached name; it retains the portrait and name label until the next assignment.

Missing UI references are allowed. Text setters skip labels whose font or font material is missing. `ValidateFonts()` runs in `Awake` and can be called after rebinding labels; it updates `HasFontError` and returns whether assigned primary fonts are valid. An unassigned optional label is not an error.

The current fallback validation removes null/material-less entries from the assigned font asset's direct `fallbackFontAssetTable`. This mutates the shared TMP font asset and affects other labels using it. It does not recursively validate fallback graphs; prepare shared fonts before playback when those entries must be retained.

This legacy repair behavior is preserved in 11.0.0: `ValidateFonts()` performs validation and fallback cleanup. A separate read-only inspection/editor repair policy remains a proposal; this release introduces no font cloning, automatic asset migration or new font configuration.

## See Also

- [Module Root](./README.md)
- [DialogueController](./DialogueController.md)

# DialogueUI

`Neo.Tools.DialogueUI` binds a dialogue name, portrait and body to TMP/uGUI elements. It is optional for the controller and can also be driven directly from C# or UnityEvents.

## Setup

- Add **Neoxider → Tools → Dialogue → DialogueUI** next to `DialogueController`, or assign it in the controller's Inspector.
- Assign `characterNameText` and `dialogueText` to TMP labels with valid font assets/materials. Assign an optional uGUI `Image` to `characterImage`.
- Choose whether missing portraits clear the image and whether new portraits use their native size. Wire `OnCharacterChange` to scene reactions if needed.

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
```

`SetCharacterName` always refreshes a valid assigned label, including an empty or unchanged name. Only the change event is deduplicated, so assigning a replacement label and repeating the same name works. `Reset()` clears body text and resets the cached name; it retains the portrait and name label until the next assignment.

Missing UI references are allowed. Text setters skip labels whose font or font material is missing. `ValidateFonts()` runs in `Awake` and can be called after rebinding labels; it updates `HasFontError` and returns whether assigned primary fonts are valid. An unassigned optional label is not an error.

The current fallback validation removes null/material-less entries from the assigned font asset's direct `fallbackFontAssetTable`. This mutates the shared TMP font asset and affects other labels using it. It does not recursively validate fallback graphs; prepare shared fonts before playback when those entries must be retained.

This legacy repair behavior is preserved in 11.0.0: `ValidateFonts()` performs validation and fallback cleanup. A separate read-only inspection/editor repair policy remains a proposal; this release introduces no font cloning, automatic asset migration or new font configuration.

## See Also

- [Module Root](./README.md)
- [DialogueController](./DialogueController.md)

# ResolutionPresetEntry

**What it is:** A serializable struct for a fixed resolution option (width × height). Used when preset resolution lists are needed beyond `Screen.resolutions`.

**How to use:** Fill the `_customResolutionPresets` array of `GameSettingsComponent` with positive width/height entries; leave it empty to use `Screen.resolutions`.

## Fields

| Field | Description |
|-------|-------------|
| `Width` | Width in pixels (≥ 1). |
| `Height` | Height in pixels (≥ 1). |

## See Also
- [GameSettingsComponent](GameSettingsComponent.md) — ← [Settings](README.md)

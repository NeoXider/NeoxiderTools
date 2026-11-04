# SaveProviderType

**What it is:** Enum for selecting the save backend type.

**How to use:** Select it in a `SaveProviderSettings` asset, or read `ISaveProvider.ProviderType` at runtime.

## Values

| Value | Description |
|-------|-------------|
| `PlayerPrefs` | Save via Unity `PlayerPrefs` (default). |
| `File` | Save to a JSON file. |

## See Also
- [ISaveProvider](ISaveProvider.md)
- ← [Save](README.md)

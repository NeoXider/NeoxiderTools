# GlobalData

**Purpose:** A serializable container class for global game data (currency, levels, settings). Empty by default — add your own fields. Saved via `GlobalSave`. Namespace: `Neo.Save`.

## Usage
Open `GlobalData.cs` and add your fields (public or `[SerializeField]`). Access from code: `GlobalSave.data.fieldName`. Fields must be serializable.

## Example
```csharp
[Serializable]
public class GlobalData
{
    public int coins = 0;
    public int lastCompletedLevel = -1;
    public bool isMusicEnabled = true;
}
```

Access: `GlobalSave.data.coins`

## See Also
- [GlobalSave](GlobalSave.md)
- ← [Save](README.md)

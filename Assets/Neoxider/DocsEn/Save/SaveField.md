# SaveField

**Purpose:** Attribute for marking fields to be saved by `SaveManager`. The key is passed in the constructor; optionally `autoSaveOnQuit` and `autoLoadOnAwake`. Namespace: `Neo.Save`.

## Constructor

```csharp
public SaveField(string key, bool autoSaveOnQuit = true, bool autoLoadOnAwake = true)
```

| Parameter | Description |
|-----------|-------------|
| `key` | **Required.** Unique string key for storing the field value. Must be unique within a component. |
| `autoSaveOnQuit` | (Default `true`) Auto-save the field on application quit. |
| `autoLoadOnAwake` | (Default `true`) Auto-load the field on application start. |

## Example
```csharp
public class PlayerStats : SaveableBehaviour
{
    [SaveField("player_health")]
    private int health = 100;

    [SaveField("player_name")]
    public string playerName = "Hero";
}
```

## See Also
- [ISaveableComponent](ISaveableComponent.md)
- [SaveManager](SaveManager.md)
- ← [Save](README.md)

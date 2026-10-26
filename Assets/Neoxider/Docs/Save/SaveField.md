# SaveField

**Purpose:** Attribute for marking fields to be saved by `SaveManager`. The key is passed in the constructor; optionally `autoSaveOnQuit` and `autoLoadOnAwake`. Namespace: `Neo.Save`.

## Constructor

```csharp
public SaveField(string key, bool autoSaveOnQuit = true, bool autoLoadOnAwake = true)
```

| Parameter | Description |
|-----------|-------------|
| `key` | **Required.** Unique string key for storing the field value. Must be unique within a component. |
| `autoSaveOnQuit` | (Default `true`) Include the field in `Save()` / `SaveAutoFields()` passes, including pause, focus loss, quit, and component disable. Previously saved manual fields are preserved. |
| `autoLoadOnAwake` | (Default `true`) Include the field in bulk and scene load passes. |

Explicit `SaveManager.Save(component)` and `SaveManager.Load(component)` process all marked fields regardless of these automatic flags. Private fields in base classes are included; field keys must remain unique across the full component hierarchy. Keep keys stable across releases or migrate stored data explicitly.

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

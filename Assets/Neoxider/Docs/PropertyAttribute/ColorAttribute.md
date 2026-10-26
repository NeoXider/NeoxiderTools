# GUIColorAttribute

**Namespace:** `Neo`. **Assembly:** `Neo.PropertyAttribute`.

`[GUIColor]` tints a serialized field's Inspector controls through `GUI.color`. The existing page filename is retained for documentation links.

## Scene workflow

```csharp
using Neo;
using UnityEngine;

public sealed class PlayerStats : MonoBehaviour
{
    [GUIColor(0.7, 1.0, 0.7)]
    public int health = 100;

    [GUIColor(0.7, 0.8, 1.0, 0.9)]
    public float mana = 50f;
}
```

The drawer renders child fields when a serialized value is expanded and reserves their full height. It restores the previous GUI tint after drawing, including when a nested drawer exits with an exception.

## C# API

`GUIColorAttribute(double r, double g, double b, double a = 1f)` stores the components as a Unity `Color` exposed through the read-only `color` property. Use values from `0.0` to `1.0` for ordinary Inspector tinting; the constructor does not clamp them. There is no preset-color enum overload.

The attribute affects editor presentation. Reading `color` from C# returns the configured value; runtime rendering requires its own material/UI API.

See [PropertyAttribute](README.md).

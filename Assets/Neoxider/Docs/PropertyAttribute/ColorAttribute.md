
# [Color]

**What it is:** the `[GUIColor]` attribute (previously `[Color]` in the docs) for coloring field backgrounds in the inspector. It lets you color field backgrounds, which helps visually group related properties or draw ...

**How to use:** see the sections below.

---


**Namespace:** `Neo`
**Path:** `Scripts/PropertyAttribute/GUIColorAttribute.cs` (the attribute in code: `[GUIColor]`).

## Description

The `[Color]` attribute is a simple but useful utility for visually organizing the inspector. It lets you color field backgrounds, which helps visually group related properties or draw attention to important settings.

## How to use

Place the `[Color]` attribute above any serializable field in your component.

```csharp
public class PlayerStats : MonoBehaviour
{
    [Color(ColorEnum.SoftGreen)]
    public int health = 100;

    [Color(ColorEnum.SoftBlue)]
    public float mana = 50f;
}
```

## Ways to specify a color

The color can be specified in two ways:

### 1. Via the `ColorEnum` enumeration

For convenience and consistency, the attribute predefines a set of soft, easy-on-the-eyes colors. This is the preferred way to use it.

**Example:**
```csharp
[Color(ColorEnum.SoftYellow)]
public string playerName = "Neo";
```

**Available colors in `ColorEnum`:**
- `SoftRed`
- `SoftGreen`
- `SoftBlue`
- `SoftYellow`
- `SoftGray`
- `SoftPurple`
- `SoftCyan`
- `SoftOrange`

### 2. Via RGBA values

You can set any color by specifying its R, G, B and, optionally, A (transparency) components. Values must be in the range `0.0` to `1.0`.

**Example:**
```csharp
// Bright orange color
[Color(1.0, 0.5, 0.0)] 
public Transform targetTransform;
```

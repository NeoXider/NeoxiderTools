# Rainbow Effects for Neo Components

**What it is:** All components from the `Neo` namespace (including `Neo.Tools`, `Neo.Cards`, `Neo.UI`, and others) are displayed in the Unity Inspector with beautiful effects:

**How to use:** see the sections below.

---


## Description

All components from the `Neo` namespace (including `Neo.Tools`, `Neo.Cards`, `Neo.UI`, and others) are displayed in the Unity Inspector with beautiful effects:

- **Animated rainbow "by Neoxider" signature** - the text smoothly cycles through all the colors of the rainbow
- **Vertical rainbow line on the left** - a gradient from red to violet
- **Rainbow text outline** - an optional glow effect
- **Animation** - can be toggled separately for the text and the line

---

## Settings via Menu

**Tools → Neoxider → Visual Settings**

A settings window will open:

### Text (Signature)
- ☑ **Enable Rainbow Signature** - show the colored "by Neoxider" text
- ☑ **Text animation** - color cycling

### Line (Rainbow Line)
- ☑ **Enable Rainbow Outline** - text outline
- ☑ **Enable Rainbow Line (left)** - vertical line
- ☑ **Line animation** - gradient movement

### Animation Speed
- **Rainbow Speed** (0.0 - 1.0) - animation speed

### Reset Settings
- **[Reset all settings]** - restore default values

**Note:** All settings are stored in `EditorPrefs` and persist between Unity sessions.

---

## Settings in Code

All settings use `EditorPrefs` and persist between sessions:

### Via CustomEditorSettings

```csharp
// Text
CustomEditorSettings.EnableRainbowSignature          // Enable/disable colored text
CustomEditorSettings.EnableRainbowSignatureAnimation // Enable/disable text animation

// Line
CustomEditorSettings.EnableRainbowOutline            // Enable/disable text outline
CustomEditorSettings.EnableRainbowComponentOutline   // Enable/disable left line
CustomEditorSettings.EnableRainbowLineAnimation      // Enable/disable line animation

// Speed
CustomEditorSettings.RainbowSpeed                    // 0.0 - 1.0

// Setters
CustomEditorSettings.SetEnableRainbowSignature(bool value);
CustomEditorSettings.SetEnableRainbowSignatureAnimation(bool value);
CustomEditorSettings.SetEnableRainbowLineAnimation(bool value);
CustomEditorSettings.SetRainbowSpeed(float value);
```

### Default Values

| Parameter | Value |
|----------|----------|
| EnableRainbowSignature | `true` |
| EnableRainbowSignatureAnimation | `true` |
| EnableRainbowOutline | `true` |
| EnableRainbowComponentOutline | `true` |
| EnableRainbowLineAnimation | `true` |
| RainbowSpeed | `0.1` |
| RainbowSaturation | `0.8` |
| RainbowBrightness | `1.0` |

## How to Use

1. **Create a component in the Neo namespace:**
   ```csharp
   namespace Neo.Tools
   {
       public class MyComponent : MonoBehaviour
       {
           // Your code
       }
   }
   ```

2. **Add the component to a GameObject in the scene**

3. **Open the Inspector** - you will see the animated rainbow "by Neoxider" signature at the top of the component

## Customizing the Effect

### Disable Animation

If you want to disable the rainbow animation, change this in `CustomEditorSettings.cs`:

```csharp
public static bool EnableRainbowSignature => false;
```

### Disable Only the Outline

If you want to keep just the colored signature without the outline:

```csharp
public static bool EnableRainbowOutline => false;
```

### Change the Animation Speed

For a slower animation:

```csharp
public static float RainbowSpeed => 0.1f; // Slow rainbow
```

For a faster animation:

```csharp
public static float RainbowSpeed => 1.0f; // Fast rainbow
```

### Make the Colors More Saturated

```csharp
public static float RainbowSaturation => 1.0f; // Maximum saturation
public static float RainbowBrightness => 1.0f; // Maximum brightness
```

### Increase the Outline Size

```csharp
public static float RainbowOutlineSize => 3.0f; // Thicker outline
public static float RainbowOutlineAlpha => 0.8f; // More visible outline
```

## Usage Examples

### Example 1: Test Component

A test component `RainbowTestComponent.cs` was created to demonstrate the effect:

```csharp
namespace Neo.Tools.View
{
    [AddComponentMenu("Neoxider/Tools/Rainbow Test")]
    public class RainbowTestComponent : MonoBehaviour
    {
        public string testMessage = "Look at the 'by Neoxider' signature above!";
    }
}
```

### Example 2: Existing Neo Components

All existing components automatically get the rainbow effect:
- `HandComponent`
- `DeckComponent`
- `CardComponent`
- `StarView`
- `VisualToggle`
- And all other components in the `Neo.*` namespace

## Technical Details

### How It Works

1. **CustomEditorBase** - the base class for all custom editors of Neo components
2. **Namespace check** - the editor checks whether the component belongs to the `Neo` namespace or starts with `Neo.`
3. **Animation** - `EditorApplication.timeSinceStartup` is used to create smooth animation
4. **HSV color model** - HSV (Hue, Saturation, Value) is used to create the rainbow effect
5. **Automatic Repaint** - the editor automatically refreshes for the animation

### Performance

- The animation is optimized and does not affect Editor performance
- Uses Unity's built-in `EditorApplication.update` refresh system
- Repaint is only called for active components in the Inspector

## Compatibility

- ✅ Unity 2020.3 and above
- ✅ Works with Odin Inspector
- ✅ Works with all components in the `Neo` namespace
- ✅ No impact on runtime performance (Editor-only)

## File Paths

- **Settings**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs`
- **Implementation**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`
- **Test component**: `Assets/Neoxider/Scripts/Tools/View/RainbowTestComponent.cs`
- **Documentation**: `Assets/Neoxider/Docs/Editor/RainbowSignature.md`


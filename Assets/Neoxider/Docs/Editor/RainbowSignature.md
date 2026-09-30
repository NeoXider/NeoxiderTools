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

**Neoxider → Visual Settings**

A settings window will open:

### Animation
- ☑ **Animate inspector** - master switch for every inspector animation (mascot, rainbow line, update pulse). Off draws a still frame and schedules no repaints; turn it off if the editor feels slow
- ☑ **Animate in Play Mode** - keep animating while the game runs (off by default)
- **Max FPS** (5 - 60, default 30) - upper limit for animation repaints

With the master switch off the **Text animation** and **Line animation** toggles below are disabled: they only matter while animation runs.

### Text (Signature)
- ☑ **Enable Rainbow Signature** - show the colored "by Neoxider" text
- ☑ **Text animation** - color cycling

### Line (Rainbow Line)
- ☑ **Enable Rainbow Outline** - text outline
- ☑ **Enable Rainbow Line (left)** - vertical line
- ☑ **Line animation** - gradient movement

### Animation Speed
- **Rainbow Speed** (0.0 - 1.0) - animation speed

### Header
- **Script name color** - tint of the script-name label
- **Minimum fields for Header category** (0 - 10) - how many fields a `[Header]` group needs before it becomes a collapsible section

### Lists and Arrays
- ☑ **Default Unity list/array drawing** - use Unity's built-in list drawing instead of the custom foldouts

### Reset Settings
- **[Reset all settings]** - restore default values
- **Troubleshooting** - a hint to run `Neoxider → Tools → Fix Editor Assembly References` if the effects do not appear after a Package Manager install

**Note:** Settings are stored per-project in `ProjectSettings/NeoInspectorSettings.asset` (a `ScriptableSingleton`) and persist between Unity sessions. Legacy values from the old `EditorPrefs` keys are migrated automatically on first access.

---

## Settings in Code

All settings persist per-project in `ProjectSettings/NeoInspectorSettings.asset`:

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

// Animation (master switch, Play Mode, frame cap)
CustomEditorSettings.EnableAnimations                // Master switch for all inspector animation
CustomEditorSettings.AnimateInPlayMode               // Keep animating while the game runs
CustomEditorSettings.AnimationFps                    // 5 - 60

// Setters (each persists immediately)
CustomEditorSettings.SetEnableRainbowSignature(bool value);
CustomEditorSettings.SetEnableRainbowSignatureAnimation(bool value);
CustomEditorSettings.SetEnableRainbowOutline(bool value);
CustomEditorSettings.SetEnableRainbowComponentOutline(bool value);
CustomEditorSettings.SetEnableRainbowLineAnimation(bool value);
CustomEditorSettings.SetRainbowSpeed(float value);
CustomEditorSettings.SetEnableAnimations(bool value);
CustomEditorSettings.SetAnimateInPlayMode(bool value);
CustomEditorSettings.SetAnimationFps(int value);
```

### Default Values

| Parameter | Value |
|----------|----------|
| EnableRainbowSignature | `true` |
| EnableRainbowSignatureAnimation | `true` |
| EnableRainbowOutline | `true` |
| EnableRainbowComponentOutline | `true` |
| EnableRainbowLineAnimation | `true` |
| EnableAnimations | `true` |
| AnimateInPlayMode | `false` |
| AnimationFps | `30` |
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

Change these at runtime through the **Neoxider → Visual Settings** window, or in code via the `CustomEditorSettings.Set*` methods listed above (each writes to `NeoInspectorSettings` and repaints open inspectors). For example:

```csharp
// Disable the animated signature
CustomEditorSettings.SetEnableRainbowSignature(false);

// Keep the signature but drop the text outline
CustomEditorSettings.SetEnableRainbowOutline(false);

// Slower vs faster hue flow (0..1)
CustomEditorSettings.SetRainbowSpeed(0.1f); // slow
CustomEditorSettings.SetRainbowSpeed(1.0f); // fast
```

Saturation, brightness, outline size/alpha and line width are read-only accessors on `CustomEditorSettings`; edit their serialized fields in `NeoInspectorSettings` if you need to tune them.

## Usage Examples

### Example 1: Existing Neo Components

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
3. **Animation** - `EditorApplication.timeSinceStartup` is used to create smooth animation; with animation off the clock is frozen and one still frame is drawn
4. **HSV color model** - HSV (Hue, Saturation, Value) is used to create the rainbow effect
5. **Automatic Repaint** - animating editors request short repaint leases served by one shared, throttled ticker

### Performance

- Animation repaints are capped at *Max FPS* (default 30) by a single shared ticker, not one loop per component
- A hidden or collapsed inspector stops drawing, so its lease lapses and it stops repainting
- No animation in Play Mode unless *Animate in Play Mode* is on; the master switch stops all animation
- Per-event reflection (button methods, header titles, doc path) is cached per type, see [CustomEditorBase](CustomEditorBase.md)

## Compatibility

- ✅ Unity 2020.3 and above
- ✅ Works with Odin Inspector
- ✅ Works with all components in the `Neo` namespace
- ✅ No impact on runtime performance (Editor-only)

## File Paths

- **Settings facade**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs`
- **Settings store**: `Assets/Neoxider/Editor/PropertyAttribute/NeoInspectorSettings.cs`
- **Settings window**: `Assets/Neoxider/Editor/PropertyAttribute/NeoxiderSettingsWindow.cs` (menu `Neoxider → Visual Settings`)
- **Implementation**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`
- **Documentation**: `Assets/Neoxider/Docs/Editor/RainbowSignature.md`


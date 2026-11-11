# Visual Settings of the Neo Inspector

**What it is:** the settings behind the look of every `Neo.*` component inspector - the banner with the mascot, the compact header, the spectrum frame beside the property card, the section accent and the animation. The page name is historical: the old "by Neoxider" rainbow text signature is gone, and the settings that switched it now switch the banner.

**How to use:** nothing to add to your components - every `Neo.*` inspector gets the look; tune it or switch parts off in **Neoxider > Visual Settings**.

---


## Settings window

**Neoxider > Visual Settings** opens a window with five groups. Every change is saved at once and repaints open inspectors.

### Header
- **Show banner** - the full 44px banner (mascot, title, module tagline, version pill). Off: every Neo component gets the compact one-row header, which keeps the health badge and the update status.
- **One banner per GameObject** - with several Neo components on one GameObject only the first shows the full banner; the others use the compact header. On by default.
- **Animate mascot** - breathing, blinking and the click bounce. Needs *Animate inspector*.

### Animation
- **Animate inspector** - master switch for the mascot, the flowing spectrum and every transition. Off draws a still frame and schedules no animation repaints; turn it off if the editor feels slow.
- **Animate in Play Mode** - keep animating while the game runs (off by default).
- **Smooth transitions** - eased opening and closing of sections, hover highlights and sliding switches.
- **Max FPS** (5 - 60, default 30) - upper limit for animation repaints.

With the master switch off the dependent toggles are disabled: they only matter while animation runs. The health badge still refreshes twice a second and live data (update check, condition results) ten times a second.

### Spectrum frame
- **Spectrum frame** - the colour line along the left side of the property card. It turns amber (missing references) or red (errors, NaN values) with the component's health.
- **Flowing colours** - let the spectrum flow along the frame. Needs *Animate inspector*.
- **Flow speed**, **Saturation**, **Brightness** (0 - 1).

### Sections and lists
- **Section accent** - accent colour of the collapsible `[Header]` sections (rail, count pill, icon tint).
- **Fields per collapsible section** (0 - 10) - how many fields a `[Header]` group needs before it becomes a collapsible section; smaller groups get a plain title row.
- **Unity list drawing** - draw lists and arrays with Unity's reorderable list (recommended). Off uses the Neoxider foldout, which cannot add or remove elements.

### Reset and troubleshooting
- **Reset all visual settings** - restore the defaults below.
- A hint to run `Neoxider > Tools > Fix Editor Assembly References` if the Neo inspector does not appear after a Package Manager install.

Settings are stored per project in `ProjectSettings/NeoInspectorSettings.asset` (a `ScriptableSingleton`) and persist between Unity sessions. Legacy values from the old `EditorPrefs` keys are migrated automatically on first access.

---

## Settings in code

All settings go through `CustomEditorSettings` (namespace `Neo.Editor`, Editor assembly). The historical names are kept for compatibility; the comment says what they switch today.

```csharp
// Header
CustomEditorSettings.EnableRainbowSignature          // Show the full banner (off: compact header)
CustomEditorSettings.CompactRepeatedBanners          // One banner per GameObject
CustomEditorSettings.EnableRainbowSignatureAnimation // Animate the mascot

// Animation
CustomEditorSettings.EnableAnimations                // Master switch for all inspector animation
CustomEditorSettings.AnimateInPlayMode               // Keep animating while the game runs
CustomEditorSettings.EnableTransitions               // Eased sections, hover and switches
CustomEditorSettings.AnimationFps                    // 5 - 60

// Spectrum frame
CustomEditorSettings.EnableRainbowComponentOutline   // Show the frame
CustomEditorSettings.EnableRainbowLineAnimation      // Let the colours flow
CustomEditorSettings.RainbowSpeed                    // 0.0 - 1.0
CustomEditorSettings.RainbowSaturation               // 0.0 - 1.0
CustomEditorSettings.RainbowBrightness               // 0.0 - 1.0

// Sections and lists
CustomEditorSettings.ScriptNameColor                 // Section accent colour
CustomEditorSettings.MinFieldsForHeaderCategory      // Fields per collapsible section
CustomEditorSettings.UseDefaultListAndArrayDrawing   // Unity list drawing

// Setters (each persists immediately)
CustomEditorSettings.SetEnableRainbowSignature(bool value);
CustomEditorSettings.SetCompactRepeatedBanners(bool value);
CustomEditorSettings.SetEnableRainbowSignatureAnimation(bool value);
CustomEditorSettings.SetEnableAnimations(bool value);
CustomEditorSettings.SetAnimateInPlayMode(bool value);
CustomEditorSettings.SetEnableTransitions(bool value);
CustomEditorSettings.SetAnimationFps(int value);
CustomEditorSettings.SetEnableRainbowComponentOutline(bool value);
CustomEditorSettings.SetEnableRainbowLineAnimation(bool value);
CustomEditorSettings.SetRainbowSpeed(float value);
CustomEditorSettings.SetRainbowSaturation(float value);
CustomEditorSettings.SetRainbowBrightness(float value);
CustomEditorSettings.SetScriptNameColor(Color value);
CustomEditorSettings.SetMinFieldsForHeaderCategory(int value);
CustomEditorSettings.SetUseDefaultListAndArrayDrawing(bool value);
```

`EnableRainbowOutline` / `SetEnableRainbowOutline` are `[Obsolete]` (a compiler warning): they outlined the old text signature and have had no visible effect since the v10 banner replaced it. The value is still stored. `RainbowOutlineSize`, `RainbowOutlineAlpha` and `RainbowComponentOutlineWidth` are read-only accessors of stored values that the current frame does not use.

### Default values

| Parameter | Value |
|----------|----------|
| EnableRainbowSignature | `true` |
| CompactRepeatedBanners | `true` |
| EnableRainbowSignatureAnimation | `true` |
| EnableAnimations | `true` |
| AnimateInPlayMode | `false` |
| EnableTransitions | `true` |
| AnimationFps | `30` |
| EnableRainbowComponentOutline | `true` |
| EnableRainbowLineAnimation | `true` |
| RainbowSpeed | `0.1` |
| RainbowSaturation | `0.8` |
| RainbowBrightness | `1.0` |
| MinFieldsForHeaderCategory | `3` |
| UseDefaultListAndArrayDrawing | `true` |

For example:

```csharp
// A quiet inspector: compact headers everywhere, no frame, no animation.
CustomEditorSettings.SetEnableRainbowSignature(false);
CustomEditorSettings.SetEnableRainbowComponentOutline(false);
CustomEditorSettings.SetEnableAnimations(false);
```

## How it works

1. **CustomEditorBase** - the base class for all custom editors of Neo components; `NeoCustomEditor` is its fallback for every MonoBehaviour.
2. **Namespace check** - the look applies to types whose namespace is `Neo` or starts with `Neo.`.
3. **Animation** - every motion is a function of `EditorApplication.timeSinceStartup` with easing curves (ease-out cubic for opening, ease-in-out for the switch knob, a damped spring for the mascot pop), so it is frame-rate independent. With animation off the clock is frozen and one still frame is drawn.
4. **HSV colour model** - the frame's hue runs along its path and over time; HSV hue is circular, so it wraps with no seam.
5. **Repaints** - animating editors request short repaint leases served by one shared, throttled ticker. A hidden or collapsed inspector stops drawing, so its lease lapses; while Unity is in the background the animation idles, while live data and the health badge keep refreshing.

### Performance

- Animation repaints are capped at *Max FPS* (default 30) by a single shared ticker, not one loop per component.
- No animation in Play Mode unless *Animate in Play Mode* is on; the master switch stops all animation.
- The spectrum frame is one polyline call with a fixed vertex budget; the chrome reuses its styles, layout options and texts, so a warm repaint is meant to allocate nothing. See [CustomEditorBase](CustomEditorBase.md#4-animation-and-performance) for what is cached.

## Compatibility

- Unity 6.0 and newer (the package targets Unity 6).
- Works with Odin Inspector (Odin keeps drawing the properties; the Neo header and card stay).
- Dark and light editor skins; HiDPI displays.
- No impact on runtime performance (Editor-only).

## File paths

- **Settings facade**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs`
- **Settings store**: `Assets/Neoxider/Editor/PropertyAttribute/NeoInspectorSettings.cs`
- **Settings window**: `Assets/Neoxider/Editor/PropertyAttribute/NeoxiderSettingsWindow.cs` (menu `Neoxider > Visual Settings`)
- **Animation policy and easing**: `Assets/Neoxider/Editor/PropertyAttribute/NeoInspectorAnimation.cs`
- **Theme, styles, shared controls**: `NeoInspectorTheme.cs`, `NeoInspectorStyles.cs`, `NeoxiderEditorGUI.cs` (same folder)
- **Implementation**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs` and `Internal/CustomEditorBase.*.cs`

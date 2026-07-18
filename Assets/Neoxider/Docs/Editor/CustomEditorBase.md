# CustomEditorBase Base Class

**What it is:** It also provides a convenient interface for setting method parameters right in the inspector before invoking them. This class is the foundation of `NeoCustomEditor`.

**How to use:** see the sections below.

---


## 1. Introduction

`CustomEditorBase` is an abstract base class for Unity editors that implements one very useful feature: the ability to turn methods of your class into inspector buttons using the `[Button]` attribute.

It also provides a convenient interface for setting method parameters right in the inspector before invoking them. This class is the foundation of `NeoCustomEditor`.

---

## 2. Class Description

### CustomEditorBase
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`

**Description**
An abstract editor class that adds inspector button rendering for methods marked with the `[Button]` attribute.

**Key features**
- **Button methods**: Any public or private method with the `[Button]` attribute is displayed in the inspector as a button.
- **Editable parameters**: If a method has parameters (e.g. `int`, `float`, `string`, `bool`, `GameObject`), they are shown in a dropdown below the button and can be modified before invocation.
- **Default value support**: Initial parameter values are taken from the default values specified in the method signature.

**Public methods**
- This class has no public methods intended to be called from other scripts. It extends the functionality of the Unity inspector.

---

## 3. v10 Inspector Theme

Since v10 every `Neo.*` component inspector is drawn with a shared visual theme (`NeoInspectorTheme`, same folder):

- **Hero banner** — gradient header with the animated Neoxider mascot (idle breathing, periodic blink, laugh-pop on click, drawn as a close-up that nearly fills its chip), the package title, a version pill and an update strip that appears when a newer package version is published.
- **Property card** — the default property block sits on a rounded, accent-tinted card with a 1px edge.
- **Spectrum half-frame** — a continuous HSV gradient line hugs the card's left side with rounded corners and short fading arms; the hue flows over time. Controlled by the same `CustomEditorSettings` toggles as the legacy rainbow options (`EnableRainbowComponentOutline`, `EnableRainbowLineAnimation`, `RainbowSpeed`, saturation/brightness).
- **Section chips** — collapsible property sections and Actions/Documentation foldouts use rounded, color-coded headers.
- **Mascot health ("slime linter")** — the banner slime reflects the inspected component's health and doubles as a shortcut:
  - *Faces* — neutral/blink when healthy, worried on missing object references, angry on console errors or NaN/Infinity float fields, a brief surprised reaction when a new error appears, and a "watching" face in Play Mode.
  - *Console memory* — errors are attributed to the component type by parsing the stack trace and remembered for the session (`SessionState`, survives domain reloads). A count badge on the chip opens a compact issue list with a **Clear** action.
  - *Validation* — a cached, throttled, property-capped `SerializedObject` scan flags missing references and invalid numbers; it runs only for the object currently inspected, so cost stays negligible regardless of scene size.
  - *Click = solo* — poking the slime collapses every other component on the GameObject (state backed up); poke again to restore.

All chrome is wrapped in exception guards: a failure inside decorative drawing never breaks the property layout below.

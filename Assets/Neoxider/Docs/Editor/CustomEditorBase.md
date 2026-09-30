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
  - *Play Mode gaze* — the watching face looks toward the Game view: when the Game view sits to the left of the inspector the face is mirrored horizontally (the art looks right by default). The Game view position is rescanned at most once per second; with no Game view open the face stays unmirrored.
  - *Console memory* — errors are attributed to the component type by parsing the stack trace and remembered for the session (`SessionState`, survives domain reloads). A count badge on the chip opens a compact issue list with a **Clear** action.
  - *Console mirroring* — the memory follows the Unity Console: whenever the console error count drops to zero (Clear button, Clear on Play, clear before recompile), every remembered error is wiped (memory and `SessionState`) and inspectors repaint, so the mascot calms down the moment the console is clean. Real errors simply re-anger it when they recur.
  - *Validation* — a cached, throttled, property-capped `SerializedObject` scan flags missing references and invalid numbers; it runs only for the object currently inspected, so cost stays negligible regardless of scene size.
  - *Click* — poking the slime plays a springy bounce and a startled face. It is decorative for now; the click action is intentionally left open for a future feature.

All chrome is wrapped in exception guards: a failure inside decorative drawing never breaks the property layout below.

---

## 4. Animation and performance

The banner mascot (breathing, blink, pop), the spectrum half-frame and the update-pill pulse are decorative, and a permanently animating inspector costs editor time. They can be switched off in **Neoxider > Visual Settings > Animation**:

- **Animate inspector** - master switch. Off draws the header as one still frame (no breathing, blink, pop, flowing spectrum or pulse) and schedules no repaints at all.
- **Animate in Play Mode** - off by default. The game already competes with the editor for time, so the inspector stays still while the game runs; turn it on to keep the mascot and the flowing spectrum alive in Play Mode.
- **Max FPS** (5-60, default 30) - upper limit for animation repaints.

In code: `CustomEditorSettings.EnableAnimations`, `AnimateInPlayMode`, `AnimationFps` and the matching `SetEnableAnimations`, `SetAnimateInPlayMode`, `SetAnimationFps`. The rainbow toggles (`EnableRainbowSignatureAnimation`, `EnableRainbowLineAnimation`) still switch their own effect; the master switch overrides all of them.

What keeps refreshing with animation off: the header status (console-error and missing-reference badge) polls at 2 Hz, and content that is live data - update-check progress, the `NeoCondition` result - refreshes at 10 Hz.

**For custom editors deriving from `CustomEditorBase`:**

- `EnsureRepaint()` - decorative animation. Does nothing while animation is off (setting, or Play Mode without *Animate in Play Mode*) and is capped at *Max FPS*.
- `EnsureLiveRepaint()` - data that must stay current whatever the animation setting says (10 Hz).

Both are short leases renewed by every draw and served by one shared ticker, so a hidden or collapsed inspector stops repainting on its own and N components on a GameObject do not run N repaint loops.

**What is cached per type** (instead of reflected on every IMGUI event): the method list and `[Button]` metadata, `[Header]` titles and field-by-property-path lookups, the `Neo` namespace check, the resolved doc page and its rich-text preview (doc caches are dropped on `EditorApplication.projectChanged`). The chrome `GUIStyle`s live in `NeoInspectorStyles` and are built once per editor skin.

---

## 5. Rule for decorative drawing: control ids must not depend on the event

IMGUI control ids are a running counter that Unity compares across events (the press on MouseDown, the pressed look on Repaint, keyboard focus, text selection). A call that takes an id (`GUI.BeginGroup`, `GUI.Box`, `GUI.Button`, `EditorGUI.LabelField`, any `EditorGUILayout` / `GUILayout` call) inside an `Event.current.type == EventType.Repaint` branch shifts every control after it on Repaint only: a click is drawn one control up and text fields lose their selection highlight. Keep such calls outside the branch and leave only pure drawing inside it (`GUI.DrawTexture`, `GUI.Label`, `EditorGUI.DrawRect`, `Handles`, `NeoInspectorTheme.Draw*`). `InspectorControlIdStabilityTests` enforces this with a source scan and with a real IMGUI pass over the header.

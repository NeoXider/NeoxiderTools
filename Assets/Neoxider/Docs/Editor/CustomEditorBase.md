# CustomEditorBase Base Class

**What it is:** the abstract base of every Neoxider inspector. It draws the shared Neoxider look (header with the mascot, property card, collapsible `[Header]` sections, Events and Actions sections) and turns `[Button]` methods into inspector buttons with editable parameters. `NeoCustomEditor` is its fallback implementation for all MonoBehaviours.

**How to use:** nothing to do for ordinary components: any `Neo.*` MonoBehaviour gets this inspector. Add `[Button]` to a method to get an action button. For a module-specific inspector derive from `CustomEditorBase` (see *Deriving a custom editor* below). The look and the animation are configured in **Neoxider > Visual Settings** ([RainbowSignature](RainbowSignature.md) lists every option).

---


## 1. Introduction

`CustomEditorBase` is an abstract base class for Unity editors that implements one very useful feature: the ability to turn methods of your class into inspector buttons using the `[Button]` attribute.

It also provides a convenient interface for setting method parameters right in the inspector before invoking them. This class is the foundation of `NeoCustomEditor`.

---

## 2. Class Description

### CustomEditorBase
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs` (partials in `Editor/PropertyAttribute/Internal/`)

**Description**
An abstract editor class that adds inspector button rendering for methods marked with the `[Button]` attribute.

**Key features**
- **Button methods**: Any public or private method with the `[Button]` attribute is displayed in the Actions section as a button. Its tooltip shows the C# signature.
- **Editable parameters**: A method with parameters gets a "N params" chip next to its button; the chip unfolds one row per parameter (`int`, `float`, `string`, `bool`, enums, `Vector2`, `Vector3`, `Color`, any `UnityEngine.Object`). Values are kept for the inspector session; **Reset** restores the declared defaults.
- **Default value support**: Initial parameter values are taken from the default values specified in the method signature.
- **Overload support**: Methods with the same name keep separate parameter values and foldout state for each signature.
- **Edit Mode guard**: `[Button(PlayModeOnly = true)]` actions are disabled outside Play Mode (the tooltip says so), including the custom button's mouse handler. Direct method calls retain normal C# behavior.
- **Multi-object editing**: with several objects selected, an instance `[Button]` method runs on each selected object (static methods run once). Each object is called independently, so one failing object does not stop the others. Attribute assignments (`[GetComponent]`, `[FindInScene]`, ...) are applied to every selected object as well, and a boolean with different values shows a dash in its switch; clicking it sets all objects to on.
- **Undo and prefab overrides**: outside Play Mode the inspected object is recorded with Undo before an action runs, so serialized fields the action changes are undoable and mark the object dirty. Switches and nested class headers go through `BeginProperty`, so overrides show in bold with the blue margin bar and the right-click Revert menu; a closed section that hides an overridden field shows the blue marker on its header.

**Public methods**
- No public methods are meant to be called from game code; the protected members below are the extension surface for module editors.

### Deriving a custom editor

| Member | Use |
| --- | --- |
| `UseCustomNeoxiderInspectorGUI` + `DrawCustomNeoxiderInspectorGUI()` | Draw your own body inside the Neoxider chrome; the Actions section is still added. |
| `NeoxiderModuleName` | Module label shown in the banner tagline and the compact header. |
| `OnAfterDrawNeoProperties()` | Extra UI right after the default property pass. |
| `DrawCustomProperty(SerializedProperty)` / `DrawStandardProperty(SerializedProperty)` | Take over one field while every other field keeps the standard look. The override gets its own copy of the property, so walking it with `Next` / `NextVisible` is safe. |
| `DrawNeoSection(title, count, drawBody)` / `DrawPropertyFieldNoHeader(property)` | A standard animated section and a field without its doubled `[Header]`. |
| `DrawCollapsibleUnityEvents()` / `DrawUnityEventsFoldout(list)` | The standard Events section. |
| `EnsureRepaint()` / `EnsureLiveRepaint()` | Decorative or live-data repaints (see section 4). |

Property drawers cannot inherit this class; they reach the same look through `NeoxiderEditorGUI`: `DrawPillToggleField` (labelled switch with prefab/Undo/multi-object support), `DrawPillToggle`, `DrawSectionHeaderRect`, `DrawSectionBodyPanel`, `DrawSummaryCard`, `BeginSection`/`EndSection`, `DrawBadges`.

```csharp
using Neo.Editor;
using UnityEditor;

[CustomEditor(typeof(MySpawner))]
public sealed class MySpawnerEditor : CustomEditorBase
{
    protected override string NeoxiderModuleName => "Spawning";

    protected override void ProcessAttributeAssignments()
    {
        ComponentDrawer.ProcessComponentAttributes((MySpawner)target);
    }

    protected override bool DrawCustomProperty(SerializedProperty property)
    {
        if (property.name != "_prefabs")
        {
            return false;
        }

        DrawStandardProperty(property);
        EditorGUILayout.HelpBox("Drop prefabs here.", MessageType.None);
        return true;
    }
}
```

---

## 3. Inspector look (v12)

Every `Neo.*` component inspector is drawn with one visual language (`NeoInspectorTheme`, `NeoInspectorStyles`, `NeoxiderEditorGUI`), tuned for both the dark (Pro) and the light (Personal) skin. Sizes follow the 18px IMGUI row grid, edges are snapped to device pixels so 1px strokes stay crisp on HiDPI screens, arrows are vector chevrons instead of text glyphs, and built-in icons are loaded for the active skin (no dark glyphs on the light skin).

- **Header** - a 44px brand banner: the mascot in a chip, the title, a tagline with the module name, and the version pill. The pill carries a status dot (green: up to date, coral: update available, amber: newer than the published release, blue: checking, grey: unknown or error); its tooltip explains the state and clicking it checks again. When an update is published an **Update** pill appears next to it and opens the release page. The separate update strip of v10/v11 is gone, so the header is a third shorter.
- **Compact header** - a one-row header (small mascot, "Neoxider · Module", version pill, health badge) used when **Show banner** is off, and by default for every Neo component after the first one on the same GameObject (**One banner per GameObject**). A GameObject with five Neo components shows one banner instead of five.
- **Property card** - the property block sits on a rounded card with a faint accent tint and a 1px edge.
- **Spectrum frame** - a thin continuous HSV line hugs the card's left side with rounded corners and short arms whose tips fade out. It is one anti-aliased polyline with per-vertex colours (a fixed 57-vertex budget, whatever the card height), and it turns amber or red when the component has missing references or errors.
- **Sections** - collapsible `[Header]` sections, Events, Actions and Documentation share one 24px section bar: chevron, skin-aware icon, title in the theme text colour, and a subtle accent count pill. The open state shows in the chevron and a slightly stronger tint; the body has no extra box, just an inset and a faint accent guide line. Small groups (below **Fields per collapsible section**) get a plain title row with a short accent rail.
- **Switches** - booleans are drawn as compact switches at the start of the value column, like Unity's own toggle. Disabled switches are dimmed; mixed multi-object values show a dash.
- **Actions** - a parameterless method is a single gradient button with its signature in the tooltip. A method with parameters adds the "N params" chip; the parameter rows hide their type hint below 260px of width so the label and the field keep room.
- **Documentation** - the preview of the linked page (headings, bold, `inline code`, links and bullets rendered in skin-aware colours) with an **Open in window** button. `[NeoDoc]` links are inherited: a subclass of a documented component shows its base page. A `[NeoDoc]` that points to a missing page is reported as a warning instead of "No documentation linked", also when the type's own link is broken and an inherited or `TypeName.md` page is shown below the warning.
- **Mascot health ("slime linter")** - the mascot reflects the inspected component's health and doubles as a shortcut:
  - *Faces* - neutral/blink when healthy, worried on missing object references, angry on console errors or NaN/Infinity float fields, a brief surprised reaction when a new error appears, and a "watching" face in Play Mode.
  - *Play Mode gaze* - the watching face looks toward the Game view: when the Game view sits to the left of the inspector the face is mirrored horizontally. The Game view position is rescanned at most once per second.
  - *Console memory* - errors are attributed to the component type by parsing the stack trace and remembered for the session (`SessionState`, survives domain reloads). A count badge on the mascot (tooltip: number of issues) opens a compact issue list with a **Clear** action.
  - *Console mirroring* - whenever the console error count drops to zero (Clear button, Clear on Play, clear before recompile), every remembered error is wiped and inspectors repaint.
  - *Validation* - a cached, throttled, property-capped `SerializedObject` scan flags missing references and invalid numbers; it runs only for the object currently inspected.
  - *Click* - poking the mascot plays a damped-spring bounce and a laughing face. It is decorative for now.

All chrome is wrapped in exception guards: a failure inside decorative drawing never breaks the property layout below.

---

## 4. Animation and performance

The mascot (breathing, blink, pop), the flowing spectrum and the UI transitions are configured in **Neoxider > Visual Settings > Animation**:

- **Animate inspector** - master switch. Off draws the header as one still frame and turns every transition into an instant change. Status and live-data refreshes described below remain active.
- **Animate in Play Mode** - off by default. The game already competes with the editor for time, so the inspector stays still while the game runs.
- **Smooth transitions** - eased motion for sections opening and closing (height and fade, 0.18 s, ease-out cubic), hover highlights on section bars, pills, chips and buttons (0.12 s fade-in), the switch knob sliding (0.16 s, ease-in-out) and pressed buttons (darker fill, label one pixel lower). Follows the master switch and the Play Mode rule.
- **Max FPS** (5-60, default 30) - upper limit for decorative animation repaints. Transitions repaint every frame for their fraction of a second.

All motion is time-based (frame-rate independent). A section's openness is computed from one clock sampled at the Layout event of each IMGUI frame, so Layout and the following Repaint lay out the same height and nothing jumps.

In code: `CustomEditorSettings.EnableAnimations`, `AnimateInPlayMode`, `AnimationFps`, `EnableTransitions` and the matching setters. The header and frame toggles (`EnableRainbowSignatureAnimation`, `EnableRainbowLineAnimation`) still switch their own effect; the master switch overrides all of them.

What keeps refreshing with animation off: the header status (console-error and missing-reference badge) polls at 2 Hz, and content that is live data - update-check progress, the `NeoCondition` result - refreshes at 10 Hz.

**For custom editors deriving from `CustomEditorBase`:**

- `EnsureRepaint()` - decorative animation. Does nothing while animation is off (setting, or Play Mode without *Animate in Play Mode*) and is capped at *Max FPS*.
- `EnsureLiveRepaint()` - data that must stay current whatever the animation setting says (10 Hz).

Both are short leases renewed by every draw and served by one shared ticker, so a hidden or collapsed inspector stops repainting on its own and N components on a GameObject do not run N repaint loops. While Unity is in the background (another application has focus) the decorative animation idles, but live data (10 Hz) and the header status (2 Hz) keep refreshing, so a `NeoCondition` result or an update check stays current on a second monitor. When the editor is focused again every inspector that was waiting gets one repaint and picks up where it left off.

Header decorator suppression is scoped to one property draw. If a custom drawer throws or exits IMGUI, the exception propagates after restoring the decorators; the callback is not retried, so edits and callbacks are not applied twice.

**What is cached per type** (instead of reflected on every IMGUI event): the method list and `[Button]` metadata, each action row's parameters, labels, tooltip and session keys, `[Header]` titles and field-by-property-path lookups (private fields of base classes included), foldout keys of sections, Events, Actions and Documentation, the `Neo` namespace check, the resolved doc page, broken links and the rich-text preview (doc caches are dropped on `EditorApplication.projectChanged`), and the injection-attribute field lists used by `ComponentDrawer` / `ResourceDrawer`. The update-check state is read from `SessionState` once and kept in memory until a check writes it.

**What is cached per editor:** the *property plan* - the visible top-level properties, their `[Header]` sections, the UnityEvents and the replication flags - is resolved once; later passes reuse the same `SerializedProperty` objects (the pattern Unity's own editors use) instead of walking, copying and naming every field on every event. Event listener counts, previews and labels are recounted on a Layout event only after the object's dirty count changed (and at most once a second otherwise).

**What is reused per event:** every `GUIStyle` and layout option of the chrome lives in `NeoInspectorStyles`, built once per editor skin; tooltips, version and count texts are cached strings. A warm Repaint of the whole chrome (header, pills, documentation bar, card, spectrum frame) is meant to allocate nothing; `InspectorVisualPassTests` checks it with the shared calibrated `GcAllocCounter` (the native GC.Alloc sampler) inside a real IMGUI pass. Layout passes still allocate Unity's own layout entries, and `PropertyField` allocates inside Unity for every field it draws.

**Attribute lookups on the inspector path:** `NeoCustomEditor` runs `ComponentDrawer` / `ResourceDrawer` through two independent throttles (`InjectionLookupThrottle.InspectorComponents` and `InspectorResources`, so a field with both a component and a Resources attribute still gets its Resources fallback). A `[FindInScene]` or `[LoadFromResources]` lookup that found nothing is not repeated on every IMGUI event; it runs again after a hierarchy or project change (so a newly added object or asset is still picked up on the next draw) and at least every 0.5 s. The public `ProcessComponentAttributes` / `ProcessResourceAttributes` keep their unthrottled behaviour.

---

## 5. Rule for decorative drawing: control ids must not depend on the event

IMGUI control ids are a running counter that Unity compares across events (the press on MouseDown, the pressed look on Repaint, keyboard focus, text selection). A call that takes an id (`GUI.BeginGroup`, `GUI.Box`, `GUI.Button`, `EditorGUI.LabelField`, any `EditorGUILayout` / `GUILayout` call) inside an `Event.current.type == EventType.Repaint` branch shifts every control after it on Repaint only: a click is drawn one control up and text fields lose their selection highlight. Keep such calls outside the branch and leave only pure drawing inside it (`GUI.DrawTexture`, `GUI.Label`, `EditorGUI.DrawRect`, `Handles`, `NeoInspectorTheme.Draw*`). Header elements that appear and disappear with state (health badge, update pill) take no control id at all: they handle their click and tooltip without one. `InspectorControlIdStabilityTests` enforces the rule with a source scan and with a real IMGUI pass over the header.

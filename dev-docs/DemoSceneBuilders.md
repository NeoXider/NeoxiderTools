# Demo Scene Builders (DemoSceneKit)

How NeoxiderTools demo scenes are authored. Audience: agents and developers converting or creating demo scenes in `Assets/Neoxider/Samples~/Demo` (and `Samples~/NeoxiderPages`).

## Why

Demo scenes must not be assembled by a runtime bootstrap. Everything a demo shows - UI, objects, configured components, persistent UnityEvent listeners, NoCode / NeoCondition bindings - is saved in the `.unity` file, so the wiring is visible in the Inspector without entering Play Mode. The look stays the bright NeoDemoShell look (dark accent gradient, header with accent underline, rounded content card, log feed, info card). Never IMGUI.

Scenes are not hand-edited YAML and not clicked together by hand: every converted scene has a **builder**, a static editor method that authors the scene through the kit. Re-running a builder reproduces the same scene (idempotent), so the builder is the source of truth and the saved scene is its output.

`NeoDemoShell.Build(...)` and `ModuleDemoSceneInfo.Show(...)` / `NeoDemoShell.ShowInfoCardOnce(...)` still work but are `[Obsolete]` fallbacks. A converted demo must not call them.

## Where things live

| What | Path (release layout `Samples~`; during development the folder is `Samples`) |
|---|---|
| Kit (editor assembly `Neo.Demo.Editor`) | `Samples~/Demo/Editor/Kit/*.cs`, asmdef `Samples~/Demo/Editor/Neo.Demo.Editor.asmdef` |
| Builders | `Samples~/Demo/Editor/Builders/<Module>/<Scene>Builder.cs`, namespace `Neo.Demo.Editor.Builders` |
| Group helpers on top of the kit | `Builders/Move/WorldDemoBuild.cs` (world demos: right card, inline log, top-left info), `Builders/GridSystem/GridSystemDemoStage.cs`, `Builders/Bonus/BonusDemoStage.cs`, `Builders/Abilities/AbilitiesDemoBuild.cs`, `Builders/NoCode/LogicDemoBuildUtility.cs`, `Builders/Rpg/RpgDemoBuilderUtility.cs`, `Builders/Shop/ShopNpcBuildSupport.cs`, `Builders/Random/ToolsDemoBuildUtility.cs` |
| Kit tests (`Neo.Demo.Editor.Tests`) | `Samples~/Demo/Editor/Tests/` (`DemoTestScenes` opens temporary scenes) |
| Runtime pieces (assembly `Neo.Rpg.Demo`) | `Samples~/Demo/Scripts/Shell/` (`DemoLogFeed`, `DemoValueReadout`, `DemoGeneratedRoot`, `DemoPalette`, `DemoPointerGate`), `Samples~/Demo/Scripts/ModuleDemoSceneInfo.cs` |
| Kit sprites (PNG assets, never runtime textures) | `Samples~/Demo/Sprites/DemoKit/` (`DemoRoundedRect`, `DemoGradient`, `DemoDisc`, `DemoRing`, `DemoGlow`, `DemoWhite`) |
| Controllers | `Samples~/Demo/Scripts/Shell/<Module>DemoController.cs` |
| Exemplar | builder `Editor/Builders/Settings/SettingsDemoBuilder.cs`, controller `Scripts/Shell/SettingsDemoController.cs`, scene `Scenes/Settings/SettingsDemo.unity` |
| Decorated hand-authored game | builder `Editor/Builders/Cards/CardGameBuilder.cs` (info card canvas with accent, corner live panel, owned listeners on the game's events) |
| Hand-authored NoCode example games | builders `Editor/Builders/ExampleGames/*Builder.cs` (`ClickerExampleBuilder`, `Shooter2DBuilder`, `AdventureBuilder`): `ExampleGameGuide.AuthorInfoCard` = info card + input-module upgrade, hints only for objects that exist; `DemoSaveKeys` sets demo save keys. The games themselves are never rebuilt. |
| Decorated NeoxiderPages scene | builder `Editor/Builders/NeoxiderPages/PagesUIBuilder.cs` (plain uGUI card without Demo-assembly components, see below) |

`Neo.Demo.Editor` references `Neo.Rpg.Demo`, every runtime module assembly, `Unity.TextMeshPro`, `UnityEngine.UI` and `Mirror`. If a builder needs another assembly, add one line to its asmdef.

The sample assemblies only exist while the sample is visible (`Samples`, or imported under `Assets/Samples/NeoxiderTools/<version>/Demo Scenes`). Never reference them from `Neo.Editor.Tests` / `Neo.Tests.Play`; use reflection with `Assume`/`Assert.Ignore` there, or put tests into `Neo.Demo.Editor.Tests`.

The NeoxiderPages sample must work when it is imported alone, so its scenes may not contain components of the Demo sample assembly (`DemoGeneratedRoot`, `DemoLogFeed`, `ModuleDemoSceneInfo`...). The runner builds `DemoSample.NeoxiderPages` scenes with **marker-less roots** (see below) and fails validation when a Demo sample component ends up in such a scene.

## The pattern in 30 lines

```csharp
using Neo.Samples;
using Neo.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    internal static class SettingsDemoBuilder
    {
        [DemoSceneBuilder("Scenes/Settings/SettingsDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            // Hand-authored objects are found; generated ones are created only when missing.
            GameSettingsComponent service = ctx.FindOrCreate<GameSettingsComponent>("Services", "Game Settings Service");
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            SettingsDemoController controller = ctx.FindOrCreate<SettingsDemoController>("Services", "Demo Controller");

            DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.Settings", new Color(0.98f, 0.72f, 0.30f)));
            DemoPanel card = frame.Content;

            TMP_Text fpsValue = card.ValueLabel("Framerate cap");
            Button[] fps = card.ButtonRow("30 fps", "60 fps", "Unlimited");
            DemoWiring.AddInt(fps[0].onClick, controller.SetFramerateCap, 30);
            DemoWiring.AddInt(fps[1].onClick, controller.SetFramerateCap, 60);
            DemoWiring.AddInt(fps[2].onClick, controller.SetFramerateCap, -1);

            Slider slider = card.Slider("Mouse sensitivity", 0.1f, 10f, 2f);
            DemoWiring.AddDynamic(slider.onValueChanged, service.SetMouseSensitivityForMenu); // NoCode path

            frame.InfoCard(info, new DemoInfoContent { Title = "Neo.Settings", Purpose = "..." });

            ctx.Set(controller, "_framerateValue", fpsValue); // serialized references, no runtime lookup
            ctx.Set(controller, "_log", frame.Log);
        }
    }
}
```

The controller only has `[SerializeField]` references to the authored objects and **public instance methods** that the persistent listeners call. It builds nothing at runtime.

## How a build pass works

1. The runner opens the scene and creates a `DemoSceneBuildContext` (`Begin` snapshots every object under existing generated roots and makes the scene active). When the builder's `.unity` does not exist yet, `RebuildAll` (and `DemoSceneBatch.RebuildAll`) first creates it with `DemoSceneRunner.TryCreateSceneAsset(path, out error)`: an `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)` saved at the builder path, with missing folders created through `AssetDatabase.CreateFolder` (so they get their `.meta`). A new builder therefore needs no hand-made scene file; the summary notes `Created the missing scene asset ...`. If that first build fails, the empty scene stays on disk and the next pass reuses it. Commit the new `.unity`, its `.meta` and any new folder `.meta` with the builder.
2. Every builder registered for the scene runs (ordered by `Order`, then id). A builder that throws is reported (and its root cause logged with `Debug.LogException`); the others still run.
3. `Finish` prunes and marks dirty:
   - **Generated roots** are objects carrying the `DemoGeneratedRoot` marker (top level: `[Demo] <Key>`), or - in scenes built with `ctx.UseRootMarkers = false` (NeoxiderPages) - plain objects whose name starts with the reserved prefix `[Demo] `. Roots not requested in this pass are deleted.
   - Under each generated root, every pre-existing object the builders did not touch is deleted (stale rows, renamed controls).
   - Objects outside generated roots are **hand-authored** and never pruned or renamed. A builder deletes one only on purpose with `ctx.Retire(go, reason)`.
4. Validation (any error = the scene is **not saved**, the file on disk stays unchanged):
   - missing scripts under generated roots;
   - persistent listeners without a target or with an unknown method - on every component under generated roots, on the events of hand-authored components re-owned with `ctx.OwnListeners` / `ctx.TrackListeners` (there an unknown method on the author's own call into a hand-authored object is only a warning), and on whole components registered with `ctx.ValidateListeners`;
   - runtime-generated sprites/materials (they would be lost on save) unless the holder was allowed with `ctx.AllowRuntimeAsset`; Filled images without a sprite;
   - NeoxiderPages scenes: components of the Demo sample assembly anywhere in the scene.
   - Warnings (scene is still saved): characters a static TMP atlas lacks (for example U+2192), scene `NetworkIdentity` objects with `sceneId` 0 under Mirror, `ModuleDemoSceneInfo` without an authored card, objects created under a generated root without the context.
5. The scene is saved and a summary line is logged: `[DemoSceneKit] OK   <scene> | builders 1 | +N created, M reused, -K removed | listeners L | saved | T ms`. `Removed` also counts objects retired with `ctx.Retire`.

### Matching rules (why rebuilds are idempotent)

- `ctx.Child(parent, name)` / `ctx.UiChild(parent, name)` return the **first child with that name not yet used in this pass**, create it when missing, and move it to the next sibling index (call order = hierarchy order).
- Names are the identity. Kit rows are named `<Kind> · <label>` (`Slider · Mouse sensitivity`, `Buttons · Low | High`); buttons are `Btn_<label>` (play-mode tests click demo buttons by that name). Changing a label creates a new object and prunes the old one, which is correct.
- `ctx.Reuse` / `Attach` / `Prefab` / `Primitive` / `Keep` mark the object as used and its whole subtree as **kept** (never pruned). Kept descendants stay matchable, so `ctx.Child(reusedPrimitive.transform, "Body")` finds the existing child on the next pass instead of creating a duplicate.
- `ctx.Ensure<T>(go)` gets or adds a component. The first time a component **on a generated object** is ensured in a pass, all of its persistent UnityEvent listeners are cleared, so builders simply add their wiring again every run. Serialized UnityEvent fields that are still null (fields without an initializer on a freshly added component) get an empty event, so `DemoWiring` can wire them at once.
- `DemoWiring.Add*` never adds an identical call twice (same target, method, mode, argument), which also keeps wiring on hand-authored objects idempotent.
- Objects created outside the context (`new GameObject`, `Instantiate`) under a generated root are kept but reported as warnings; route them through `ctx.Attach(created, parent, name)` instead.

### Idempotency rules for builders

- Do: derive every value from code constants or assets; set values (`x = 5`), never accumulate (`x += 5`, `list.Add` without clearing).
- Do: create data assets with `DemoSceneAssets.EnsureAsset<T>("Data/<Module>/<Name>.asset", a => a.Configure(...))` instead of `ScriptableObject.CreateInstance` at runtime; prefabs with `DemoSceneAssets.EnsurePrefab`, render textures with `DemoSceneAssets.EnsureRenderTexture`.
- Do: use `ctx.FindOrCreate<T>(rootKey, name)` for scene services so hand-authored instances win.
- Do: re-own events of hand-authored components before wiring them: `ctx.OwnListeners(component, component.OnX)`, then `DemoWiring.Add*`.
- Do not: parent generated children under hand-authored objects with `ctx.Child` (it throws); use `ctx.RootUnder(handAuthoredParent, key)` to own a generated subtree there.
- Do not: call `DemoSceneKit.Frame` twice in one scene or reuse a root key for unrelated content.
- Do not: wire lambdas or static methods - persistent listeners need public instance methods on a component or asset (`DemoWiring` throws with a clear message). Property setters go through `DemoWiring.AddSetter` / `DemoWiring.Setter`.
- Do not: generate textures/sprites/materials in code for the scene (validation fails). Use `DemoSceneAssets` or project assets.
- Do not: name hand-authored objects `[Demo] ...`; the prefix is reserved for generated roots.
- Do: persisting demo components use `NeoDemo.<Demo>.<Data>` save keys, set with `DemoSaveKeys.Use(ctx, component, field, key)` (a hand-authored component included). `SampleSceneSaveKeyTests` (Neo.Editor.Tests) fails on library default keys; its `KnownOffenders` list may only shrink.
- Do: keep builder texts inside the static TMP atlas (LiberationSans SDF): write `->` instead of U+2192; validation warns about missing characters.

### Decorating hand-authored scenes

Many demos are a hand-authored game plus a generated guide (CardGame, the GridSystem boards, the Tools world demos, RPG). The kit covers the recurring steps:

| Need | API |
|---|---|
| Find a hand-authored top-level object | `ctx.FindHandAuthoredRoot("Canvas")` |
| One-time migration of a pre-kit layout or bootstrap leftover | Check its known shape, then `ctx.Retire(go, "why")` (counted in `Removed`, noted, refuses generated objects and objects of this pass). |
| Wire generated objects to events of hand-authored components | `ctx.OwnListeners(owner, owner.OnX [, alsoOwned...])` removes calls whose target is missing, generated or one of `alsoOwned` (e.g. the demo's hand-authored controller), keeps the author's other calls, and registers the event for validation. `DemoWiring.PruneOwned` / `PruneWhere` do the pruning alone. |
| Validate a whole hand-authored component's listeners | `ctx.ValidateListeners(component)` |
| An `[ExecuteAlways]` adapter puts a runtime sprite/material on a component | `ctx.AllowRuntimeAsset(holder, reason)`; prefer pointing the holder at a persistent asset when one exists (see `UIMeshRigDemoBuilder.RestoreSourceSprite`). |
| Legacy `StandaloneInputModule` on a hand-authored EventSystem (throws in Input-System-only projects) | Automatic with `DemoFrameOptions.UpgradeLegacyInputModule` (default on), `InfoCardCanvas(..., upgradeLegacyInputModule: true)`, or `DemoSceneKit.UpgradeLegacyInputModule(ctx, eventSystem)` / `UpgradeLegacyInputModules(ctx)`. A note in the summary records the swap. |
| Info card plus panels next to the game without the full frame | `DemoSceneKit.InfoCardCanvas(ctx, info, content, rootKey, accent)`, then `DemoSceneKit.CornerPanel(ctx, (RectTransform)ctx.Root(rootKey, true).transform, name, placement, width, accent, offset, raycastBackground: false)` |
| Demo backdrop behind a hand-authored canvas | `DemoSceneKit.Backdrop(ctx, parentRect, accent, alpha)` (child) or `DemoSceneKit.StyleBackdrop(ctx, hostRect, accent)` (the host itself) |
| World input must ignore presses on the demo UI | `frame.GateWorldInput(drawer, mover, ...)` (`DemoPointerGate` on `[Demo] Services`) |
| Steps that name the scene in errors | `DemoSceneKit.Step(scene, "find the player", () => ...)` -> `[scene] step 'find the player' failed (Type): message` |

**NeoxiderPages scenes** (`PagesUIBuilder`): the scene must keep working when the Pages sample is imported without
Demo Scenes, so the card uses no Demo-assembly component and no Demo sprite. Build it as plain uGUI under
`ctx.Root(key, true)` (marker-less root), take images from `DemoSceneAssets.BuiltinStandard` instead of the kit
`Image` / `AccentButton` helpers, wire the collapse button with `DemoWiring.AddBool(onClick, go.SetActive, ...)`,
and bind the Pages types (compiled into `Assembly-CSharp`, which the kit cannot reference) with
`Delegate.CreateDelegate` by method name. Give the scene's `Money`, `ScoreManager` and `LevelManager` demo keys
(`NeoDemo.Pages.*`) and match the scene's own `CanvasScaler` (portrait 1080x1920, match width).

## API cheat sheet

### Registration and runner

| API | Purpose |
|---|---|
| `[DemoSceneBuilder("Scenes/X/Y.unity", Sample = DemoSample.Demo, Order = 0)]` | Registers `static void Build(DemoSceneBuildContext ctx)` (or parameterless). Path is relative to the sample root; an `Assets/...` path is used as is. `DemoSample.NeoxiderPages` scenes get marker-less roots. |
| `DemoSceneRunner.RebuildAll(filter, authorMissingInfoCards)` | Open, build, prune, validate, save every registered scene (or the filtered ones). |
| `DemoSceneRunner.AuthorMissingInfoCards(filter)` | Only the info-card pass over scenes without a builder. |
| `DemoSceneRunner.BuildOpenScene(scene, builders)` | Build an already open scene without saving (tests, tools). |
| `DemoSceneRunner.BuildOpenScene(scene, ctx => ..., useRootMarkers)` | Same with a delegate instead of a registered builder (kit tests, one-off tools). |
| `DemoSceneRunner.DiscoverBuilders()` / `FindBuildersForScene(path)` | Registry. |
| `DemoSceneCapture.CaptureScenes(filter, dir, w, h)` / `TryCapture(scene, png, w, h, ...)` / `Render(scene, w, h)` | Game view PNGs without Play Mode (see Capturing). |
| `DemoSampleRoots.Demo`, `Resolve(DemoSample)` | Active sample root (`-neoDemoRoot` override, kit asmdef location, `Assets/Neoxider/Samples/Demo`, imported `Assets/Samples/NeoxiderTools/<version>/Demo Scenes`). |

Menu (`Neoxider/Demo/...`): **Rebuild All Demo Scenes**, **Rebuild Active Demo Scene**, **Author Info Card In Active Scene** (scenes without a builder), **Author Missing Info Cards (All Demo Scenes)**, **Capture Active Scene (Game View PNG)...**, **Capture All Demo Scenes (Game View PNGs)...**, **List Scene Builders**.

### DemoSceneBuildContext (`ctx`)

| Member | Purpose |
|---|---|
| `Root(key, ui)` | Top-level generated root `[Demo] key` (`ui: true` adds RectTransform + UI layer). |
| `RootUnder(parent, key, ui)` | Generated root under a hand-authored parent. |
| `UseRootMarkers` | `true`: roots carry `DemoGeneratedRoot`. `false`: marker-less roots named `[Demo] key` (set by the runner for NeoxiderPages scenes; set it before the first root). |
| `Child(parent, name)` / `UiChild(parent, name)` | Ensure child by name (parent must come from this pass, kept subtrees included). |
| `Ensure<T>(go)` / `Ensure(go, type)` | Get or add component (clears its persistent listeners once per pass on generated objects; fills null UnityEvent fields). |
| `Reuse(parent, name)` + `Attach(created, parent, name)` | For objects made by factories (TMP_DefaultControls, `GameObject.CreatePrimitive`). |
| `Prefab(parent, name, prefabAsset)` / `Primitive(parent, name, type)` | Prefab instance / primitive child, reused across passes. |
| `Keep(go)` | Mark a subtree as produced by this pass (never pruned; descendants stay matchable). |
| `Find<T>(includeGenerated)` / `FindAll<T>()` / `FindOrCreate<T>(rootKey, name)` / `FindHandAuthoredRoot(name)` | Scene queries (hand-authored first). |
| `Retire(go, reason)` | Delete a hand-authored object on purpose (counted in `Removed`, noted). |
| `OwnListeners(owner, evt, alsoOwned...)` / `TrackListeners(owner, evt)` / `ValidateListeners(component)` | Listener ownership and validation on hand-authored components. |
| `AllowRuntimeAsset(holder, reason)` | Skip the runtime-asset check for a component an adapter rebuilds. |
| `Set(target, "_field", value)` | Serialized field setter (refs, numbers, bool, string, enum, color, vectors, arrays). Nested paths work: `"_binding._memberName"`, `"_maps.Array.size"`, `"_maps.Array.data[2]._id"` (set the size first). Throws listing the available fields when the name is wrong. A GameObject value resolves to the matching component. |
| `MarkModified(obj)` | Changed through the C# API; makes sure it is saved (records prefab overrides). |
| `Note(msg)` / `Warn(msg)` | Lines in the scene summary. |
| `IsGenerated(go)` / `IsGeneratedTarget(obj)` / `HostOf(obj)` / `IsGeneratedRoot(go)` / `TryGetRootKey(go, out key)` (static) | Generated-or-authored checks for persistent-call targets and roots. |

### DemoSceneKit and DemoFrame

| API | Purpose |
|---|---|
| `DemoSceneKit.Frame(ctx, new DemoFrameOptions(title, accent) { ... })` | Camera (hand-authored kept, else `[Demo] Camera`), EventSystem (hand-authored kept and its legacy module upgraded, else `[Demo] EventSystem`), `[Demo] Frame` overlay canvas 1920x1080 ScaleWithScreenSize, backdrop, header, content card, log feed. |
| `DemoFrameOptions` | `Backdrop` (Gradient / Dim / None - use None or Dim when the world must stay visible), `Placement` (Center / Left / Right), `ContentWidth`, `ContentOffsetY`, `ContentCard` (off for HUD-only game demos), `Header`, `HeaderWidth`, `LogFeed`, `LogWidth`, `LogLines`, `SortingOrder`, `InputModule`, `UpgradeLegacyInputModule`, `Camera`, `EventSystem`, `RootKey`, `Overline`. `HeaderWidth` / `LogWidth` default to `ContentWidth + 40`; set them when a narrow side card should not narrow the header and log. |
| `frame.Content` | Main `DemoPanel` (null with `ContentCard = false`). |
| `frame.Panel(name, DemoPanelPlacement, width, offset)` | Extra floating card (TopLeft, TopRight, BottomLeft, BottomRight, Left, Right, Center). |
| `frame.Layer(name)` | Full-screen empty RectTransform for custom UI. |
| `frame.Log` | `DemoLogFeed`; assign it to the controller's `_log` field or wire `AddString(button.onClick, frame.Log.Log, "text")`. |
| `frame.InfoCard(info, content)` | Authors the info card on the frame, binds `ModuleDemoSceneInfo`, wires the collapse button to `ToggleCollapsed`, syncs the accent, saves the card in its `StartCollapsed` state. |
| `frame.GateWorldInput(behaviours...)` | `DemoPointerGate` so world input ignores presses on the frame UI. |
| `DemoSceneKit.InfoCardCanvas(ctx, info, content, rootKey, accent, upgradeLegacyInputModule)` | Info card on its own canvas (scenes without a frame). |
| `DemoSceneKit.CornerPanel(ctx, parentRect, name, placement, width, accent, offset, raycastBackground, padding, spacing)` | Frame-less card on any canvas rect. |
| `DemoSceneKit.Backdrop / StyleBackdrop / Shadow / FitHeight` | Card and backdrop pieces for custom layouts. |
| `DemoSceneKit.WorldLabel(ctx, parent, name, text, localPosition, fontSize, color, size, sortingOrder)` | World-space TextMeshPro annotation. |
| `DemoSceneKit.Readout(ctx, label, format, prefix, suffix, onText, offText)` | `DemoValueReadout` on any authored label. |
| `DemoSceneKit.Step(scene, step, action)` / `StepFailed(scene, step, exception)` | Build steps whose failures name the scene and the step. |
| `DemoSceneKit.Text / Image / AccentButton(…, labelColor) / Anchor / Stretch / SetAnchors / VerticalLayout / OverlayCanvas / WorldSprite` | Building blocks for custom layouts. Pass `labelColor: DemoPalette.Text` on saturated or dark accents. |
| `DemoSceneKit.EnsureCamera / EnsureEventSystem(ctx, module, upgradeLegacyModule) / ResolveInputModule / UpgradeLegacyInputModule(s)` | Used by `Frame`; callable directly. |

Input module: `DemoInputModule.Auto` authors `InputSystemUIInputModule` whenever the Input System package exists (a package dependency) and `StandaloneInputModule` otherwise. This deliberately differs from NeoDemoShell's runtime rule for "Both" input handling: an authored scene is imported into other projects, and `StandaloneInputModule` throws in Input-System-only projects (the Unity 6 default). Hand-authored EventSystems are upgraded the same way (see the table above).

### DemoPanel rows (in call order)

| Row | Returns |
|---|---|
| `Section(caption)` | Uppercase accent caption `TMP_Text`. |
| `Label(text)`, `Paragraph(text, height)`, `Note(text, height)` | `TMP_Text`. Their row height is fixed; give long notes a taller height. |
| `TextRow(name, initial, fontSize, height, wrap, alignment, color)` | `TMP_Text` in a row with a stable name (`Text · <name>`) for runtime-updated, possibly multi-line text. |
| `ValueLabel(caption, initial)` | Value `TMP_Text` (update it from the controller). |
| `ValueRow(caption, initial, fitOneLine)` | `DemoValueRow` (Row, Caption, Value); `fitOneLine` auto-sizes caption and value onto one line (half-width `Columns`). |
| `CompactValue(caption, initial, split, captionSize, valueSize, displayCaption)` | Value `TMP_Text` for long live values: fixed smaller fonts, caption takes `split` of the width, ellipsis. |
| `ValueReadout(caption, format, initial, onText, offText)` | `DemoValueReadout` on a value row; wire reactive events straight to it (`AddDynamic(property.OnChanged, readout.SetFloat)`). |
| `BigLabel(initial)` | Hero `TMP_Text`. |
| `Button(label)`, `ButtonRow(params labels)` | `Button` / `Button[]` named `Btn_<label>`. |
| `Toggle(label, isOn)` | `Toggle`. The Toggle component sits on the row's `Box` child: reference that GameObject from NoCode slots / ConditionEntry. |
| `Slider(label, min, max, value, wholeNumbers, format)` / `SliderRow(...)` | `Slider` / `DemoSliderRow` (Slider, Caption, Value, Readout). The value label follows the slider through a persistent `DemoValueReadout.SetFloat`; pass `format: null` to omit it. |
| `InputField(label, text, placeholder)` | `TMP_InputField` (built from `TMP_DefaultControls`, restyled). |
| `Dropdown(label, options, value)` | `TMP_Dropdown`. |
| `Bar(label, color, fill)` | Fill `Image` (anchor-driven: `SurvivorUI.SetFill` at runtime). |
| `BarRow(label, color, fill, filled)` | `DemoBarRow` (Row, Caption, Track, Fill). `filled: true` makes the fill a Filled image on the plain `DemoWhite` sprite, so a persistent listener drives it without code: `DemoWiring.AddSetter(binding.OnPercent, bar.Fill, "fillAmount")` or NoCode `SetProgress`. |
| `LogRow(lines, readyMessage, section)` | `DemoLogFeed` inside the card (world demos whose bottom edge belongs to the world: set `LogFeed = false` on the frame). |
| `Columns(name, count)` | `DemoPanel[]` side by side. |
| `Row(name, height)`, `Spacer(height)` | Custom content rows. |
| `DemoPanel.FitOneLine(text, min, max)` (static) | Single-line auto-size for any label. |

### DemoWiring (persistent listeners via `UnityEventTools`)

| Call | Listener mode |
|---|---|
| `AddVoid(evt, target.Method)` | Void (any event). |
| `AddDynamic(evt, target.Method)` | Forwards the event value (Slider float, Toggle bool, Dropdown int, InputField string). Two- and three-argument events too (`UnityEvent<string, string>` such as `StateMachineBehaviourBase.OnStateChanged`, `QuestManager.OnObjectiveProgress`). |
| `AddSetter(evt, target, "property")` / `Setter<T>(target, "property")` | Property setter like the Inspector's "float fillAmount" / "bool enabled" entries; combine `Setter` with `AddBool` / `AddFloat` for a fixed value. |
| `AddInt / AddFloat / AddString / AddBool(evt, target.Method, argument)` | Static argument. |
| `AddObject(evt, target.Method, unityObject)` | Static Unity object argument. |
| `PruneOwned(evt, owners...)` / `PruneOwned(evt, predicate)` / `PruneWhere(evt, (target, method) => ...)` | Remove builder-owned calls from a hand-authored event (prefer `ctx.OwnListeners`). |
| `Clear(evt)`, `Count(evt)`, `Describe(evt)` | Maintenance and asserts. |

NoCode bindings: wire controls straight to module components (`service.SetMouseSensitivityForMenu`, a `*NoCodeAction.Execute`, a `NeoCondition` component method) when the module already exposes the behaviour; route through the controller only when the demo needs extra logging or glue. Configure NoCode / NeoCondition components with `ctx.Ensure<T>` + `ctx.Set(...)` so their settings are saved too.

### DemoSceneAssets

| API | Purpose |
|---|---|
| `RoundedRect`, `Gradient`, `Disc`, `Ring`, `Glow`, `WhiteSquare`, `Font`, `Builtin*` | Kit sprites (PNG assets in `Sprites/DemoKit`), TMP default font, built-in UI sprites. |
| `EnsureAsset<T>("Data/<Module>/<Name>.asset", configure)` | ScriptableObject data asset, created once, configured every pass. |
| `EnsureAsset<T>(path, () => create, configure)` | Any other asset type (Material, AnimationClip...). |
| `EnsureRenderTexture(path, width, height, depth)` | Render texture asset (camera targets shown by a RawImage). |
| `EnsurePrefab("Prefabs/<Module>/<Name>.prefab", root => build, ui)` | Prefab created once (pooled spawns need a real prefab); an existing prefab is kept as is. |
| `LoadDemoAsset<T>(relativePath)` | Ready asset of the Demo sample; throws naming the path and type. |

### Runtime pieces (assembly `Neo.Rpg.Demo`)

| Type | Use |
|---|---|
| `DemoLogFeed` | `Log(string)` from code or UnityEvents; newest first, bounded (`_maxLines`), repeats collapse to `x N`, no Update, reused `StringBuilder`. |
| `DemoValueReadout` | `SetFloat/SetInt/SetBool/SetText` formats into a label from a persistent listener. |
| `ModuleDemoSceneInfo` | Info texts (existing serialized fields kept) + authored card references; `ApplyContent`, `ToggleCollapsed`, `SetCollapsed`, `SetOverlayVisible`, `Configure`. No runtime UI construction; an unauthored card logs a one-time editor warning. |
| `DemoGeneratedRoot` | Marker of generated roots (hidden from Add Component). NeoxiderPages scenes use marker-less roots instead. |
| `DemoPointerGate` | Disables world-input behaviours while the pointer is over the frame UI (`frame.GateWorldInput`). |
| `DemoPalette` | Shared colors (`Ink`, `Card`, `Track`, `Text`, `Muted`, `Accent`, `Good`, `Danger`, `Cyan`, `Warm`, backdrop helpers). |

## Converting a NeoDemoShell controller

1. Create `Editor/Builders/<Module>/<Scene>Builder.cs` with `[DemoSceneBuilder("Scenes/<Folder>/<Scene>.unity")]`.
2. Move every `_shell.Add*` call into the builder, in the same order:

| NeoDemoShell (runtime) | Builder (authored) |
|---|---|
| `NeoDemoShell.Build(title, accent, transform)` | `DemoSceneKit.Frame(ctx, new DemoFrameOptions(title, accent))` |
| `_shell.AddButtonRow(("A", () => DoA()), ("B", DoB))` | `Button[] b = card.ButtonRow("A", "B"); DemoWiring.AddVoid(b[0].onClick, controller.DoA);` |
| lambda with an argument `() => SetFps(1)` | public method with a parameter + `DemoWiring.AddInt(b.onClick, controller.SetFps, 1)` |
| `_shell.AddSlider(label, min, max, v, onChanged)` | `card.Slider(...)` + `DemoWiring.AddDynamic(slider.onValueChanged, controller.OnX)` |
| `_shell.AddToggle(label, on, onChanged)` | `card.Toggle(label, on)` + `AddDynamic(toggle.onValueChanged, ...)` |
| `_shell.AddValueLabel / AddBigLabel / AddParagraph / AddBar` | `card.ValueLabel / BigLabel / Paragraph / Bar` (or `ValueRow` / `BarRow` for all parts) + `ctx.Set(controller, "_field", result)` |
| `_shell.Log(msg)` | `[SerializeField] DemoLogFeed _log;` + `_log.Log(msg)`; builder: `ctx.Set(controller, "_log", frame.Log)` |
| `NeoDemoShell.ShowInfoCardOnce(title, text, hints)` | `frame.InfoCard(info, new DemoInfoContent { Title, Purpose, Hints })` |
| `HideBackdrop()` / `FadeBackdrop()` | `DemoFrameOptions.Backdrop = DemoBackdrop.None` / `Dim` |
| `new GameObject(...)` + `AddComponent` in Start (services, props) | `ctx.FindOrCreate<T>` or `ctx.Child(ctx.Root("World").transform, name)` + `ctx.Ensure<T>` + `ctx.Set` |
| `ScriptableObject.CreateInstance` in Start | `DemoSceneAssets.EnsureAsset<T>("Data/<Module>/<Name>.asset", configure)` |
| `OnEnable/OnDisable` -> `_shell.SetVisible`, `OnDestroy` -> `_shell.Dispose()` | Delete: the frame is a scene object. |

3. In the controller: delete the `NeoDemoShell.Context` field and all UI construction; add `[SerializeField]` references; make every listener target a `public` instance method; keep only runtime logic (sync control values with `SetValueWithoutNotify` / `SetIsOnWithoutNotify`, refresh labels from module events, log). Warn once (editor only, `global::Neo.Demo.SampleDiagnostics.Warn`) when references are missing instead of throwing.
4. Keep button labels when play-mode tests click them (`Btn_<label>`), and keep public controller methods/properties that tests call.
5. Wrap each build step in `DemoSceneKit.Step(scene, step, ...)`, so a failure names the scene and the step.
6. Compile: `python <scratchpad>/ncheck.py --tree <tree> Neo.Rpg.Demo Neo.Demo.Editor Neo.Demo.Editor.Tests`.
7. The orchestrator (single Unity operator) runs the batch entries (rebuild, then capture); check the summary line and the PNG of your scene.

## Naming conventions

| Thing | Convention |
|---|---|
| Top-level generated roots | `[Demo] <Key>` with keys `Frame`, `EventSystem`, `Camera`, `Services`, `World`, `Info Card`; one key per role, unique per scene. The `[Demo] ` prefix is reserved for generated roots. |
| Rows | `<Kind> · <label>` (made by the kit). |
| Buttons | `Btn_<label>`. |
| Builder class / file | `<Scene>Builder` in `Editor/Builders/<Module>/`, `internal static`, method `Build`. |
| Hand-authored objects | Plain names without the `[Demo]` prefix. |
| Texts | Characters of the kit font's static atlas (`·`, `›`, `»`, `×`, `•` are in it; `→` is not and is reported as a warning). |

## Running the builders

Development layout: the orchestrator renames `Assets/Neoxider/Samples~` to `Samples` so the sample assemblies compile, runs the batch entry, then renames back for release.

```text
Unity.exe -batchmode -nographics -projectPath <project> -logFile <log>
  -executeMethod Neo.Demo.Editor.DemoSceneBatch.RebuildAll
  [-neoDemoScenes "Scenes/Settings/SettingsDemo.unity;SaveDemo"]
  [-neoDemoInfoCards]
  [-neoDemoReport <path>.json]
  [-neoDemoRoot Assets/Neoxider/Samples/Demo]
```

- `-neoDemoScenes`: `;` or `,` separated; matches the full asset path, the sample-relative path, the file name or the file name without extension. An entry that matches nothing is an error.
- `-neoDemoInfoCards`: also author a standalone `[Demo] Info Card` in every Demo scene without a builder whose `ModuleDemoSceneInfo` has no authored card (keeps the info overlay of not-yet-converted scenes).
- `-neoDemoReport`: JSON with `succeeded`, global `errors` and per-scene `scene/success/saved/created/reused/removed/listeners/ms/builders/errors/warnings`.
- Exit code 0 when every selected scene built, validated and saved; 1 otherwise. Grep the log for `[DemoSceneKit]` (`OK` / `FAIL` lines and the final `SUCCESS` / `FAILED` summary).
- Interactive editor: same through the `Neoxider/Demo` menu; the runner asks to save modified scenes first and restores the open scenes afterwards. Outside batch mode the batch entry never exits the editor.

## Capturing (Game view PNGs without Play Mode)

`DemoSceneCapture` renders what the Game view shows for a scene and writes a PNG, without entering Play Mode and without saving the scene: every enabled scene camera (display 1) in depth order into one render texture (render-texture cameras first, so RawImages show their content), then every Screen Space Overlay canvas, which is pointed at a temporary capture camera for the shot (the CanvasScaler factor is computed for the shot size) and restored afterwards. Layouts and TextMeshPro meshes are rebuilt before the render. It needs a graphics device, so run it **without** `-nographics`.

```text
Unity.exe -batchmode -projectPath <project> -logFile <log>
  -executeMethod Neo.Demo.Editor.DemoSceneBatch.CaptureAll
  -neoDemoShots <output folder>
  [-neoDemoScenes "SettingsDemo;UI.unity"]
  [-neoDemoShotSize 1920x1080]
```

- Without `-neoDemoScenes` every registered builder scene is captured; with it, any scene of the Demo / NeoxiderPages samples that matches (same matching as the rebuild).
- Output: `<folder>/<SceneName>.png` (a repeated scene name gets `_2`). Exit code 0 when every scene was captured; the log has a `[DemoSceneKit] Capture SUCCESS/FAILED` summary with one `OK` / `FAIL` line per scene and warnings such as "no enabled camera".
- Interactive: **Neoxider/Demo/Capture Active Scene (Game View PNG)...** (asks for the file) and **Capture All Demo Scenes (Game View PNGs)...** (asks for a folder, restores the open scenes).
- Limits: only what is authored in Edit Mode is visible (runtime-spawned objects and Play-only animation are not); the built-in render pipeline is the reference, URP cameras render through `Camera.Render` as well.

## Tests

`Neo.Demo.Editor.Tests` (`Samples~/Demo/Editor/Tests`) covers the log feed, value readout, info card collapse, sample-root resolution, wiring validation and de-duplication, frame rebuild idempotency/pruning with a hand-authored object, the kit v2 contracts (`DemoSceneKitV2Tests`: marker-less roots, kept subtrees, `ctx.Retire`, owned and tracked listeners, NeoxiderPages validation, row parts, collapsed info card, input-module upgrade, multi-argument and setter wiring, the Game view capture) and every builder end to end in a temporary scene (built twice: zero created, zero removed, same listener count).

Add a builder test there when a builder has non-trivial wiring: open a temporary scene, call `DemoSceneRunner.BuildOpenScene`, assert `result.Success`, the serialized references and `DemoWiring.Describe(...)`.

Temporary scenes: never use `EditorSceneManager.NewScene(..., NewSceneMode.Additive)` - it throws while the test runner's untitled scene has unsaved changes (batch runs). Use `DemoTestScenes.OpenEmpty("__NeoMyTests", out _folder)` (writes an empty scene asset into a temporary folder and opens it additively), or copy a shipped scene into a temporary folder and open the copy additively; close the scene, restore the active scene and delete the folder in `[TearDown]`. A test that expects a builder to fail must `LogAssert.Expect(LogType.Exception, ...)` the root cause, because the runner logs it with `Debug.LogException`.

## Troubleshooting

| Message | Fix |
|---|---|
| `Parent '...' was not produced in this build` | Take parents from `ctx.Root/RootUnder/Child/UiChild` or kit factories. |
| `... has no serialized field '_x'. Serialized fields: ...` | Fix the field name (private serialized fields are addressed by their C# name). |
| `Persistent listeners need a method on a component or asset` | Pass a method group of a component (`controller.Save`), not a lambda or static method; property setters through `DemoWiring.AddSetter`. |
| `DemoWiring: the UnityEvent is null` | The event field has no initializer and the component was not added through `ctx.Ensure`; ensure it through the context (it fills null events) or add an initializer. |
| `Persistent listener '...' without a target on ... (… event …)` | A re-owned hand-authored event still points at a deleted object; call `ctx.OwnListeners` before wiring it. |
| `Runtime-generated sprite ... would be lost on save` | Use `DemoSceneAssets` sprites or project assets; for adapter-owned runtime assets `ctx.AllowRuntimeAsset`. |
| `... comes from the Demo sample assembly ...` | NeoxiderPages scenes may only use package and Unity components. |
| `Retire: '...' is generated` | The context prunes generated objects itself; drop the `Retire` call. |
| `No scene builder matches '...'` / `No demo scene matches '...'` | Check `-neoDemoScenes` spelling or register the builder. |
| `Scene asset not found and could not be created` | The runner creates a missing builder scene itself; this error means the path is not an `Assets/....unity` path inside the visible sample root, or the folder or save failed (the reason is in brackets). Fix the builder path. |
| `No graphics device (-nographics)` | Run captures without `-nographics`. |
| `ModuleDemoSceneInfo ... has no authored card` (warning) | Call `frame.InfoCard(info)` in the builder. |
| `Characters missing from the static font atlas` (warning) | Replace the character (for example `→` with `-` or `›`) or extend the font atlas. |
| `NetworkIdentity ... has sceneId 0` (warning) | Open and save the scene in the editor, or let the builder assign stable ids. |
| `'...' was created under a generated root without the build context` (warning) | Route the object through `ctx.Attach` / `ctx.Child`. |

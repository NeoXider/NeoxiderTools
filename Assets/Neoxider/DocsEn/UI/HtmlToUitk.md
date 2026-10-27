# HTML/CSS → UI Toolkit

HtmlToUitk converts a supported HTML/CSS subset into UXML, USS, localization keys and C# runtime metadata. Buttons, navigation, modals, tabs, media queries and animations use UI Toolkit. The player does not need a browser, JavaScript engine, CLI/Core assemblies or Neo.UIKit.

This module reuses the project's own HtmlToUitk implementation from VampireDota. The canonical CLI, conversion configuration, examples and license information live in `Tools/HtmlToUitk`. Unity code lives in `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime` and `Editor`, with separate `Neo.HtmlToUitk.Runtime` and `Neo.HtmlToUitk.Editor` assemblies.

The proposed runtime-first direction and Editor export are documented in the [roadmap](HtmlToUitkRuntimePlan.md). These are planned capabilities, not features available today.

## Repository demo

In the full Unity project, open [VeloraShowcase.unity](../../../HtmlToUitkShowcase/VeloraShowcase.unity) and press Play. The demo adapts [Velora UI SaaS](https://velora.colorlib.com/templates/saas) and showcases multiple pages, navigation, forms, animations, gradients and responsive layouts. The [demo README](../../../HtmlToUitkShowcase/README.md) documents the source HTML/CSS, conversion settings and limitations. React and arbitrary JavaScript do not run in UI Toolkit.

The scene is in the project folder `Assets/HtmlToUitkShowcase` for direct Editor use; it is not included in the UPM package or its `Samples~`.

## Editor workflow

1. Build/publish the CLI using `Tools/HtmlToUitk/README.md`. Its published assembly is `Tools/HtmlToUitk/bin/htmltouitk.dll`. .NET 9 is required by the external tool, not by the game runtime.
2. Open **Neoxider → UI → HTML to UI Toolkit**. Use **New** to create a `.htujob.json`, or **Open...** to open an existing job.
3. Set HTML/CSS inputs and the output root. Relative paths resolve from the job directory. Put output inside `Assets` when Unity should import it.
4. Validate, inspect findings, then Run. Findings include the source location, rule, explanation and suggested correction. Watch debounces input changes; only one job runs at a time.
5. Assign generated UXML to `UIDocument`, configure `PanelSettings`, add `UitkScreen` and assign required `.nav.json`, `.mq.json` and `.anim.json` files as `TextAsset` references.

The editor discovers the canonical published CLI first, then its Release build at `Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Release/net9.0/htmltouitk.dll`, compatible older assembly names, and finally `htmltouitk` on PATH. **CLI...** selects an external executable or DLL; an explicit choice takes precedence. A DLL runs through a separate `dotnet` process. Missing executables/DLLs/.NET, timeout, nonzero exit and FAIL diagnostics remain visible. Invalid or failing reports do not trigger a successful asset refresh.

Example job:

```json
{
  "schema": 1,
  "name": "menu",
  "inputs": { "html": ["pages/menu.html"], "css": ["pages/menu.css"] },
  "output": { "root": "../Assets/UI/MenuGenerated" }
}
```

Use the CLI README for the complete job and conversion configuration. Unsupported CSS/JS does not imply browser parity. Arbitrary JavaScript is not executed at runtime.

## Output contract

| File | Purpose |
|---|---|
| `.uxml`, `.uss` | Element tree and styles; move the output together to preserve relative asset URLs |
| `.mq.json` | Viewport width/height/aspect/orientation conditions and media classes |
| `.nav.json` | Pages, transitions, history, modals, tabs and focus targets |
| `.anim.json` | Keyframe definitions and onLoad/onShow/onHide/manual bindings |
| `.runtime.json` | Required component manifest; it does not execute JavaScript |
| `Generated/gradient-<hash>.png` | Baked gradient images; the CLI can configure this folder |
| `generated-assets.json` | Exact PNG paths, SHA256, dimensions and texture policy |
| `diagnostics.json` | PASS / PASS WITH WARNINGS / FAIL, counts and findings |

Runtime component identifiers remain `BreakpointWatcher`, `UitkRouter` and `UitkAnimator`. A typical screen uses one `UitkScreen` to construct the required C# objects from assigned metadata.

## Typed actions and ownership

`RegisterAction` accepts the UXML `name` or `#id`, rather than an arbitrary CSS selector. It uses native `Button.clicked` with no second submit handler. Disabled buttons do not execute actions. Binding the same name replaces the previous binding; disposing the previous token cannot remove its replacement.

```csharp
using System;
using Neo.HtmlToUitk.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MenuActions : MonoBehaviour
{
    [SerializeField] private UitkScreen screen;
    private IDisposable _play;

    private void OnEnable() => _play = screen.RegisterAction("play", Play);
    private void OnDisable() { _play?.Dispose(); _play = null; }
    private void Play(Button button) => Debug.Log("Play requested by " + button.name);
}
```

For an explicitly owned root, use `UitkActionBindings` directly:

```csharp
using System;
using Neo.HtmlToUitk.Actions;

UitkActionBindings actions = new UitkActionBindings(root);
IDisposable token = actions.Bind("save", () => Save());
IDisposable typed = actions.Bind("select", () => selectedId, id => Select(id));
actions.Rebind(replacementRoot);
// When the owner closes:
token.Dispose();
typed.Dispose();
actions.Dispose();
```

Rebinding transfers registrations to the new root and removes old callbacks. Root detach suspends actions; attach restores them. Removing a child subtree removes only its callbacks while other buttons remain active. `Refresh()` searches for replaced buttons within the same root.

`UitkScreen.Configure(UIDocument, nav, anim, media, ownResponsiveLayout)` handles document hosts. `ConfigureRoot(VisualElement, ...)` and `ReloadRoot(VisualElement)` handle external hosts. A disabled component stores its configured root until OnEnable. OnDisable releases projections, callbacks and responsive ownership; OnDestroy releases action registrations. `LastError` reports binding failures and `MissingElements` reports unresolved page/modal names.

## Document and renderer compatibility

`UitkRootConnection` is the single root/reload adapter. Unity 6000.3 observes `UIDocument.rootVisualElement`. If a supported Unity version has a `PanelRenderer` on the host, it uses its native reload subscription; Unity 6000.6 uses the versioned callback. Replacing the UI tree rebuilds the projection after releasing the previous owner.

Do not attach multiple responsive controllers/screens/watchers to the same root. If an external layout system owns responsiveness, use `ownResponsiveLayout: false` and choose one owner of media classes. No game-specific component or Neo.UIKit dependency is required.

## Media queries

```json
{
  "root": "app",
  "classes": [
    { "class": "wide", "query": { "minWidth": 1280, "minHeight": 720 } },
    { "class": "card-wide", "selector": ".card",
      "query": { "minAspect": 1.7, "maxAspect": 2.5 } }
  ]
}
```

The legacy format without `selector` applies its class to the root. The optional selector applies the class to matched elements, including a matching root. Generated USS carries the declarations and cascade rules; the controller toggles classes. Unsupported media selectors, malformed ranges and unknown orientation values are rejected.

`BreakpointController.Refresh()` uses the UI Toolkit viewport dimensions in panel coordinates. Panel/root geometry and attachment update the state. Initial zero geometry does not activate queries. Render texture or external hosts can call `ApplyViewport(width, height)` with their actual panel dimensions. Bounds are inclusive; NaN, Infinity and nonpositive dimensions are ignored. One root has one responsive owner. Disposal removes only classes that owner added and preserves existing authored classes.

## Navigation, animation and localization

`screen.Router` exposes `Navigate(pageId)`, `Back()`, `OpenModal(id)`, `CloseModal()`, `SelectTab(group, tab)` and navigation events. `WireTriggers()` uses stable UXML names from the nav spec. With backOnCancel enabled, `NavigationCancelEvent` calls Back. Page/modal focus follows declared targets and remembered focus. C# actions remain the boundary to application/game commands.

`UitkAnimator` executes supported keyframe channels through UI Toolkit scheduling. `ReducedMotion` and `SystemReducedMotion` apply skip/simplify policies. Use `PlayManual`, `Stop`, `StopAll`, `AttachRouter` and `Dispose` to own animation lifetime. Repeated AttachRouter removes prior subscriptions. Disposal stops scheduled work. Superseded/disposed router runs cannot publish late completion/focus, and transitions restore prior inline layout/transform values.

Localizable text remains a TextElement. Implement `ILocalizer.Resolve(key)`, assign `screen.Localizer` and call `Relocalize()`. `LocalizationKey` preserves the original key after translation. Unsupported text effects produce converter diagnostics; rasterizing text is not used as a hidden workaround.

## Gradient assets

The CLI defaults to baking supported linear/radial/conic gradients into PNG. Unsupported responsive bake geometry must produce an explicit diagnostic instead of stretching an incorrect image. Refer to CLI docs for the supported CSS cases. An emitted PNG alone does not prove visual fidelity.

The importer always requires `generated-assets.json` schema 1: exact path/SHA256, positive dimensions, `kind: gradient`, `sRGB: true`, `wrap: clamp`, `mipmaps: false`, including when `HtmlToUitkConfig` is present. It checks path boundaries, PNG signature and hash. A folder named Generated alone does not authorize modifying its textures. `HtmlToUitkConfig` can only narrow the manifest-proven set through managed folders or disable management; it never replaces ownership proof. Managed import uses sRGB, clamp, no mipmaps, uncompressed textures and configured filtering/alpha/Sprite-or-Default settings.

## Verification status and limits

At source-final on 2026-10-14, pure logic is **217 passed, 0 failed, 0 skipped**. Runtime/editor and all native test sources typecheck against installed Unity **6000.3.14f1** and **6000.6.0f1** managed DLLs with no errors. A compiler warning about an unassigned private serialized reducedMotion field follows standard Unity serialization. Source typechecking is separate from native import, PlayMode and visual QA.

Native fixtures cover action ownership, disabled buttons, child/root detach, repeated registration/reload, per-element media classes, stale transition completion/focus, animator subscriptions and restoration of authored styles. The disabled ConfigureRoot fixture uses actual MonoBehaviour Play Mode lifecycle and exits through its owned UnityTearDown.

Until the independent Unity operator reports the applicable receipts, native rendering, generated texture assignment, pointer/focus behavior, 1280×720 / 1920×1080 / 3440×1440 captures, player build and performance remain **unverified**. Other Unity 6.0+ versions are not claimed tested.

Reproducible source-check projects, pure-logic tests and the SHA receipt `source-final.json` are in `Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~`. Their DLL/obj outputs are not imported by Unity or shipped in the runtime. From the repository root:

```powershell
dotnet test Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/PureLogic.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.3.14f1.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.6.0f1.Tests.csproj
```

Source-check reference paths match the current Windows QA machine; configure them for another machine. Run Unity tests through Test Runner with one Editor operator. Keep reports/screenshots outside the runtime module. Accepted status requires all applicable gates rather than one passing test.

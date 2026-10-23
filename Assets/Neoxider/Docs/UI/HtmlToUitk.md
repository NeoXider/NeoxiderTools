# HTML/CSS → UI Toolkit

HtmlToUitk converts a supported subset of HTML/CSS into UXML, USS, text keys and data for the C# runtime. Buttons, navigation, modal windows, tabs, media queries and animations work through UI Toolkit. No browser or JavaScript engine is needed in the player.

The module was moved from its own HtmlToUitk into VampireDota. The canonical CLI, with its licences, configuration and examples, lives in `Tools/HtmlToUitk`. Runtime is `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime`, editor is the neighbouring `Editor` folder; these are separate assemblies `Neo.HtmlToUitk.Runtime` and `Neo.HtmlToUitk.Editor`. The Neo.UIKit dependency is optional.

## Quick start in the editor

1. Set up the CLI following the README in `Tools/HtmlToUitk`. The published file is `Tools/HtmlToUitk/bin/htmltouitk.dll`; .NET 9 is needed by the external tool, not by the game runtime.
2. Open **Neoxider → UI → HTML to UI Toolkit**. Use **New** to create a `.htujob.json`, or **Open...** to open an existing job.
3. Set the HTML/CSS inputs and the output root. Relative paths are resolved from the job folder. For Unity import the output must be inside `Assets`.
4. Run Validate, read the findings, then Run. The report names the rule, the source file and line, the reason and a suggested fix. Watch follows the inputs with a debounce; only one job runs at a time.
5. Assign the generated UXML to a `UIDocument`, set up `PanelSettings`, add `UitkScreen` and assign the `.nav.json`, `.mq.json` and `.anim.json` files you need as `TextAsset`.

The CLI is found automatically from the canonical publish path; next comes the Release build at `Tools/HtmlToUitk/src/HtmlToUitk.Cli/bin/Release/net9.0/htmltouitk.dll`, then compatible legacy names and the `htmltouitk` command in PATH. The **CLI...** button sets an external executable or DLL; an explicit path wins. The DLL runs as a separate `dotnet` process, without loading the CLI/Core DLLs into the Unity player. A missing executable/DLL/.NET, a timeout, a nonzero exit and FAIL diagnostics all show up in the result. FAIL and a failed report do not trigger a successful asset refresh.

Example job:

```json
{
  "schema": 1,
  "name": "menu",
  "inputs": { "html": ["pages/menu.html"], "css": ["pages/menu.css"] },
  "output": { "root": "../Assets/UI/MenuGenerated" }
}
```

Check the job and conversion config fields against the CLI README: unknown CSS/JS constructs get no browser-parity promise. Arbitrary JavaScript is not executed at runtime.

## Output files

| File | Purpose |
|---|---|
| `.uxml`, `.uss` | UI tree and styles; relative links survive when the output is moved as a whole |
| `.mq.json` | Viewport width/height/aspect/orientation and media query classes |
| `.nav.json` | Pages, transitions, history, modal windows, tabs, focus targets |
| `.anim.json` | Keyframe definitions and onLoad/onShow/onHide/manual bindings |
| `.runtime.json` | Manifest of required components; not automatic JavaScript execution |
| `Generated/gradient-<hash>.png` | Baked gradients; the path can be configured in the CLI |
| `generated-assets.json` | Exact PNG paths, SHA256, sizes and texture policy |
| `diagnostics.json` | PASS / PASS WITH WARNINGS / FAIL, counts and findings |

Runtime component identifiers stay the same: `BreakpointWatcher`, `UitkRouter`, `UitkAnimator`. A page usually needs a single `UitkScreen`, which creates the required C# objects from the assigned metadata.

## C# buttons and lifecycle

`RegisterAction` takes a UXML `name` or `#id`, not an arbitrary CSS selector. A native `Button` calls the handler through `clicked`; no separate second submit handler is added. A disabled button does not run its action. Registering the same name again replaces the previous one; disposing the old token does not remove the new registration.

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

Without a MonoBehaviour wrapper you can use `UitkActionBindings`:

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

One binder moves the registrations to a new root; old callbacks are removed. Detaching the root pauses the actions, attaching restores them. Removing a child subtree turns off its callbacks and keeps the other buttons. `Refresh()` looks again for replaced buttons inside the same root.

`UitkScreen.Configure(UIDocument, nav, anim, media, ownResponsiveLayout)` supports a regular document. `ConfigureRoot(VisualElement, ...)` and `ReloadRoot(VisualElement)` support an external host. On a disabled component the root is only stored until OnEnable. OnDisable removes projections, callbacks and responsive ownership; OnDestroy releases the action registrations. `LastError` reports a binding error, `MissingElements` the page/modal names that were not found.

## UIDocument and PanelRenderer

`UitkRootConnection` is the only root/reload adapter. On Unity 6000.3 it watches `UIDocument.rootVisualElement`. When a `PanelRenderer` is present, Unity versions that support it use the native reload subscription; Unity 6000.6 uses a versioned callback. Replacing the tree triggers a rebuild that releases the old owner.

Do not create several screens/watchers for one root at the same time. When an external system controls the layout, pass `ownResponsiveLayout: false` and pick a single owner for the media classes. The assembly does not require `Neo.UIKit` or any game components.

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

Without `selector` the class is applied to the root — the earlier format still works. With `selector` the class is applied to matching elements, including the root if it matches. The generated USS carries the actual values and cascade rules; the controller only toggles classes. Unsupported media selectors, invalid ranges and unknown orientation are rejected.

`BreakpointController.Refresh()` uses the UI Toolkit viewport size in panel coordinates. Panel/root geometry and attach update the state; an initial zero geometry does not activate queries. For a render texture or an external host you can call `ApplyViewport(width, height)` with the real panel size. Thresholds are inclusive; NaN, Infinity and non-positive sizes are ignored. One root has one responsive controller. Dispose removes only the classes it added and keeps the original authored classes.

## Navigation and animations

`screen.Router` exposes `Navigate(pageId)`, `Back()`, `OpenModal(id)`, `CloseModal()`, `SelectTab(group, tab)` and transition events. `WireTriggers()` takes stable UXML names from the nav spec. `NavigationCancelEvent` calls Back when backOnCancel is enabled. Focus returns through targets/history; a modal gets a focusable element. None of this replaces game logic: actions call your C# commands.

`UitkAnimator` runs the supported keyframe channels through the UI Toolkit scheduler. `ReducedMotion` and `SystemReducedMotion` apply the skip/simplify policy. `PlayManual`, `Stop`, `StopAll`, `AttachRouter` and `Dispose` let you control the lifetime. Attaching the router again removes the previous subscriptions. Dispose stops scheduled work. A cancelled or replaced router run publishes no late completion/focus; a transition restores the previous inline layout/transform values.

Localizable text stays a TextElement. Implement `ILocalizer.Resolve(key)`, assign `screen.Localizer`, call `Relocalize()`. The source keys stay in `LocalizationKey` after translation. Unsupported text effects are diagnosed by the converter; string rasterization is not used as a hidden workaround.

## Gradients and PNG import

By default the CLI bakes the supported linear/radial/conic gradients into PNG. For unsupported adaptive geometry the bake must report an explicit diagnostic instead of stretching a wrong raster. The capabilities and limits of each CSS case are listed in the CLI docs; the presence of a file alone does not prove visual accuracy.

Importer always requires `generated-assets.json` schema 1 with an exact path/SHA256, positive sizes, `kind: gradient`, `sRGB: true`, `wrap: clamp`, `mipmaps: false`, including when `HtmlToUitkConfig` is present. Path boundaries, the PNG signature and the hash are checked. A folder named Generated does not by itself give the right to change its textures. `HtmlToUitkConfig` can only narrow the confirmed manifest set through managed folders or turn texture management off; it does not replace proof of ownership. For managed import: sRGB, clamp, no mipmaps, uncompressed, configured filtering/alpha and Sprite/Default.

## Verification and current limits

On source-final 2026-10-14: pure logic **217 passed, 0 failed, 0 skipped**. Runtime/editor and all native test sources type-check against the installed Unity managed DLLs **6000.3.14f1** and **6000.6.0f1**, with no errors. This does not replace native import/PlayMode/visual QA. One compiler warning about a private serialized reducedMotion without an assignment belongs to a normal Unity serialization field.

Native fixtures cover action owner/disable/child/root detach, repeated registration/reload, per-element media classes, stale transition completion/focus, animator subscriptions, restoration of authored styles and the actual disabled MonoBehaviour ConfigureRoot. The last check enters Play Mode and exits through the owned UnityTearDown.

Until an independent Unity operator produces results, native rendering, PNG texture assignment, pointer/focus, screenshots at 1280×720 / 1920×1080 / 3440×1440, player build and performance stay **unverified**. Compatibility with other Unity 6.0+ versions is not claimed to be tested.

`Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~` holds reproducible source-check projects, a pure-logic test project and the SHA receipt `source-final.json`. Source checks use the installed Unity managed DLLs; the DLL/obj files in this directory are not imported by Unity and are not part of the runtime delivery. Run from the repository root:

```powershell
dotnet test Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/PureLogic.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.3.14f1.Tests.csproj
dotnet build Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/6000.6.0f1.Tests.csproj
```

Source check paths reflect the current Windows QA machine; on another machine configure the reference paths. Run Unity tests through Test Runner with a single Editor operator. Keep working reports/screenshots outside the runtime module; the Accepted status is defined by all required gates, not by one passing test.

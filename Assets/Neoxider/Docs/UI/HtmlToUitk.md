# HTML/CSS → UI Toolkit

HtmlToUitk converts a supported subset of HTML/CSS into UXML, USS, text keys and data for the C# runtime. Buttons, navigation, modal windows, tabs, media queries and animations work through UI Toolkit. No browser or JavaScript engine is needed in the player.

The canonical NeoxiderTools CLI, configuration and examples live in the separate `Tools/HtmlToUitk` checkout folder. Runtime is `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime`, editor is the neighbouring `Editor` folder; these are separate assemblies `Neo.HtmlToUitk.Runtime` and `Neo.HtmlToUitk.Editor`. The Neo.UIKit dependency is optional.

This is the module's primary guide. See the [editor workflow and importer contracts](HtmlToUitkEditor.md), [testing guide](HtmlToUitkTesting.md) and [11.0.0 verification](HtmlToUitkVerification11.md) for actual runs, native images and limits. Round3 executed the complete Unity assembly **370/370, no skips**, in each tested Editor, `6000.3.14f1` and `6000.6.0f1`.

## Quick start in the editor

1. Obtain the separate `HtmlToUitk.Cli.v11.0.0.zip` release delivery or build from `Tools/HtmlToUitk`. Keep its complete extracted publish folder and install the tested **.NET 10 runtime**. The CLI targets net9.0 but the portable delivery uses `LatestMajor`; no .NET 9 runtime is bundled. Source builds were validated with SDK 10.0.301. Select the extracted `htmltouitk.dll` with **CLI...**; Unity deliveries contain runtime/editor, not the external CLI.
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

Runtime metadata uses `Neo.HtmlToUitk.Json.MiniJson`. `Parse(text)` throws `JsonParseException` with a line/column; `TryParse(text, out JsonValue value, out string error)` returns a diagnostic and a null value on invalid JSON. Number syntax follows JSON: leading zeros, missing fractional/exponent digits and values outside the finite `double` range are rejected. Control characters in strings must be escaped. `Write` preserves finite numeric values, including small fractions and values close to an integer; programmatically supplied NaN/Infinity are written as `null`.

Assign valid generated metadata as `TextAsset` in the Inspector, or parse it through the C# APIs before binding a custom host. Syntax diagnostics do not replace semantic validation: `NavSpec.Validate()` reports navigation reference problems; it is useful when loading hand-authored navigation data.

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

Reentrant localization or cancellation callbacks can configure a newer projection. The newest configuration wins, including replacement on the same root; old router/responsive leases release before callback-producing animator cleanup. An obsolete continuation or failure cleanup does not clear the newer projection. This is an ownership boundary, not an atomic transaction over arbitrary application callbacks.

## UIDocument and PanelRenderer

`UitkRootConnection` is the only root/reload adapter. On Unity 6000.3 it watches `UIDocument.rootVisualElement`. When a `PanelRenderer` is present, Unity versions that support it use the native reload subscription; Unity 6000.6 uses a versioned callback. Replacing the tree triggers a rebuild that releases the old owner.

Do not create several screens/watchers for one root at the same time. When an external system controls the layout, pass `ownResponsiveLayout: false` and pick a single owner for the media classes. The assembly does not require `Neo.UIKit` or any game components.

Runtime diagnostics use the shared `NeoDiagnostics` gate from `Neo.Extensions`. Animation warnings are disabled by default; enable them with `NeoDiagnostics.Configure(warnings: true)`. Enabled warnings are emitted once per message per animator; a suppressed warning does not consume that allowance. Screen and breakpoint binding errors follow `NeoDiagnostics.Configure(errors: ...)` (enabled by default), and `LastError` remains available when console errors are disabled. Runtime also depends on `Neo.Extensions` and its `Neo.PropertyAttribute` dependency.

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

The runtime selector subset supports types, `#name`, classes, a leading `*`, comma alternatives, descendants and child `>` combinators. Every `>` needs an element selector on both sides. Repeated/trailing combinators, misplaced wildcards and conflicting IDs are rejected before a responsive controller acquires the root.

`BreakpointController.Refresh()` uses the UI Toolkit viewport size in panel coordinates. Panel/root geometry and attach update the state; an initial zero geometry does not activate queries. For a render texture or an external host you can call `ApplyViewport(width, height)` with the real panel size. Thresholds are inclusive; NaN, Infinity and non-positive sizes are ignored. One root has one responsive controller. Dispose removes only the classes it added and keeps the original authored classes.

## Navigation and animations

`screen.Router` exposes `Navigate(pageId)`, `Back()`, `OpenModal(id)`, `CloseModal()`, `SelectTab(group, tab)` and transition events. `WireTriggers()` takes stable UXML names from the nav spec. `NavigationCancelEvent` calls Back when backOnCancel is enabled. Focus returns through targets/history; a modal gets a focusable element. None of this replaces game logic: actions call your C# commands.

Navigation from an event callback supersedes the remaining work of the earlier plan. For example, redirecting from `OnTabSelected` during startup must leave only the destination page visible. The interrupted outer call returns `false`; the callback's navigation has its own result and `Current` reflects that destination. Disposing or redirecting from a hook/`OnEvent` also suppresses the superseded typed event. Closing a nested modal restores focus in the remaining modal, then restores the page's remembered focus when the final modal closes.

`UitkAnimator` runs the supported keyframe channels through the UI Toolkit scheduler. `ReducedMotion` and `SystemReducedMotion` apply the skip/simplify policy. `PlayManual`, `Stop`, `StopAll`, `AttachRouter` and `Dispose` let you control the lifetime. Attaching the router again removes the previous subscriptions. Dispose stops scheduled work. A cancelled or replaced router run publishes no late completion/focus; a transition restores the previous inline layout/transform values.

`Stop(element, jumpToEnd: true)` and reduced-motion Skip use the actual terminal timeline sample: `iterations: 0.25` stops a normal linear animation at 25%, and alternate/reverse directions still apply. Missing endpoints blend toward the underlying value captured when the run first applies that property, so subsequent frames do not feed the animation's own output back into its base value. Cancellation callbacks may stop other runs or dispose the animator. Custom cubic-bezier coordinates must be finite; invalid easing names retain the linear fallback.

Animation replacement is scoped to the element instance and animation ID. If a `Cancelled` callback starts a newer run for the same pair, the pending outer `PlayManual` returns `false` and leaves that newer run as the sole owner. Starting another element or animation ID does not supersede the pending replacement. For a live animator, `StopAll` stops the runs present at entry; a run started from a cancellation callback remains active until separately stopped or disposed.

Localizable text stays a TextElement. Implement `ILocalizer.Resolve(key)`, assign `screen.Localizer`, call `Relocalize()`. The default heuristic accepts converter-default mixed-case keys such as `app.Home.Title`, retaining numeric-prefix compatibility. A nonempty explicit `LocalizationKey` is authoritative, including custom keys such as `CUSTOM_KEY`; custom converter regexes are not automatically transported as runtime configuration. Source keys survive repeated localization and ordinary literal text stays literal. Unsupported text effects are diagnosed; text is not silently rasterized.

## Backdrop blur ownership

Add `BackdropBlurPass` beside a `UIDocument`, assign the target element name and choose Manual, OnEnable or EveryFrame refresh. The native path requires a supported Unity version and screen-space overlay panel. The fallback needs an explicit source texture or camera; it does not capture the entire screen automatically.

The same workflow is available from C#:

```csharp
using Neo.HtmlToUitk.Effects;

blurPass.Configure(document, "blur-panel", sourceCamera, sourceTexture);
blurPass.Refresh(); // Required explicitly in Manual mode and after configuring an enabled pass.
// When the effect owner closes:
blurPass.Release();
```

`Output` exposes the owned fallback render texture. `Configure` releases the previous target; `Refresh` also releases it when the document tree or named element changes. `Release` and OnDisable restore the background image/size or native backdrop value that the pass replaced and release its textures. Resolving a target without a fallback source does not give the pass ownership of its background. Camera target textures and the active render target are restored, and temporary blur textures are released through exception cleanup. Avoid several blur owners writing the same element styles.

Read the applied fallback image through `element.resolvedStyle.backgroundImage.renderTexture`. Unity 6.3's inline `style.backgroundImage` getter omits render textures. A pre-existing RenderTexture background therefore cannot be reliably captured through that getter; use a separate blur element when an external renderer owns the background. Preserving that authored RT remains a native acceptance gap.

## Gradients and PNG import

By default the CLI bakes the supported linear/radial/conic gradients into PNG. For unsupported adaptive geometry the bake must report an explicit diagnostic instead of stretching a wrong raster. The capabilities and limits of each CSS case are listed in the CLI docs; the presence of a file alone does not prove visual accuracy.

Importer always requires `generated-assets.json` schema 1 with an exact path/SHA256, positive sizes, `kind: gradient`, `sRGB: true`, `wrap: clamp`, `mipmaps: false`, including when `HtmlToUitkConfig` is present. Path boundaries, the PNG signature and the hash are checked. A folder named Generated does not by itself give the right to change its textures. `HtmlToUitkConfig` can only narrow the confirmed manifest set through managed folders or turn texture management off; it does not replace proof of ownership. For managed import: sRGB, clamp, no mipmaps, uncompressed, configured filtering/alpha and Sprite/Default.

## Verification and current limits

Current accepted results are recorded in [11.0.0 verification](HtmlToUitkVerification11.md): fresh Round2 CLI solution **2573 passed / 30 NotExecuted / 0 failed**, separate Root pure logic **291/291, no skips**, and Round3 complete Unity assembly **370/370, no skips, per Editor**. The CLI's199 real capability cases all passed using Unity6.6 managed/UnityEngine inputs; capability metadata remains distinct from native rendering.

On source-final 2026-10-14: pure logic **217 passed, 0 failed, 0 skipped**. Runtime/editor and all native test sources type-check against the installed Unity managed DLLs **6000.3.14f1** and **6000.6.0f1**, with no errors. This does not replace native import/PlayMode/visual QA. One compiler warning about a private serialized reducedMotion without an assignment belongs to a normal Unity serialization field.

Round3 includes five new runtime regressions and a real external-CLI integration case: three converter runs produce UXML/USS/nav/media/animation plus seven PNGs, followed by native ownership import/theme and Play UIDocument gradient-pixel, navigation/modal/tab/action, disable-enable and tree-replacement checks. These are actual converter/native results, not a fake bridge or source type-check substitute.

Manual OS interaction in Unity 6.3 at **1280×720** exercised Settings once → Video tab → Back → Quit modal → Cancel → public UIDocument replacement with Profile → physical Save once. Counters were settingsClicks=1/saveClicks=1 with an empty error field. This does not establish all resolutions, mobile/player builds, performance, arbitrary JavaScript or full browser/CSS parity. The30 CLI nonexecuted cases remain debt:7 validate-only,22 open goldens and1 S08 expected-diagnostic case.

`Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~` contains the portable pure-logic project and legacy diagnostic support. Four `6000.3.14f1[.Tests].csproj` / `6000.6.0f1[.Tests].csproj` projects retain machine-specific Unity references; they are not portable consumer build instructions. DLL/obj files are not imported by Unity or shipped as runtime delivery. From a full checkout with the validated SDK/runtime setup:

```powershell
dotnet test Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/PureLogic.Tests.csproj
```

Source net9 verification used process-local Major on actual.NET 10.0.9; net8 used runtime8.0.21. The portable CLI was extracted outside the repository and passed without roll-forward ENV, through its `LatestMajor` runtimeconfig. No runtime9 execution is claimed. Historical217/type-check results above retain their dated scope; release-archive export/import acceptance is separate in [Verification 11](../../Verification11.md).

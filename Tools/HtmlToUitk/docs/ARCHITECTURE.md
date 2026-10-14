# HtmlToUitk architecture (C# product)

Status: design for implementation by four parallel workers. Nothing in this document has been compiled in Unity; the capability facts in section 2 were read from the real Unity 6000.6.0f1 assemblies with a throw-away probe (`Scratch/HtmlToUitk/capprobe`), and the Python code in `oracle/python` is a working prototype of the Native tier only.

## 0. Decisions and requirements digest

| Id | Source (2026-10-08) | Requirement |
|---|---|---|
| R1 | user | Product is C#: netstandard2.1 core with no third-party dependencies, a Unity Editor package, a dotnet CLI, a runtime package. Python stays as prototype/oracle. |
| R2 | user | HTML + CSS in, UXML + USS out. USS is always generated, tokens shared, class names kebab-case with project prefixes, deterministic bytes, output passes the project lint. |
| R3 | user | Validation at the HTML stage: verdict PASS / PASS WITH WARNINGS / FAIL, numbered findings with file:line, offending construct, reason, concrete rewrite; JSON and text; non-zero exit on FAIL; project rules (keys not raw text, unique names, minimum font, touch target); live in-browser validator `uitk-validate.js`. |
| R4 | user | Everything USS cannot do must still work through tiered emulation, chosen automatically and reported: Native, ConvertTime, Runtime, Reject. |
| R5 | user | The capability matrix is DETECTED from the real editor assemblies (System.Reflection.Metadata, no Unity needed), written to `capabilities.<unity>.json`, and read by converter and validator. Supporting a newer Unity means regenerating the file. |
| R6 | user | Transitions map 1:1; keyframes through a generated spec and a runtime animator; page switching through the NeoxiderUiKit router and popups (see `Docs/Features/HtmlToUitkReuseStudy.md`). |
| R7 | user | Many HTML inputs and many style inputs, exact cascade order, conflicts reported, one UXML per page (or one document with page containers), shared USS deduplicated, partials/templates, cycle detection. |
| R8 | user | Unity window `Neoxider > UI > HTML to UI Toolkit`: input list with PASS/WARN/FAIL icons, findings with click-to-line, Convert All / Selected / Validate Only, watched folders, a committed job asset; the same job file drives `htmltouitk run job.json`. |
| R9 | user | The core lives in its own repository (this folder is a repository root); NeoxiderTools gets a thin integration. |
| R10 | root | No licence text or notes in files; no VampireDota names, paths or art in the core, tests or examples; examples and fixtures are generic invented screens. |
| R11 | root | Rule catalogue, dialect, capability data and golden fixtures are language-neutral DATA files, so any implementation must pass the same fixtures. |
| R12 | root | Unity-side code is type-checked with a csproj against the UnityEngine/UnityEditor assemblies; Unity compile and behaviour are NotRun until the Unity operator verifies. |

## 1. Goals and non-goals

Goals: a designer (or an AI agent) writes the screen as HTML in a strict dialect, previews it in a browser, gets machine-checkable answers about convertibility, and converts it to project-ready UXML + USS + string keys with emulation of the features Unity lacks.

Non-goals: arbitrary HTML pages (scripts, forms, tables), pixel-exact browser-to-Unity equality, converting running web apps, a layout engine of our own (the preview is an approximation; fidelity is measured, not claimed).

## 2. Evidence from the Unity 6000.6.0f1 assemblies

Read from `UnityEngine.UIElementsModule.dll` with System.Reflection.Metadata (enum members and the user-string heap), cross-checked with the offline manual. These facts differ from the Python oracle in places, which is exactly why detection is mandatory.

| Area | Finding |
|---|---|
| Properties | `StylePropertyId` lists the USS properties (AlignContent ... WordSpacing, plus Unity* ones such as UnityBackgroundScaleMode, UnitySliceScale/Type, UnityTextAutoSize, UnityAnimationClip, UnityMaterial). No gap, row-gap, column-gap, z-index, box-shadow, line-height, text-transform, grid, inset. |
| Selectors | `StyleSelectorRelationship`: None, Child, Descendent (no `+`, `~`). `StyleSelectorType`: Wildcard, Type, Class, PseudoClass, RecursivePseudoClass, ID, Predicate. `PseudoStates`: Active, Hover, Checked, Disabled, Focus, Root. |
| Functions | `StyleValueFunction`: Var, Env, LinearGradient, MaterialProperty and the filter functions None, Custom, Tint, Opacity, Invert, Grayscale, Sepia, Blur, Contrast, HueRotate, DropShadow. No calc, min, max, clamp. |
| Keywords | `StyleValueKeyword`: Inherit, Initial, Auto, Unset, True, False, None, Cover, Contain. |
| Enums | `Justify` includes SpaceEvenly; `Align`: Auto, FlexStart, Center, FlexEnd, Stretch; `Wrap`; `DisplayStyle`: Flex, None; `Position`: Relative, Absolute; `Overflow`: Visible, Hidden; `WhiteSpace`: Normal, NoWrap, Pre, PreWrap; `BackgroundSizeType`: Length, Cover, Contain; `Repeat`: NoRepeat, Space, Round, Repeat; `SliceType`: Sliced, Tiled; `TextAutoSizeMode`: None, BestFit. |
| Easing | `EasingMode` has 23 members (Ease, EaseIn, EaseOut, EaseInOut, Linear and the Sine, Cubic, Circ, Elastic, Back, Bounce families). No cubic-bezier, steps. |
| Animation | `-unity-animation-clip` and `animation-play-state` exist as properties (semantics not verified here; they take Unity AnimationClip assets, not CSS keyframes); there is no @keyframes. |
| Filters | `filter` and `backdrop-filter` exist. Manual: `backdrop-filter` renders only on panels in Screen Space Overlay mode, not World Space. |
| Gradients | `linear-gradient` exists as a USS function in the assembly and renderer (GradientSettingsAtlas); the manual does not document it. Record as Native with `evidence: assembly-only` and verify on a screenshot. Radial and conic do not exist. |
| Units | Manual: px and % only for lengths; deg, s, ms for angles and times. |
| Panel | PanelSettings scale mode, reference resolution and match come from the asset (`m_ScaleMode`, `m_ReferenceResolution`, `m_ScreenMatchMode`, `m_Match`); the converter reads them for viewport-unit emulation. |

Verified on this machine while writing this document: .NET SDK 9.0.315; `System.Reflection.Metadata` reads the UIElements module without Unity; a throw-away project with `Microsoft.NET.Test.Sdk`, `NUnit` 4 and `NUnit3TestAdapter` 4 restores and passes `dotnet test`; the Unity assemblies for the type-check project are `Managed/UnityEngine/*.dll` (modules such as `UnityEngine.UIElementsModule.dll`, `UnityEditor.*Module.dll`) and `Managed/UnityEditor.dll`.

## 3. Tiers and the capability matrix

### 3.1 Tiers

| Tier | Meaning | Output | Validator verdict contribution |
|---|---|---|---|
| Native | Maps 1:1 to UXML/USS the target Unity supports. | USS/UXML | none (INFO at most) |
| ConvertTime | The converter rewrites DOM and style at conversion time so plain UXML/USS reproduces the intent. | UXML/USS plus generated classes, elements, textures | Warning when the result is an approximation, Info when exact |
| Runtime | Needs a small helper from the runtime package (animator, breakpoint watcher, blur pass, router). | UXML/USS plus a JSON spec and a runtime component reference | Warning "requires runtime component X" |
| Reject | Cannot be represented; the diagnostic carries a concrete rewrite. | nothing | Error (FAIL) |

The tier of every construct is decided by `TierPlanner` from three inputs: the construct key (for example `prop:display=grid`), the capability matrix, and the config (`emulation` overrides, `strictNative`, `allowRuntime`). The same construct can be Native on one Unity version and ConvertTime on another (for example `gap` once Unity adds `row-gap`).

### 3.2 Capability detection tool (`HtmlToUitk.Capabilities.Reader`)

A `net8.0` console project. It opens an assembly with `PEReader`/`MetadataReader` (no Unity, no reflection loading), reads enum members of the named types and the user-string heap, and writes `capabilities.<unity-version>.json`. Input: `--unity-managed <dir>` (default: the Editor's `Data/Managed/UnityEngine`) plus optional `--overrides spec/capabilities.overrides.json`.

Types read: `StylePropertyId`, `StyleSelectorRelationship`, `StyleSelectorType`, `PseudoStates`, `StyleValueFunction`, `StyleValueKeyword`, `FilterFunctionType`, `EasingMode`, `Justify`, `Align`, `Wrap`, `DisplayStyle`, `Position`, `Overflow`, `OverflowClipBox`, `WhiteSpace`, `TextOverflow`, `TextOverflowPosition`, `Visibility`, `BackgroundSizeType`, `Repeat`, `SliceType`, `TextAutoSizeMode`, `AnimationPlayState`, `BackgroundPositionKeyword`, plus enums from further assemblies where a property's values live outside the UIElements module (for example the text anchor and font style enums backing `-unity-text-align` and `-unity-font-style`); the reader takes a list of assembly and type names from its own config so a new Unity version needs no code change.

Property names are derived from enum members (`BorderBottomLeftRadius` to `border-bottom-left-radius`, `Unity*` to `-unity-*`) and each derived name is confirmed against the user-string heap; unconfirmed names and exceptions (`UnityFontStyleAndWeight`) are listed in `warnings` and fixed in the overrides file. Things the assembly cannot tell (animatable or inherited, shorthand expansions, documented-in-manual flag) come from the hand-maintained `spec/capabilities.overrides.json`, which the manual's property reference table already provides (animatability and inheritance columns).

### 3.3 `capabilities.<unity>.json` format

```json
{
  "schema": 1,
  "unityVersion": "6000.6.0f1",
  "generatedFrom": [{ "assembly": "UnityEngine.UIElementsModule.dll", "sha256": "..." }],
  "properties": {
    "justify-content": { "id": "JustifyContent", "inherited": false, "animatable": "discrete",
                         "values": ["flex-start", "center", "flex-end", "space-between", "space-around", "space-evenly"],
                         "evidence": "assembly+manual" },
    "filter": { "id": "Filter", "inherited": false, "animatable": "full", "evidence": "assembly+manual" }
  },
  "shorthands": { "margin": ["margin-top", "margin-right", "margin-bottom", "margin-left"] },
  "selectors": { "combinators": [" ", ">"], "types": ["*", "type", "class", "id"], "pseudoClasses": ["hover", "active", "focus", "disabled", "enabled", "checked", "inactive", "root"] },
  "functions": { "var": true, "env": true, "linear-gradient": { "evidence": "assembly-only" }, "calc": false },
  "keywords": ["inherit", "initial", "auto", "unset", "none", "cover", "contain"],
  "filters": ["tint", "opacity", "invert", "grayscale", "sepia", "blur", "contrast", "hue-rotate", "drop-shadow"],
  "easings": ["ease", "ease-in", "ease-out", "ease-in-out", "linear", "ease-in-sine"],
  "units": { "length": ["px", "%"], "angle": ["deg"], "time": ["s", "ms"] },
  "conditions": { "backdrop-filter": "panel.renderMode == ScreenSpaceOverlay" },
  "warnings": []
}
```

`HtmlToUitk.Capabilities.Model` (netstandard2.1) loads it into `CapabilityMatrix` (section 5.9). A test asserts the checked-in `capabilities.6000.6.json` equals a fresh reader run when the Editor is present (skipped in CI without Unity).

### 3.4 Construct to tier table (initial policy, matrix-driven)

`Emulation id` is the registry key of the strategy that performs the rewrite. "Needs" lists what must be present for the Native tier; when absent the next tier applies.

| Construct (construct key) | Native when | Otherwise | Emulation id | Notes |
|---|---|---|---|---|
| `display:grid` with static templates (`repeat(n, 1fr)`, px, %, fr, auto) | never | ConvertTime | `grid-flatten` | Nested flex rows with computed `flex-basis`/`flex-grow`, spans through basis sums, explicit placement and named areas; the children are static at conversion time. |
| grid with `auto-fill`, `auto-fit`, `minmax()`, dense auto-placement, subgrid | never | Reject | none | Rewrite: fixed template or nested flex. |
| `gap`, `row-gap`, `column-gap` | the property exists in the matrix | ConvertTime | `gap-margins` | nowrap: margin on all but the last item per axis; wrap: trailing margins plus a negative container margin (diagnostic explains the size effect). |
| `:not()`, `:is()`, `:where()`, `:has()` | never | ConvertTime (`:has` Reject) | `static-selectors` | Evaluated against the static DOM; matching elements receive a generated class (`htu-s-<hash>`), the rule selector is rewritten. Runtime-created elements are not covered (INFO). |
| `:nth-child()`, `:nth-of-type()`, `:first-child`, `:last-child`, `:only-child`, `:empty`, `+`, `~`, attribute selectors | never | ConvertTime | `static-selectors` | Same mechanism. |
| `:focus-visible`, `:focus-within`, `:visited` | never | Reject | none | Suggest `:focus` or a class toggled in code. |
| `::before`, `::after` | never | ConvertTime | `pseudo-elements` | Real child elements inserted first/last; `content` glyphs become Label text, `content: ""` plus background becomes an image element; text content needs a key. |
| `calc()` of constants and same-unit terms | never | ConvertTime | `calc-fold` | Folded; mixed `%` and `px` terms Reject with a flex rewrite. `min()`, `max()`, `clamp()` of constants fold, otherwise Reject. |
| `vw`, `vh`, `vmin`, `vmax` | never | ConvertTime | `viewport-units` | px at the reference resolution from the config (`unity.referenceResolution`, default 1920x1080); a later refinement may emit `%` for full-bleed boxes. |
| `em`, `rem`, `ch`, `pt` | never | ConvertTime | `relative-units` | Resolved with the static computed font-size. |
| `@media` on width, height, aspect-ratio, orientation | never | Runtime | `media-breakpoints` | Rules gain a root class (`mq-w-max-900`); `mq.json` plus `BreakpointWatcher` toggle the classes from the panel size. Other media features Reject. |
| `@keyframes`, `animation*` | never | Runtime | `keyframes-animator` | `*.anim.json` plus `UitkAnimator`; layout properties in keyframes are Reject. |
| `transition*` | property and easing exist in the matrix | Reject for unknown easing | none | 1:1; discrete properties and layout properties produce warnings. |
| `linear-gradient()` | function present (evidence may be assembly-only) | ConvertTime | `gradient-bake` | Bakes a small PNG strip when Native is absent or the syntax is outside the supported subset. |
| `radial-gradient()`, `conic-gradient()` | never | ConvertTime | `gradient-bake` | Baked texture (size from the element's static box or 256x256 stretched). |
| `box-shadow` (outer) | the matrix has the `drop-shadow` filter (rewrite to `filter: drop-shadow(...)`) | ConvertTime | `shadow-bake` | Without the filter: generated 9-slice PNG in a wrapper element (a later fixture); `inset` shadow: Reject. |
| `filter` and `backdrop-filter` with functions in the matrix | function present | Runtime (`backdrop-filter`) / ConvertTime for static images (`filter`) | `backdrop-blur`, `filter-bake` | Diagnostic notes that `backdrop-filter` needs a Screen Space Overlay panel. Without matrix support: RenderTexture pass in `BackdropBlur`. |
| `mix-blend-mode` | never | Runtime if UI shader material support exists, else Reject | `blend-material` | Optional, M6. |
| `overflow: auto`, `overflow: scroll`, `overflow-x/y` | never | ConvertTime | `overflow-scrollview` | The element becomes a ScrollView; children move into the content container; classes and name stay. |
| `position: fixed` | never | ConvertTime | `fixed-to-root` | Re-parented as an absolute child of the panel root, order preserved. `sticky`: Reject. |
| `z-index` | never | ConvertTime | `z-order` | Stable sort of siblings by z-index; cross-parent stacking Reject. |
| `transform` with several functions | never | ConvertTime | `transform-decompose` | Into `translate`, `rotate`, `scale` when order-independent or reducible; `matrix`, 3D Reject. |
| `inset`, `place-*`, `margin-inline`, `flex-flow`, `border` shorthand | partly (shorthands exist: margin, padding, border-*) | ConvertTime | `shorthand-expand` | Expansion only. |
| `text-transform`, `text-decoration`, `line-height`, `letter-spacing` in em, `outline` | never | Reject (suggestions) | none | `text-transform` may become ConvertTime for the string stub in M6. |
| `!important` | never | Reject | none | Rewrite with a more specific selector. |
| `justify-content: space-evenly` | value exists | Reject if absent | none | Detected, not hard-coded (the Python oracle misses it). |

Rules in `spec/rules.json` keep their ids (D, L, N, C, T, R) and gain two columns: `tier` (default tier on the reference matrix) and `emulation`. New rule families: `J` (multi-file jobs), `P` (pages and navigation), `A` (animation), `E` (emulation failure, for example grid-flatten could not resolve).

## 4. Solution layout

One repository (this folder becomes the repository root). Target frameworks in parentheses.

```
htmltouitk/
  README.md  CHANGELOG.md  .gitignore  Directory.Build.props  HtmlToUitk.sln
  .github/workflows/ci.yml           build, test, golden, validate examples (windows + ubuntu)
  spec/                              DATA, language-neutral
    rules.json  dialect.json  uss.json(legacy, replaced by capabilities)  capabilities.6000.6.json
    capabilities.overrides.json  schemas/*.schema.json
  fixtures/                          golden fixtures (data only), section 11
  assets/                            uitk-base.css  uitk-theme.css  uitk-validate.js  uitk-prototype.js
  examples/                          generic invented screens, one job file, config
  docs/                              ARCHITECTURE.md  RULES.md  DIALECT.md
  oracle/python/                     the Python prototype (frozen after parity)
  src/
    HtmlToUitk.Contracts/            (netstandard2.1)  shared DTOs and interfaces, section 5
    HtmlToUitk.Core/                 (netstandard2.1)  parsers, selectors, style engine, json, text
    HtmlToUitk.Capabilities.Model/   (netstandard2.1)  CapabilityMatrix + loader
    HtmlToUitk.Capabilities.Reader/  (net8.0)          the assembly reader tool
    HtmlToUitk.Rules/                (netstandard2.1)  rule engine, validators, construct detectors, TierPlanner
    HtmlToUitk.Convert/              (netstandard2.1)  pipeline, dialect mapper, emulations, writers, job runner
    HtmlToUitk.Bake/                 (netstandard2.1)  PNG encoder, gradient/shadow/blur bakers (pure C#)
    HtmlToUitk.Preview/              (netstandard2.1)  UXML+USS to HTML preview writer
    HtmlToUitk.Cli/                  (net8.0)          dotnet tool `htmltouitk`
  unity/
    com.neoxider.htmltouitk/         UPM package: Editor/ (window, importer, watcher) + Runtime/ (animator, router, blur, breakpoints) + Plugins (built DLLs)
    typecheck/                       csproj that compiles the package sources against the Unity DLLs
  tests/
    HtmlToUitk.Core.Tests/  HtmlToUitk.Rules.Tests/  HtmlToUitk.Convert.Tests/  HtmlToUitk.Bake.Tests/
    HtmlToUitk.Cli.Tests/   HtmlToUitk.Golden/  (the data-driven runner over fixtures/)
  tools/
    pack-unity.ps1|sh                build netstandard2.1 DLLs and copy them into unity/.../Plugins
    oracle-diff.ps1|sh               run the Python oracle over fixtures and diff against expected/
```

Dependency graph (arrows point to dependencies; no cycles):

```
Contracts <- Core <- Capabilities.Model <- Rules <- Convert <- Cli
                              ^              ^        ^   ^
                              |              |        |   +-- Bake   (Convert -> Bake)
                              +--------------+--------+------ Preview (Preview -> Core, Capabilities.Model)
Capabilities.Reader -> Capabilities.Model      Golden tests -> Cli/Convert/Rules
unity Editor -> Plugins (Contracts, Core, Capabilities.Model, Rules, Convert, Bake)     unity Runtime -> UnityEngine only
```

Unity packaging: the Core family is built as netstandard2.1 DLLs by `tools/pack-unity` and committed under `unity/com.neoxider.htmltouitk/Editor/Plugins/` with Editor-only `.meta` files (GUIDs stable). One source of truth, no copied `.cs` files. The Runtime assembly contains only code that must run in a player and references UnityEngine only.

## 5. Public contracts (frozen at milestone M0)

Namespace `HtmlToUitk`. All collections are exposed as `IReadOnlyList<T>`/`IReadOnlyDictionary<K,V>`. All output ordering uses `StringComparer.Ordinal` and invariant culture. Nothing in the contracts references `UnityEngine`.

### 5.1 Source positions and diagnostics (`HtmlToUitk.Contracts`)

```csharp
public readonly struct SourcePos { public int Offset; public int Line; public int Column; }   // Line, Column 1-based
public readonly struct SourceSpan { public string File; public SourcePos Start; public SourcePos End; }

public enum Severity { Info, Warning, Error }
public enum Tier { Native, ConvertTime, Runtime, Reject }
public enum Verdict { Pass, PassWithWarnings, Fail }

public sealed class Diagnostic
{
    public string RuleId;                      // "C001", "J004"
    public Severity Severity;
    public Tier Tier;
    public SourceSpan Span;
    public IReadOnlyList<SourceSpan> Related;  // for conflicts: both locations
    public string Construct;                   // the offending text
    public string Message;
    public string Why;                         // why UI Toolkit cannot do it
    public string Suggestion;                  // concrete rewrite
    public string EmulationId;                 // null when none
}

public interface IDiagnosticSink { void Report(Diagnostic diagnostic); }

public sealed class DiagnosticBag : IDiagnosticSink
{
    public IReadOnlyList<Diagnostic> Items { get; }
    public int Count(Severity severity);
    public Verdict Verdict { get; }            // Fail if any Error, PassWithWarnings if any Warning
    public IReadOnlyList<Diagnostic> Sorted();   // severity, file, line, column, rule
}

public sealed class RuleInfo { public string Id; public Severity DefaultSeverity; public Tier DefaultTier; public string Title, Why, Fix, Emulation; }
public interface IRuleCatalog { RuleInfo Get(string id); IReadOnlyList<RuleInfo> All { get; } }
```

### 5.2 HTML DOM (`HtmlToUitk.Core.Html`)

```csharp
public interface IElement                      // implemented by DomElement and UxmlElement so one style engine serves both
{
    string TypeName { get; }                   // "div" or "VisualElement"
    string Name { get; }                       // id/name
    IReadOnlyList<string> Classes { get; }
    IElement Parent { get; }
    IReadOnlyList<IElement> ChildElements { get; }
    string GetAttribute(string name);
}

public abstract class DomNode { public SourceSpan Span; public DomElement Parent; }
public sealed class DomText : DomNode { public string Text; }
public sealed class DomAttribute { public string Name; public string Value; public SourceSpan Span; }
public sealed class DomElement : DomNode, IElement
{
    public string Tag;
    public IReadOnlyList<DomAttribute> Attributes;
    public IReadOnlyList<DomNode> Children;
}
public sealed class StyleBlock { public string Text; public SourceSpan Span; public IReadOnlyList<DomAttribute> Attributes; }
public sealed class LinkRef { public string Rel, Href; public IReadOnlyList<DomAttribute> Attributes; public SourceSpan Span; }
public sealed class HtmlDocument
{
    public string Path;
    public DomElement Body;                    // virtual root; html/head/body tags are folded into the properties below
    public IReadOnlyList<StyleBlock> StyleBlocks;
    public IReadOnlyList<LinkRef> Links;
    public IReadOnlyList<DomElement> Metas, Scripts;
    public IReadOnlyList<DomElement> Templates;   // <template id> partials
    public IReadOnlyList<StyleSourceRef> StyleOrder; // document order of <link>/<style>
}
public interface IHtmlParser { HtmlDocument Parse(string text, string path, IDiagnosticSink sink); }
```

### 5.3 CSS and USS model (`HtmlToUitk.Core.Css`)

```csharp
public enum StyleOrigin { Page, Linked, Imported, ConfigListed, UssPassThrough, Skin }

public abstract class CssValue { public abstract string ToText(); public SourceSpan Span; }
// CssNumber, CssDimension(value, unit), CssPercent, CssColor(r,g,b,a, authored), CssIdent, CssString, CssUrl,
// CssFunction(name, args), CssVarRef(name, fallback), CssList(items, separator), CssRaw(text)

public sealed class Declaration { public string Name; public CssValue Value; public bool Important; public SourceSpan Span; }
public sealed class Specificity : IComparable<Specificity> { public int Ids, Classes, Types; }
public sealed class SimpleSelector { public string Type; public string Id; public IReadOnlyList<string> Classes; public IReadOnlyList<PseudoClass> Pseudos; public IReadOnlyList<AttributeSelector> Attributes; public IReadOnlyList<string> PseudoElements; public bool Universal; }
public sealed class Selector { public IReadOnlyList<(char Combinator, SimpleSelector Part)> Parts; public Specificity Specificity; public string Text; public SourceSpan Span; }
public sealed class StyleRule { public IReadOnlyList<Selector> Selectors; public IReadOnlyList<Declaration> Declarations; public IReadOnlyList<string> AtContext; public SourceSpan Span; public int Order; }
public sealed class AtRule { public string Name; public string Prelude; public IReadOnlyList<StyleRule> Rules; public IReadOnlyList<Declaration> Declarations; public SourceSpan Span; }
public sealed class ImportRef { public string Target; public SourceSpan Span; }
public sealed class Stylesheet
{
    public string Path; public StyleOrigin Origin;
    public IReadOnlyList<StyleRule> Rules; public IReadOnlyList<AtRule> AtRules; public IReadOnlyList<ImportRef> Imports;
}
public interface ICssParser { Stylesheet Parse(string text, string path, StyleOrigin origin, IDiagnosticSink sink); }
```

### 5.4 UXML model (`HtmlToUitk.Core.Uxml`)

```csharp
public sealed class UxmlAttribute { public string Name; public string Value; }
public sealed class UxmlElement : IElement
{
    public string UxmlType;                     // "VisualElement", "Button", ...
    public IReadOnlyList<UxmlAttribute> Attributes;   // ordered
    public IReadOnlyList<UxmlElement> Children;
    public string LocKey, MockText;             // dialect metadata, never written
    public DomElement Source;                   // origin, null for generated elements
    public string GeneratedBy;                  // emulation id or null
}
public sealed class UiDocument
{
    public string Name;                         // screen name
    public string SourcePath;
    public IReadOnlyList<UxmlElement> Roots;
    public IReadOnlyList<StyleRef> Styles;      // <Style src>, cascade order
    public IReadOnlyList<TemplateRef> Templates;
    public IReadOnlyList<PageDef> Pages;        // data-page containers (section 7.6)
}
public interface IUxmlWriter { string Write(UiDocument document, UxmlWriterOptions options); }
public interface IUxmlReader { UiDocument Read(string text, string path, IDiagnosticSink sink); }
```

### 5.5 Style engine (`HtmlToUitk.Core.Style`)

```csharp
public sealed class ResolvedValue { public CssValue Value; public Declaration Source; public StyleRule Rule; public Stylesheet Sheet; }
public sealed class ResolvedStyle { public IReadOnlyDictionary<string, ResolvedValue> Properties; }
[Flags] public enum PseudoStateSet { None = 0, Hover = 1, Active = 2, Focus = 4, Disabled = 8, Checked = 16, Root = 32 }

public interface IStyleModel
{
    IReadOnlyList<Stylesheet> Cascade { get; }                       // exact order, imports expanded
    ResolvedStyle Resolve(IElement element, PseudoStateSet states);  // specificity then source order; var() resolved; inheritance applied
    IReadOnlyList<IElement> Match(Selector selector, IElement root);
    IReadOnlyDictionary<string, CssValue> Variables(IElement element);
    IReadOnlyList<StyleConflict> Conflicts { get; }                  // same selector, different value, with both spans
}
public interface IStyleEngine { IStyleModel Build(IReadOnlyList<Stylesheet> cascade, StyleOptions options, IDiagnosticSink sink); }
```

### 5.6 Capability model (`HtmlToUitk.Capabilities.Model`)

```csharp
public enum Animatable { None, Discrete, Full }
public sealed class PropertyCapability { public string Name, Id; public bool Inherited; public Animatable Animatable; public IReadOnlyList<string> Values; public IReadOnlyList<string> Longhands; public string Evidence; }
public sealed class CapabilityMatrix
{
    public string UnityVersion { get; }
    public bool HasProperty(string name);                  PropertyCapability Property(string name);
    public bool HasPseudoClass(string name);               public bool HasCombinator(char c);
    public bool HasFunction(string name);                  public bool HasKeyword(string name);
    public bool HasFilter(string name);                    public bool HasEasing(string name);
    public bool Supports(string capabilityKey);            // "css:gap", "uss:linear-gradient", "filter:blur", "unit:vh"
    public string Condition(string key);                   // for example the Screen Space Overlay requirement
    public static CapabilityMatrix Load(string json);
}
```

### 5.7 Rules, constructs, tiers (`HtmlToUitk.Rules`)

```csharp
public enum ConstructKind { Property, Value, Selector, AtRule, Element, Attribute, Page }
public sealed class Construct { public ConstructKind Kind; public string Key; public SourceSpan Span; public object Node; public IReadOnlyDictionary<string, string> Data; }

public interface IConstructDetector { void Detect(AnalysisInput input, ICollection<Construct> output); }

public sealed class TierDecision { public Tier Tier; public string EmulationId; public string RuleId; public string Reason; public IReadOnlyList<string> RequiredRuntime; }
public interface ITierPlanner { TierDecision Decide(Construct construct, CapabilityMatrix caps, ConversionConfig config); }

public interface IRule { string Id { get; } void Evaluate(RuleContext context); }
public interface IRuleEngine { void Run(AnalysisInput input, IDiagnosticSink sink); }   // runs detectors, planner and rules, reports Diagnostics
```

### 5.8 Pipeline (`HtmlToUitk.Convert`)

```csharp
public sealed class StageContext { public ConversionConfig Config; public CapabilityMatrix Capabilities; public DiagnosticBag Diagnostics; public IFileSystem Fs; public CancellationToken Cancel; }
public interface IPipelineStage<TIn, TOut> { string Name { get; } TOut Run(TIn input, StageContext context); }

// Stages and their data
// 1 LoadStage      ConversionJob      -> InputSet          read files, expand globs/folders, @import chains, includes, partials
// 2 AnalyseStage   InputSet           -> AnalysisResult    parse HTML/CSS/USS, dialect mapping to UiDocument, style model, tokens, keys, nav, anim, run IRuleEngine
// 3 PlanStage      AnalysisResult     -> ConversionPlan    one TierDecision per construct
// 4 TransformStage (AnalysisResult, ConversionPlan) -> TransformedModel   run IEmulation in a fixed order, then merge/dedupe/order USS
// 5 EmitStage      TransformedModel   -> OutputSet         IWriter instances
// 6 LintStage      OutputSet          -> (diagnostics)     built-in lint + optional external lint command
// 7 CommitStage    OutputSet          -> disk              overwrite guard, unchanged-file skip, report

public sealed class ConversionJob { public JobSpec Spec; public ConversionConfig Config; public CapabilityMatrix Capabilities; }
public sealed class InputSet { public IReadOnlyList<PageInput> Pages; public IReadOnlyList<StyleInput> Styles; public IReadOnlyList<UssInput> UssPassThrough; public IReadOnlyList<PartialInput> Partials; public IncludeGraph Includes; }
public sealed class AnalysisResult { public IReadOnlyList<UiDocument> Documents; public IStyleModel Style; public TokenTable Tokens; public KeyTable Keys; public NavModel Nav; public AnimModel Anim; public IReadOnlyList<Construct> Constructs; }
public sealed class ConversionPlan { public IReadOnlyList<(Construct Construct, TierDecision Decision)> Actions; }
public sealed class TransformedModel { public IReadOnlyList<UiDocument> Documents; public SheetSet Sheets; public IReadOnlyList<GeneratedAsset> Assets; public Specs Specs; public IReadOnlyList<string> RuntimeComponents; }
public sealed class OutputFile { public string RelativePath; public byte[] Content; public FileKind Kind; }
public sealed class OutputSet { public IReadOnlyList<OutputFile> Files; }
```

### 5.9 Emulation strategy (`HtmlToUitk.Convert.Emulations`)

```csharp
public interface IEmulation
{
    string Id { get; }                              // "grid-flatten"
    Tier Tier { get; }                              // ConvertTime or Runtime
    IReadOnlyList<string> Handles { get; }          // construct keys: "prop:display=grid", "pseudo:not", "at:media"
    IReadOnlyList<string> RuntimeComponents { get; }// ids written to the runtime manifest
    int Order { get; }                              // fixed application order, lower first
    EmulationOutcome Apply(EmulationContext context, Construct construct);
}
public sealed class EmulationOutcome { public bool Handled; public IReadOnlyList<Diagnostic> Diagnostics; }
public sealed class EmulationContext
{
    public UiDocument Document; public IStyleModel Style; public SheetBuilder Sheets; public IAssetSink Assets; public ISpecSink Specs;
    public ConversionConfig Config; public CapabilityMatrix Capabilities; public INameGenerator Names; public IElementEditor Editor;
}
public sealed class EmulationRegistry { public void Register(IEmulation e); public IEmulation For(string constructKey); public IReadOnlyList<IEmulation> Ordered { get; } }
```

Application order is fixed: `shorthand-expand`, `relative-units`, `viewport-units`, `calc-fold`, `static-selectors`, `pseudo-elements`, `grid-flatten`, `gap-margins`, `overflow-scrollview`, `fixed-to-root`, `z-order`, `transform-decompose`, `gradient-bake`, `shadow-bake`, `filter-bake`, `media-breakpoints`, `keyframes-animator`, then merge, dedupe and section ordering. Generated names come from `INameGenerator` (stable: prefix + kebab role + short hash of the construct), never from counters that depend on traversal side effects.

### 5.10 Writers (`HtmlToUitk.Convert.Writers`)

```csharp
public interface IWriter { string Id { get; } IEnumerable<OutputFile> Write(TransformedModel model, WriterContext context); }
// UxmlWriter, UssWriter (sections: variables, layout, components, states, utilities), TokensWriter, KeysStubWriter,
// AnimSpecWriter (*.anim.json), NavMapWriter (*.nav.json), MediaQueryWriter (*.mq.json), AssetWriter (PNG),
// RuntimeManifestWriter (*.runtime.json), ReportWriter (diagnostics.json)
```

Determinism contract: UTF-8 without BOM, LF, no timestamps, ordinal sorting for every map, invariant number formatting (`0.####`), four-space indent (configurable), identical bytes for identical inputs on every platform. PNG output is deterministic (fixed filter, compression level, no ancillary chunks).

### 5.11 Config and job model (`HtmlToUitk.Convert.Config`)

`ConversionConfig` mirrors `schemas/config.schema.json` (section 6.1) one class per section. `JobSpec` mirrors `schemas/job.schema.json` (6.2). Both are loaded by the same dependency-free JSON reader in Core (`HtmlToUitk.Core.Json`: parser, writer with ordered objects, no reflection).

## 6. Data formats

### 6.1 `htmltouitk.config.json`

Sections and the keys the Python prototype already honours (schema file is authoritative):

```json
{
  "projectRoot": ".", "uiRoot": "UI",
  "layout": { "screens": "Screens", "skin": "Skin", "strings": "Localization", "shared": "Shared", "generated": "Generated" },
  "unity": { "version": "6000.6", "capabilities": "spec/capabilities.6000.6.json", "panelSettings": null, "referenceResolution": [1920, 1080] },
  "localization": { "files": [], "format": "locale-map", "locales": ["en"], "mockLocale": "en", "keyPattern": "...", "placeholder": "[TODO] ", "allowGlyphOnly": true, "keysStub": "{screen}.newkeys.json" },
  "naming": { "namePattern": "...", "classPattern": "...", "prefixes": [], "dynamicClassPattern": "^is-", "dynamicClasses": [] },
  "rules": { "minFontSize": 13, "minTouchTarget": 40, "undefinedClass": "warn", "colorLiterals": "allow", "strictWarnings": false, "disable": [] },
  "fonts": { "FamilyName": "/Assets/path/Font.asset" },
  "uxml": { "indent": 4, "namespaces": ["ui", "uie"], "interactiveAttributes": {}, "allowInlineStyles": ["display: none;"] },
  "uss": { "tokenScopeSelector": null, "redefineSkinTokens": false, "pruneDeadRules": false, "sectionOrder": true, "utilityPattern": "^u-" },
  "emulation": { "strictNative": false, "allowRuntime": true, "disable": [], "gridMaxTracks": 24, "bakedTextureFolder": "Generated" },
  "lint": { "builtin": true, "external": { "command": [], "mirror": [], "cwd": "." } },
  "preview": { "theme": [], "viewports": ["1920x1080"], "backdrop": "#101418", "locale": null, "fragments": {}, "textOverrides": {}, "codeBindings": null },
  "pages": { "backend": "standalone|neoxider-uikit", "classes": { "hidden": "is-hidden", "active": "is-active" } },
  "animation": { "backend": "animator-json", "reducedMotion": "simplify" }
}
```

### 6.2 `*.htujob.json` (job file; also the Unity job asset source)

```json
{
  "schema": 1,
  "name": "main-app",
  "config": "htmltouitk.config.json",
  "configOverrides": { "rules": { "minFontSize": 14 } },
  "inputs": {
    "html": ["pages/**/*.html", "extra/about.html"],
    "css": ["css/shared/*.css"],
    "cssOrder": "before",
    "uss": ["existing/skin.uss"],
    "partials": ["partials/**/*.html"]
  },
  "output": {
    "root": "Assets/UI/Generated",
    "assemble": "perFile",
    "folders": { "uxml": "Screens", "uss": "Screens", "shared": "Shared", "tokens": "Shared", "strings": "Localization", "generated": "Generated" },
    "perFile": { "pages/home.html": { "name": "Home", "folder": "Screens/Home", "assemble": "perFile" } }
  },
  "watch": { "enabled": false, "debounceMs": 400 }
}
```

CLI parity: `htmltouitk run job.json [--validate-only] [--format json] [--force]` runs exactly what the window runs.

### 6.3 `Diagnostics` report (`diagnostics.json`)

```json
{
  "tool": "htmltouitk", "version": "0.1.0", "verdict": "FAIL",
  "counts": { "error": 2, "warning": 1, "info": 3 },
  "tiers": { "native": 41, "convertTime": 6, "runtime": 1, "reject": 2 },
  "findings": [
    { "rule": "C001", "severity": "Error", "tier": "ConvertTime", "emulation": "grid-flatten",
      "file": "pages/home.html", "line": 12, "column": 5, "endLine": 12, "endColumn": 24,
      "construct": "display: grid", "message": "...", "why": "...", "suggestion": "...", "related": [] }
  ],
  "newKeys": ["menu.home.title"]
}
```

Verdict mapping: any Error is FAIL; else any Warning is PASS WITH WARNINGS; else PASS. A construct that is handled by ConvertTime or Runtime emulation is reported with the emulation's severity (Info when exact, Warning when approximate or when a runtime component is required), never Error. Exit codes: 0 pass, 1 FAIL (or warnings in strict mode), 2 usage or I/O error.

### 6.4 `*.anim.json`

```json
{
  "schema": 1,
  "animations": [
    { "id": "fade-in-up", "keyframes": "fade-in-up", "duration": 0.4, "delay": 0.0, "iterations": 1, "direction": "normal", "fill": "forwards", "easing": "ease-out-cubic",
      "steps": [
        { "offset": 0.0, "set": { "opacity": "0", "translate": "0 12px" } },
        { "offset": 1.0, "set": { "opacity": "1", "translate": "0 0" } } ] }
  ],
  "bindings": [
    { "selector": ".menu__item", "animation": "fade-in-up", "trigger": "onShow", "stagger": 0.05, "reducedMotion": "skip" }
  ]
}
```

Animated properties must be Full or Discrete animatable per the capability matrix; layout properties (width, height, margin, padding, left, top, flex-*) are rejected unless `rules.allowLayoutAnimation`.

### 6.5 `*.nav.json`

```json
{
  "schema": 1,
  "document": "App",
  "start": "home",
  "pages": [ { "id": "home", "element": "app-home", "focus": "app-home-play", "transition": { "in": "fade", "out": "fade", "ms": 220 } } ],
  "edges": [ { "from": "home", "trigger": "app-home-settings", "action": "navigate", "to": "settings", "mode": "push", "transition": "slide-left", "ms": 260 },
             { "from": "settings", "trigger": "app-settings-back", "action": "back" } ],
  "modals": [ { "id": "confirm", "element": "app-confirm", "ownerPage": "home", "closeTriggers": ["app-confirm-cancel"] } ],
  "tabs": [ { "group": "settings-tabs", "page": "settings", "tabs": [ { "tab": "app-tab-audio", "panel": "app-panel-audio" } ], "default": "app-tab-audio" } ]
}
```

Backends: `standalone` (the thin `UitkRouter` in the Runtime assembly) and `neoxider-uikit` (pages as separate UXML with `fui_type_screen`, popups `fui_type_popup`, a `UiKitConfig` JSON fragment with `pages` and `navigation` entries; see the study document for the mapping and its gaps).

### 6.6 `*.mq.json`

```json
{ "schema": 1, "classes": [ { "class": "mq-w-max-900", "query": { "maxWidth": 900 } }, { "class": "mq-portrait", "query": { "orientation": "portrait" } } ], "root": "app-root" }
```

### 6.7 Golden fixture format

```
fixtures/<ID>-<name>/
  fixture.json            id, title, proves[], rules[], tiers[], mode ("validate" | "convert"), expectVerdict,
                          capabilities ("6000.6" or a path to an alternate matrix), multiFile (bool)
  input/                  *.html, *.css, *.uss, partials, assets (tiny PNGs), strings table, skin
  job.json                job file (or config.json for single-file fixtures)
  expected/               exact outputs: Screens/<Name>.uxml, .uss, Shared/*.uss, Localization/*.newkeys.json,
                          *.anim.json, *.nav.json, *.mq.json, Generated/*.png, diagnostics.json
```

`expected/diagnostics.json` is the normalised form `[{rule, severity, tier, emulation, file, line, column}]` sorted. The golden runner (`tests/HtmlToUitk.Golden`) runs each fixture through the real pipeline in memory and compares: text files byte-for-byte after LF normalisation, JSON canonically, PNG by decoded RGBA (exact for baked output because encoding is deterministic). `tools/oracle-diff` runs the Python prototype on the Native-tier fixtures to prove parity while both exist.

## 7. Multi-file jobs

### 7.1 Input set

- HTML inputs: files, folders (recursive) and globs; drag and drop in the window adds paths to `inputs.html`.
- Style inputs come from four places and are merged into one cascade per page, in this exact order: (1) `inputs.css` then `inputs.uss` when `cssOrder` is `before` (listed order), (2) the page's `<link rel="stylesheet">` and `<style>` elements in document order, each link expanded in place with its `@import` chain (imports first, depth-first, source order), (3) `inputs.css` then `inputs.uss` when `cssOrder` is `after`, (4) the same positions apply to `inputs.uss` entries: with `cssOrder` before they come right after `inputs.css` (skin first, lowest priority); with `after` they come last. Specificity and source-order semantics are those of UI Toolkit (specificity, then order of the sheet in the cascade, then order in the sheet).
- Existing USS pass-through files are not converted; they are referenced from every UXML in the cascade position they occupy and, when they define tokens or classes, they feed class checks, the token table and conflict detection.
- Conflicts: a selector with identical canonical text defined in several sheets with different values for the same property produces `J001` (Warning) with both spans in `related`.

### 7.2 Output mapping

- `assemble: "perFile"` (default): each HTML becomes `<uxml>/<Name>.uxml` and its own `<uss>/<Name>.uss` holding the page-specific rules (page `<style>` blocks and CSS files linked by that page only).
- A CSS file linked by two or more pages, or declared in `inputs.css`, becomes one shared USS (`<shared>/<CssStem>.uss`), referenced by every UXML that uses it, in cascade order. Identical content (after normalisation) deduplicates to one file; rules are never copied into page USS files.
- Tokens: all `:root` custom properties of shared sheets go to one shared tokens USS referenced first; page-local tokens go to the page USS. Existing skin tokens are not redefined.
- Names: `<Name>` is the PascalCase stem of the input path, or the `meta uitk:screen` value, or `perFile[...].name`. Collisions get a deterministic suffix (`-2`) ordered by path. Output folder is configurable per job and per file.
- `assemble: "singleDocument"`: all pages of the job become one UXML with a root container and one container per page (`name=<page id>`, class `htu-page`, hidden state class from `pages.classes.hidden`), one merged page USS plus shared sheets, and the `*.nav.json` router map.

### 7.3 Partials and components

`<template id="card">...</template>` in any input, or `data-include="partials/card.html#card"` on an element, resolves at conversion:

- mode `templates` (default for files): each partial becomes `<Components>/<Id>.uxml`, the use site becomes `<ui:Instance template="Id" name="..."/>` with a `<ui:Template name="Id" src="..."/>` in the header;
- mode `inline`: the partial body is copied into the use site (names must stay unique; a deterministic suffix is added to repeated names and reported).

Include graphs are checked for cycles (`J007`).

### 7.4 Cross-file validation rules (family `J`)

| Id | Severity | Meaning |
|---|---|---|
| J001 | Warning | Same selector defined in several sheets with different values (both spans). |
| J002 | Error | Missing stylesheet, `@import` target or skin file. |
| J003 | Error | Circular `@import`. |
| J004 | Error | Duplicate element name across pages that end up in one document (`singleDocument`) or in one template scope. |
| J005 | Warning | CSS rule that no page matches (rules used by at least one page are fine). |
| J006 | Error | Missing partial or include target. |
| J007 | Error | Include cycle. |
| J008 | Error | `data-nav` or `data-modal` target does not exist in the input set. |
| J009 | Error | Two inputs map to the same output path. |
| J010 | Warning | An input glob matched nothing. |

Page, tab and navigation rules (`P001` to `P010`: start page, orphan page, unknown target, unreachable page, missing back from start, duplicate modal id, inconsistent tab group, unknown transition, no focusable element on a page, dead link) and animation rules (`A001` to `A006`: missing `@keyframes`, unsupported animated property, layout property animated, too many steps, missing reduced-motion handling, invalid timing or iteration) are defined in `spec/rules.json` in M0.

### 7.5 Unity window and CLI parity

The window and `htmltouitk run` share `JobRunner`. The window adds only presentation: input list, status icons (PASS, WARN, FAIL), findings pane, buttons, watcher, job asset. Section 8 specifies it.

### 7.6 Pages and navigation dialect (summary)

`<section data-page="id">`, `data-start`, `data-nav="id"` (+ `data-nav-mode="push|show"`), `data-back`, `data-modal="id"` / `data-close`, `data-tabs` / `data-tab`, `data-transition="fade|slide-left|slide-right|slide-up|scale|none"` and `data-transition-ms`, `data-reduced-motion="off|simplify"`. The browser prototype runtime `assets/uitk-prototype.js` implements history, back, modals, tabs and transitions so the flow can be clicked through before conversion.

## 8. Unity package `com.neoxider.htmltouitk`

```
unity/com.neoxider.htmltouitk/
  package.json                       name, version, unity 6000.0, no dependencies
  Editor/
    Neo.HtmlToUitk.Editor.asmdef     Editor only; references the Plugins DLLs and UnityEditor
    Plugins/                         built netstandard2.1 DLLs (Contracts, Core, Capabilities.Model, Rules, Convert, Bake, Preview)
    Window/HtmlToUitkWindow.cs       menu "Neoxider/UI/HTML to UI Toolkit"; UI Toolkit window; styles in Window/HtmlToUitkWindow.uss
    Window/InputListView.cs          list of inputs, drag and drop of files and folders, status icons
    Window/FindingsView.cs           findings grouped by file, severity filter, click opens the file at the line
    Jobs/HtmlToUitkJob.cs            ScriptableObject view of a *.htujob.json
    Jobs/HtmlToUitkJobImporter.cs    ScriptedImporter for *.htujob.json (source of truth stays the JSON)
    Watch/HtmlToUitkWatcher.cs       FileSystemWatcher plus EditorApplication.update debounce; regenerates changed pages
    Import/GeneratedAssetPostprocessor.cs  imports generated UXML/USS/PNG and refreshes the AssetDatabase once per run
  Runtime/
    Neo.HtmlToUitk.Runtime.asmdef    no Editor references
    Animation/UitkAnimator.cs, AnimSpec.cs
    Navigation/UitkRouter.cs, NavSpec.cs, NeoxiderUiKitAdapter.cs (versionDefines: com.neoxider.uikit)
    Responsive/BreakpointWatcher.cs
    Effects/BackdropBlur.cs          RenderTexture blur pass for panels or Unity versions without backdrop-filter
    Localization/ILocalizer.cs       hook that resolves keys; the package never owns the string table
  Tests~/                            Editor and Runtime tests (not compiled by us)
unity/typecheck/Typecheck.csproj     compiles the package .cs against UnityEngine/UnityEditor DLLs from the Editor install
```

Window behaviour: input list shows each HTML with an icon (PASS, WARN, FAIL, NotRun) computed by the last validate; selecting a file shows its findings; double-click on a finding opens the source at the line; buttons Validate Only, Convert Selected, Convert All; "Watch" toggles the watcher (debounce 400 ms, one run per quiet period, only changed pages and their dependants); the job asset keeps inputs, style files, output rules and config overrides; saving writes `*.htujob.json`. The same JSON file is accepted by `htmltouitk run`.

Runtime components are referenced from `*.runtime.json` written by the converter, so the Editor can warn when a generated screen needs a component the project does not instantiate.

## 9. Worker split (four workers, disjoint folders)

All workers work in this repository folder; nobody edits another worker's folder. Shared contracts are frozen after M0; a change request goes to the root.

| Worker | Owns (exclusive) | Delivers | Must not touch |
|---|---|---|---|
| W1 Core | `src/HtmlToUitk.Contracts/**` (after M0 read-only), `src/HtmlToUitk.Core/**`, `tests/HtmlToUitk.Core.Tests/**` | HTML parser, CSS/USS parser, selector parser and matcher, style engine (cascade, `var()`, inheritance, shorthand expansion, conflicts), UXML reader/writer model, dependency-free JSON reader/writer, canonical text formatting, include/import expansion primitives | everything else |
| W2 Convert | `src/HtmlToUitk.Convert/**`, `src/HtmlToUitk.Bake/**`, `src/HtmlToUitk.Preview/**`, `tests/HtmlToUitk.Convert.Tests/**`, `tests/HtmlToUitk.Bake.Tests/**` | pipeline stages, dialect mapper, all emulations, writers (UXML, USS, tokens, keys, anim, nav, mq, runtime manifest), job runner and multi-file assembly, partials, PNG encoder and bakers, preview writer. After M2 the Bake and Preview projects can move to a fifth worker without file conflicts | Core, Rules, Cli, unity |
| W3 Rules and tools | `src/HtmlToUitk.Rules/**`, `src/HtmlToUitk.Capabilities.Model/**`, `src/HtmlToUitk.Capabilities.Reader/**`, `src/HtmlToUitk.Cli/**`, `tests/HtmlToUitk.Rules.Tests/**`, `tests/HtmlToUitk.Cli.Tests/**`, `tests/HtmlToUitk.Golden/**`, `spec/capabilities.*.json`, `.github/workflows/**`, `tools/**` | capability reader and model, rule engine, construct detectors, TierPlanner, all D/L/N/C/T/R/J/P/A rules, diagnostics rendering (text and JSON), CLI (`validate`, `convert`, `run`, `preview`, `compare`, `rules`, `capabilities`), golden runner, CI | Core, Convert, unity |
| W4 Unity | `unity/**` | package scaffolding, window, importer, watcher, runtime router, animator, blur, breakpoint watcher, NeoxiderUiKit adapter, typecheck csproj | src, spec, fixtures |
| Root (architect) | `spec/rules.json`, `spec/dialect.json`, `spec/schemas/**`, `fixtures/**`, `assets/**`, `docs/**`, `oracle/**`, `examples/**` | data, fixtures, browser harness, review, integration | code projects |

Interface handoff: W2 and W3 code against `Contracts` and the interfaces of sections 5.5 to 5.9 using fakes until W1's engine lands (M1). W4 consumes only the CLI JSON contracts and the Plugins DLLs; until M1 it works against stub DLLs generated from Contracts.

## 10. Milestones and acceptance checks

| M | Content | Acceptance |
|---|---|---|
| M0 | Contracts, schemas, rule catalogue with tiers, capability file for 6000.6 (reader prototype output), fixture format, 12 Native fixtures authored. Root reviews and freezes. | `dotnet build` of Contracts; every schema validates the example config, job, anim, nav, diagnostics; `capabilities.6000.6.json` generated by the reader from the real DLL. |
| M1 | W1 parsers and style engine; W2 pipeline skeleton with Native tier and writers; W3 reader, model, CLI skeleton, golden runner; W4 package scaffold and typecheck. | Fixtures S01 to S16 pass in the golden runner; Python oracle and C# agree byte-for-byte on S01 to S16 (`tools/oracle-diff`). |
| M2 | Rules and validator complete for Native tier and all Reject constructs; verdicts, JSON, exit codes; project rules; capability-driven enums. | S01 to S16 plus all Reject diagnostics (S12, S18 reject part, S19 reject part, S40, S44); `htmltouitk validate` exit codes tested. |
| M3 | ConvertTime emulations: shorthand, units, calc, static selectors, pseudo-elements, grid, gap, overflow, fixed, z-order, transform, media (with `BreakpointWatcher`). | S17 to S32, S38; browser mock and converted output compared with `htmltouitk compare` on a sample (Unity operator step). |
| M4 | Bake: PNG encoder, gradient, shadow, blur; `BackdropBlur` runtime; filters per capability. | S33 to S37 with pixel-exact baked PNGs; determinism check running the job twice. |
| M5 | Multi-file jobs, assembly, partials, shared USS, conflicts; Unity window, watcher, job asset. | M01 to M16; window typecheck clean; Unity operator checklist for the window (NotRun until verified). |
| M6 | Animations (`UitkAnimator`, spec generation), pages and navigation (`UitkRouter`, NeoxiderUiKit adapter, `uitk-prototype.js`). | S39, S41 to S44; nav and anim schema validation; Python-free model tests of back/modal/tab semantics. |
| M7 | Preview writer, compare tool, README, CI matrix, packaging of the Unity DLLs, release. | CI green on ubuntu and windows; all 60 fixtures green; Unity operator acceptance of the package. |

## 11. The first 60 golden fixtures

Single-file fixtures (S) prove one feature each; multi-file fixtures (M) prove the job features of section 7. Every fixture ships `input/`, `expected/`, `fixture.json`. IDs are stable.

| Id | Name | What it proves |
|---|---|---|
| S01 | minimal-page | div, span, button to UXML plus USS skeleton, header and namespaces, USS always generated, deterministic bytes. |
| S02 | element-mapping | every dialect tag, input type, select, img, progress and `data-uxml` maps to the right UXML element and attributes. |
| S03 | localisation-keys | `data-loc` keys, text attribute carries the key, new-key stub with placeholder locales, L001, L003, L004, L005. |
| S04 | glyph-tooltips-placeholders | glyph-only raw text allowed, `data-loc-tooltip`, `data-loc-placeholder`, raw `title` and `placeholder` rejected. |
| S05 | naming-rules | duplicate names, kebab-case, prefixes, class pattern, undefined classes, interactive without name (N001 to N007). |
| S06 | shorthand-expansion | border, inset, flex-flow, background colour shorthand, single-function transform expand to USS longhands. |
| S07 | value-normalisation | colours (rgb modern syntax, hex alpha), numbers, unitless lengths rejected, canonical formatting. |
| S08 | tokens-and-skin | `:root` tokens, skin dedupe, token conflict warning, scope-selector redefinition, undefined `var()`. |
| S09 | section-order-dedupe | variables, layout, components, states, utilities order; safe fallback to source order; duplicate and mergeable rules. |
| S10 | pseudo-states | hover, active, focus, disabled, checked, root native; unsupported pseudo-classes rejected with rewrite. |
| S11 | transitions-native | `transition` shorthand to longhands, easings from the matrix, discrete and layout property warnings. |
| S12 | transition-rejects | cubic-bezier, steps, unknown property, wrong time unit produce R007, R009 with suggestions. |
| S13 | text-and-fonts | text-align mapping, font-weight and style, font-family via config, text-overflow without clipping, nowrap without ellipsis. |
| S14 | images-and-slices | `img`, `url()` resolution to project paths, slice properties, missing and external images rejected. |
| S15 | hidden-inline-style | `hidden` to display none, inline styles rejected, allowed inline list. |
| S16 | select-dropdown | select/option to choices and index with keys, raw option text rejected. |
| S17 | grid-flatten-basic | `repeat(3, 1fr)` plus gap becomes nested flex rows with computed basis (ConvertTime). |
| S18 | grid-spans-areas-reject | spans, explicit placement and named areas flatten; `auto-fill` and `minmax()` Reject with a rewrite. |
| S19 | gap-nowrap | gap in row and column nowrap containers becomes margins on all but the last item. |
| S20 | gap-wrap | gap with wrap becomes trailing margins plus negative container margin with the explanatory diagnostic. |
| S21 | gap-native-capable | with an alternate matrix that contains `row-gap` the same input stays Native (tier switching proof). |
| S22 | not-is-where | `:not()`, `:is()`, `:where()` resolved against the static DOM into generated classes; `:has()` Reject. |
| S23 | nth-child | odd, even, `3n+1`, `nth-of-type` to generated classes. |
| S24 | first-last-only | `:first-child`, `:last-child`, `:only-child`, `:empty` to generated classes. |
| S25 | sibling-attribute-selectors | `+`, `~` and attribute selectors resolved statically; dynamic-content INFO. |
| S26 | pseudo-elements-glyph | `::before`, `::after` with glyph content become child elements, order and name stability. |
| S27 | pseudo-elements-image-text | `content: ""` plus background becomes an image element; text content without key is L001. |
| S28 | calc-constants | `calc()` of constants folds; mixed `%` and `px` Reject with a flex rewrite. |
| S29 | viewport-and-relative-units | vw, vh against the reference resolution and PanelSettings, `%` substitution, em and rem against static font-size. |
| S30 | media-breakpoints | width, height, orientation and aspect-ratio media queries to root classes plus `mq.json`; unsupported feature Reject. |
| S31 | overflow-scroll | `overflow: auto` becomes a ScrollView with children in the content container. |
| S32 | fixed-sticky-zindex | fixed to root child, z-index sibling reorder, cross-parent z-index and sticky Reject. |
| S33 | linear-gradient-native | `linear-gradient()` stays Native on the 6000.6 matrix; outside the supported subset it is baked. |
| S34 | radial-gradient-bake | radial and conic gradients baked to a deterministic PNG. |
| S35 | box-shadow-drop-shadow | outer shadows rewrite to the drop-shadow filter on the 6000.6 matrix (Native, warning); inset shadow Reject; the bake fallback is a later fixture. |
| S36 | backdrop-blur-native | `backdrop-filter: blur()` native on 6000.6 with the Screen Space Overlay condition diagnostic. |
| S37 | blur-runtime-fallback | with a matrix without blur the same input uses `BackdropBlur` and lists the runtime component. |
| S38 | transform-decompose | multi-function transforms decompose into translate, rotate, scale; matrix Reject. |
| S39 | keyframes-spec | `@keyframes` plus `animation` to `*.anim.json`, bindings, runtime manifest. |
| S40 | keyframes-layout-reject | animating width, height, margin rejected with a translate or scale rewrite. |
| S41 | stagger-reduced-motion | stagger from delay, `data-reduced-motion`, missing handling warning (A005). |
| S42 | class-toggle-transitions | enter and leave animations through class toggles and transitions only. |
| S43 | pages-nav-modal-tabs | `data-page`, start, nav, back, modal, tabs to `*.nav.json` and page containers. |
| S44 | nav-validation-errors | dead link, no start page, orphan page, duplicate modal id, bad transition, no focusable element (P rules). |
| M01 | multipage-shared-css | three HTML pages, one shared CSS, one UXML and page USS per file, shared USS once. |
| M02 | css-link-order-override | several `<link>` and `<style>` in different orders; later sheets override per cascade. |
| M03 | css-conflict-diagnostic | same selector in two files with different values: `J001` with both spans. |
| M04 | uss-passthrough-merge | existing USS passed through unchanged, referenced in cascade position, feeds class and token checks. |
| M05 | import-chain-and-cycle | `@import` chains expand in order; a cycle reports `J003`. |
| M06 | missing-stylesheet-import | missing link or import and missing skin: `J002`, conversion refused. |
| M07 | partials-templates | `<template id>` and `data-include` become `ui:Template` plus `ui:Instance`. |
| M08 | partials-inline | the same partials inlined with deterministic name suffixes. |
| M09 | partials-include-cycle | A includes B includes A: `J007`. |
| M10 | single-document-assembly | `assemble: singleDocument` gives one UXML with page containers plus `*.nav.json`. |
| M11 | duplicate-name-across-pages | duplicate names are fine per file, `J004` in singleDocument. |
| M12 | unused-css-rules | rules unused by every page report `J005`; rules used by one page are kept. |
| M13 | per-file-output-mapping | per-file name and folder overrides, collision suffix, `J009`. |
| M14 | shared-tokens-multi | one shared tokens USS referenced by all pages; page-local tokens stay local. |
| M15 | job-file-folders-globs | `run job.json` with folders and globs, `J010` for an empty glob, parity with the window job. |
| M16 | incremental-regeneration-stable-bytes | unchanged inputs produce identical bytes and no file rewrite; one changed page rewrites only its outputs. |

The conventions that the expected files follow (generated names and hashes, USS ordering and formatting, every emulation, pages, animation spec, multi-file output rules, the golden runner) are specified in `docs/EMULATIONS.md`; the dialect is in `docs/dialect.md`.

## 12. Risks and open questions

1. Gradient support is assembly-only; the manual does not document `linear-gradient`. Keep `gradient-bake` as the fallback and confirm with a screenshot (Unity operator).
2. `StyleValueKeyword` contains `Inherit` and `Unset` while the manual says only `initial` is supported; the matrix records the assembly fact, the converter warns (not rejects) until a Unity check settles it.
3. Baked textures need `.meta` files and import settings (Texture type, sprite mode, filter) that Unity creates on import; a postprocessor in the Unity package sets them by path convention (`Generated/`).
4. The static selector emulations assume the DOM is complete at conversion time; elements created in code do not get generated classes (reported as INFO per rule).
5. The preview is an approximation. A Yoga-based layout mode is deferred until the measured deviation (section 5 of the study) justifies the download listed there.
6. NeoxiderUiKit routes pages through `PanelRenderer`; the standalone router is the default for `UIDocument` projects. The root must confirm the primary backend.
7. Everything in `unity/` is NotRun in Unity until the operator verifies; only type-check is claimed.

## Appendix A. Python oracle

`oracle/python` implements the Native tier: dialect mapper, CSS translation, rules D, L, N, C, T, R, section ordering, tokens, key stub, built-in and external lint. It reads `spec/rules.json`, `spec/uss.json` and `spec/dialect.json`. It is frozen after M1 parity and removed or kept as an optional script after M7. Known gaps against the capability facts (to be fixed in data, not code): `space-evenly`, gradient and filter tiering, `inherit` and `unset` keywords, and everything in section 3.4 beyond Native.

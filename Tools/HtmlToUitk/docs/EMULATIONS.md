# Conventions and emulation specifications (normative for the golden fixtures)

Every expected file in `fixtures/` follows this document. Where the Python oracle (`oracle/python`) produced the expected file the fixture is marked `oracle` in `fixture.json` (`expectedStatus`); all other expected files are `authored` from the rules below. If an implementation disagrees with a fixture, either the fixture or this document is wrong: report it with evidence to the architect.

## 1. Names, hashes, keys

- Hash: FNV-1a 32 bit over the UTF-8 bytes of the key (`h = 0x811C9DC5; for each byte: h ^= b; h = (h * 0x01000193) mod 2^32`), written as 8 lower-case hex digits (`hash8`).
- Generated class names are `htu-<kind>-<hash8>` with an optional suffix, and use these keys:

| Class | Key | Used by |
|---|---|---|
| `htu-s-<h>` | canonical text of the replaced selector fragment (a pseudo-class such as `:not(.on)`, `:nth-child(odd)`, `:first-child`, a sibling prefix such as `.a +` or `.a ~`, an attribute test such as `[type="text"]`) | static-selectors |
| `htu-before-<h>`, `htu-after-<h>` | canonical text of the pseudo-element selector, for example `.chip::before` | pseudo-elements |
| `htu-grid-row-<h>`, `htu-grid-rowgap-<h>`, `htu-grid-colgap-<h>`, `htu-grid-cell-<h>-<k>` | canonical selector text of the rule that declared `display: grid` | grid-flatten |
| `htu-gap-<h>`, `htu-gapwrap-<h>` | canonical selector text of the rule that declared `gap` | gap-margins |

- Canonical selector text: type and pseudo names lower-case, one space for a descendant combinator, ` > ` for a child combinator, `, ` between list items, no space inside pseudo arguments except after a comma, attribute values in double quotes (`[type="text"]`).
- Generated names never depend on traversal counters. The only counter is `k` in `htu-grid-cell-<h>-<k>`: the 1-based index of the distinct declaration block in first-use order (row-major).
- Generated classes are appended to the element's authored classes, in the order of the rules that created them.
- Generated elements have no `name`.

## 2. Diagnostics

- `file` is relative to the fixture's `input/` folder with forward slashes (`page.html`, `pages/home.html`, `css/shared.css`); job-level diagnostics use the job file name (`job.json`) and line 0.
- `line` is 1-based and points at the construct: the declaration, the rule selector, the at-rule, the HTML start tag, the attribute's tag, or the import/link line.
- `tier` and `emulation`: a construct handled by an emulation reports `ConvertTime` or `Runtime` and the emulation id with severity Info (exact) or Warning (approximate, or a runtime component is required); a rejected construct reports `Reject` and severity Error; Native constructs report `Native`.
- Comparison key: `(rule, severity, tier, emulation, file, line)`. Columns are compared only when `fixture.json` sets `compare.columns` to true. `related` (conflicts, cycles, duplicates) is compared when present in the expected file: the later definition is the primary location, the earlier one is in `related`.
- Expected files list diagnostics sorted by severity (Error, Warning, Info), then file, line, rule.
- Verdict: any Error is FAIL; otherwise any Warning is PASS WITH WARNINGS; otherwise PASS.
- `--force` mode (`"force": true` in the fixture): conversion proceeds although the verdict is FAIL. A rejected declaration or rule is dropped; elements are always written; an invalid key (L002) and a missing text key (L001) are written as authored (the text attribute is omitted when there is no key); pseudo-elements and pages that failed their check are not materialised. Lint findings in force mode are expected.
- Character references: numeric references and `amp lt gt quot apos nbsp` are decoded; any other named reference is D016.

## 3. USS writer

### 3.1 Formatting

UTF-8, no BOM, LF, four spaces, one declaration per line, a blank line between rules, a single trailing newline, `selector,\nselector {` for lists, no comments. Colours: `#rgb` and `#rrggbb` stay as written; every other colour is `rgb(r, g, b)` or `rgba(r, g, b, a)` with `a` trimmed (`0.5`, not `0.50`). Lengths lose redundant zeros (`.5px` becomes `0.5px`, `0px` becomes `0`). Percent opacity becomes a number. Variables keep `var(--name)`.

### 3.2 Declaration order inside a rule

Declarations sort by group, then by position in the group, then by source order; when a rule declares a shorthand together with one of its longhands the author's order is kept. Groups:

1. position, left, top, right, bottom, display, visibility, overflow, opacity
2. flex-direction, flex-wrap, flex-grow, flex-shrink, flex-basis, flex, align-content, align-items, align-self, justify-content, row-gap, column-gap
3. width, min-width, max-width, height, min-height, max-height, aspect-ratio
4. margin, margin-top, margin-right, margin-bottom, margin-left
5. padding, padding-top, padding-right, padding-bottom, padding-left
6. border-width, border-top-width, border-right-width, border-bottom-width, border-left-width, border-color, border-top-color, border-right-color, border-bottom-color, border-left-color, border-radius, border-top-left-radius, border-top-right-radius, border-bottom-right-radius, border-bottom-left-radius
7. background-color, background-image, background-position, background-position-x, background-position-y, background-repeat, background-size, -unity-background-scale-mode, -unity-background-image-tint-color, -unity-slice-left, -unity-slice-top, -unity-slice-right, -unity-slice-bottom, -unity-slice-scale, -unity-slice-type
8. color, -unity-font-definition, -unity-font, -unity-font-style, font-size, letter-spacing, word-spacing, -unity-text-align, white-space, text-overflow, text-shadow, -unity-text-outline, -unity-text-outline-width, -unity-text-outline-color
9. translate, rotate, scale, transform-origin, cursor, filter, backdrop-filter
10. transition-property, transition-duration, transition-timing-function, transition-delay
11. everything else in source order; custom properties (`--x`) first of all.

A property declared twice in one rule keeps the last declaration only.

### 3.3 Rule order

1. Rules are classified into sections: variables (`:root` with custom properties), layout (all declarations in: position left top right bottom display visibility overflow opacity flex flex-direction flex-wrap flex-grow flex-shrink flex-basis align-content align-items align-self justify-content width min-width max-width height min-height max-height aspect-ratio and the margin and padding families), components (everything else without a state), states (a pseudo-class in any selector, or a class matching naming.dynamicClassPattern), utilities (a single class matching uss.utilityPattern, default `^u-`). Responsive rules (section 6.9) come last.
2. Rules are emitted section by section in that order, stable by source position. When a rule moves across sections it must not change the cascade: if any pair of rules whose relative order would change declares a common property (equal name, or a shorthand and one of its longhands; two longhands of one shorthand do not conflict) and has at least one selector pair of equal specificity, the whole file keeps source order instead. Selector overlap is not analysed (conservative on purpose).
3. After ordering: a rule identical to a later rule (same selectors and declarations) is removed; adjacent rules with the same selector list merge, later declarations overriding earlier ones.
4. Rules left without declarations are dropped. Generated rules take the place of the rule that produced them (in the order the emulation creates them) and are classified like any other rule.
5. Tokens: `:root` custom properties go to the screen tokens file when the screen has no skin tokens of the same value (`Skin/<Screen>.tokens.uss` for single files, `Shared/Tokens.uss` for multi-file jobs); identical skin tokens are not repeated; a token that changes a skin value is written and reported (C034). `uss.tokenScopeSelector` additionally writes one rule with the tokens (and the skin tokens when `redefineSkinTokens`) at the top of the page USS.
6. Hidden state: when a page uses the hidden class (pages.classes.hidden, default `is-hidden`) and the sheets do not define it, `.is-hidden { display: none; }` is appended at the end.

## 4. UXML writer

- Header: `<?xml version="1.0" encoding="utf-8"?>`, then `<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">` (the `uie` declaration follows uxml.namespaces), then `<Style src="..." />` lines in cascade order, then `<ui:Template .../>` lines, then the elements, then `</ui:UXML>`.
- Style references are relative to the UXML folder with forward slashes. Pass-through USS files must lie under uiRoot and are referenced relative to `<uiRoot>/<screens>`.
- Attribute order: name, text, label, title, class, value, low-value, high-value, choices, index, placeholder-text, max-length, multiline, password, readonly, picking-mode, tooltip, enabled, focusable, tabindex, other attributes alphabetically, style last. The text attribute is `text` for Label, Button, Foldout, GroupBox, `label` for fields and toggles, `title` for ProgressBar.
- Empty elements are self-closed (`<ui:Label ... />`); children indent four spaces; hidden elements carry `style="display: none;"` only when the source used `hidden` or that exact inline style.
- `data-*` dialect attributes never reach the UXML except `data-uxml-<name>`, which becomes the UXML attribute `<name>`.

## 5. Dialect additions used by the fixtures

| Attribute or CSS | Meaning |
|---|---|
| `data-text` | mock text of controls that cannot hold text (inputs, select, progress, Foldout, GroupBox) |
| `data-loc`, `data-loc-tooltip`, `data-loc-placeholder` | key of the text, tooltip, placeholder |
| `data-loc-before`, `data-loc-after`, `data-text-before`, `data-text-after` | key and mock of pseudo-element text; the CSS uses `content: attr(data-text-after)` |
| `data-picking="ignore"` | picking-mode="Ignore" |
| `data-uxml`, `data-uxml-<name>` | explicit UXML element, UXML attribute passthrough |
| `data-uss` on `<link>` | the stylesheet is the CSS rendering of an existing USS; the value is the `src` written into the UXML |
| `data-uitk-harness` on `<link>`/`<script>` | mock harness, ignored |
| plain `<link rel="stylesheet" href="x.css">` | a CSS input of the job (resolved relative to the HTML file) |
| `data-include="file#id"` | partial include (section 8.4) |
| `data-page`, `data-start`, `data-nav`, `data-nav-mode`, `data-back`, `data-open-modal`, `data-modal`, `data-close`, `data-tabs`, `data-tab`, `data-focus`, `data-transition`, `data-transition-ms` | pages and navigation (section 7) |
| `--anim-stagger`, `--anim-reduced-motion`, `--anim-trigger` | animation options inside the rule that declares `animation` (section 6.7) |

## 6. Emulations

Common rule: a rewrite replaces the construct, keeps every other declaration of the rule, and reports a diagnostic at the construct.

### 6.1 `grid-flatten` (S17, S18)

Input: a rule with `display: grid` and `grid-template-columns` made of `fr`, `px` and `%` tracks and `repeat(<n>, <track>)`; optional `gap` and `grid-column: span <n>` on children. Rows are implicit.

1. Placement is row-major: a child occupies `span` columns (default 1); a child that does not fit in the rest of the row starts a new row.
2. The container keeps its element and classes; its children are replaced by one row wrapper `<ui:VisualElement class="htu-grid-row-<h>">` per grid row (the second and later rows also carry `htu-grid-rowgap-<h>`). An incomplete row is padded with `<ui:VisualElement class="htu-grid-cell-<h>-<k>" picking-mode="Ignore" />` fillers for the missing tracks.
3. Cell sizing by the spanned tracks: only `fr` tracks: `flex-grow: <sum>; flex-basis: 0`; a single `px` track: `flex-shrink: 0; width: <px>`; a single `%` track: `flex-shrink: 0; width: <percent>`; a span over mixed kinds is E001.
4. `gap: <row> <column>`: `htu-grid-colgap-<h>` (`margin-right: <column>`) on every cell that is not the last in its row (a cell followed by a filler counts as not last), `htu-grid-rowgap-<h>` (`margin-top: <row>`) on rows after the first. A gap together with a span greater than 1 is approximate: Warning.
5. Generated rules, in this order: row, rowgap, colgap (only the ones needed), then the cell blocks by `k`. The container's `display`, `grid-template-*`, `gap` and `grid-column` declarations are removed.
6. `auto-fill`, `auto-fit`, `minmax()`, named areas, `grid-auto-flow: dense`, explicit row templates: E001 Reject (the container is written unflattened under force).

### 6.2 `gap-margins` (S19, S20)

Rule with `gap` (or `row-gap`/`column-gap`) on a flex container; the direction comes from the same rule's `flex-direction` (default column).

- nowrap: row direction uses the column gap, column direction the row gap; the other value is ignored. Class `htu-gap-<h>` with `margin-right` (row) or `margin-bottom` (column) on every element child except the last.
- wrap: class `htu-gap-<h>` with `margin-right: <column>; margin-bottom: <row>` on every child and `htu-gapwrap-<h>` with the negated values on the container (generated rule order: gapwrap, then gap); Warning.
- Native (S21): the capability matrix contains `row-gap` and `column-gap`: `gap: a b` becomes `row-gap: a; column-gap: b` in the rule, no classes, Info.

### 6.3 `static-selectors` (S22 to S25)

Applies to `:not()`, `:is()`, `:where()`, `:nth-child()`, `:nth-of-type()`, `:first-child`, `:last-child`, `:only-child`, `:empty`, the sibling combinators `+` and `~`, and attribute selectors, evaluated on the static DOM of the converted page.

1. The fragment (pseudo-class, `<left> +`, `<left> ~`, attribute test) is replaced by the marker class `htu-s-<hash8(fragment)>`; for a sibling combinator the left part and the combinator disappear (`.a + .b` becomes `.b.htu-s-<h>`).
2. The marker is added to every element that satisfies the fragment together with the rest of its compound (ancestors are ignored: a marker may be added to an element that the full selector would not match; the rewritten selector still restricts by the other parts).
3. Specificity: the marker stands for the fragment, so class-level specificity is preserved: `:not(.x)` counts 1 like its argument, `nth-child` and the like count 1, `[a=b]` counts 1, a sibling prefix counts as the left compound (1 for a class). `:where()` counts 0 but the marker adds 1: the message says so (Info).
4. `:has()` is C003 Reject. Dynamic pseudo-classes (hover, active, focus, disabled, enabled, checked, root) stay in the selector after the marker.
5. `:nth-child(odd|even|an+b)` counts elements among all element siblings; `:nth-of-type(n)` among siblings with the same tag name; `:empty` is true for elements without element or non-whitespace text children.

### 6.4 `pseudo-elements` (S26, S27)

`.x::before` / `.x::after` on a container element (never Label or Button: E001): for every element matching `.x` insert a child first (before) or last (after). With `content: ""` the child is `<ui:VisualElement class="htu-before-<h>" picking-mode="Ignore" />`; with a glyph string it is `<ui:Label text="<glyph>" class="..." picking-mode="Ignore" />`; with `content: attr(data-text-after)` it is a Label whose `text` is the key from `data-loc-after` (error L001 when the key is missing). Raw text with letters in `content` is L001 Reject. The rule becomes `.htu-before-<h> { <declarations except content> }`.

### 6.5 `calc-fold`, `relative-units`, `viewport-units` (S28, S29)

Order: `em`/`rem`, then viewport units, then `calc()`.

- `rem` uses `emulation.rootFontSize` (default 16); `em` uses the element's own `font-size` declared in the same rule, otherwise the inherited static font-size, otherwise `rootFontSize`.
- `vw`, `vh`, `vmin`, `vmax` use `unity.referenceResolution` (default 1920x1080) and become px (`10vw` is `192px`).
- `calc()` folds when every term is a number or a length of one resolved unit (`+ - * /` with a number operand); mixed `%` and px terms are C006 Reject. `min()`, `max()`, `clamp()` of constants fold, otherwise Reject.
- Each converted declaration reports its own diagnostic (C008 with the emulation id, C006 for calc).

### 6.6 `overflow-scrollview`, `fixed-to-root`, `z-order`, `transform-decompose` (S31, S32, S38)

- `overflow: auto|scroll` or `overflow-y: auto|scroll` turns each matching element into a `ui:ScrollView` (mode Vertical, attribute omitted); `overflow-x` alone gives `mode="Horizontal"`; both axes `mode="VerticalAndHorizontal"`; the overflow declarations are dropped.
- `position: fixed` becomes `position: absolute` and the element moves to the end of the children of the first root element. `position: sticky` and `static` are C009 Reject.
- `z-index`: among siblings, elements with z-index move after the others sorted by z-index ascending (stable); the declaration is dropped; z-index across parents is E001.
- `transform` with `translate()`, `rotate()`, `scale()` in that order, each at most once, becomes `translate`, `rotate`, `scale` (single-function transforms are plain rewrites without a diagnostic); another order is E001; `matrix()`, 3D functions and `skew` are C022 Reject.

### 6.7 `keyframes-animator` (S39 to S42)

- Each `@keyframes` produces one animation id (the keyframes name; a second set of parameters on the same keyframes gets `-2`). `from` is offset 0, `to` is 1, `p%` is `p/100`. Step values are the USS values as strings (`"opacity": "0"`).
- `animation` shorthand tokens: name; times (first is duration, second delay, in seconds, `ms` converted); easing (CSS keyword or any Unity easing keyword; `cubic-bezier` and `steps` are R007); iteration count (integer or `infinite`); direction; fill mode. Defaults: delay 0, iterations 1, direction normal, fill none, easing ease.
- Each rule that declares `animation` produces a binding `{selector, animation, trigger}` with the rule's canonical selector; `trigger` defaults to `onShow` (`--anim-trigger`), `stagger` from `--anim-stagger` (seconds), `reducedMotion` from `--anim-reduced-motion`; the three custom properties and the animation declarations are removed from the USS.
- Checks: missing keyframes A001; animated property that is not a USS property or not animatable A002; layout property A003 (once per keyframes and property, at its first declaration; `rules.allowLayoutAnimation` lifts it); more than `rules.maxKeyframeSteps` steps A004; infinite or large animations without reduced-motion handling A005; invalid timing A006.
- Outputs: `<Screen>.anim.json` (animations in order of first use, bindings in rule order) and `<Screen>.runtime.json` with `UitkAnimator` (`uses` the anim file); each `@keyframes` reports C007 Warning at tier Runtime, emulation `keyframes-animator`.

### 6.8 Gradients, shadows, blur (S33 to S37)

- `linear-gradient`: Native when the matrix has the function; `to <side>` becomes degrees (top 0deg, right 90deg, bottom 180deg, left 270deg); warning C011 while the evidence is assembly-only.
- `radial-gradient`, `conic-gradient`: baked to `Generated/gradient-<hash8(canonical value|WxH)>.png`, `WxH` the rule's declared width and height in px or `256x256`; the declaration becomes `background-image: url("../Generated/gradient-<h>.png")` (path relative to the USS file). Pixel centres are (x+0.5, y+0.5); `circle` means farthest-corner. The golden runner checks `*.png.expect.json` (size and sample pixels within `tolerance`), never PNG bytes.
- `box-shadow` (outer): `filter: drop-shadow(<x> <y> <blur> <colour>)` when the matrix has the drop-shadow filter (Native, Warning C013: follows the rendered alpha, spread ignored); `inset` is C013 Reject; without drop-shadow the shadow-bake emulation applies (not in the first 60 fixtures).
- `backdrop-filter`: Native with Warning C010 (needs a Screen Space Overlay panel). When the matrix lacks the property or the blur filter: `BackdropBlur` runtime component, declaration dropped, `{"id": "BackdropBlur", "selector": <rule selector>, "params": {"radius": "<blur>"}}` in `<Screen>.runtime.json`, Warning at tier Runtime, emulation `backdrop-blur`.

### 6.9 `media-breakpoints` (S30)

`@media` conditions on `width`, `height`, `orientation` (and `aspect-ratio`) with `min-`/`max-` forms become a root class `mq-<parts>` where a part is `w-max-<n>`, `w-min-<n>`, `h-max-<n>`, `h-min-<n>`, `portrait`, `landscape` joined by `-` in the order min before max; each rule inside is emitted with the selector prefixed by `.mq-... ` (descendant), after all other rules in source order. `<Screen>.mq.json` lists `{class, query}` and the id of the first root element as `root`; the runtime manifest lists `BreakpointWatcher`. Other media types or features (print, hover, prefers-*) are C007 Reject. Each supported query reports C007 Warning, tier Runtime, emulation `media-breakpoints`.

## 7. Pages and navigation (S43, S44, M10)

- A page is an element with `data-page="<id>"` (or, in `singleDocument` jobs, the first root element of an input file, id = kebab-case file stem). The start page has `data-start`; without any start in a single-page flow P001 is raised, with several P002.
- `data-nav="<id>"` on a trigger: edge `navigate` with `mode` from `data-nav-mode` (`push` or `show`, default `push`), `transition` from the trigger's `data-transition`, `ms` from `data-transition-ms`. `data-back`: edge `back`. `data-open-modal="<id>"`: edge `openModal`. `data-close` inside a modal: listed in the modal's `closeTriggers`.
- A modal is an element with `data-modal="<id>"`; its owner page is the nearest ancestor page. Tabs: a container with `data-tabs="<group>"`; each tab trigger has `data-tab="<panel element name>"`; the default tab is the first one (or the one with `data-default`).
- Page transition (`data-transition`, `data-transition-ms` on the page element): `transition.in` and `transition.out` both take the value. Page `focus` is `data-focus` or the first interactive element.
- UXML: every page, modal and non-default tab panel is a normal element; all except the start page, the default tab panel and the elements that are not hidden by nature get the hidden class. Edges are written in document order of their triggers.
- Checks: P001 to P010 (see the fixture S44). `*.runtime.json` lists `UitkRouter` (or `NeoxiderUiKitAdapter` for the `neoxider-uikit` backend) and the diagnostic P011 (Info, tier Runtime) is reported once at the first page.

## 8. Multi-file jobs (M01 to M16)

### 8.1 Cascade

For each page the cascade is: `inputs.css` and `inputs.uss` entries (when `cssOrder` is `before`, the default; css first, then uss), then the page's `<link rel="stylesheet">` (CSS and `data-uss`) and `<style>` elements in document order with every `@import` expanded in place (depth first, source order), then the same job-level entries when `cssOrder` is `after`.

### 8.2 Output files

- A CSS file used by two or more pages, or listed in `inputs.css`, is shared: `Shared/<stem>.uss` (once, content-hash deduplicated). Its `:root` custom properties go to `Shared/Tokens.uss`; a shared CSS file that only holds `:root` writes no file of its own.
- Page-specific sheets (used by one page) and the page's `<style>` blocks merge into `<Screen>.uss` in cascade order (a page-specific sheet that precedes a shared sheet in the cascade gets its own `<Screen>.<stem>.uss` so that the cascade is preserved). A page without rules has no page USS.
- `<Style src>` lines follow the cascade: Tokens.uss, shared sheets in cascade order, pass-through USS, then the page USS.
- Screen name: `perFile[<input>].name`, else `meta uitk:screen`, else the PascalCase file stem; a clash gets `-2`, `-3` in path order and J012; `perFile[...].folder` is relative to the output root; the default folders come from `output.folders`.
- Pages with Errors are not written; the job verdict is the worst page verdict.

### 8.3 Conflicts and unused rules

- J001: for each page, a rule whose selector text equals an earlier rule's and which sets a different value for the same property is reported at the later rule with the nearest earlier definition in `related`.
- J005: a rule whose subject compound matches no element of any page of the job (classes in `naming.dynamicClasses` and matching `naming.dynamicClassPattern` are exempt).

### 8.4 Partials

`<template id="x">` in any input and `data-include="path#x"` on an empty element (path relative to the including file). `output.partials = "templates"` (default): `Components/<PascalId>.uxml` (header and elements only, no Style lines), `<ui:Template name="<PascalId>" src="../Components/<PascalId>.uxml" />` after the Style lines, and the use site becomes `<ui:Instance template="<PascalId>" name="<element name>" class="<element classes>" />`. `"inline"`: a wrapper `ui:VisualElement` with the include element's name and classes containing the partial's elements; names of the second and later copies get `-2`, `-3` (J011). Cycles are J007 at the include that closes the cycle.

### 8.5 `singleDocument`

One `<JobName>.uxml` (PascalCase of `name`) with a root `ui:VisualElement name="<job-kebab>-root"` containing the root element of each input file in path order, `<JobName>.uss`, shared sheets, `<JobName>.nav.json`, `<JobName>.runtime.json`. Names must be unique across the document (J004).

### 8.6 Idempotence

The commit stage writes a file only when its bytes differ from the file on disk. Running a job twice rewrites nothing; editing one input rewrites only the outputs derived from it (fixture M16: `runs`, `edits`, `expectRewrittenOnRun`, final files in `expected-final/`).

## 9. Golden runner contract

1. Copy the fixture to a temp folder; run the CLI (`validate` or `convert`, with `--force` when set; `run job.json` for jobs, with `output.root` overridden to a temp output folder).
2. Apply `capabilityPatch` to a copy of `spec/capabilities.<version>.json`: `add` inserts `value` under `key` of `path` (objects) or `item` into a list; `remove` deletes `key` or `item`.
3. Compare the produced diagnostics with `expected/diagnostics.json` (normalised, section 2), every expected file byte for byte after LF normalisation (JSON files canonically), PNG files through their `*.png.expect.json`, and the verdict with `expectVerdict`.
4. No extra files may be produced except lint noise listed in `notes`.
5. For `runs` greater than 1 repeat the run (applying `edits` before their run) and compare the files rewritten on that run with `expectRewrittenOnRun`.

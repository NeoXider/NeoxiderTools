# The HTML/CSS dialect

A mock is a normal HTML page that a browser can show. The converter accepts a strict subset so that every construct has a defined meaning in UI Toolkit. Everything outside the subset is a finding with a rule id (`docs/RULES.md`).

## 1. Page structure

```html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="uitk:screen" content="Login">                          <!-- screen name (file name of the output) -->
<meta name="uitk:strings" content="strings/Login.strings.json">    <!-- optional extra string table -->
<link rel="stylesheet" href="uitk-base.css" data-uitk-harness>     <!-- mock harness, ignored -->
<link rel="stylesheet" href="skin.css" data-uss="../Skin/Skin.uss"><!-- CSS rendering of an existing USS -->
<link rel="stylesheet" href="css/shared.css">                      <!-- a CSS input of the job -->
<style> ... page CSS ... </style>
</head>
<body>
  <div name="lg-root" class="lg"> ... </div>
</body>
</html>
```

- The root elements of `<body>` become the roots of the UXML.
- `<link data-uss="...">`: the value is the `src` that is written into `<Style src>` (relative to the UXML folder); the converter reads that USS from the project to know its classes and tokens. The CSS file is only for the browser (generate it from the USS).
- `<link data-uitk-harness>` and `<script data-uitk-harness>` are ignored. Any other script is rejected (D009); a stylesheet link that is not a local CSS file is rejected (D010); a missing file is J002/D015.
- `uitk-base.css` gives the browser the UI Toolkit defaults (column flex, relative positioning, border-box, `flex-shrink: 1`, `min-width: 0`, no margin collapse) so that the mock lays out like UI Toolkit. Without it the mock may differ from Unity in basic ways.

## 2. Elements

| HTML | UXML | Notes |
|---|---|---|
| `div section nav header footer main aside article` | `VisualElement` | no text allowed (D006) |
| `span label p h1`..`h6` | `Label` | no element children (D007); text from `data-loc` |
| `button` | `Button` | children allowed but warned (D008) |
| `input type=text` / `password` | `TextField` | `value`, `maxlength` becomes `max-length`, `readonly`, `password="true"` |
| `textarea` | `TextField` | `multiline="true"` |
| `input type=range` | `Slider` | `min`, `max`, `value` become `low-value`, `high-value`, `value`; `data-uxml="SliderInt"` for integers |
| `input type=checkbox` / `radio` | `Toggle` / `RadioButton` | `checked` becomes `value="true"` |
| `input type=number` | `IntegerField` | `data-uxml="FloatField"` for floats |
| `select` + `option` | `DropdownField` | `choices` is the comma-joined option keys, `selected` becomes `index` |
| `progress` | `ProgressBar` | `value`, `max` become `value`, `high-value` |
| `img` | `Image` | needs a `name`; `src` becomes a `#name { background-image }` rule with a project path |
| `data-uxml="ScrollView|Foldout|Box|GroupBox|RadioButtonGroup|MinMaxSlider|Scroller|TabView|Tab|ListView|TwoPaneSplitView|TextElement|VisualElement|..."` | the named element | explicit mapping for anything |

Not available (D001, with a suggested rewrite): `table`, `ul`, `li`, `a`, `strong`, `em`, `svg`, `canvas`, `video`, `iframe`, `form`, `pre` and the other HTML-only elements.

Attributes: `name` (or `id`, both equal), `class`, `hidden`, `disabled` (becomes `enabled="false"`), `tabindex`, `data-picking="ignore"` (becomes `picking-mode="Ignore"`), `data-uxml`, `data-uxml-<attribute>` (copied as `<attribute>`), `data-loc*`, `data-text`. Event handlers and `href` are rejected (D005), inline `style` is rejected (D011) except `display: none`; other attributes are dropped with a warning (D004). Character references: numeric and `amp lt gt quot apos nbsp` only (D016).

## 3. Text and localisation

- Every visible text carries a key: `<span data-loc="menu.title">Title</span>`. The UXML gets `text="menu.title"`; the text between the tags is the mock and is compared with the string table (L004).
- Controls that cannot hold text use `data-text` for the mock: `<input type="checkbox" data-loc="opt.sound" data-text="Sound">`. The key goes to `label` (fields, toggles), `text` (Label, Button, Foldout, GroupBox) or `title` (ProgressBar).
- `data-loc-tooltip` becomes `tooltip`, `data-loc-placeholder` becomes `placeholder-text`. A raw `title` or `placeholder` is L001.
- Glyph-only text (no letters: `+`, an arrow, a bullet) may stay raw.
- Keys missing from the string table are listed (L003) and written to a stub with placeholder locales (`[TODO] ...`); the table files are never modified.
- Table format: `{"version": 1, "<locale>": {"<key>": "<text>"}}` (one file) or one flat file per locale (`localization.format`).

## 4. Names and classes

Names are unique per document (N001), kebab-case (N002), carry a configured prefix (N005) and are required on interactive elements (N003) so code can query them. Classes follow `naming.classPattern` (kebab-case with optional BEM `__` and `--` parts, N004), a configured prefix (N005) and need a rule in the page CSS or a skin unless they are code-toggled (`naming.dynamicClassPattern`, default `^is-`; N007).

## 5. CSS

The page CSS becomes USS. Authors write real CSS so the browser shows the result; the converter maps or rejects each property.

| CSS | USS | Status |
|---|---|---|
| `display: flex|none`, `position: relative|absolute`, `left top right bottom`, `flex*`, `align-*`, `justify-content`, `width height min-* max-*`, `margin*`, `padding*`, `overflow: visible|hidden`, `opacity`, `visibility`, `white-space`, `text-overflow`, `font-size`, `letter-spacing`, `word-spacing`, `color`, `text-shadow` (one), `cursor`, `aspect-ratio`, `translate`, `rotate`, `scale`, `transform-origin`, `border-width/color/radius`, `background-color`, `background-image` (one `url()`), `background-position/size/repeat`, `transition*`, `filter`, `backdrop-filter` | same | Native |
| `border`, `border-<side>`, `border-style: solid|none` | `border-*-width`, `border-*-color` | rewrite |
| `background: <colour>` | `background-color` | rewrite |
| `font-weight`, `font-style` | `-unity-font-style` | rewrite |
| `font-family` | `-unity-font-definition` through `fonts` | rewrite (C031 when unmapped) |
| `text-align` | `-unity-text-align` (middle assumed, T001; write `-unity-text-align` yourself for the vertical part) | rewrite |
| `inset`, `flex-flow`, single-function `transform` | longhands, `translate`/`rotate`/`scale` | rewrite |
| `-unity-*` properties | same | Native (browsers ignore them) |
| `var(--x)`, `:root { --x: ... }` | same; new tokens go to the tokens file | Native |
| `display: grid`, `gap`, `:not()`, `:nth-child()`, `::before`, `calc()`, `vh`, `em`, `overflow:auto`, `z-index`, `position: fixed`, multi-function `transform`, `box-shadow`, radial gradients, `@media`, `@keyframes` | see `docs/EMULATIONS.md` section 6 | ConvertTime or Runtime |
| `position: sticky`, `:has()`, `!important`, `line-height`, `text-transform`, `outline`, `pointer-events`, `calc()` with mixed units, `cubic-bezier()` | none | Reject with advice |

Selectors: `.class`, `#name`, `*`, descendant, child, the pseudo-classes `:hover :active :focus :disabled :enabled :checked :root`, selector lists. Type selectors (`div`), attribute selectors and sibling combinators are emulated or rejected as listed above.

Colours: hex, `rgb()`, `rgba()` (modern space syntax is normalised), keywords; `hsl()` and `currentColor` are rejected. Lengths: `px` and `%`; a unitless non-zero length is invalid CSS (C023). Colour literals can be required to come from tokens (`rules.colorLiterals: "tokens-only"`).

## 6. Images and skins

`<img src>` and `background-image: url()` paths are relative to the HTML file and must resolve to a file inside the project root; they are written as project paths (`/Assets/...`). Slices use `-unity-slice-left/top/right/bottom` and `-unity-background-scale-mode`. An existing skin is declared with `<link data-uss>`; its tokens are not repeated and its classes are known.

## 7. Partials and includes

`<template id="card">...</template>` in any input, used with `<div name="home-card-1" class="slot" data-include="partials/card.html#card"></div>`. The include element must be empty; it becomes a `ui:Instance` (or a wrapper with the inlined elements). Include cycles are J007.

## 8. Pages and navigation

| Attribute | On | Meaning |
|---|---|---|
| `data-page="id"` | element | a page; `data-start` marks the start page |
| `data-nav="id"`, `data-nav-mode="push|show"` | trigger | navigate (default `push`) |
| `data-back` | trigger | back (closes the top modal first) |
| `data-modal="id"` | element | an overlay dialog owned by the enclosing page |
| `data-open-modal="id"`, `data-close` | trigger | open and close |
| `data-tabs="group"`, `data-tab="panel-name"`, `data-default` | container, trigger | tab group; the panel is the element with that name |
| `data-transition="fade|slide-left|slide-right|slide-up|scale|none"`, `data-transition-ms` | trigger or page | transition of the edge, or in/out of the page |
| `data-focus` | element | focus target when the page is shown |

Checks P001 to P010 (start page, unknown targets and transitions, orphan pages, duplicate modals, inconsistent tabs, missing focusable element). Output: `*.nav.json` and a runtime manifest. In the browser `uitk-prototype.js` implements the same semantics.

## 9. Animation

```css
@keyframes fade-in-up { from { opacity: 0; translate: 0 12px; } to { opacity: 1; translate: 0 0; } }
.card { animation: fade-in-up 0.4s ease-out forwards; --anim-stagger: 0.05s; --anim-reduced-motion: skip; }
```

Animate USS animatable, non-layout properties (`opacity`, colours, `translate`, `rotate`, `scale`). Layout properties are rejected (A003). `transition` needs no runtime component: toggle a class from code. Options live in custom properties of the rule: `--anim-trigger` (`onShow|onHide|onLoad|manual`), `--anim-stagger` (seconds between matched elements), `--anim-reduced-motion` (`skip|simplify`).

## 10. Design-time checks in the browser

`uitk-validate.js` applies the same rules to the page that is open (stylesheet rules and computed layout) and adds checks only a layout can answer: text wider than its box (X001), overlapping siblings (X002), minimum font size (R001), touch targets (R002), nowrap text without ellipsis (R003), missing keys (L001), duplicate names (N001). Offenders are outlined (red: Reject, dashed amber: warning or emulated). `window.uitkValidate.run()` re-runs it and `report()` returns JSON.

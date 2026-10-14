# Rule catalogue

Generated from `spec/rules.json` by `tools/gen_rules_doc.py`. Do not edit by hand.

Severity is the severity when the construct is rejected. A construct handled by an emulation is reported with the emulated severity at its tier (Native, ConvertTime, Runtime). See `docs/EMULATIONS.md` section 2.

## D: Dialect: elements, attributes, files

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| D001 | Error | Reject |  | HTML element without a UI Toolkit counterpart | UXML only contains VisualElement-derived controls; HTML-only elements such as tables, lists, headings or inline emphasis cannot be represented. | Use div, span, label, button, input, select, img, progress or data-uxml="<Element>". |
| D002 | Error | Reject |  | Unsupported input type | UI Toolkit has no native control for this input type. | Use type=text, range, checkbox, radio, number, or map the element explicitly with data-uxml. |
| D003 | Error | Reject |  | Unknown data-uxml value | data-uxml must name a known UI Toolkit element of the dialect. | Pick one of the documented data-uxml names (see README). |
| D004 | Warning | Native |  | Attribute is ignored by the conversion | The attribute has no UXML equivalent and is dropped. | Remove it or move the behaviour to code. |
| D005 | Error | Reject |  | Event handler or link attribute | UXML cannot carry inline scripts or hyperlinks. | Remove it; bind behaviour in C# by element name (and data-nav in the page flow). |
| D006 | Error | Reject |  | Text inside an element that cannot hold text | Only Label, Button (and controls with a label) show text; a VisualElement has no text property. | Wrap the text in <span data-loc="key">. |
| D007 | Error | Reject |  | Label with child elements | A UI Toolkit Label is a text element and cannot contain children. | Make the container a div and put the Label (span) and the other elements inside it. |
| D008 | Warning | Native |  | Button with child elements | A Button may contain children but they are drawn on top of its text and are not clipped by it. | Prefer a div container with a button and sibling elements, or keep the button text-only. |
| D009 | Error | Reject |  | Script not allowed in a convertible page | UXML has no scripting; scripts in the mock are only allowed for the mock harness (uitk-validate.js, uitk-prototype.js). | Remove the script, or mark the harness include with data-uitk-harness. |
| D010 | Error | Reject |  | Stylesheet link that is not a local CSS file | Local .css files are job inputs and data-uss declares an existing USS; remote URLs and other link types cannot be converted. | Link a local .css file, declare a skin with data-uss="<src as written in UXML>", or inline the CSS in a <style> block. |
| D011 | Error | Reject |  | Inline style attribute | Inline styles end up as inline UXML styles, which the style rules of the project forbid and which cannot use selectors, states or variables. | Move the declarations into a class in the <style> block. |
| D012 | Error | Reject |  | Image source cannot be resolved inside the project | UI Toolkit loads images from project assets, not from arbitrary paths or URLs. | Reference a file inside the project (src relative to the HTML file) or use a USS background-image. |
| D013 | Error | Reject |  | Missing element for the selected dialect feature | The element needs an attribute that the converter requires (for example a name for an img). | Add the missing attribute. |
| D014 | Warning | Native |  | Invalid UXML attribute value | The value cannot be represented as a UXML attribute. | Use a plain number or keyword. |
| D015 | Warning | Native |  | Skin stylesheet file not found | The class inventory of the skin cannot be checked, so classes it defines look undefined. | Fix the data-uss path (it is the src written into <Style src> relative to the UXML folder) or the config layout. |
| D016 | Error | Reject |  | Unsupported named character reference | Only numeric references and the predefined amp, lt, gt, quot, apos and nbsp are decoded; other named references are not part of the dialect. | Write the character itself or a numeric reference such as &#8594;. |

## H: HTML recovery

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| H001 | Warning | Native |  | End tag without a matching start tag | The HTML parser had to guess how to recover; browsers and the converter may disagree. | Remove the end tag or add the missing start tag. |
| H002 | Warning | Native |  | Element not closed | The HTML parser had to guess how to recover; browsers and the converter may disagree. | Close the element explicitly. |
| H003 | Warning | Native |  | Unterminated comment, tag, attribute or raw text | The HTML parser had to guess how to recover; browsers and the converter may disagree. | Close the construct at the reported position. |
| H004 | Warning | Native |  | Self-closing syntax on a non-void element | The parser ignores the self-closing slash on non-void elements, as browsers do. | Write the element with an explicit end tag. |
| H005 | Error | Native |  | Elements nested deeper than 512 | The parser refuses to nest deeper than 512 to bound stack use. | Flatten the markup. |

## L: Localisation

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| L001 | Error | Reject |  | Visible text without a localisation key | Project rule: the UI contains no raw text, only keys resolved through the string table. | Add data-loc="<key>" (the text between the tags stays as the mock text). |
| L002 | Error | Reject |  | Localisation key has an invalid format | Keys must match the key pattern of the project (see config localization.keyPattern). | Use dotted lower-case identifiers, for example menu.main.play. |
| L003 | Info | Native |  | New localisation key | The key is not in the string table; it is listed in the generated keys stub. | Add the key to the table (merge the stub) before shipping. |
| L004 | Warning | Native |  | Mock text differs from the string table | The mock should show the real text so the design review sees the real lengths. | Copy the text from the table (or update the table). |
| L005 | Warning | Native |  | Same key with different mock texts | One key resolves to one text at runtime; the mock shows two. | Use one text or two keys. |
| L006 | Error | Reject |  | data-loc on an element without a text property | The mapped UI Toolkit element has no text, label or title attribute. | Move data-loc to a span inside, or map the element to a control with text. |
| L007 | Warning | Native |  | Control with data-loc but no data-text | Inputs cannot hold text in HTML, so the mock label needs data-text to show the real length. | Add data-text="<mock text>". |

## N: Names and classes

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| N001 | Error | Reject |  | Duplicate element name | Names are used by code lookups (Q<T>("name")); duplicates make lookups ambiguous. | Rename one of the elements. |
| N002 | Warning | Native |  | Element name is not kebab-case | Project naming convention: lower-case kebab-case names. | Rename to lower-case words separated by '-'. |
| N003 | Warning | Native |  | Interactive element without a name | Code cannot look up an element without a name. | Add name="...". |
| N004 | Error | Reject |  | Class name does not match the naming pattern | Class names must match the project pattern (kebab-case with optional BEM __ and -- parts). | Rename the class. |
| N005 | Warning | Native |  | Name or class without an allowed prefix | Project convention: names and classes carry the prefix of their screen or component family. | Add one of the configured prefixes. |
| N006 | Error | Reject |  | id and name disagree | HTML id and name both map to the UXML name; they must be identical. | Keep only name. |
| N007 | Warning | Native |  | Class has no style rule | A class used in the markup is defined neither in the page CSS nor in a referenced skin stylesheet nor listed as a code-toggled class. | Add a rule, remove the class, or list it in usage.dynamicClasses. |

## C: CSS and USS constructs

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| C001 | Error | ConvertTime | grid-flatten | display: grid / inline-grid | UI Toolkit implements a subset of flexbox (Yoga); there is no grid layout. | Use nested flex rows and columns: flex-direction: row for a row, a column container per column. |
| C002 | Error | ConvertTime | gap-margins | gap / row-gap / column-gap | USS of Unity 6000.6 has no gap, row-gap or column-gap (not in the property reference nor in the UIElements module). | Use margins on the children, for example margin-right on every item but the last, or margin-top between rows. |
| C003 | Error | ConvertTime | static-selectors | :not() / :is() / :where() / :has() | USS selectors support only type, #name, .class, *, descendant, child and a fixed list of pseudo-classes. | Toggle an explicit class from code (for example .is-active) and style that class. |
| C004 | Error | ConvertTime | static-selectors | Structural pseudo-class | :nth-child(), :first-child, :last-child, :only-child, :empty and similar are not supported by USS. | Add an explicit class to the element (for example .first, .last) and select it. |
| C005 | Error | ConvertTime | pseudo-elements | Pseudo-element | ::before, ::after, ::placeholder and other pseudo-elements do not exist in UI Toolkit. | Add a real child element (a div or span) and style it. |
| C006 | Error | ConvertTime | calc-fold | calc() / min() / max() / clamp() / env() / attr() | USS values are literals, var() references or colour/url functions; there are no math functions. | Use fixed values, percentages, flex-grow/flex-basis or nested containers. |
| C007 | Error | Runtime | media-breakpoints | Unsupported at-rule | @media, @supports, @container, @layer, @keyframes, @font-face, @import and @page are not generated by this converter. | Express responsive rules with classes toggled from code (for example .ui--phone), and animations with transitions or the animation spec. |
| C008 | Error | ConvertTime | relative-units | Unsupported unit | USS lengths are px and %, angles deg, times s and ms; vh, vw, vmin, vmax, em, rem, pt, ch and fr do not exist. | Use px, %, or flex sizing. |
| C009 | Error | ConvertTime | fixed-to-root | Unsupported position value | USS supports position: relative (default) and absolute only. | Use relative or absolute; for sticky or fixed bars use an absolute child of the panel root. |
| C010 | Warning | Native |  | filter / backdrop-filter | Filters exist in Unity 6000.6 USS but only a fixed set of functions, they cost fill rate on mobile and the browser preview is not a faithful reference. | Check the result in Unity; prefer a pre-baked image or an overlay element. |
| C011 | Error | Native | gradient-bake | Gradient | USS has no gradient background; the project rules also forbid them. | Use a pre-rendered image (background-image: url()) or a solid colour. |
| C012 | Error | Reject |  | background shorthand or multiple backgrounds | USS has background-color, background-image (one image), background-position, background-size and background-repeat longhands only. | Split into the longhands; use child elements for extra layers. |
| C013 | Error | ConvertTime | shadow-bake | box-shadow | USS has no box-shadow (only text-shadow). | Add a sibling or child element with a darker, offset background, or use a border. |
| C014 | Error | Reject |  | Property is not a USS property | The property does not exist in the Unity 6000.6 USS property reference, so the importer ignores it. | See the suggested replacement. |
| C015 | Error | ConvertTime | static-selectors | Unsupported selector syntax | Sibling combinators (+, ~), attribute selectors and the >>> combinator are not supported by USS. | Use classes and the descendant (space) or child (>) combinator. |
| C016 | Error | Reject |  | Type selector | HTML tag selectors do not map to UI Toolkit element types; a div is not a VisualElement selector and element types match exactly. | Select by class (.name) or name (#name). |
| C017 | Error | Reject |  | !important | USS has no !important; precedence follows specificity and order. | Raise specificity with a more specific selector instead. |
| C018 | Error | Native |  | CSS-wide keyword | Only initial is supported in USS; inherit, unset and revert are not. | Write the explicit value, or use initial. |
| C019 | Error | Reject |  | Unsupported display value | USS display is flex (default) or none. | Use display: flex; every element is already a flex container (column by default). |
| C020 | Error | ConvertTime | overflow-scrollview | Unsupported overflow value | USS overflow is visible or hidden; there is no auto, scroll, overflow-x or overflow-y. | Use a ScrollView (data-uxml="ScrollView") for scrolling content. |
| C021 | Error | ConvertTime | z-order | z-index | UI Toolkit draws siblings in document order; there is no z-index. | Reorder the elements so later siblings draw on top, or use position: absolute children placed last. |
| C022 | Error | ConvertTime | transform-decompose | Unsupported transform | USS has separate rotate, scale and translate properties and no transform shorthand with several functions. | Use rotate: 45deg, scale: 1.1 or translate: 10px 0, one function per property. |
| C023 | Error | Reject |  | Invalid or unsupported value | The value is not valid for this USS property. | Use a value from the USS property reference. |
| C024 | Error | Reject |  | Unsupported border style | USS borders are always solid; there is no dashed, dotted or double border and no border-style property. | Use solid borders or an image. |
| C025 | Error | Reject |  | box-sizing other than border-box | UI Toolkit always uses border-box. | Remove box-sizing or use border-box and adjust the size. |
| C026 | Error | Reject |  | Several text shadows | USS text-shadow takes exactly one shadow. | Keep one shadow, or duplicate the label. |
| C028 | Error | Reject |  | Nested CSS rule | The converter reads flat rules only. | Flatten the nesting into separate selectors. |
| C029 | Error | Reject |  | CSS syntax error | The stylesheet cannot be parsed reliably; the declaration or block is dropped. | Fix the syntax (missing colon, semicolon or brace). |
| C030 | Warning | Native |  | text-overflow: ellipsis without clipping | Ellipsis only applies when overflow is hidden and white-space is nowrap. | Add overflow: hidden and white-space: nowrap. |
| C031 | Warning | Native |  | font-family without a font asset mapping | USS fonts are font assets (-unity-font-definition); the family name has no meaning in Unity. | Map the family in the config (fonts) or set -unity-font-definition in the page CSS. |
| C033 | Error | Reject |  | var() reference to an undefined custom property | An undefined var() makes UI Toolkit ignore the whole declaration. | Define the variable in :root (page CSS or a referenced skin) or write the literal value. |
| C034 | Warning | Native |  | Token redefines a skin token with a different value | The page tokens file is loaded after the skin, so the new value replaces the skin value for the whole screen. | Rename the token or align the value. |
| C035 | Error | Reject |  | Unsupported pseudo-class | USS supports :hover, :active, :inactive, :focus, :disabled, :enabled, :checked and :root. | Use one of the supported pseudo-classes or toggle a class from code. |

## T: Text mapping

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| T001 | Info | Native |  | text-align mapped to -unity-text-align | USS has a 9-position -unity-text-align instead of CSS text-align; vertical alignment was assumed to be middle. | Write -unity-text-align explicitly (for example middle-center) next to text-align. |

## U: UXML

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| U001 | Error | Reject |  | UXML syntax error | The UXML document is not well-formed XML. | Fix the markup at the reported position. |

## R: Project rules and transitions

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| R001 | Warning | Native |  | Font size below the project minimum | Small text is unreadable on a 1080p reference and on phones. | Raise font-size to the configured minimum. |
| R002 | Warning | Native |  | Touch target below the project minimum | Interactive elements smaller than the minimum are hard to hit with a finger or a gamepad cursor. | Raise min-height/min-width or the padding of the interactive element. |
| R003 | Warning | Native |  | nowrap text without ellipsis clipping | A single-line label wider than its box overflows visibly and overlaps its neighbours. | Add overflow: hidden and text-overflow: ellipsis (and a width or flex-shrink). |
| R004 | Warning | Native |  | Colour literal outside the token block | The project style rules require colours to come from tokens (var(--...)). | Move the colour into a :root token and reference it with var(). |
| R006 | Warning | Native |  | Transition on a property that does not animate | Discrete and non-animatable properties switch in one step; the transition has no effect. | Transition opacity, colours, translate, scale or rotate instead. |
| R007 | Error | Reject |  | Unsupported easing function | USS transition-timing-function accepts the keyword list of Unity only (ease, ease-in, ease-out, ease-in-out, linear and the extended ease-* names); no cubic-bezier() or steps(). | Use a keyword; ease-out-cubic is close to cubic-bezier(0.33, 1, 0.68, 1). |
| R008 | Warning | Native |  | Transition on a layout property | Animating width, height, margin, padding or position recalculates the layout every frame and can drop the frame rate. | Animate translate, scale, rotate or opacity instead. |
| R009 | Error | Reject |  | Invalid transition declaration | The transition list is malformed (wrong time unit, empty list or too many values). | Write transition: <property> <duration> [<easing>] [<delay>]. |

## E: Emulation failures

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| E001 | Error | Reject |  | Emulation could not be applied | The construct is emulated at conversion time but this input is outside what the emulation can resolve. | See the message for the unsupported part and the suggested rewrite. |

## J: Multi-file jobs

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| J001 | Warning | Native |  | Selector defined in several sheets with different values | The later sheet wins by cascade order; the two definitions are probably unintended. | Remove one definition or give the rules different selectors. |
| J002 | Error | Reject |  | Missing stylesheet, import or skin file | The input set refers to a file that does not exist. | Fix the path or add the file. |
| J003 | Error | Reject |  | Circular @import | A stylesheet imports itself directly or through other sheets. | Break the cycle. |
| J004 | Error | Reject |  | Duplicate element name across pages of one document | Names are used by code lookups; pages assembled into one document share one name scope. | Rename one of the elements or convert the pages per file. |
| J005 | Warning | Native |  | CSS rule that no page matches | The rule is dead weight in the shared stylesheet. | Remove the rule or list its class in naming.dynamicClasses. |
| J006 | Error | Reject |  | Missing partial or include target | The include or template reference cannot be resolved. | Fix the path or the id. |
| J007 | Error | Reject |  | Include cycle | A partial includes itself directly or through other partials. | Break the cycle. |
| J008 | Error | Reject |  | Navigation or modal target not found in the input set | data-nav and data-modal must point to a page or modal that exists in the assembled inputs. | Fix the target id or add the page. |
| J009 | Error | Reject |  | Two inputs map to the same output path | One file would overwrite the other. | Rename one page (meta uitk:screen) or set a per-file name in the job. |
| J010 | Warning | Native |  | Input pattern matched nothing | The glob or folder in the job has no files. | Fix the pattern. |
| J011 | Info | Native |  | Inlined partial renamed repeated elements | Names must stay unique when a partial is inlined more than once. | Use templates (ui:Instance) to keep one name scope per instance. |
| J012 | Info | Native |  | Output name collision resolved with a suffix | Two inputs derive the same screen name. | Set meta uitk:screen or a per-file name in the job. |

## P: Pages and navigation

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| P001 | Error | Reject |  | No start page | A flow with pages needs exactly one data-start page. | Add data-start to one page. |
| P002 | Error | Reject |  | More than one start page | The router needs a single start page. | Keep data-start on one page. |
| P003 | Error | Reject |  | Navigation target does not exist | data-nav points to a page id that is not defined. | Fix the id or add the page. |
| P004 | Warning | Native |  | Orphan page | No data-nav, data-modal or data-back leads to or from the page and it is not the start page. | Link the page or remove it. |
| P005 | Warning | Native |  | Unreachable page | No path from the start page reaches this page. | Add a navigation edge. |
| P006 | Warning | Native |  | Back from the start page | data-back on the start page has nothing to return to. | Remove it or define the quit behaviour in code. |
| P007 | Error | Reject |  | Duplicate modal id | Modal ids must be unique in the flow. | Rename one modal. |
| P008 | Error | Reject |  | Inconsistent tab group | Every data-tab needs a panel in the same group and the group needs a default. | Fix data-tabs, data-tab and the panel names. |
| P009 | Error | Reject |  | Unknown page transition | data-transition must be fade, slide-left, slide-right, slide-up, scale or none. | Use one of the known transitions. |
| P010 | Warning | Native |  | No focusable element on a page | Keyboard and gamepad navigation needs a focusable element on every page. | Add a button or mark a focus target. |
| P011 | Info | Runtime | page-router | Navigation requires a runtime component | Pages, modals and tabs are switched by the router component of the runtime package (or the NeoxiderUiKit adapter), not by USS. | Add the router component to the scene and load the generated nav.json. |

## A: Animation

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| A001 | Error | Reject |  | animation refers to a missing @keyframes | The animation name has no @keyframes rule. | Define the keyframes or fix the name. |
| A002 | Error | Reject |  | Unsupported animated property | The property is not animatable in USS (or not a USS property). | Animate opacity, colours, translate, scale or rotate. |
| A003 | Error | Reject |  | Layout property animated | Animating layout properties recalculates the layout each frame. | Animate translate or scale instead. |
| A004 | Warning | Native |  | Too many keyframe steps | Long step chains are costly and hard to maintain. | Reduce the number of steps. |
| A005 | Warning | Native |  | No reduced-motion handling | Looping or large motion should be skippable for players who disable motion. | Add data-reduced-motion="skip" or "simplify". |
| A006 | Error | Reject |  | Invalid animation timing or iteration | Duration, delay or iteration-count cannot be represented. | Use positive seconds or milliseconds and an integer or infinite. |

## X: Design-time checks (browser only)

| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |
|---|---|---|---|---|---|---|
| X001 | Warning | Native |  | Text is wider or taller than its box (browser design-time check) | In UI Toolkit overflowing text paints over its neighbours or is clipped, so the overlap seen in the browser also appears in Unity. | Widen the box, allow wrapping, or add overflow: hidden with text-overflow: ellipsis. |
| X002 | Warning | Native |  | Siblings overlap (browser design-time check) | Two in-flow siblings occupy the same area; the converted screen overlaps the same way. | Fix margins, widths or flex-shrink so the siblings do not overlap. |

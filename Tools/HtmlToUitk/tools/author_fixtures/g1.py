from fx import *

HEAD = '''<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="uitk:screen" content="%s">
<link rel="stylesheet" href="../../../assets/uitk-base.css" data-uitk-harness>
%s<style>
%s</style>
</head>
<body>
%s</body>
</html>
'''


def page(screen, css, body, extra_head=""):
    return HEAD % (screen, extra_head, css.strip("\n") + "\n", body.strip("\n") + "\n")


def strings(prefix, en, ru=None):
    d = {"version": 1, "en": en}
    if ru is not None:
        d["ru"] = ru
    return dump(d)


def cfg(strings_file, prefixes, locales=("en",), extra=None, ui_root="live"):
    c = {
        "projectRoot": ".",
        "uiRoot": ui_root,
        "localization": {"files": [strings_file], "locales": list(locales), "mockLocale": "en"},
        "naming": {"prefixes": prefixes},
    }
    if extra:
        for k, v in extra.items():
            if isinstance(v, dict) and k in c:
                c[k].update(v)
            else:
                c[k] = v
    return c


# S01
S01_CSS = '''
.mn {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
    align-items: center;
    justify-content: center;
}
.mn__title {
    font-size: 24px;
    margin-bottom: 12px;
}
.mn__action {
    height: 44px;
    min-width: 160px;
    background-color: rgba(30, 58, 58, 0.8);
    border-radius: 6px;
}
'''
S01_BODY = '''
<div name="mn-root" class="mn">
  <span name="mn-title" class="mn__title" data-loc="minimal.title">Welcome</span>
  <button name="mn-action" class="mn__action" data-loc="minimal.action">Start</button>
</div>
'''
make("S01", "minimal-page",
     ["div, span and button map to VisualElement, Label and Button",
      "USS is always generated next to the UXML",
      "UXML header with ui and uie namespaces and Style references in cascade order",
      "text attributes carry localisation keys, never raw text",
      "declaration order inside rules is canonical and output bytes are deterministic"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Minimal", S01_CSS, S01_BODY),
      "Localization/Minimal.strings.json": strings("minimal", {"minimal.title": "Welcome", "minimal.action": "Start"})},
     config=cfg("input/Localization/Minimal.strings.json", ["mn"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S02
S02_CSS = '''
.el { position: absolute; left: 0; top: 0; right: 0; bottom: 0; padding: 16px; }
.el__box { margin-bottom: 8px; }
.el__t { font-size: 16px; color: #f2eada; }
.el__b { height: 44px; min-width: 120px; }
.el__f { height: 44px; margin-bottom: 4px; }
.el__icon { width: 32px; height: 32px; }
.el__scroll { height: 80px; }
'''
S02_BODY = '''
<div name="el-root" class="el">
  <section name="el-section" class="el__box">
    <h1 name="el-h1" class="el__t" data-loc="el.h1">Heading</h1>
    <p name="el-p" class="el__t" data-loc="el.p">Paragraph</p>
    <label name="el-label" class="el__t" data-loc="el.label">Label</label>
  </section>
  <button name="el-button" class="el__b" data-loc="el.button">Press</button>
  <input type="text" name="el-text" class="el__f" data-loc="el.text" data-text="Name" value="abc" maxlength="12">
  <input type="password" name="el-pass" class="el__f" data-loc="el.pass" data-text="Password">
  <textarea name="el-area" class="el__f" data-loc="el.area" data-text="Notes"></textarea>
  <input type="range" name="el-slider" class="el__f" data-loc="el.slider" data-text="Volume" min="0" max="100" value="40">
  <input type="range" name="el-slider-int" class="el__f" data-uxml="SliderInt" data-loc="el.sliderint" data-text="Level" min="1" max="10" value="3">
  <input type="checkbox" name="el-check" class="el__f" data-loc="el.check" data-text="Enabled" checked>
  <input type="radio" name="el-radio" class="el__f" data-loc="el.radio" data-text="Choice">
  <input type="number" name="el-number" class="el__f" data-loc="el.number" data-text="Count" value="3">
  <select name="el-select" class="el__f" data-loc="el.select" data-text="Mode">
    <option data-loc="el.mode.a">Mode A</option>
    <option data-loc="el.mode.b" selected>Mode B</option>
  </select>
  <progress name="el-progress" class="el__f" data-loc="el.progress" data-text="Loading" value="30" max="100"></progress>
  <img name="el-icon" class="el__icon" src="images/icon.png">
  <div name="el-scroll" data-uxml="ScrollView" data-uxml-mode="Vertical" class="el__scroll">
    <span name="el-scroll-text" class="el__t" data-loc="el.scroll">Scrolling text</span>
  </div>
  <div name="el-fold" data-uxml="Foldout" data-loc="el.fold" data-text="Details" class="el__f">
    <span name="el-fold-text" class="el__t" data-loc="el.fold.text">Hidden text</span>
  </div>
  <div name="el-group" data-uxml="GroupBox" data-loc="el.group" data-text="Group" class="el__box"></div>
</div>
'''
S02_STR = {"el.h1": "Heading", "el.p": "Paragraph", "el.label": "Label", "el.button": "Press", "el.text": "Name",
           "el.pass": "Password", "el.area": "Notes", "el.slider": "Volume", "el.sliderint": "Level", "el.check": "Enabled",
           "el.radio": "Choice", "el.number": "Count", "el.select": "Mode", "el.mode.a": "Mode A", "el.mode.b": "Mode B",
           "el.progress": "Loading", "el.scroll": "Scrolling text", "el.fold": "Details", "el.fold.text": "Hidden text",
           "el.group": "Group"}
make("S02", "element-mapping",
     ["every dialect tag maps to its UXML element: div/section to VisualElement, span/label/p/h1 to Label, button, textarea to TextField multiline",
      "input types map to TextField, Slider, Toggle, RadioButton, IntegerField; data-uxml selects SliderInt, ScrollView, Foldout, GroupBox",
      "select/option become DropdownField choices and index; progress becomes ProgressBar; img becomes Image with a #name background rule",
      "value, min, max, checked, maxlength and data-uxml-* attributes map to UXML attributes"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Elements", S02_CSS, S02_BODY),
      "Localization/Elements.strings.json": strings("el", S02_STR),
      "images/icon.png": png_bytes(4, 4, (200, 160, 60, 255))},
     config=cfg("input/Localization/Elements.strings.json", ["el"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S03
S03_CSS = '''
.lk { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.lk__t { font-size: 16px; }
.lk__b { height: 44px; min-width: 120px; }
'''
S03_BODY = '''
<div name="lk-root" class="lk">
  <span name="lk-known" class="lk__t" data-loc="lk.known">Known text</span>
  <span name="lk-drift" class="lk__t" data-loc="lk.drift">Text that drifted</span>
  <span name="lk-new" class="lk__t" data-loc="lk.new">A brand new text</span>
  <span name="lk-same-a" class="lk__t" data-loc="lk.shared">Shared text</span>
  <span name="lk-same-b" class="lk__t" data-loc="lk.shared">Shared text</span>
  <span name="lk-conflict" class="lk__t" data-loc="lk.shared">Another text</span>
  <button name="lk-go" class="lk__b" data-loc="lk.go">Go</button>
</div>
'''
make("S03", "localisation-keys",
     ["data-loc keys become text attributes; the text between the tags is only the mock",
      "a key missing from the table is reported (L003) and listed in the generated newkeys stub with placeholder locales",
      "mock text that differs from the table (L004) and one key with two mock texts (L005) are warnings"],
     ["L003", "L004", "L005"], ["Native"], "convert", "PASS WITH WARNINGS",
     {"page.html": page("Keys", S03_CSS, S03_BODY),
      "Localization/Keys.strings.json": strings("lk", {"lk.known": "Known text", "lk.drift": "Original text", "lk.shared": "Shared text", "lk.go": "Go"},
                                                {"lk.known": "Известный текст", "lk.drift": "Исходный текст", "lk.shared": "Общий текст", "lk.go": "Вперёд"})},
     config=cfg("input/Localization/Keys.strings.json", ["lk"], locales=("en", "ru")), main=["page.html"], oracle=True,
     compare={"columns": False})

# S04
S04_CSS = '''
.gl { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.gl__t { font-size: 16px; }
.gl__b { height: 44px; min-width: 44px; }
.gl__f { height: 44px; }
'''
S04_BODY = '''
<div name="gl-root" class="gl">
  <button name="gl-plus" class="gl__b" data-loc-tooltip="gl.tip.plus">+</button>
  <span name="gl-close" class="gl__t">&#215;</span>
  <input type="text" name="gl-field" class="gl__f" data-loc="gl.field" data-text="Field" data-loc-placeholder="gl.hint" placeholder="Type here">
  <input type="text" name="gl-raw" class="gl__f" placeholder="Raw placeholder">
  <span name="gl-title" class="gl__t" title="Raw tooltip" data-loc="gl.title">Title</span>
  <span name="gl-visible" class="gl__t">Visible without key</span>
  <span name="gl-badkey" class="gl__t" data-loc="Bad Key">Bad key</span>
</div>
'''
make("S04", "glyph-tooltips-placeholders",
     ["glyph-only raw text such as + and the multiplication sign is allowed without a key",
      "data-loc-tooltip and data-loc-placeholder map to tooltip and placeholder-text keys",
      "raw title, raw placeholder, visible text without a key (L001) and a malformed key (L002) are rejected; --force still writes the convertible rest"],
     ["L001", "L002"], ["Native", "Reject"], "convert", "FAIL",
     {"page.html": page("Glyphs", S04_CSS, S04_BODY),
      "Localization/Glyphs.strings.json": strings("gl", {"gl.field": "Field", "gl.title": "Title", "gl.tip.plus": "Add", "gl.hint": "Type here"})},
     config=cfg("input/Localization/Glyphs.strings.json", ["gl"]), main=["page.html"], oracle=True, force=True,
     compare={"columns": False})

# S05
S05_CSS = '''
.nm { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.nm__t { font-size: 16px; }
'''
S05_BODY = '''
<div name="nm-root" class="nm">
  <span name="nm-dup" class="nm__t">+</span>
  <span name="nm-dup" class="nm__t">-</span>
  <span name="nmCamel" class="nm__t">+</span>
  <span name="other-prefix" class="nm__t">+</span>
  <span name="nm-ok" class="nm__t nmBadClass">+</span>
  <span name="nm-mismatch" id="nm-other" class="nm__t">+</span>
  <button class="nm__t" data-loc="nm.go">Go</button>
  <span name="nm-undef" class="nm__t nm__undefined">+</span>
</div>
'''
make("S05", "naming-rules",
     ["duplicate names (N001), non kebab-case names (N002), interactive element without a name (N003) are reported with the offending construct",
      "class pattern violations (N004) and missing prefixes (N005) are reported",
      "id and name that disagree (N006) fail; a class without a style rule (N007) is a warning by default"],
     ["N001", "N002", "N003", "N004", "N005", "N006", "N007"], ["Native", "Reject"], "validate", "FAIL",
     {"page.html": page("Naming", S05_CSS, S05_BODY),
      "Localization/Naming.strings.json": strings("nm", {"nm.go": "Go"})},
     config=cfg("input/Localization/Naming.strings.json", ["nm"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S06
S06_CSS = '''
.sh { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.sh__a {
    border: 1px solid #ffffff;
    border-bottom: 2px solid rgba(255, 255, 255, 0.5);
    margin: 0 auto;
    padding: 4px 8px 12px;
    border-radius: 4px 8px;
    flex-flow: row wrap;
    background: #112233;
}
.sh__b {
    inset: 0;
    transform: rotate(45deg);
    font-weight: 700;
    font-style: italic;
    border: none;
}
.sh__c {
    transform: translateY(8px);
    border-top: 3px solid var(--sh-line);
    background: transparent;
}
:root { --sh-line: #ccddee; }
'''
S06_BODY = '''
<div name="sh-root" class="sh">
  <div name="sh-a" class="sh__a"></div>
  <div name="sh-b" class="sh__b"></div>
  <div name="sh-c" class="sh__c"></div>
</div>
'''
make("S06", "shorthand-expansion",
     ["border, border-<side>, inset, flex-flow, background colour, border: none and single-function transform expand to USS longhands",
      "margin, padding and border-radius shorthands are kept as USS supports them",
      "values keep var() references; tokens move to the tokens file"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Shorthands", S06_CSS, S06_BODY)},
     config=cfg("input/none.json", ["sh"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S07
S07_CSS = '''
.vl { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.vl__a {
    color: rgb(255 128 0 / 50%);
    background-color: #abcd;
    border-color: RED;
    opacity: 50%;
    margin-left: .5px;
    padding-top: 0px;
    letter-spacing: 1.50px;
}
.vl__b { width: 10; color: hsl(120, 50%, 50%); height: var(--vl-missing); }
'''
S07_BODY = '''
<div name="vl-root" class="vl">
  <div name="vl-a" class="vl__a"></div>
  <div name="vl-b" class="vl__b"></div>
</div>
'''
make("S07", "value-normalisation",
     ["colours normalise to hex or rgba(r, g, b, a); modern rgb() syntax, 4-digit hex, keywords and percent opacity are converted",
      "numbers lose redundant zeros and units: .5px, 0px, 1.50px",
      "unitless lengths, hsl() and an undefined var() are rejected (C023, C033); --force writes the valid rest"],
     ["C023", "C033"], ["Native", "Reject"], "convert", "FAIL",
     {"page.html": page("Values", S07_CSS, S07_BODY)},
     config=cfg("input/none.json", ["vl"]), main=["page.html"], oracle=True, force=True,
     compare={"columns": False})

# S08
SKIN = '''
:root {
    --tk-ink: #0b1014;
    --tk-gold: #e4c07a;
    --tk-fs: 18px;
}

.tk-skin {
    color: var(--tk-gold);
}
'''
S08_CSS = '''
:root {
    --tk-ink: #0b1014;
    --tk-gold: #f0d090;
    --tk-accent: #3fbf9f;
}
.tk { position: absolute; left: 0; top: 0; right: 0; bottom: 0; background-color: var(--tk-ink); }
.tk__a { color: var(--tk-accent); font-size: var(--tk-fs); }
'''
S08_BODY = '''
<div name="tk-root" class="tk">
  <div name="tk-a" class="tk__a tk-skin"></div>
</div>
'''
make("S08", "tokens-and-skin",
     ["a skin declared with data-uss feeds known classes and tokens; identical tokens are not repeated, new tokens go to the screen tokens file",
      "a token that changes a skin value is emitted and reported (C034)",
      "uss.tokenScopeSelector redefines tokens (and the skin tokens) on the screen root class so nested hosting still resolves them"],
     ["C034"], ["Native"], "convert", "PASS WITH WARNINGS",
     {"page.html": page("Tokens", S08_CSS, S08_BODY,
                        '<link rel="stylesheet" href="skin.css" data-uss="../Skin/Skin.uss">\n'),
      "ui/Skin/Skin.uss": SKIN},
     config=cfg("input/none.json", ["tk"], ui_root="input/ui", extra={"uss": {"tokenScopeSelector": ".tk", "redefineSkinTokens": True}}),
     main=["page.html"], oracle=True, compare={"columns": False})

# S09
S09_CSS = '''
.u-hidden { display: none; }
.od__btn:hover { background-color: #335566; }
.od__row { flex-direction: row; align-items: center; }
.od__btn { color: #ffffff; background-color: #223344; height: 44px; }
:root { --od-ink: #111111; }
.od__row { flex-direction: row; align-items: center; }
.od__btn.is-active { border-width: 2px; }
.od__btn { border-radius: 6px; }
.od { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
'''
S09_BODY = '''
<div name="od-root" class="od">
  <div name="od-row" class="od__row u-hidden">
    <button name="od-btn" class="od__btn" data-loc="od.go">Go</button>
  </div>
</div>
'''
make("S09", "section-order-dedupe",
     ["USS sections are ordered variables, layout, components, states, utilities regardless of the source order",
      "exact duplicate rules are removed (the last one is kept) and adjacent rules with the same selector are merged",
      "declarations inside a rule follow the canonical order"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Order", S09_CSS, S09_BODY),
      "Localization/Order.strings.json": strings("od", {"od.go": "Go"})},
     config=cfg("input/Localization/Order.strings.json", ["od", "is", "u"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S10
S10_CSS = '''
.ps { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.ps__b { height: 44px; min-width: 120px; background-color: #223344; }
.ps__b:hover { background-color: #335566; }
.ps__b:active { background-color: #112233; }
.ps__b:focus { border-width: 2px; border-color: #8ff0d4; }
.ps__b:disabled { opacity: 0.5; }
.ps__t:checked { color: #8ff0d4; }
.ps__b:focus-visible { border-width: 3px; }
'''
S10_BODY = '''
<div name="ps-root" class="ps">
  <button name="ps-b" class="ps__b" data-loc="ps.b">Button</button>
  <input type="checkbox" name="ps-t" class="ps__t" data-loc="ps.t" data-text="Toggle">
</div>
'''
make("S10", "pseudo-states",
     ["hover, active, focus, disabled and checked pseudo-classes are native and kept",
      "an unsupported pseudo-class (:focus-visible) is rejected with the list of supported ones (C035)"],
     ["C035"], ["Native", "Reject"], "convert", "FAIL",
     {"page.html": page("States", S10_CSS, S10_BODY),
      "Localization/States.strings.json": strings("ps", {"ps.b": "Button", "ps.t": "Toggle"})},
     config=cfg("input/Localization/States.strings.json", ["ps"]), main=["page.html"], oracle=True, force=True,
     compare={"columns": False})

# S11
S11_CSS = '''
.tr { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.tr__a {
    opacity: 0.6;
    transition: opacity 0.2s ease-out, scale 120ms ease-in;
}
.tr__a:hover { opacity: 1; scale: 1.05; }
.tr__b {
    transition: all .3s;
}
.tr__c {
    width: 100px;
    background-image: url("images/panel.png");
    transition: width 0.2s linear 50ms, background-image 0.2s;
}
'''
S11_BODY = '''
<div name="tr-root" class="tr">
  <div name="tr-a" class="tr__a"></div>
  <div name="tr-b" class="tr__b"></div>
  <div name="tr-c" class="tr__c"></div>
</div>
'''
make("S11", "transitions-native",
     ["transition shorthand expands to transition-property, -duration, -timing-function and -delay longhands",
      "easing keywords are validated against the capability list; durations are normalised (.3s to 0.3s)",
      "transitions on discrete properties (background-image, R006) and layout properties (width, R008) are warnings"],
     ["R006", "R008"], ["Native"], "convert", "PASS WITH WARNINGS",
     {"page.html": page("Transitions", S11_CSS, S11_BODY),
      "images/panel.png": png_bytes(4, 4, (20, 40, 60, 255))},
     config=cfg("input/none.json", ["tr"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S12
S12_CSS = '''
.tx { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.tx__a { transition: opacity 0.2s cubic-bezier(0.4, 0, 0.2, 1); }
.tx__b { transition: opacity 0.2s steps(4); }
.tx__c { transition: boxy 0.2s; }
.tx__d { transition: opacity 200 ease; }
'''
S12_BODY = '''
<div name="tx-root" class="tx">
  <div name="tx-a" class="tx__a"></div>
</div>
'''
make("S12", "transition-rejects",
     ["cubic-bezier() and steps() are rejected (R007) with the nearest Unity easing as the rewrite",
      "an unknown transition property and a duration without a unit are rejected (C023, R009)"],
     ["C023", "R007", "R009"], ["Reject"], "validate", "FAIL",
     {"page.html": page("TransitionRejects", S12_CSS, S12_BODY)},
     config=cfg("input/none.json", ["tx"]), main=["page.html"], oracle=True,
     compare={"columns": False})

# S13
FONTS = {"Rubik": "/input/Fonts/Rubik.asset"}
S13_CSS = '''
.tf { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.tf__a { font-family: "Rubik", sans-serif; font-weight: 600; text-align: center; font-size: 18px; }
.tf__b { font-family: Georgia; font-style: italic; text-align: right; -unity-text-align: lower-right; }
.tf__c { white-space: nowrap; }
.tf__d { text-overflow: ellipsis; }
.tf__e { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; width: 120px; }
'''
S13_BODY = '''
<div name="tf-root" class="tf">
  <span name="tf-a" class="tf__a" data-loc="tf.a">Alpha</span>
  <span name="tf-b" class="tf__b" data-loc="tf.b">Beta</span>
  <span name="tf-c" class="tf__c" data-loc="tf.c">Gamma without clipping</span>
  <span name="tf-d" class="tf__d" data-loc="tf.d">Delta ellipsis only</span>
  <span name="tf-e" class="tf__e" data-loc="tf.e">Epsilon clipped properly</span>
</div>
'''
make("S13", "text-and-fonts",
     ["text-align maps to -unity-text-align (assumed middle) and reports T001; an explicit -unity-text-align wins",
      "font-weight and font-style map to -unity-font-style; font-family maps through config fonts to -unity-font-definition, an unmapped family is C031",
      "nowrap without ellipsis clipping (R003) and ellipsis without clipping (C030) are warnings"],
     ["C030", "C031", "R003", "T001"], ["Native"], "convert", "PASS WITH WARNINGS",
     {"page.html": page("Text", S13_CSS, S13_BODY),
      "Localization/Text.strings.json": strings("tf", {"tf.a": "Alpha", "tf.b": "Beta", "tf.c": "Gamma without clipping", "tf.d": "Delta ellipsis only", "tf.e": "Epsilon clipped properly"}),
      "Fonts/Rubik.asset": "font asset placeholder\n"},
     config=cfg("input/Localization/Text.strings.json", ["tf"], extra={"fonts": FONTS}), main=["page.html"], oracle=True,
     compare={"columns": False})

# S14
S14_CSS = '''
.im { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.im__panel {
    width: 200px;
    height: 100px;
    background-image: url("images/panel.png");
    -unity-slice-left: 2;
    -unity-slice-top: 2;
    -unity-slice-right: 2;
    -unity-slice-bottom: 2;
    -unity-background-scale-mode: stretch-to-fill;
}
.im__missing { background-image: url("images/missing.png"); }
.im__remote { background-image: url("https://example.com/a.png"); }
'''
S14_BODY = '''
<div name="im-root" class="im">
  <div name="im-panel" class="im__panel"></div>
  <div name="im-missing" class="im__missing"></div>
  <div name="im-remote" class="im__remote"></div>
  <img name="im-ok" src="images/panel.png">
  <img src="images/panel.png">
</div>
'''
make("S14", "images-and-slices",
     ["url() resolves to a project-root absolute path; slice properties and scale mode pass through",
      "<img> becomes an Image with a #name background rule; an <img> without a name is rejected (D013)",
      "a missing file and an external URL are rejected (D012)"],
     ["D012", "D013"], ["Native", "Reject"], "convert", "FAIL",
     {"page.html": page("Images", S14_CSS, S14_BODY),
      "images/panel.png": png_bytes(8, 8, (20, 40, 60, 255))},
     config=cfg("input/none.json", ["im"]), main=["page.html"], oracle=True, force=True,
     compare={"columns": False})

# S15
S15_CSS = '''
.hd { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.hd__t { font-size: 16px; }
.hd__hidden { display: none; }
'''
S15_BODY = '''
<div name="hd-root" class="hd">
  <span name="hd-a" class="hd__t" hidden>+</span>
  <span name="hd-b" class="hd__t" style="display: none;">+</span>
  <span name="hd-c" class="hd__t" style="color: red;">+</span>
  <span name="hd-d" class="hd__t hd__hidden">+</span>
</div>
'''
make("S15", "hidden-inline-style",
     ["the hidden attribute and style=\"display: none;\" become the single allowed inline style",
      "any other inline style is rejected with a move-to-class rewrite (D011); hidden state by class stays a plain class"],
     ["D011"], ["Native", "Reject"], "convert", "FAIL",
     {"page.html": page("Hidden", S15_CSS, S15_BODY)},
     config=cfg("input/none.json", ["hd"]), main=["page.html"], oracle=True, force=True,
     compare={"columns": False})

# S16
S16_CSS = '''
.dd { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.dd__f { height: 44px; }
'''
S16_BODY = '''
<div name="dd-root" class="dd">
  <select name="dd-mode" class="dd__f" data-loc="dd.mode" data-text="Mode">
    <option data-loc="dd.easy">Easy</option>
    <option data-loc="dd.normal" selected>Normal</option>
    <option data-loc="dd.hard">Hard</option>
  </select>
  <select name="dd-plain" class="dd__f">
    <option data-loc="dd.easy">Easy</option>
    <option>1</option>
  </select>
</div>
'''
make("S16", "select-dropdown",
     ["select and option become a DropdownField whose choices are the option keys and whose index is the selected option",
      "glyph-only option text is allowed raw"],
     [], ["Native"], "convert", "PASS",
     {"page.html": page("Dropdowns", S16_CSS, S16_BODY),
      "Localization/Dropdowns.strings.json": strings("dd", {"dd.mode": "Mode", "dd.easy": "Easy", "dd.normal": "Normal", "dd.hard": "Hard"})},
     config=cfg("input/Localization/Dropdowns.strings.json", ["dd"]), main=["page.html"], oracle=True,
     compare={"columns": False})

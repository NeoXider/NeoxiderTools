from g1 import *

UXML_HEAD = ('<?xml version="1.0" encoding="utf-8"?>\n'
             '<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">\n')
UXML_TAIL = '</ui:UXML>\n'


def uxml(styles, body):
    out = UXML_HEAD
    for s in styles:
        out += '    <Style src="%s" />\n' % s
    out += body.strip("\n") + "\n" + UXML_TAIL
    return out


def find_line(html, needle, nth=1):
    count = 0
    for i, line in enumerate(html.split("\n"), start=1):
        if needle in line:
            count += 1
            if count == nth:
                return i
    raise AssertionError("not found: " + needle)


def D(rule, severity, tier, file, line, emulation=None):
    d = {"rule": rule, "severity": severity, "tier": tier, "file": file, "line": line}
    if emulation:
        d["emulation"] = emulation
    return d


def mk(fid, title, proves, rules, tiers, mode, verdict, css, body, prefixes, expected, diags, screen,
       force=False, config_extra=None, caps="6000.6", patch=None, extra_files=None, notes=None, strings_=None, head_extra=""):
    html = page(screen, css, body, head_extra)
    files = {"page.html": html}
    if extra_files:
        files.update(extra_files)
    config = cfg("input/none.json", prefixes, extra=config_extra)
    if strings_:
        files["Localization/%s.strings.json" % screen] = strings("x", strings_)
        config = cfg("input/Localization/%s.strings.json" % screen, prefixes, extra=config_extra)
    make(fid, title, proves, rules, tiers, mode, verdict, files, config=config, main=["page.html"], force=force,
         caps=caps, patch=patch, expected=expected, diagnostics=diags, notes=notes, compare={"columns": False})
    return html


STYLES = lambda screen: [screen + ".uss"]

# ---------------------------------------------------------------- S17 grid-flatten-basic
h = fnv1a(".gr__grid")
css = '''
.gr { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.gr__grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; }
.gr__cell { height: 40px; background-color: #223344; }
'''
body = '''
<div name="gr-root" class="gr">
  <div name="gr-grid" class="gr__grid">
    <div name="gr-c1" class="gr__cell"></div>
    <div name="gr-c2" class="gr__cell"></div>
    <div name="gr-c3" class="gr__cell"></div>
    <div name="gr-c4" class="gr__cell"></div>
    <div name="gr-c5" class="gr__cell"></div>
  </div>
</div>
'''
exp_uxml = uxml(["Grid.uss"], '''
    <ui:VisualElement name="gr-root" class="gr">
        <ui:VisualElement name="gr-grid" class="gr__grid">
            <ui:VisualElement class="htu-grid-row-%(h)s">
                <ui:VisualElement name="gr-c1" class="gr__cell htu-grid-cell-%(h)s-1 htu-grid-colgap-%(h)s" />
                <ui:VisualElement name="gr-c2" class="gr__cell htu-grid-cell-%(h)s-1 htu-grid-colgap-%(h)s" />
                <ui:VisualElement name="gr-c3" class="gr__cell htu-grid-cell-%(h)s-1" />
            </ui:VisualElement>
            <ui:VisualElement class="htu-grid-row-%(h)s htu-grid-rowgap-%(h)s">
                <ui:VisualElement name="gr-c4" class="gr__cell htu-grid-cell-%(h)s-1 htu-grid-colgap-%(h)s" />
                <ui:VisualElement name="gr-c5" class="gr__cell htu-grid-cell-%(h)s-1 htu-grid-colgap-%(h)s" />
                <ui:VisualElement class="htu-grid-cell-%(h)s-1" picking-mode="Ignore" />
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>''' % {"h": h})
exp_uss = '''.gr {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.htu-grid-row-%(h)s {
    flex-direction: row;
}

.htu-grid-rowgap-%(h)s {
    margin-top: 8px;
}

.htu-grid-colgap-%(h)s {
    margin-right: 8px;
}

.htu-grid-cell-%(h)s-1 {
    flex-grow: 1;
    flex-basis: 0;
}

.gr__cell {
    height: 40px;
    background-color: #223344;
}
''' % {"h": h}
html = mk("S17", "grid-flatten-basic",
          ["a static grid (repeat(3, 1fr) plus gap) becomes nested flex rows: a row wrapper per grid row, cell classes with flex-grow and flex-basis 0",
           "gap becomes margin-right on all but the last cell of a row and margin-top on rows after the first",
           "an incomplete last row is padded with an invisible filler element so the tracks stay aligned"],
          ["C001"], ["ConvertTime"], "convert", "PASS", css, body, ["gr", "htu"],
          {"Screens/Grid.uxml": exp_uxml, "Screens/Grid.uss": exp_uss},
          [D("C001", "Info", "ConvertTime", "page.html", 0, "grid-flatten")], "Grid")
# patch the line number now that html is known
import json as _j, os as _o
p = ROOT + "/fixtures/S17-grid-flatten-basic/expected/diagnostics.json"
dd = _j.load(open(p, encoding="utf-8"))
dd[0]["line"] = find_line(html, "display: grid")
open(p, "w", encoding="utf-8", newline="\n").write(dump(dd))


def fix_lines(fixture_dir, html, specs):
    p = ROOT + "/fixtures/" + fixture_dir + "/expected/diagnostics.json"
    dd = _j.load(open(p, encoding="utf-8"))
    for item, (needle, nth) in zip(dd, specs):
        item["line"] = find_line(html, needle, nth)
    open(p, "w", encoding="utf-8", newline="\n").write(dump(dd))


# ---------------------------------------------------------------- S18 grid-spans-areas-reject
h = fnv1a(".sp__grid")
css = '''
.sp { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.sp__grid { display: grid; grid-template-columns: 100px 1fr 1fr; }
.sp__wide { grid-column: span 2; }
.sp__auto { display: grid; grid-template-columns: repeat(auto-fill, minmax(100px, 1fr)); }
.sp__cell { height: 30px; background-color: #334455; }
'''
body = '''
<div name="sp-root" class="sp">
  <div name="sp-grid" class="sp__grid">
    <div name="sp-a" class="sp__cell"></div>
    <div name="sp-b" class="sp__cell"></div>
    <div name="sp-c" class="sp__cell"></div>
    <div name="sp-e" class="sp__cell"></div>
    <div name="sp-d" class="sp__cell sp__wide"></div>
  </div>
  <div name="sp-auto" class="sp__auto">
    <div name="sp-x" class="sp__cell"></div>
    <div name="sp-y" class="sp__cell"></div>
  </div>
</div>
'''
exp_uxml = uxml(["Spans.uss"], '''
    <ui:VisualElement name="sp-root" class="sp">
        <ui:VisualElement name="sp-grid" class="sp__grid">
            <ui:VisualElement class="htu-grid-row-%(h)s">
                <ui:VisualElement name="sp-a" class="sp__cell htu-grid-cell-%(h)s-1" />
                <ui:VisualElement name="sp-b" class="sp__cell htu-grid-cell-%(h)s-2" />
                <ui:VisualElement name="sp-c" class="sp__cell htu-grid-cell-%(h)s-2" />
            </ui:VisualElement>
            <ui:VisualElement class="htu-grid-row-%(h)s">
                <ui:VisualElement name="sp-e" class="sp__cell htu-grid-cell-%(h)s-1" />
                <ui:VisualElement name="sp-d" class="sp__cell sp__wide htu-grid-cell-%(h)s-3" />
            </ui:VisualElement>
        </ui:VisualElement>
        <ui:VisualElement name="sp-auto" class="sp__auto">
            <ui:VisualElement name="sp-x" class="sp__cell" />
            <ui:VisualElement name="sp-y" class="sp__cell" />
        </ui:VisualElement>
    </ui:VisualElement>''' % {"h": h})
exp_uss = '''.sp {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.htu-grid-row-%(h)s {
    flex-direction: row;
}

.htu-grid-cell-%(h)s-1 {
    flex-shrink: 0;
    width: 100px;
}

.htu-grid-cell-%(h)s-2 {
    flex-grow: 1;
    flex-basis: 0;
}

.htu-grid-cell-%(h)s-3 {
    flex-grow: 2;
    flex-basis: 0;
}

.sp__cell {
    height: 30px;
    background-color: #334455;
}
''' % {"h": h}
html = mk("S18", "grid-spans-areas-reject",
          ["mixed px and fr tracks flatten into rows; a span over fr tracks becomes flex-grow of the sum; a px track becomes a fixed width without shrink",
           "distinct declaration blocks are numbered in first-use order (cell classes -1, -2, -3)",
           "repeat(auto-fill, minmax()) cannot be resolved statically: E001 Reject, the container is emitted unflattened under --force"],
          ["C001", "E001"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["sp", "htu"],
          {"Screens/Spans.uxml": exp_uxml, "Screens/Spans.uss": exp_uss},
          [D("E001", "Error", "Reject", "page.html", 0),
           D("C001", "Info", "ConvertTime", "page.html", 0, "grid-flatten")], "Spans", force=True)
fix_lines("S18-grid-spans-areas-reject", html, [("repeat(auto-fill", 1), ("display: grid; grid-template-columns: 100px", 1)])

# ---------------------------------------------------------------- S19 gap-nowrap
h1 = fnv1a(".gp__row")
h2 = fnv1a(".gp__col")
css = '''
.gp { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.gp__row { flex-direction: row; gap: 10px 20px; }
.gp__col { gap: 6px; }
.gp__item { width: 40px; height: 40px; background-color: #335566; }
'''
body = '''
<div name="gp-root" class="gp">
  <div name="gp-row" class="gp__row">
    <div name="gp-i1" class="gp__item"></div>
    <div name="gp-i2" class="gp__item"></div>
    <div name="gp-i3" class="gp__item"></div>
  </div>
  <div name="gp-col" class="gp__col">
    <div name="gp-j1" class="gp__item"></div>
    <div name="gp-j2" class="gp__item"></div>
  </div>
</div>
'''
exp_uxml = uxml(["Gaps.uss"], '''
    <ui:VisualElement name="gp-root" class="gp">
        <ui:VisualElement name="gp-row" class="gp__row">
            <ui:VisualElement name="gp-i1" class="gp__item htu-gap-%(h1)s" />
            <ui:VisualElement name="gp-i2" class="gp__item htu-gap-%(h1)s" />
            <ui:VisualElement name="gp-i3" class="gp__item" />
        </ui:VisualElement>
        <ui:VisualElement name="gp-col" class="gp__col">
            <ui:VisualElement name="gp-j1" class="gp__item htu-gap-%(h2)s" />
            <ui:VisualElement name="gp-j2" class="gp__item" />
        </ui:VisualElement>
    </ui:VisualElement>''' % {"h1": h1, "h2": h2})
exp_uss = '''.gp {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.gp__row {
    flex-direction: row;
}

.htu-gap-%(h1)s {
    margin-right: 20px;
}

.htu-gap-%(h2)s {
    margin-bottom: 6px;
}

.gp__item {
    width: 40px;
    height: 40px;
    background-color: #335566;
}
''' % {"h1": h1, "h2": h2}
html = mk("S19", "gap-nowrap",
          ["gap in a nowrap row container becomes margin-right (column-gap) on all children but the last; the row-gap value is ignored",
           "gap in a column container becomes margin-bottom (row-gap) on all children but the last",
           "the generated rule replaces the emptied gap rule at its position and the container rule keeps its other declarations"],
          ["C002"], ["ConvertTime"], "convert", "PASS", css, body, ["gp", "htu"],
          {"Screens/Gaps.uxml": exp_uxml, "Screens/Gaps.uss": exp_uss},
          [D("C002", "Info", "ConvertTime", "page.html", 0, "gap-margins"), D("C002", "Info", "ConvertTime", "page.html", 0, "gap-margins")], "Gaps")
fix_lines("S19-gap-nowrap", html, [("gap: 10px 20px", 1), ("gap: 6px", 1)])

# ---------------------------------------------------------------- S20 gap-wrap
h = fnv1a(".gw__wrap")
css = '''
.gw { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.gw__wrap { flex-direction: row; flex-wrap: wrap; gap: 8px 12px; }
.gw__item { width: 60px; height: 30px; background-color: #335566; }
'''
body = '''
<div name="gw-root" class="gw">
  <div name="gw-wrap" class="gw__wrap">
    <div name="gw-a" class="gw__item"></div>
    <div name="gw-b" class="gw__item"></div>
    <div name="gw-c" class="gw__item"></div>
  </div>
</div>
'''
exp_uxml = uxml(["Wrap.uss"], '''
    <ui:VisualElement name="gw-root" class="gw">
        <ui:VisualElement name="gw-wrap" class="gw__wrap htu-gapwrap-%(h)s">
            <ui:VisualElement name="gw-a" class="gw__item htu-gap-%(h)s" />
            <ui:VisualElement name="gw-b" class="gw__item htu-gap-%(h)s" />
            <ui:VisualElement name="gw-c" class="gw__item htu-gap-%(h)s" />
        </ui:VisualElement>
    </ui:VisualElement>''' % {"h": h})
exp_uss = '''.gw {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.gw__wrap {
    flex-direction: row;
    flex-wrap: wrap;
}

.htu-gapwrap-%(h)s {
    margin-right: -12px;
    margin-bottom: -8px;
}

.htu-gap-%(h)s {
    margin-right: 12px;
    margin-bottom: 8px;
}

.gw__item {
    width: 60px;
    height: 30px;
    background-color: #335566;
}
''' % {"h": h}
html = mk("S20", "gap-wrap",
          ["gap with flex-wrap becomes trailing margins on every child plus negative margins on the container (class htu-gapwrap)",
           "the diagnostic is a warning because the container box grows by the gap and wrapping is approximate"],
          ["C002"], ["ConvertTime"], "convert", "PASS WITH WARNINGS", css, body, ["gw", "htu"],
          {"Screens/Wrap.uxml": exp_uxml, "Screens/Wrap.uss": exp_uss},
          [D("C002", "Warning", "ConvertTime", "page.html", 0, "gap-margins")], "Wrap")
fix_lines("S20-gap-wrap", html, [("gap: 8px 12px", 1)])

# ---------------------------------------------------------------- S21 gap-native-capable
css = '''
.rg { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.rg__row { flex-direction: row; gap: 6px 12px; }
.rg__item { width: 40px; height: 40px; background-color: #335566; }
'''
body = '''
<div name="rg-root" class="rg">
  <div name="rg-row" class="rg__row">
    <div name="rg-a" class="rg__item"></div>
    <div name="rg-b" class="rg__item"></div>
  </div>
</div>
'''
exp_uxml = uxml(["NativeGap.uss"], '''
    <ui:VisualElement name="rg-root" class="rg">
        <ui:VisualElement name="rg-row" class="rg__row">
            <ui:VisualElement name="rg-a" class="rg__item" />
            <ui:VisualElement name="rg-b" class="rg__item" />
        </ui:VisualElement>
    </ui:VisualElement>''')
exp_uss = '''.rg {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.rg__row {
    flex-direction: row;
    row-gap: 6px;
    column-gap: 12px;
}

.rg__item {
    width: 40px;
    height: 40px;
    background-color: #335566;
}
'''
patch = [{"op": "add", "path": "properties", "key": "row-gap", "value": {"animatable": "full", "evidence": "override"}},
         {"op": "add", "path": "properties", "key": "column-gap", "value": {"animatable": "full", "evidence": "override"}}]
html = mk("S21", "gap-native-capable",
          ["when the capability matrix contains row-gap and column-gap the same gap input stays Native and expands to the two longhands",
           "no generated classes and no margins: the tier is decided by the matrix, not by the construct"],
          ["C002"], ["Native"], "convert", "PASS", css, body, ["rg"],
          {"Screens/NativeGap.uxml": exp_uxml, "Screens/NativeGap.uss": exp_uss},
          [D("C002", "Info", "Native", "page.html", 0)], "NativeGap", patch=patch,
          notes="Uses a patched copy of the 6000.6 matrix; the real 6000.6 matrix has no gap properties (see S19, S20).")
fix_lines("S21-gap-native-capable", html, [("gap: 6px 12px", 1)])

# ---------------------------------------------------------------- S22 not-is-where
hn = fnv1a(":not(.on)")
hi = fnv1a(":is(.a, .b)")
hw = fnv1a(":where(.c)")
css = '''
.sl { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.sl__item { height: 30px; }
.sl__item:not(.sl__on) { color: #111111; }
.sl__item:is(.sl__a, .sl__b) { color: #222222; }
.sl__item:where(.sl__c) { color: #333333; }
.sl__item:has(.sl__x) { color: #444444; }
'''
css = css.replace(".sl__item:not(.sl__on)", ".sl__item:not(.sl__on)")
hn = fnv1a(":not(.sl__on)")
hi = fnv1a(":is(.sl__a, .sl__b)")
hw = fnv1a(":where(.sl__c)")
body = '''
<div name="sl-root" class="sl">
  <div name="sl-1" class="sl__item"></div>
  <div name="sl-2" class="sl__item sl__on"></div>
  <div name="sl-3" class="sl__item sl__a"></div>
  <div name="sl-4" class="sl__item sl__c"></div>
</div>
'''
exp_uxml = uxml(["Selectors.uss"], '''
    <ui:VisualElement name="sl-root" class="sl">
        <ui:VisualElement name="sl-1" class="sl__item htu-s-%(hn)s" />
        <ui:VisualElement name="sl-2" class="sl__item sl__on" />
        <ui:VisualElement name="sl-3" class="sl__item sl__a htu-s-%(hn)s htu-s-%(hi)s" />
        <ui:VisualElement name="sl-4" class="sl__item sl__c htu-s-%(hn)s htu-s-%(hw)s" />
    </ui:VisualElement>''' % {"hn": hn, "hi": hi, "hw": hw})
exp_uss = '''.sl {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.sl__item {
    height: 30px;
}

.sl__item.htu-s-%(hn)s {
    color: #111111;
}

.sl__item.htu-s-%(hi)s {
    color: #222222;
}

.sl__item.htu-s-%(hw)s {
    color: #333333;
}
''' % {"hn": hn, "hi": hi, "hw": hw}
html = mk("S22", "not-is-where",
          [":not(), :is() and :where() are evaluated against the static DOM; matching elements get a marker class htu-s-<fnv1a of the pseudo text>",
           "the rewritten selector keeps the other compound parts and replaces only the unsupported pseudo with the marker",
           ":has() cannot be evaluated by a marker: C003 Reject; the rule is dropped under --force"],
          ["C003"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["sl", "htu"],
          {"Screens/Selectors.uxml": exp_uxml, "Screens/Selectors.uss": exp_uss},
          [D("C003", "Error", "Reject", "page.html", 0),
           D("C003", "Info", "ConvertTime", "page.html", 0, "static-selectors"),
           D("C003", "Info", "ConvertTime", "page.html", 0, "static-selectors"),
           D("C003", "Info", "ConvertTime", "page.html", 0, "static-selectors")], "Selectors", force=True,
          notes=":where() contributes no specificity but the marker class adds one; the converter reports it in the message (INFO).")
fix_lines("S22-not-is-where", html, [(":has(", 1), (":not(", 1), (":is(", 1), (":where(", 1)])

# ---------------------------------------------------------------- S23 nth-child
ho = fnv1a(":nth-child(odd)")
he = fnv1a(":nth-child(even)")
ht = fnv1a(":nth-of-type(3)")
css = '''
.nt { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.nt__cell { height: 24px; }
.nt__cell:nth-child(odd) { color: #111111; }
.nt__cell:nth-child(even) { color: #222222; }
.nt__cell:nth-of-type(3) { color: #333333; }
'''
cells = "".join('  <div name="nt-%d" class="nt__cell"></div>\n' % i for i in range(1, 7))
body = '<div name="nt-root" class="nt">\n' + cells + '</div>\n'
classes = {1: "nt__cell htu-s-%s" % ho, 2: "nt__cell htu-s-%s" % he, 3: "nt__cell htu-s-%s htu-s-%s" % (ho, ht),
           4: "nt__cell htu-s-%s" % he, 5: "nt__cell htu-s-%s" % ho, 6: "nt__cell htu-s-%s" % he}
inner = "".join('        <ui:VisualElement name="nt-%d" class="%s" />\n' % (i, classes[i]) for i in range(1, 7))
exp_uxml = uxml(["NthChild.uss"], '    <ui:VisualElement name="nt-root" class="nt">\n' + inner + '    </ui:VisualElement>')
exp_uss = '''.nt {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.nt__cell {
    height: 24px;
}

.nt__cell.htu-s-%s {
    color: #111111;
}

.nt__cell.htu-s-%s {
    color: #222222;
}

.nt__cell.htu-s-%s {
    color: #333333;
}
''' % (ho, he, ht)
html = mk("S23", "nth-child",
          [":nth-child(odd), :nth-child(even) and :nth-of-type(3) resolve by the element position among its siblings",
           "an element matched by two markers gets both, in rule order; the cascade position of each rule is preserved"],
          ["C004"], ["ConvertTime"], "convert", "PASS", css, body, ["nt", "htu"],
          {"Screens/NthChild.uxml": exp_uxml, "Screens/NthChild.uss": exp_uss},
          [D("C004", "Info", "ConvertTime", "page.html", 0, "static-selectors")] * 3, "NthChild")
fix_lines("S23-nth-child", html, [(":nth-child(odd)", 1), (":nth-child(even)", 1), (":nth-of-type(3)", 1)])

# ---------------------------------------------------------------- S24 first-last-only
hf = fnv1a(":first-child")
hl = fnv1a(":last-child")
hy = fnv1a(":only-child")
he2 = fnv1a(":empty")
css = '''
.fl { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.fl__li { height: 24px; }
.fl__li:first-child { color: #111111; }
.fl__li:last-child { color: #222222; }
.fl__solo:only-child { color: #333333; }
.fl__void:empty { color: #444444; }
'''
body = '''
<div name="fl-root" class="fl">
  <div name="fl-list">
    <div name="fl-1" class="fl__li"></div>
    <div name="fl-2" class="fl__li"></div>
    <div name="fl-3" class="fl__li"></div>
  </div>
  <div name="fl-box">
    <div name="fl-solo" class="fl__solo"></div>
  </div>
  <div name="fl-void" class="fl__void"></div>
</div>
'''
exp_uxml = uxml(["FirstLast.uss"], '''
    <ui:VisualElement name="fl-root" class="fl">
        <ui:VisualElement name="fl-list">
            <ui:VisualElement name="fl-1" class="fl__li htu-s-%(hf)s" />
            <ui:VisualElement name="fl-2" class="fl__li" />
            <ui:VisualElement name="fl-3" class="fl__li htu-s-%(hl)s" />
        </ui:VisualElement>
        <ui:VisualElement name="fl-box">
            <ui:VisualElement name="fl-solo" class="fl__solo htu-s-%(hy)s" />
        </ui:VisualElement>
        <ui:VisualElement name="fl-void" class="fl__void htu-s-%(he)s" />
    </ui:VisualElement>''' % {"hf": hf, "hl": hl, "hy": hy, "he": he2})
exp_uss = '''.fl {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.fl__li {
    height: 24px;
}

.fl__li.htu-s-%(hf)s {
    color: #111111;
}

.fl__li.htu-s-%(hl)s {
    color: #222222;
}

.fl__solo.htu-s-%(hy)s {
    color: #333333;
}

.fl__void.htu-s-%(he)s {
    color: #444444;
}
''' % {"hf": hf, "hl": hl, "hy": hy, "he": he2}
html = mk("S24", "first-last-only",
          [":first-child, :last-child, :only-child and :empty resolve statically into marker classes",
           "an element without children counts as empty; whitespace-only content counts as empty"],
          ["C004"], ["ConvertTime"], "convert", "PASS", css, body, ["fl", "htu"],
          {"Screens/FirstLast.uxml": exp_uxml, "Screens/FirstLast.uss": exp_uss},
          [D("C004", "Info", "ConvertTime", "page.html", 0, "static-selectors")] * 4, "FirstLast")
fix_lines("S24-first-last-only", html, [(":first-child", 1), (":last-child", 1), (":only-child", 1), (":empty", 1)])

# ---------------------------------------------------------------- S25 sibling-attribute-selectors
hs = fnv1a(".sa__a +")
ht2 = fnv1a(".sa__a ~")
ha = fnv1a('[type="text"]')
css = '''
.sa { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.sa__a { height: 20px; }
.sa__b { height: 20px; }
.sa__c { height: 20px; }
.sa__f { height: 44px; }
.sa__a + .sa__b { color: #111111; }
.sa__a ~ .sa__c { color: #222222; }
.sa__f[type="text"] { color: #333333; }
'''
body = '''
<div name="sa-root" class="sa">
  <div name="sa-a" class="sa__a"></div>
  <div name="sa-b" class="sa__b"></div>
  <div name="sa-c1" class="sa__c"></div>
  <div name="sa-c2" class="sa__c"></div>
  <input type="text" name="sa-text" class="sa__f" data-loc="sa.text" data-text="Name">
  <input type="checkbox" name="sa-check" class="sa__f" data-loc="sa.check" data-text="Check">
</div>
'''
exp_uxml = uxml(["Siblings.uss"], '''
    <ui:VisualElement name="sa-root" class="sa">
        <ui:VisualElement name="sa-a" class="sa__a" />
        <ui:VisualElement name="sa-b" class="sa__b htu-s-%(hs)s" />
        <ui:VisualElement name="sa-c1" class="sa__c htu-s-%(ht)s" />
        <ui:VisualElement name="sa-c2" class="sa__c htu-s-%(ht)s" />
        <ui:TextField name="sa-text" label="sa.text" class="sa__f htu-s-%(ha)s" />
        <ui:Toggle name="sa-check" label="sa.check" class="sa__f" />
    </ui:VisualElement>''' % {"hs": hs, "ht": ht2, "ha": ha})
exp_uss = '''.sa {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.sa__a {
    height: 20px;
}

.sa__b {
    height: 20px;
}

.sa__c {
    height: 20px;
}

.sa__f {
    height: 44px;
}

.sa__b.htu-s-%(hs)s {
    color: #111111;
}

.sa__c.htu-s-%(ht)s {
    color: #222222;
}

.sa__f.htu-s-%(ha)s {
    color: #333333;
}
''' % {"hs": hs, "ht": ht2, "ha": ha}
html = mk("S25", "sibling-attribute-selectors",
          ["the adjacent (+) and general (~) sibling combinators and attribute selectors are resolved against the static DOM",
           "the marker replaces the left part of the combinator (key '.sa__a +') or the attribute test (key '[type=\"text\"]'); specificity stays equal",
           "attribute values come from the HTML source (type of the input)"],
          ["C015"], ["ConvertTime"], "convert", "PASS", css, body, ["sa", "htu"],
          {"Screens/Siblings.uxml": exp_uxml, "Screens/Siblings.uss": exp_uss},
          [D("C015", "Info", "ConvertTime", "page.html", 0, "static-selectors")] * 3, "Siblings",
          strings_={"sa.text": "Name", "sa.check": "Check"})
fix_lines("S25-sibling-attribute-selectors", html, [(".sa__a + .sa__b", 1), (".sa__a ~ .sa__c", 1), ('[type="text"]', 1)])

# ---------------------------------------------------------------- S26 pseudo-elements-glyph
hb = fnv1a(".pe__chip::before")
hf2 = fnv1a(".pe__chip::after")
css = '''
.pe { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.pe__chip { flex-direction: row; align-items: center; height: 44px; }
.pe__chip::before { content: "★"; color: #e4c07a; margin-right: 4px; }
.pe__chip::after { content: "→"; margin-left: 4px; }
.pe__t { font-size: 16px; }
'''
body = '''
<div name="pe-root" class="pe">
  <div name="pe-chip" class="pe__chip">
    <span name="pe-text" class="pe__t" data-loc="pe.text">Play</span>
  </div>
</div>
'''
exp_uxml = uxml(["Pseudo.uss"], '''
    <ui:VisualElement name="pe-root" class="pe">
        <ui:VisualElement name="pe-chip" class="pe__chip">
            <ui:Label text="%(star)s" class="htu-before-%(hb)s" picking-mode="Ignore" />
            <ui:Label name="pe-text" text="pe.text" class="pe__t" />
            <ui:Label text="%(arrow)s" class="htu-after-%(hf)s" picking-mode="Ignore" />
        </ui:VisualElement>
    </ui:VisualElement>''' % {"hb": hb, "hf": hf2, "star": chr(0x2605), "arrow": chr(0x2192)})
exp_uss = '''.pe {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.pe__chip {
    flex-direction: row;
    align-items: center;
    height: 44px;
}

.htu-after-%(hf)s {
    margin-left: 4px;
}

.htu-before-%(hb)s {
    margin-right: 4px;
    color: #e4c07a;
}

.pe__t {
    font-size: 16px;
}
''' % {"hb": hb, "hf": hf2}
html = mk("S26", "pseudo-elements-glyph",
          ["::before and ::after with glyph content become real Label children inserted first and last, with picking-mode Ignore",
           "the class is htu-before-<hash> or htu-after-<hash> (key: the canonical selector text); the content declaration is dropped, the other declarations form the rule",
           "pseudo-elements are only materialised on container elements; a Label or Button target is E001 (see S27)"],
          ["C005"], ["ConvertTime"], "convert", "PASS", css, body, ["pe", "htu"],
          {"Screens/Pseudo.uxml": exp_uxml, "Screens/Pseudo.uss": exp_uss},
          [D("C005", "Info", "ConvertTime", "page.html", 0, "pseudo-elements")] * 2, "Pseudo",
          strings_={"pe.text": "Play"})
fix_lines("S26-pseudo-elements-glyph", html, [(".pe__chip::before", 1), (".pe__chip::after", 1)])

# ---------------------------------------------------------------- S27 pseudo-elements-image-text
hi1 = fnv1a(".pi__card::before")
hi2 = fnv1a(".pi__tag::after")
css = '''
.pi { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.pi__card { flex-direction: row; height: 44px; }
.pi__card::before { content: ""; width: 16px; height: 16px; background-image: url("images/dot.png"); }
.pi__tag { flex-direction: row; height: 44px; }
.pi__tag::after { content: attr(data-text-after); color: #e4c07a; }
.pi__bad { height: 44px; }
.pi__bad::after { content: "SALE"; }
.pi__t { font-size: 16px; }
'''
body = '''
<div name="pi-root" class="pi">
  <div name="pi-card" class="pi__card">
    <span name="pi-card-text" class="pi__t" data-loc="pi.card">Card</span>
  </div>
  <div name="pi-tag" class="pi__tag" data-loc-after="pi.new" data-text-after="NEW">
    <span name="pi-tag-text" class="pi__t" data-loc="pi.tag">Tag</span>
  </div>
  <div name="pi-bad" class="pi__bad"></div>
</div>
'''
exp_uxml = uxml(["PseudoImage.uss"], '''
    <ui:VisualElement name="pi-root" class="pi">
        <ui:VisualElement name="pi-card" class="pi__card">
            <ui:VisualElement class="htu-before-%(h1)s" picking-mode="Ignore" />
            <ui:Label name="pi-card-text" text="pi.card" class="pi__t" />
        </ui:VisualElement>
        <ui:VisualElement name="pi-tag" class="pi__tag">
            <ui:Label name="pi-tag-text" text="pi.tag" class="pi__t" />
            <ui:Label text="pi.new" class="htu-after-%(h2)s" picking-mode="Ignore" />
        </ui:VisualElement>
        <ui:VisualElement name="pi-bad" class="pi__bad" />
    </ui:VisualElement>''' % {"h1": hi1, "h2": hi2})
exp_uss = '''.pi {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
}

.pi__card {
    flex-direction: row;
    height: 44px;
}

.htu-before-%(h1)s {
    width: 16px;
    height: 16px;
    background-image: url("/input/images/dot.png");
}

.pi__tag {
    flex-direction: row;
    height: 44px;
}

.htu-after-%(h2)s {
    color: #e4c07a;
}

.pi__bad {
    height: 44px;
}

.pi__t {
    font-size: 16px;
}
''' % {"h1": hi1, "h2": hi2}
html = mk("S27", "pseudo-elements-image-text",
          ["content: \"\" plus a background image becomes an empty VisualElement; the image url resolves like any other",
           "text content is only allowed as attr(data-text-after) or attr(data-text-before) with data-loc-after/before: the Label carries the key",
           "raw letters in content (\"SALE\") need a key: L001 Reject, the pseudo-element is not materialised under --force"],
          ["C005", "L001"], ["ConvertTime", "Reject"], "convert", "FAIL", css, body, ["pi", "htu"],
          {"Screens/PseudoImage.uxml": exp_uxml, "Screens/PseudoImage.uss": exp_uss},
          [D("L001", "Error", "Reject", "page.html", 0),
           D("C005", "Info", "ConvertTime", "page.html", 0, "pseudo-elements"),
           D("C005", "Info", "ConvertTime", "page.html", 0, "pseudo-elements")], "PseudoImage", force=True,
          extra_files={"images/dot.png": png_bytes(2, 2, (228, 192, 122, 255))},
          strings_={"pi.card": "Card", "pi.tag": "Tag", "pi.new": "NEW"})
fix_lines("S27-pseudo-elements-image-text", html, [('content: "SALE"', 1), (".pi__card::before", 1), (".pi__tag::after", 1)])
